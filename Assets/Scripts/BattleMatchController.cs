using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    public enum BattleTurnOwner
    {
        Player,
        Rival
    }

    public enum BattleMatchWinner
    {
        None,
        Player,
        Rival,
        Draw
    }

    [DisallowMultipleComponent]
    public sealed class BattleMatchController : MonoBehaviour
    {
        const int StartingCardCount = 6;

        [Header("Existing Round Controller")]
        [SerializeField]
        BattleController battle;

        [Header("Player Starting Hand - Exactly 6")]
        [SerializeField]
        CardData[] playerStartingCards =
            new CardData[StartingCardCount];

        [Header("Rival Starting Hand - Exactly 6")]
        [SerializeField]
        CardData[] rivalStartingCards =
            new CardData[StartingCardCount];

        [Header("Match")]
        [SerializeField]
        BattleTurnOwner startingTurn =
            BattleTurnOwner.Player;

        [SerializeField]
        bool startAutomatically = true;

        [Header("Opponent")]
        [SerializeField, Min(0f)]
        float rivalThinkDelay = 0.9f;

        [Header("Round Flow")]
        [SerializeField, Min(0f)]
        float nextRoundDelay = 0.5f;

        readonly Queue<CardData> playerDeck =
            new Queue<CardData>();

        readonly Queue<CardData> rivalDeck =
            new Queue<CardData>();

        readonly List<CardData> drawPot =
            new List<CardData>();

        CardData playerActiveCard;
        CardData rivalActiveCard;

        BattleTurnOwner turnOwner;

        bool matchRunning;
        bool waitingForRoundSettlement;

        Coroutine flowCoroutine;
        Coroutine rivalTurnCoroutine;

        public event Action MatchStarted;

        public event Action<int, int, int>
            CardCountsChanged;

        public event Action<
            CardData,
            CardData,
            BattleTurnOwner
        > RoundPrepared;

        public event Action<
            BattleResult,
            int
        > RoundSettled;

        public event Action<BattleMatchWinner>
            MatchEnded;

        public bool IsMatchRunning =>
            matchRunning;

        public BattleTurnOwner TurnOwner =>
            turnOwner;

        public CardData PlayerActiveCard =>
            playerActiveCard;

        public CardData RivalActiveCard =>
            rivalActiveCard;

        public int PotCount =>
            drawPot.Count;

        public int PlayerCardCount =>
            playerDeck.Count +
            (playerActiveCard != null ? 1 : 0);

        public int RivalCardCount =>
            rivalDeck.Count +
            (rivalActiveCard != null ? 1 : 0);

        void Awake()
        {
            if (battle == null)
            {
                battle =
                    GetComponentInChildren<
                        BattleController
                    >(true);
            }

            if (battle != null)
            {
                /*
                 * The match controller now decides
                 * when the next pair of cards appears.
                 */
                battle.autoNextRound = false;
            }
        }

        void OnEnable()
        {
            if (battle != null)
            {
                battle.WinnerRevealed +=
                    OnWinnerRevealed;
            }
        }

        void OnDisable()
        {
            if (battle != null)
            {
                battle.WinnerRevealed -=
                    OnWinnerRevealed;
            }

            StopFlowCoroutines();
        }

        void Start()
        {
            if (startAutomatically)
            {
                StartMatch();
            }
        }

        public void StartMatch()
        {
            if (!ValidateSetup())
                return;

            StopFlowCoroutines();

            playerDeck.Clear();
            rivalDeck.Clear();
            drawPot.Clear();

            playerActiveCard = null;
            rivalActiveCard = null;

            foreach (
                CardData card
                in playerStartingCards
            )
            {
                playerDeck.Enqueue(card);
            }

            foreach (
                CardData card
                in rivalStartingCards
            )
            {
                rivalDeck.Enqueue(card);
            }

            turnOwner = startingTurn;

            matchRunning = true;
            waitingForRoundSettlement = false;

            battle.autoNextRound = false;

            MatchStarted?.Invoke();

            DealNextRound();
        }

        bool ValidateSetup()
        {
            if (battle == null)
            {
                Debug.LogError(
                    "BattleMatchController requires an existing BattleController.",
                    this
                );

                return false;
            }

            if (battle.playerCard == null ||
                battle.opponentCard == null)
            {
                Debug.LogError(
                    "BattleController requires both CardView references.",
                    this
                );

                return false;
            }

            if (!ValidateHand(
                    playerStartingCards,
                    "Player"
                ))
            {
                return false;
            }

            if (!ValidateHand(
                    rivalStartingCards,
                    "Rival"
                ))
            {
                return false;
            }

            return true;
        }

        bool ValidateHand(
            CardData[] cards,
            string owner
        )
        {
            if (cards == null ||
                cards.Length !=
                StartingCardCount)
            {
                Debug.LogError(
                    owner +
                    " must have exactly " +
                    StartingCardCount +
                    " starting cards.",
                    this
                );

                return false;
            }

            HashSet<CardData> unique =
                new HashSet<CardData>();

            for (
                int i = 0;
                i < cards.Length;
                i++
            )
            {
                CardData card = cards[i];

                if (card == null)
                {
                    Debug.LogError(
                        owner +
                        " card slot " +
                        (i + 1) +
                        " is empty.",
                        this
                    );

                    return false;
                }

                if (!unique.Add(card))
                {
                    Debug.LogError(
                        owner +
                        " must use six different cards. Duplicate: " +
                        card.displayName,
                        this
                    );

                    return false;
                }
            }

            return true;
        }

        void DealNextRound()
        {
            if (!matchRunning)
                return;

            if (playerDeck.Count == 0 ||
                rivalDeck.Count == 0)
            {
                ResolveDeckExhaustion();
                return;
            }

            playerActiveCard =
                playerDeck.Dequeue();

            rivalActiveCard =
                rivalDeck.Dequeue();

            /*
             * BattleController still resolves
             * one card against one card.
             */
            battle.playerCard.data =
                playerActiveCard;

            battle.opponentCard.data =
                rivalActiveCard;

            /*
             * Reset its one-round state.
             */
            battle.Replay();

            NotifyCardCounts();

            RoundPrepared?.Invoke(
                playerActiveCard,
                rivalActiveCard,
                turnOwner
            );

            PrepareTurn();
        }

        void PrepareTurn()
        {
            if (!matchRunning)
                return;

            bool playerTurn =
                turnOwner ==
                BattleTurnOwner.Player;

            SetPlayerStatButtons(
                playerTurn
            );

            if (!playerTurn)
            {
                rivalTurnCoroutine =
                    StartCoroutine(
                        RivalChooseStat()
                    );
            }
        }

        IEnumerator RivalChooseStat()
        {
            yield return
                new WaitForSecondsRealtime(
                    rivalThinkDelay
                );

            if (!matchRunning ||
                turnOwner !=
                BattleTurnOwner.Rival ||
                rivalActiveCard == null ||
                battle.IsResolving)
            {
                rivalTurnCoroutine = null;
                yield break;
            }

            CardStat chosenStat =
                ChooseRivalStat(
                    rivalActiveCard
                );

            battle.Choose(
                chosenStat
            );

            rivalTurnCoroutine = null;
        }

        CardStat ChooseRivalStat(
            CardData card
        )
        {
            /*
             * The rival only looks at its own card,
             * just as the human player does.
             */
            CardStat best =
                CardStat.Power;

            int bestValue =
                int.MinValue;

            for (int i = 0; i < 5; i++)
            {
                CardStat stat =
                    (CardStat)i;

                int value =
                    card.EffectiveValue(
                        stat
                    );

                if (value > bestValue)
                {
                    bestValue = value;
                    best = stat;
                }
            }

            return best;
        }

        void OnWinnerRevealed(
            BattleResult result
        )
        {
            if (!matchRunning ||
                waitingForRoundSettlement)
            {
                return;
            }

            waitingForRoundSettlement =
                true;

            flowCoroutine =
                StartCoroutine(
                    SettleRoundAfterAnimation(
                        result
                    )
                );
        }

        IEnumerator SettleRoundAfterAnimation(
            BattleResult result
        )
        {
            /*
             * WinnerRevealed fires before the existing
             * 3D clash animation finishes.
             *
             * Wait until BattleController has fully
             * completed the round.
             */
            while (battle.IsResolving)
            {
                yield return null;
            }

            if (!matchRunning)
            {
                waitingForRoundSettlement =
                    false;

                flowCoroutine = null;

                yield break;
            }

            if (result.Winner ==
                BattleWinner.Draw)
            {
                HandleDraw(result);
            }
            else
            {
                HandleWin(result);
            }

            waitingForRoundSettlement =
                false;

            flowCoroutine = null;

            if (!matchRunning)
                yield break;

            if (nextRoundDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        nextRoundDelay
                    );
            }

            DealNextRound();
        }

        void HandleWin(
            BattleResult result
        )
        {
            BattleTurnOwner winner =
                result.Winner ==
                BattleWinner.Player
                    ? BattleTurnOwner.Player
                    : BattleTurnOwner.Rival;

            Queue<CardData> winnerDeck =
                winner ==
                BattleTurnOwner.Player
                    ? playerDeck
                    : rivalDeck;

            /*
             * Winner keeps their played card,
             * captures the opponent card,
             * and captures any previous draw pot.
             */
            if (winner ==
                BattleTurnOwner.Player)
            {
                winnerDeck.Enqueue(
                    playerActiveCard
                );

                winnerDeck.Enqueue(
                    rivalActiveCard
                );
            }
            else
            {
                winnerDeck.Enqueue(
                    rivalActiveCard
                );

                winnerDeck.Enqueue(
                    playerActiveCard
                );
            }

            int cardsCaptured =
                2 + drawPot.Count;

            foreach (
                CardData card
                in drawPot
            )
            {
                winnerDeck.Enqueue(card);
            }

            drawPot.Clear();

            playerActiveCard = null;
            rivalActiveCard = null;

            /*
             * Round winner chooses the stat
             * on the next round.
             */
            turnOwner = winner;

            NotifyCardCounts();

            RoundSettled?.Invoke(
                result,
                cardsCaptured
            );

            if (playerDeck.Count == 0)
            {
                EndMatch(
                    BattleMatchWinner.Rival
                );
            }
            else if (rivalDeck.Count == 0)
            {
                EndMatch(
                    BattleMatchWinner.Player
                );
            }
        }

        void HandleDraw(
            BattleResult result
        )
        {
            /*
             * Both cards move into the draw pot.
             * The same side retains stat control.
             */
            drawPot.Add(
                playerActiveCard
            );

            drawPot.Add(
                rivalActiveCard
            );

            playerActiveCard = null;
            rivalActiveCard = null;

            NotifyCardCounts();

            RoundSettled?.Invoke(
                result,
                0
            );

            /*
             * If one player cannot provide another
             * card after a draw, the other player
             * receives the pot and wins.
             */
            if (playerDeck.Count == 0 &&
                rivalDeck.Count == 0)
            {
                EndMatch(
                    BattleMatchWinner.Draw
                );

                return;
            }

            if (playerDeck.Count == 0)
            {
                AwardPot(
                    rivalDeck
                );

                EndMatch(
                    BattleMatchWinner.Rival
                );

                return;
            }

            if (rivalDeck.Count == 0)
            {
                AwardPot(
                    playerDeck
                );

                EndMatch(
                    BattleMatchWinner.Player
                );
            }
        }

        void AwardPot(
            Queue<CardData> deck
        )
        {
            foreach (
                CardData card
                in drawPot
            )
            {
                deck.Enqueue(card);
            }

            drawPot.Clear();

            NotifyCardCounts();
        }

        void ResolveDeckExhaustion()
        {
            if (playerDeck.Count == 0 &&
                rivalDeck.Count == 0)
            {
                EndMatch(
                    BattleMatchWinner.Draw
                );

                return;
            }

            if (playerDeck.Count == 0)
            {
                AwardPot(
                    rivalDeck
                );

                EndMatch(
                    BattleMatchWinner.Rival
                );

                return;
            }

            AwardPot(
                playerDeck
            );

            EndMatch(
                BattleMatchWinner.Player
            );
        }

        void EndMatch(
            BattleMatchWinner winner
        )
        {
            matchRunning = false;

            StopRivalTurn();

            SetPlayerStatButtons(
                false
            );

            NotifyCardCounts();

            if (battle.resultTitle != null)
            {
                battle.resultTitle.text =
                    winner ==
                    BattleMatchWinner.Player
                        ? "MATCH VICTORY"
                        : winner ==
                          BattleMatchWinner.Rival
                            ? "MATCH DEFEAT"
                            : "MATCH DRAW";
            }

            if (battle.resultDetail != null)
            {
                battle.resultDetail.text =
                    winner ==
                    BattleMatchWinner.Player
                        ? "YOU CAPTURED ALL 12 CARDS"
                        : winner ==
                          BattleMatchWinner.Rival
                            ? "THE RIVAL CAPTURED ALL 12 CARDS"
                            : "ALL CARDS ARE LOCKED IN THE DRAW POT";
            }

            MatchEnded?.Invoke(
                winner
            );
        }

        void NotifyCardCounts()
        {
            CardCountsChanged?.Invoke(
                PlayerCardCount,
                RivalCardCount,
                PotCount
            );
        }

        void SetPlayerStatButtons(
            bool enabledState
        )
        {
            if (battle == null)
                return;

            if (battle.statBindings != null &&
                battle.statBindings.Length > 0)
            {
                foreach (
                    BattleController.StatButtonBinding
                    binding
                    in battle.statBindings
                )
                {
                    if (binding != null &&
                        binding.button != null)
                    {
                        binding.button.interactable =
                            enabledState;
                    }
                }

                return;
            }

            if (battle.statButtons == null)
                return;

            foreach (
                Button button
                in battle.statButtons
            )
            {
                if (button != null)
                {
                    button.interactable =
                        enabledState;
                }
            }
        }

        void StopFlowCoroutines()
        {
            if (flowCoroutine != null)
            {
                StopCoroutine(
                    flowCoroutine
                );

                flowCoroutine = null;
            }

            StopRivalTurn();
        }

        void StopRivalTurn()
        {
            if (rivalTurnCoroutine != null)
            {
                StopCoroutine(
                    rivalTurnCoroutine
                );

                rivalTurnCoroutine = null;
            }
        }
    }
}
