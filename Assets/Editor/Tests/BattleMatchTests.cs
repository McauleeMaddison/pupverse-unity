using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Pupverse.Tests
{
    public class BattleMatchTests
    {
        BattleMatchController match;
        BattleController battle;
        BattleHandSelection selector;
        CardData[] player,rival;
        readonly List<CardData> temporary=new List<CardData>();
        bool reduced;
        string saved;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Battle3D.unity");
            saved=PlayerPrefs.GetString("Pupverse.StartingHand.v1",""); reduced=GameSettings.ReducedMotion;
            yield return new EnterPlayMode();
            match=Object.FindAnyObjectByType<BattleMatchController>(); battle=match.Battle;
            selector=match.GetComponent<BattleHandSelection>();
            yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            GameSettings.ReducedMotion=reduced;
            PlayerPrefs.SetString("Pupverse.StartingHand.v1",saved); PlayerPrefs.Save();
            foreach(var card in temporary) Object.Destroy(card);
            temporary.Clear();
            yield return new ExitPlayMode();
        }
        void Set(string name,object value) => typeof(BattleMatchController).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(match,value);
        CardData[] Hand(string prefix,int value)
        {
            return Enumerable.Range(0,6).Select(i=> {
                var c=ScriptableObject.CreateInstance<CardData>(); temporary.Add(c); c.id=prefix+i; c.displayName=c.id;
                c.originalCardArt=Texture2D.whiteTexture; c.stats=new StatBlock { power=value,speed=value,intelligence=value,defence=value,luck=value }; return c;
            }).ToArray();
        }
        void Configure(int p=100,int r=10)
        {
            GameSettings.ReducedMotion=true;
            player=Hand("player",p); rival=Hand("rival",r);
            Set("playerStartingCards",player); Set("rivalStartingCards",rival);
            Set("nextRoundDelay",0f); Set("rivalThinkDelay",60f);
            match.availableCards=player.Concat(rival).ToArray();
            match.cardDisplay=null; battle.cardAnimator=null; battle.winnerReadDuration=0;
        }
        IEnumerator Ready()
        {
            float deadline=Time.realtimeSinceStartup+5;
            while(match.IsMatchRunning && (!match.CanChoose(match.TurnOwner) || battle.IsResolving) && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
            Assert.That(match.PlayerCardCount+match.RivalCardCount+match.PotCount,Is.EqualTo(12));
        }
        IEnumerator Choose()
        {
            if(match.TurnOwner==BattleTurnOwner.Player) battle.Choose(CardStat.Power);
            else typeof(BattleController).GetMethod("ChooseForRival",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,new object[]{CardStat.Power});
            yield return Ready();
        }
        [UnityTest] public IEnumerator WinnerQueuesBothCardsRevealsNextAndCapturesAllTwelve()
        {
            Configure(); match.StartMatch(); yield return Ready();
            battle.Choose(CardStat.Power); battle.Choose(CardStat.Speed); battle.Replay();
            yield return Ready();
            Assert.That(battle.Wins,Is.EqualTo(1)); Assert.That(match.PlayerCardCount,Is.EqualTo(7)); Assert.That(match.RivalCardCount,Is.EqualTo(5));
            Assert.That(match.PlayerActiveCard,Is.SameAs(player[1])); Assert.That(match.RivalActiveCard,Is.SameAs(rival[1]));
            var queue=(Queue<CardData>)typeof(BattleMatchController).GetField("playerDeck",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(match);
            CollectionAssert.AreEqual(player.Skip(2).Concat(new[]{player[0],rival[0]}),queue);
            for(int i=0;i<5;i++) yield return Choose();
            Assert.That(match.LastWinner,Is.EqualTo(BattleMatchWinner.Player)); Assert.That(match.PlayerCardCount,Is.EqualTo(12)); Assert.That(match.RivalCardCount,Is.Zero);
            battle.Choose(CardStat.Power); Assert.That(battle.Wins,Is.EqualTo(6));
            match.StartMatch(); yield return Ready();
            Assert.That(battle.Wins,Is.Zero); Assert.That(match.PlayerCardCount,Is.EqualTo(6));
        }
        [UnityTest] public IEnumerator RivalCapturesAndPlayerCannotChooseOutOfTurn()
        {
            Configure(10,100); match.StartMatch(); yield return Ready(); yield return Choose();
            Assert.That(match.PlayerCardCount,Is.EqualTo(5)); Assert.That(match.RivalCardCount,Is.EqualTo(7));
            Assert.That(match.TurnOwner,Is.EqualTo(BattleTurnOwner.Rival));
            battle.Choose(CardStat.Luck); Assert.That(battle.IsResolving,Is.False);
            for(int i=0;i<5;i++) yield return Choose();
            Assert.That(match.LastWinner,Is.EqualTo(BattleMatchWinner.Rival)); Assert.That(match.RivalCardCount,Is.EqualTo(12));
        }
        [UnityTest] public IEnumerator DrawPotIsCollectedByNextWinnerWithoutLosingCards()
        {
            Configure(); player[0].stats=rival[0].stats;
            match.StartMatch(); yield return Ready(); yield return Choose();
            Assert.That(match.PotCount,Is.EqualTo(2)); Assert.That(match.PlayerCardCount,Is.EqualTo(5));
            yield return Choose(); Assert.That(match.PotCount,Is.Zero); Assert.That(match.PlayerCardCount,Is.EqualTo(8)); Assert.That(match.RivalCardCount,Is.EqualTo(4));
        }
        [UnityTest] public IEnumerator AllDrawsEndInTwelveCardPot()
        {
            Configure(10,10); match.StartMatch(); yield return Ready();
            for(int i=0;i<6;i++) yield return Choose();
            Assert.That(match.LastWinner,Is.EqualTo(BattleMatchWinner.Draw)); Assert.That(match.PotCount,Is.EqualTo(12));
        }
        [UnityTest] public IEnumerator HandBuilderSwapsOrdersAndArenaUsesTheChosenArtAndStats()
        {
            Assert.That(selector.IsOpen,Is.True); Assert.That(match.IsMatchRunning,Is.False);
            battle.Choose(CardStat.Power); Assert.That(battle.IsResolving,Is.False);
            var original=selector.SelectedHand;
            selector.SelectSlot(0); Assert.That(selector.SelectCard(original[1]),Is.True);
            Assert.That(selector.SelectedHand[1],Is.SameAs(original[0]));
            selector.MoveSelected(1); CollectionAssert.AreEqual(original,selector.SelectedHand);
            var replacement=match.availableCards.First(c=>!original.Contains(c));
            selector.SelectSlot(0); selector.SelectCard(replacement);
            Capture("hand-builder"); yield return new WaitForSecondsRealtime(.25f);
            selector.BeginMatch(); yield return Ready(); yield return null;
            Assert.That(selector.IsOpen,Is.False); Assert.That(match.PlayerActiveCard,Is.SameAs(replacement));
            var block=new MaterialPropertyBlock(); match.cardDisplay.playerFront.GetPropertyBlock(block);
            Assert.That(block.GetTexture("_MainTex"),Is.SameAs(replacement.originalCardArt));
            Assert.That(battle.statButtons[0].GetComponent<BattleStatTile>().number.text,Is.EqualTo(replacement.EffectiveValue(CardStat.Power).ToString()));
            Assert.That(match.PlayerCardCount,Is.EqualTo(6)); Assert.That(match.RivalCardCount,Is.EqualTo(6));
            Capture("six-card-match"); yield return new WaitForSecondsRealtime(.25f);
            Set("rivalThinkDelay",60f);
            battle.Choose(CardStat.Speed); yield return Ready(); yield return null;
            Assert.That(match.PlayerActiveCard,Is.SameAs(selector.SelectedHand[1]));
            match.cardDisplay.playerFront.GetPropertyBlock(block);
            Assert.That(block.GetTexture("_MainTex"),Is.SameAs(match.PlayerActiveCard.originalCardArt));
            match.cardDisplay.rivalFront.GetPropertyBlock(block);
            Assert.That(block.GetTexture("_MainTex"),Is.SameAs(match.RivalActiveCard.originalCardArt));
            Assert.That(battle.statButtons[0].GetComponent<BattleStatTile>().number.text,Is.EqualTo(match.PlayerActiveCard.EffectiveValue(CardStat.Power).ToString()));
            Capture("next-round"); yield return new WaitForSecondsRealtime(.25f);
        }
        [UnityTest] public IEnumerator DisablingDuringRevealRestoresExactRootScales()
        {
            var display=match.cardDisplay; var p=display.playerRoot.localScale; var r=display.rivalRoot.localScale;
            GameSettings.ReducedMotion=false; selector.BeginMatch(); yield return null;
            match.enabled=false; yield return null;
            Assert.That(display.playerRoot.localScale,Is.EqualTo(p)); Assert.That(display.rivalRoot.localScale,Is.EqualTo(r));
            Assert.That(match.IsMatchRunning,Is.False); Assert.That(battle.IsResolving,Is.False);
        }
        [UnityTest] public IEnumerator InvalidAndDuplicateStartingHandsAreRejected()
        {
            Configure();
            LogAssert.Expect(LogType.Error,"Player must have exactly 6 starting cards.");
            Assert.That(match.SelectHand(player.Take(5).ToArray()),Is.False);
            var duplicated=player.ToArray(); duplicated[1]=player[0];
            LogAssert.Expect(LogType.Error,"Player must use six different cards. Duplicate: player0");
            Assert.That(match.SelectHand(duplicated),Is.False);
            Assert.That(match.SelectHand(player),Is.True); match.StartMatch(); yield return Ready();
            Assert.That(match.SelectHand(rival),Is.False);
        }
        static void Capture(string name)
        {
            string folder=Path.Combine(Application.temporaryCachePath,"BattleMatchChecks"); Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png")); Debug.Log("Match screenshot: "+Path.Combine(folder,name+".png"));
        }
    }
}
