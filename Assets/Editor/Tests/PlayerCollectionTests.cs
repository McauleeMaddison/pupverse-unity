using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Pupverse.Tests
{
    public class PlayerCollectionTests
    {
        readonly List<CardData> cards=new List<CardData>();
        readonly float[] weights={0,0,60,28,10,2,2};
        CardPackDefinition pack;
        string saved;
        [SetUp] public void Setup()
        {
            saved=null;
            foreach(var series in new[]{"CryptoPups","AlienPups","CyberPups"})
                foreach(var rarity in new[]{CardRarity.Rare,CardRarity.Epic,CardRarity.Legendary,CardRarity.Mythic})
                {
                    var c=ScriptableObject.CreateInstance<CardData>();c.id=series+rarity;c.series=series;c.rarity=rarity;c.originalCardArt=Texture2D.whiteTexture;cards.Add(c);
                }
            pack=new CardPackDefinition{id="alien",title="ALIEN",series="AlienPups",price=10,cardCount=4};
        }
        [TearDown] public void Cleanup(){foreach(var c in cards)Object.DestroyImmediate(c);cards.Clear();}
        PlayerCollection Collection(int coins=10)=>new PlayerCollection(PlayerCollection.Starter(cards.Take(4),coins),json=>saved=json);
        [Test] public void PurchaseIsChargedOnceAndPendingRevealSurvivesReload()
        {
            var c=Collection();Assert.That(c.Open(pack,cards.ToArray(),weights,()=>0,out _),Is.True);
            Assert.That(c.Coins,Is.Zero);Assert.That(c.Pending.cardIds.Length,Is.EqualTo(4));
            Assert.That(c.Pending.cardIds.All(id=>id.StartsWith("AlienPups")),Is.True);
            Assert.That(c.OwnedCount(c.Pending.cardIds[0]),Is.EqualTo(4));
            CollectionAssert.AreEqual(new[]{true,false,false,false},c.Pending.isNew);
            Assert.That(c.Open(pack,cards.ToArray(),weights,()=>0,out _),Is.False);
            var loaded=new PlayerCollection(JsonUtility.FromJson<CollectionSave>(saved),json=>saved=json);
            Assert.That(loaded.Coins,Is.Zero);Assert.That(loaded.Pending.cardIds,Is.EqualTo(c.Pending.cardIds));
            Assert.That(loaded.Pending.torn,Is.False);Assert.That(loaded.TearOpen(out _),Is.True);
            loaded=new PlayerCollection(JsonUtility.FromJson<CollectionSave>(saved),json=>saved=json);Assert.That(loaded.Pending.torn,Is.True);Assert.That(loaded.Coins,Is.Zero);
            Assert.That(loaded.FinishOpening(out _),Is.True);Assert.That(loaded.Pending,Is.Null);
            Assert.That(loaded.OwnedCount("AlienPupsRare"),Is.EqualTo(4));
        }
        [Test] public void InsufficientCoinsOrEmptyPackCannotSpendOrGrant()
        {
            var c=Collection(9);Assert.That(c.Open(pack,cards.ToArray(),weights,()=>0,out _),Is.False);Assert.That(c.Coins,Is.EqualTo(9));
            c=Collection();pack.series="Missing";Assert.That(c.Open(pack,cards.ToArray(),weights,()=>0,out _),Is.False);
            Assert.That(c.Coins,Is.EqualTo(10));Assert.That(c.Pending,Is.Null);Assert.That(saved,Is.Null);
        }
        [Test] public void MatchRewardsCannotBeClaimedTwice()
        {
            var c=Collection(0);Assert.That(c.AwardMatch("match-1",5),Is.True);Assert.That(c.AwardMatch("match-1",5),Is.False);
            c=new PlayerCollection(JsonUtility.FromJson<CollectionSave>(saved),json=>saved=json);
            Assert.That(c.AwardMatch("match-1",5),Is.False);Assert.That(c.Coins,Is.EqualTo(5));
            Assert.That(c.AwardMatch("match-2",5),Is.True);Assert.That(c.Coins,Is.EqualTo(10));
        }
        [Test] public void SavingFailureLeavesWalletAndCollectionUntouched()
        {
            var c=new PlayerCollection(PlayerCollection.Starter(cards.Take(4),10),json=>throw new IOException("test failure"));
            Assert.That(c.Open(pack,cards.ToArray(),weights,()=>0,out _),Is.False);
            Assert.That(c.Coins,Is.EqualTo(10));Assert.That(c.Pending,Is.Null);Assert.That(c.OwnedCount("AlienPupsRare"),Is.Zero);
        }
        [Test] public void RarityRollUsesAvailableTierWeightsAndShowsMatchingOdds()
        {
            var c=Collection();Assert.That(c.Open(pack,cards.ToArray(),weights,()=>.999,out _),Is.True);
            Assert.That(c.Pending.cardIds.All(id=>id=="AlienPupsMythic"),Is.True);
            Assert.That(PlayerCollection.Odds(pack,cards.ToArray(),weights),Is.EqualTo("Rare 60% · Epic 28% · Legendary 10% · Mythic 2%"));
        }
        [Test] public void FeaturedCardsPersistWithoutChangingOwnershipCoinsOrPendingPack()
        {
            var c=Collection();var ids=cards.Skip(1).Take(3).Select(card=>card.id).Reverse().ToArray();
            Assert.That(c.Open(pack,cards.ToArray(),weights,()=>0,out _),Is.True);
            var receipt=c.Pending.cardIds;
            Assert.That(c.SetFeatured(ids,cards.ToArray(),out _),Is.True);
            c=new PlayerCollection(JsonUtility.FromJson<CollectionSave>(saved),_=>{});
            CollectionAssert.AreEqual(ids,c.Featured(cards.ToArray(),cards.Take(3)).Select(card=>card.id));
            Assert.That(c.Coins,Is.Zero);CollectionAssert.AreEqual(receipt,c.Pending.cardIds);
            Assert.That(c.OwnedCount("AlienPupsRare"),Is.EqualTo(4));
            Assert.That(c.SetFeatured(new[]{ids[0],ids[0],ids[1]},cards.ToArray(),out _),Is.False);
            Assert.That(c.SetFeatured(cards.Skip(9).Take(3).Select(card=>card.id).ToArray(),cards.ToArray(),out _),Is.False);
            Assert.That(c.SetFeatured(ids,cards.Take(1).ToArray(),out _),Is.False);
            CollectionAssert.AreEqual(ids,c.Featured(cards.ToArray(),cards.Take(3)).Select(card=>card.id));
        }
        [Test] public void LegacyAndStaleFeaturedPreferencesFallBackAndFailedSaveDoesNotReplaceThem()
        {
            var state=PlayerCollection.Starter(cards.Take(4),10);state.featuredIds=null;
            var c=new PlayerCollection(state,_=>throw new IOException("disk full"));
            var original=c.Featured(cards.ToArray(),cards.Take(3));
            Assert.That(original.Length,Is.EqualTo(3));
            Assert.That(c.SetFeatured(cards.Skip(1).Take(3).Select(card=>card.id).ToArray(),cards.ToArray(),out _),Is.False);
            CollectionAssert.AreEqual(original,c.Featured(cards.ToArray(),cards.Take(3)));
            state.featuredIds=new List<string>{"retired",cards[0].id,cards[0].id};
            c=new PlayerCollection(state,_=>{});
            CollectionAssert.AreEqual(original,c.Featured(cards.ToArray(),cards.Take(3)));
            Assert.That(c.Coins,Is.EqualTo(10));
        }
        [Test] public void InvalidRollAndMalformedSaveAreRejected()
        {
            var c=Collection();Assert.That(c.Open(pack,cards.ToArray(),weights,()=>double.NaN,out _),Is.False);Assert.That(c.Coins,Is.EqualTo(10));
            Assert.That(PlayerCollection.Valid(new CollectionSave{version=99}),Is.False);
            Assert.That(PlayerCollection.Valid(new CollectionSave{coins=-1}),Is.False);
        }
    }
}
