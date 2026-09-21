using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Pupverse.Tests
{
    public class BattleRulesTests
    {
        CardData raven,brooklyn;
        [SetUp] public void Load()
        {
            raven=AssetDatabase.LoadAssetAtPath<CardData>("Assets/Pupverse/Data/Raven.asset");
            brooklyn=AssetDatabase.LoadAssetAtPath<CardData>("Assets/Pupverse/Data/Brooklyn.asset");
            Assert.That(raven,Is.Not.Null);Assert.That(brooklyn,Is.Not.Null);
        }
        [TestCase(CardStat.Power,109,95,BattleWinner.Player)]
        [TestCase(CardStat.Speed,82,102,BattleWinner.Opponent)]
        [TestCase(CardStat.Intelligence,104,78,BattleWinner.Player)]
        [TestCase(CardStat.Defence,84,80,BattleWinner.Player)]
        [TestCase(CardStat.Luck,77,79,BattleWinner.Opponent)]
        public void WebParity(CardStat stat,int player,int opponent,BattleWinner winner)
        {
            var r=BattleRules.Resolve(raven,brooklyn,stat);
            Assert.That(r.PlayerTotal,Is.EqualTo(player));Assert.That(r.OpponentTotal,Is.EqualTo(opponent));Assert.That(r.Winner,Is.EqualTo(winner));
        }
        [Test] public void TieHasNoWinner() => Assert.That(BattleRules.Resolve(raven,raven,CardStat.Power).Winner,Is.EqualTo(BattleWinner.Draw));
        [Test] public void RoundResolvesOnceUntilReset()
        {
            var round=new BattleRound();Assert.That(round.TryResolve(raven,brooklyn,CardStat.Power),Is.True);
            Assert.That(round.TryResolve(raven,brooklyn,CardStat.Speed),Is.False);Assert.That(round.Result.Winner,Is.EqualTo(BattleWinner.Player));
            round.Reset();Assert.That(round.TryResolve(raven,brooklyn,CardStat.Speed),Is.True);Assert.That(round.Result.Winner,Is.EqualTo(BattleWinner.Opponent));
        }
        [Test] public void InvalidStatFails() => Assert.Throws<System.ArgumentOutOfRangeException>(()=>BattleRules.Resolve(raven,brooklyn,(CardStat)99));
        [Test] public void NullCardFails() => Assert.Throws<System.ArgumentNullException>(()=>BattleRules.Resolve(null,brooklyn,CardStat.Power));
        [Test] public void StarterContentIsComplete()
        {
            foreach(var card in new[]{raven,brooklyn}) {Assert.That(card.fact,Is.Not.Empty);Assert.That(card.originalCardArt,Is.Not.Null);Assert.That(card.id,Is.Not.Empty);}
            Assert.That(raven.heroArt,Is.Not.Null);Assert.That(EditorBuildSettings.scenes.Length,Is.EqualTo(2));
        }
    }
}
