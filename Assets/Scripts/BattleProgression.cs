using System;
using System.Linq;
using UnityEngine;

namespace Pupverse
{
    [DefaultExecutionOrder(-30), DisallowMultipleComponent]
    public sealed class BattleProgression : MonoBehaviour
    {
        public const string SaveKey="Pupverse.Collection.v1", BackupKey=SaveKey+".backup";
        public BattleMatchController match;
        [Header("Local prototype economy")]
        [Min(0)] public int welcomeCoins=10, victoryCoins=5, drawCoins=1;
        public CardPackDefinition[] packs={
            new CardPackDefinition{id="alien",title="ALIEN",series="AlienPups",price=10},
            new CardPackDefinition{id="crypto",title="CRYPTO",series="CryptoPups",price=5},
            new CardPackDefinition{id="cyber",title="CYBER",series="CyberPups",price=8}};
        [Tooltip("Relative rarity chances: Common, Uncommon, Rare, Epic, Legendary, Mythic, Mystic. Empty tiers are excluded.")]
        public float[] rarityWeights={70,20,60,28,10,2,2};
        public PlayerCollection Collection {get;private set;}
        public string SaveError {get;private set;}
        public int LastReward {get;private set;}
        string matchId;
        readonly System.Random random=new System.Random();
        [Serializable] sealed class LegacyHand {public string[] ids;}
        void Awake()
        {
            if(match==null) match=GetComponent<BattleMatchController>();
            if(match==null) {enabled=false;return;}
            match.progression=this;
            string raw=PlayerPrefs.GetString(SaveKey,""); CollectionSave state=Read(raw);
            if(state==null && raw.Length>0) state=Read(PlayerPrefs.GetString(BackupKey,""));
            if(state==null && raw.Length>0) {SaveError="Your collection save could not be loaded. It has been preserved.";return;}
            if(state==null)
            {
                var starter=match.StartingHand;
                try
                {
                    var old=JsonUtility.FromJson<LegacyHand>(PlayerPrefs.GetString("Pupverse.StartingHand.v1",""));
                    if(old?.ids?.Length==6)
                    {
                        var restored=old.ids.Select(id=>match.availableCards.FirstOrDefault(c=>c!=null && c.id==id)).ToArray();
                        if(restored.All(c=>c!=null) && restored.Select(c=>c.id).Distinct().Count()==6) starter=restored;
                    }
                }
                catch(ArgumentException) { }
                state=PlayerCollection.Starter(starter,welcomeCoins);
                try {Persist(JsonUtility.ToJson(state));}
                catch(Exception e) when(e is PlayerPrefsException || e is System.IO.IOException || e is UnauthorizedAccessException)
                {SaveError="Couldn't create your collection save. Please try again.";return;}
            }
            Collection=new PlayerCollection(state,Persist);
        }
        void OnEnable() {if(match!=null){match.MatchStarted+=Begin;match.MatchEnded+=Reward;}}
        void OnDisable() {if(match!=null){match.MatchStarted-=Begin;match.MatchEnded-=Reward;}}
        void Begin() {matchId=Guid.NewGuid().ToString("N");LastReward=0;}
        void Reward(BattleMatchWinner winner)
        {
            int reward=winner==BattleMatchWinner.Player?victoryCoins:winner==BattleMatchWinner.Draw?drawCoins:0;
            if(Collection!=null && Collection.AwardMatch(matchId,reward)) LastReward=reward;
        }
        public bool Owns(CardData card) => card!=null && Collection!=null && Collection.OwnedCount(card.id)>0;
        public bool OpenPack(string id,out string error)
        {
            if(match.IsMatchRunning){error="Finish your match before opening packs.";return false;}
            if(Collection==null){error=SaveError??"Collection unavailable.";return false;}
            return Collection.Open(packs.FirstOrDefault(p=>p.id==id),match.availableCards,rarityWeights,random.NextDouble,out error);
        }
        static CollectionSave Read(string json)
        {
            if(string.IsNullOrEmpty(json)) return null;
            try {var save=JsonUtility.FromJson<CollectionSave>(json);return PlayerCollection.Valid(save)?save:null;}
            catch(ArgumentException){return null;}
        }
        static void Persist(string json)
        {
            string previous=PlayerPrefs.GetString(SaveKey,"");
            string backup=PlayerPrefs.GetString(BackupKey,"");
            try
            {
                if(Read(previous)!=null) PlayerPrefs.SetString(BackupKey,previous);
                PlayerPrefs.SetString(SaveKey,json);PlayerPrefs.Save();
            }
            catch
            {
                PlayerPrefs.SetString(SaveKey,previous);PlayerPrefs.SetString(BackupKey,backup);
                throw;
            }
        }
    }
}
