using System;
namespace Pupverse
{
    public enum BattleWinner { Draw, Player, Opponent }
    public readonly struct BattleResult
    {
        public readonly CardStat Stat;
        public readonly int PlayerBase, PlayerBoost, OpponentBase, OpponentBoost;
        public int PlayerTotal => PlayerBase + PlayerBoost;
        public int OpponentTotal => OpponentBase + OpponentBoost;
        public BattleWinner Winner => PlayerTotal == OpponentTotal ? BattleWinner.Draw :
            PlayerTotal > OpponentTotal ? BattleWinner.Player : BattleWinner.Opponent;
        public BattleResult(CardData player, CardData opponent, CardStat stat)
        {
            Stat = stat;
            PlayerBase = player.stats.Get(stat); PlayerBoost = player.abilityBoosts.Get(stat);
            OpponentBase = opponent.stats.Get(stat); OpponentBoost = opponent.abilityBoosts.Get(stat);
        }
    }
    public static class BattleRules
    {
        // Mirrors src/game/battleRules.js. Both cards receive their selected-stat bonus.
        public static BattleResult Resolve(CardData player, CardData opponent, CardStat stat)
        {
            if (player == null || opponent == null) throw new ArgumentNullException("Both cards are required.");
            if (!Enum.IsDefined(typeof(CardStat), stat)) throw new ArgumentOutOfRangeException(nameof(stat));
            return new BattleResult(player, opponent, stat);
        }
    }
    public sealed class BattleRound
    {
        public bool IsResolved { get; private set; }
        public BattleResult Result { get; private set; }
        public bool TryResolve(CardData player, CardData opponent, CardStat stat)
        {
            if (IsResolved) return false;
            Result = BattleRules.Resolve(player, opponent, stat);
            IsResolved = true;
            return true;
        }
        public void Reset() { IsResolved = false; Result = default; }
    }
}
