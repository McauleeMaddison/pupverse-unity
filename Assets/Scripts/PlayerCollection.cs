using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pupverse
{
    [Serializable] public sealed class CardPackDefinition
    {
        public string id, title, series;
        [Min(1)] public int price=5;
        [Range(1,5)] public int cardCount=4;
    }
    [Serializable] public sealed class OwnedCard { public string id; public int count; }
    [Serializable] public sealed class PackOpening
    {
        public string packId;
        public bool torn;
        public string[] cardIds;
        public bool[] isNew;
    }
    [Serializable] public sealed class CollectionSave
    {
        public int version=1, coins;
        public List<OwnedCard> owned=new List<OwnedCard>();
        public List<string> rewardedMatches=new List<string>();
        public bool hasPending;
        public PackOpening pending;
    }

    // One durable record contains wallet, ownership and pending reveal together.
    // Persistence is injected so the rules can be tested without touching player saves.
    public sealed class PlayerCollection
    {
        CollectionSave state;
        readonly Action<string> persist;
        public event Action Changed;
        public int Coins => state.coins;
        public PackOpening Pending => !state.hasPending?null:JsonUtility.FromJson<PackOpening>(JsonUtility.ToJson(state.pending));
        public int OwnedCount(string id) => state.owned.FirstOrDefault(c=>c.id==id)?.count??0;
        public PlayerCollection(CollectionSave save,Action<string> persistence)
        {
            if(!Valid(save)) throw new ArgumentException("Invalid collection save");
            state=Clone(save); persist=persistence??throw new ArgumentNullException(nameof(persistence));
        }
        public static bool Valid(CollectionSave s) => s!=null && s.version==1 && s.coins>=0 && s.owned!=null && s.rewardedMatches!=null &&
            s.owned.All(c=>c!=null && !string.IsNullOrWhiteSpace(c.id) && c.count>0) && s.owned.Select(c=>c.id).Distinct().Count()==s.owned.Count &&
            (!s.hasPending || (s.pending!=null && !string.IsNullOrEmpty(s.pending.packId) && s.pending.cardIds!=null && s.pending.cardIds.Length>0 && s.pending.isNew?.Length==s.pending.cardIds.Length &&
              s.pending.cardIds.All(id=>s.owned.Any(c=>c.id==id))));
        public static CollectionSave Starter(IEnumerable<CardData> cards,int coins)
        {
            return new CollectionSave {coins=Math.Max(0,coins),owned=cards.Where(c=>c!=null).Select(c=>c.id).Distinct().Select(id=>new OwnedCard{id=id,count=1}).ToList()};
        }
        public bool AwardMatch(string matchId,int coins)
        {
            if(string.IsNullOrEmpty(matchId) || coins<0 || state.rewardedMatches.Contains(matchId) || state.coins>int.MaxValue-coins) return false;
            var next=Clone(state); next.coins+=coins; next.rewardedMatches.Add(matchId);
            return Commit(next,out _);
        }
        public bool Open(CardPackDefinition pack,CardData[] catalog,float[] rarityWeights,Func<double> random,out string error)
        {
            error="";
            if(state.hasPending) {error="Finish revealing your previous pack first.";return false;}
            if(pack==null || string.IsNullOrEmpty(pack.id) || pack.price<1 || pack.cardCount<1 || pack.cardCount>5) {error="This pack is unavailable.";return false;}
            if(state.coins<pack.price) {error="You need "+(pack.price-state.coins)+" more coins. Win matches to earn coins.";return false;}
            var groups=Pools(pack,catalog,rarityWeights);
            double total=groups.Sum(g=>(double)rarityWeights[(int)g.Key]);
            if(total<=0 || random==null) {error="No cards are available in this pack.";return false;}
            var next=Clone(state);
            var opening=new PackOpening {packId=pack.id,cardIds=new string[pack.cardCount],isNew=new bool[pack.cardCount]};
            for(int i=0;i<pack.cardCount;i++)
            {
                double roll=random(); if(double.IsNaN(roll) || roll<0 || roll>=1) {error="Unable to open this pack.";return false;}
                double value=roll*total; var group=groups[groups.Length-1];
                foreach(var candidate in groups) {value-=rarityWeights[(int)candidate.Key];if(value<0){group=candidate;break;}}
                double pick=random(); if(double.IsNaN(pick) || pick<0 || pick>=1) {error="Unable to open this pack.";return false;}
                string id=group.ElementAt((int)(pick*group.Count())).id;
                var owned=next.owned.FirstOrDefault(c=>c.id==id); opening.isNew[i]=owned==null; opening.cardIds[i]=id;
                if(owned==null) next.owned.Add(new OwnedCard{id=id,count=1});
                else if(owned.count==int.MaxValue) {error="Card quantity limit reached.";return false;}
                else owned.count++;
            }
            next.coins-=pack.price;next.pending=opening;next.hasPending=true;
            return Commit(next,out error);
        }
        public bool TearOpen(out string error)
        {
            if(!state.hasPending){error="Choose a pack first.";return false;}
            if(state.pending.torn){error="";return true;}
            var next=Clone(state);next.pending.torn=true;return Commit(next,out error);
        }
        public bool FinishOpening(out string error)
        {
            if(!state.hasPending){error="";return true;}
            var next=Clone(state);next.pending=null;next.hasPending=false;return Commit(next,out error);
        }
        public static string Odds(CardPackDefinition pack,CardData[] catalog,float[] weights)
        {
            var groups=Pools(pack,catalog,weights); double total=groups.Sum(g=>(double)weights[(int)g.Key]);
            return total<=0?"No cards available":string.Join(" · ",groups.Select(g=>g.Key+" "+(weights[(int)g.Key]/total*100).ToString("0.#")+"%"));
        }
        static IGrouping<CardRarity,CardData>[] Pools(CardPackDefinition pack,CardData[] catalog,float[] weights) =>
            (catalog??Array.Empty<CardData>()).Where(c=>c!=null && c.originalCardArt!=null && !string.IsNullOrEmpty(c.id) && c.series==pack?.series &&
                weights!=null && (int)c.rarity>=0 && (int)c.rarity<weights.Length && weights[(int)c.rarity]>0 && !float.IsInfinity(weights[(int)c.rarity]))
            .GroupBy(c=>c.id).Select(g=>g.First()).GroupBy(c=>c.rarity).OrderBy(g=>(int)g.Key).ToArray();
        bool Commit(CollectionSave next,out string error)
        {
            try {persist(JsonUtility.ToJson(next));}
            catch(Exception e) when(e is System.IO.IOException || e is UnauthorizedAccessException || e is PlayerPrefsException)
            {error="Couldn't save your collection. Please try again.";return false;}
            state=next;error="";Changed?.Invoke();return true;
        }
        static CollectionSave Clone(CollectionSave s) => JsonUtility.FromJson<CollectionSave>(JsonUtility.ToJson(s));
    }
}
