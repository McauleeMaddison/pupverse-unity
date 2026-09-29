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
        string saved,collectionSaved,collectionBackup;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Battle3D.unity");
            saved=PlayerPrefs.GetString("Pupverse.StartingHand.v1",""); reduced=GameSettings.ReducedMotion;
            collectionSaved=PlayerPrefs.GetString(BattleProgression.SaveKey,"");collectionBackup=PlayerPrefs.GetString(BattleProgression.BackupKey,"");
            PlayerPrefs.DeleteKey(BattleProgression.SaveKey);PlayerPrefs.DeleteKey(BattleProgression.BackupKey);
            yield return new EnterPlayMode();
            match=Object.FindAnyObjectByType<BattleMatchController>(); battle=match.Battle;
            selector=match.GetComponent<BattleHandSelection>();
            yield return null; yield return null;
            match.GetComponent<BattleHomeScreen>().OpenHand();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            if(match!=null && match.progression!=null)match.progression.enabled=false;
            PlayerPrefs.SetString(BattleProgression.SaveKey,collectionSaved);PlayerPrefs.SetString(BattleProgression.BackupKey,collectionBackup);
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
            if(match.progression!=null)match.progression.enabled=false;
            match.progression=null;
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
            var locked=match.availableCards.First(c=>!match.progression.Owns(c));
            Assert.That(selector.SelectCard(locked),Is.False,"Unowned cards must be earned through packs");
            var replacement=original[0];
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
        IEnumerator WaitForTransfer(BattleDeckPresentation deck)
        {
            float deadline=Time.realtimeSinceStartup+5;
            while(!deck.IsTransferring && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(deck.IsTransferring,Is.True,"Round settlement should start a card transfer");
        }
        [UnityTest] public IEnumerator BothCardsFlyFromTheirFacesAndBlockTheNextDeal()
        {
            var display=match.cardDisplay;
            Configure(); match.cardDisplay=display; match.StartMatch(); yield return Ready();
            var deck=match.GetComponent<BattleDeckPresentation>();
            var visualRoot=Object.FindObjectsByType<RectTransform>().Single(t=>t.name=="Premium deck presentation");
            var canvas=visualRoot.GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(visualRoot,
                deck.layout.battleCamera.WorldToScreenPoint(display.rivalFront.bounds.center),
                canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out Vector2 expected);
            Assert.That(Vector2.Distance(deck.RivalTransferOrigin,expected),Is.LessThan(.01f));
            GameSettings.ReducedMotion=false; deck.captureDuration=.7f;
            battle.Choose(CardStat.Power); yield return WaitForTransfer(deck);
            Assert.That(deck.ActiveTransferCount,Is.EqualTo(2));
            Assert.That(match.PlayerActiveCard,Is.Null); Assert.That(match.RivalActiveCard,Is.Null);
            Assert.That(match.CanChoose(BattleTurnOwner.Player),Is.False);
            battle.Choose(CardStat.Speed); Assert.That(battle.Wins,Is.EqualTo(1));
            var pooled=visualRoot.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name.StartsWith("Card transfer ")).ToArray();
            yield return Ready();
            Assert.That(deck.IsTransferring,Is.False); Assert.That(match.PlayerCardCount,Is.EqualTo(7));
            Assert.That(match.PlayerActiveCard,Is.SameAs(player[1]));
            battle.Choose(CardStat.Power); yield return Ready();
            CollectionAssert.AreEqual(pooled,visualRoot.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name.StartsWith("Card transfer ")).ToArray());
        }
        [UnityTest] public IEnumerator FinalVictoryWaitsForCardsToLand()
        {
            Configure(); match.StartMatch(); yield return Ready();
            for(int i=0;i<5;i++) yield return Choose();
            var deck=match.GetComponent<BattleDeckPresentation>(); GameSettings.ReducedMotion=false; deck.captureDuration=.65f;
            bool ended=false; match.MatchEnded+=winner=>ended=true;
            battle.Choose(CardStat.Power); yield return WaitForTransfer(deck);
            Assert.That(ended,Is.False); Assert.That(match.IsMatchRunning,Is.True);
            Assert.That(match.LastWinner,Is.EqualTo(BattleMatchWinner.None));
            yield return Ready();
            Assert.That(ended,Is.True); Assert.That(deck.IsTransferring,Is.False);
            Assert.That(match.LastWinner,Is.EqualTo(BattleMatchWinner.Player)); Assert.That(match.PlayerCardCount,Is.EqualTo(12));
        }
        [UnityTest] public IEnumerator DrawPotTravelsWithBothCardsToTheNextWinner()
        {
            Configure(); player[0].stats=rival[0].stats; match.StartMatch(); yield return Ready();
            yield return Choose(); Assert.That(match.PotCount,Is.EqualTo(2));
            GameSettings.ReducedMotion=false; var deck=match.GetComponent<BattleDeckPresentation>(); deck.captureDuration=.65f;
            battle.Choose(CardStat.Power); yield return WaitForTransfer(deck);
            Assert.That(deck.ActiveTransferCount,Is.EqualTo(3));
            yield return Ready(); Assert.That(match.PotCount,Is.Zero); Assert.That(match.PlayerCardCount,Is.EqualTo(8));
        }
        [UnityTest] public IEnumerator DisablingDeckPresentationClearsFlightsAndDoesNotBlockTheMatch()
        {
            Configure(); match.StartMatch(); yield return Ready(); GameSettings.ReducedMotion=false;
            var deck=match.GetComponent<BattleDeckPresentation>(); deck.captureDuration=.8f;
            battle.Choose(CardStat.Power); yield return WaitForTransfer(deck);
            deck.enabled=false; Assert.That(deck.IsTransferring,Is.False);
            yield return Ready(); Assert.That(match.PlayerActiveCard,Is.SameAs(player[1]));
            deck.enabled=true; yield return null;
            Assert.That(deck.IsTransferring,Is.False);
            Assert.That(Object.FindObjectsByType<RectTransform>().Count(t=>t.name.StartsWith("Card transfer ")),Is.Zero);
        }

        [UnityTest] public IEnumerator PacksSpendCoinsPersistThroughInterruptedRevealAndUnlockHandChoices()
        {
            GameSettings.ReducedMotion=false;
            var progression=match.progression;var shop=match.GetComponent<CardPackShop>();
            Assert.That(progression.Collection.Coins,Is.EqualTo(10));
            shop.Open();yield return null;yield return null;Capture("pack-shop",390,844);Capture("pack-shop-compact",390,640);Capture("pack-shop-landscape",844,390);yield return new WaitForSecondsRealtime(.2f);
            Assert.That(shop.Buy("alien"),Is.True);Assert.That(shop.Buy("crypto"),Is.False);
            Assert.That(progression.Collection.Coins,Is.Zero);
            var pulled=progression.Collection.Pending.cardIds.ToArray();
            shop.enabled=false;yield return null;shop.enabled=true;shop.Open();
            Assert.That(progression.Collection.Pending.cardIds,Is.EqualTo(pulled));
            Assert.That(pulled.Length,Is.EqualTo(4));
            var foil=Object.FindAnyObjectByType<PackFoilPresentation>();Assert.That(foil.IsSealed,Is.True);
            yield return null;yield return null;foil.RenderPreview();
            var preview=(RenderTexture)foil.GetComponent<RawImage>().texture;var previous=RenderTexture.active;
            var pixels=new Texture2D(preview.width,preview.height,TextureFormat.RGBA32,false);
            RenderTexture.active=preview;pixels.ReadPixels(new Rect(0,0,preview.width,preview.height),0,0);pixels.Apply();RenderTexture.active=previous;
            int visible=pixels.GetPixels32().Count(p=>p.a>128);Object.Destroy(pixels);
            Assert.That(visible,Is.GreaterThan(10000),"The sealed foil pack must actually render, not just accept a swipe");
            Capture("foil-sealed",390,844);
            var rect=(RectTransform)foil.transform;Vector3[] corners=new Vector3[4];rect.GetWorldCorners(corners);
            var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=17};
            data.position=Vector2.Lerp(corners[0],corners[2],.2f);foil.OnPointerDown(data);
            data.position=Vector2.Lerp(corners[0],corners[2],.8f);foil.OnDrag(data);
            Assert.That(foil.IsSealed,Is.True,"Dragging from the bottom must not tear the seal");
            data.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+rect.rect.width*.2f,rect.rect.yMin+rect.rect.height*.78f,0)));foil.OnPointerDown(data);
            data.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMin+rect.rect.width*.8f,rect.rect.yMin+rect.rect.height*.78f,0)));foil.OnDrag(data);
            Assert.That(progression.Collection.Pending.torn,Is.True);Assert.That(shop.IsAnimating,Is.True);
            float deadline=Time.realtimeSinceStartup+4;
            while(shop.IsAnimating && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(shop.IsAnimating,Is.False);Capture("pack-reveal",390,844);Capture("pack-reveal-compact",390,640);Capture("pack-reveal-landscape",844,390);yield return new WaitForSecondsRealtime(.2f);
            GameSettings.ReducedMotion=true;
            for(int i=0;i<pulled.Length;i++){shop.Next();yield return null;yield return null;}
            Assert.That(shop.IsOpen,Is.False);Assert.That(progression.Collection.Pending,Is.Null);
            var card=match.availableCards.First(c=>c.id==pulled[0]);
            Assert.That(card.series,Is.EqualTo("AlienPups"));Assert.That(selector.SelectCard(card),Is.True);
            Assert.That(progression.Collection.OwnedCount(card.id),Is.GreaterThan(0));
        }

        [UnityTest] public IEnumerator CompletingAMatchPaysCoinsOnceWithoutGrantingCapturedCollectionCards()
        {
            Configure();var progression=match.GetComponent<BattleProgression>();
            var collection=new PlayerCollection(PlayerCollection.Starter(player,0),json=>{});
            typeof(BattleProgression).GetProperty("Collection").SetValue(progression,collection);
            progression.enabled=true;match.progression=progression;
            match.StartMatch();yield return Ready();yield return Choose();
            Assert.That(collection.Coins,Is.Zero,"The match reward is paid at match end");
            for(int i=0;i<5;i++)yield return Choose();
            Assert.That(collection.Coins,Is.EqualTo(progression.victoryCoins));
            Assert.That(progression.LastReward,Is.EqualTo(progression.victoryCoins));
            Assert.That(collection.OwnedCount(rival[0].id),Is.Zero,"Captures are match ownership, not permanent pack ownership");
            battle.Choose(CardStat.Power);yield return null;
            Assert.That(collection.Coins,Is.EqualTo(progression.victoryCoins));
        }

        [UnityTest] public IEnumerator HomeRoutesToPacksHandAndPersistentMotionSettings()
        {
            var home=match.GetComponent<BattleHomeScreen>();home.ShowHome();yield return null;yield return null;
            Assert.That(home.IsOpen,Is.True);Assert.That(selector.IsOpen,Is.False);Assert.That(match.IsMatchRunning,Is.False);
            Capture("home",390,844);Capture("home-compact",390,640);Capture("home-landscape",844,390);
            home.OpenSettings();Assert.That(home.SettingsOpen,Is.True);bool before=GameSettings.ReducedMotion;
            home.ToggleMotion();Assert.That(GameSettings.ReducedMotion,Is.Not.EqualTo(before));home.ShowHome();
            home.OpenPacks();Assert.That(home.IsOpen,Is.False);var shop=match.GetComponent<CardPackShop>();Assert.That(shop.IsOpen,Is.True);
            shop.Close();Assert.That(home.IsOpen,Is.True);Assert.That(selector.IsOpen,Is.False);
            home.OpenHand();Assert.That(home.IsOpen,Is.False);Assert.That(selector.IsOpen,Is.True);
            selector.BeginMatch();yield return Ready();Assert.That(match.IsMatchRunning,Is.True);home.ShowHome();Assert.That(home.IsOpen,Is.False);
        }

        static void Capture(string name,int width=0,int height=0)
        {
            string folder=Path.Combine(Application.temporaryCachePath,"BattleMatchChecks"); Directory.CreateDirectory(folder);
            string path=Path.Combine(folder,name+".png");
            if(Application.isBatchMode || width>0)
            {
                // Render the actual UI at a phone size with its own camera. The arena's asymmetric
                // projection must not be applied to an overlay Canvas.
                var canvas=Object.FindObjectsByType<Canvas>().First(c=>c.GetComponent<UnityEngine.UI.CanvasScaler>()!=null);
                var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
                bool scalerEnabled=scaler.enabled;float scale=canvas.scaleFactor;
                var active=RenderTexture.active;
                var rt=new RenderTexture(width>0?width:Screen.width,height>0?height:Screen.height,24);
                var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                var cameraObject=new GameObject("Temporary UI capture camera");var camera=cameraObject.AddComponent<Camera>();
                camera.enabled=false;camera.transform.position=new Vector3(10000,10000,10000);
                camera.orthographic=true;camera.orthographicSize=rt.height*.5f;camera.targetTexture=rt;
                camera.backgroundColor=new Color(.015f,.025f,.07f);camera.clearFlags=CameraClearFlags.SolidColor;
                try
                {
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                    if(width>0) {scaler.enabled=false;canvas.scaleFactor=1;}
                    Canvas.ForceUpdateCanvases();
                    var shop=Object.FindAnyObjectByType<CardPackShop>();shop.SendMessage("LateUpdate");
                    Object.FindAnyObjectByType<BattleHomeScreen>()?.SendMessage("LateUpdate");
                    Canvas.ForceUpdateCanvases();foreach(var stage in Object.FindObjectsByType<PackFoilPresentation>())stage.RenderPreview();camera.Render();RenderTexture.active=rt;
                    texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;
                    scaler.enabled=scalerEnabled;canvas.scaleFactor=scale;Canvas.ForceUpdateCanvases();
                    Object.Destroy(cameraObject);Object.Destroy(rt);Object.Destroy(texture);
                }
            }
            else ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Match screenshot: "+path);
        }
    }
}
