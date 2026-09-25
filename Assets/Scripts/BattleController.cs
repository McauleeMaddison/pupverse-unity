using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pupverse
{
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Cards")]
        public CardView playerCard;
        public CardView opponentCard;

        [Header("Stat Selection")]
        public Button[] statButtons;

        [Header("Navigation")]
        public Button replayButton;
        public Button homeButton;

        [Header("Result UI")]
        public Text resultTitle;
        public Text resultDetail;
        public Text scoreLabel;
        public Image resultPanel;

        [Header("Effects")]
        public VictoryEffect victory;
        public CanvasGroup winnerHighlight;

        [Header("3D Battle Animation")]
        public BattleCardAnimator cardAnimator;

        [Header("Battle Timing")]
        [Min(0f)]
        public float comparisonDuration = 0.55f;

        /*
         * These events are used by BattleHudPresentation.
         *
         * SelectionReady:
         *      The player may choose a stat.
         *
         * ComparisonStarted:
         *      A stat has been selected and both cards are being compared.
         *
         * WinnerRevealed:
         *      The comparison is complete and a winner/draw is known.
         */
        public event Action SelectionReady;
        public event Action<BattleResult> ComparisonStarted;
        public event Action<BattleResult> WinnerRevealed;

        readonly BattleRound round = new BattleRound();

        int wins;
        int losses;
        int draws;

        Coroutine resolution;

        public bool IsResolved => round.IsResolved;

        // Public score API required by BattleHudPresentation.
        public int Wins => wins;
        public int Losses => losses;
        public int Draws => draws;

        void Awake()
        {
            GameSettings.Initialize();

            WireStatButtons();

            if (replayButton != null)
            {
                replayButton.onClick.AddListener(Replay);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(
                    () => SceneManager.LoadSceneAsync("MainMenu")
                );
            }

            ResetUI();
        }

        void WireStatButtons()
        {
            if (statButtons == null)
                return;

            for (int i = 0; i < statButtons.Length; i++)
            {
                Button button = statButtons[i];

                if (button == null)
                    continue;

                int index = i;

                button.onClick.AddListener(
                    () => Choose((CardStat)index)
                );
            }
        }

        public void Choose(CardStat stat)
        {
            if (resolution != null)
                return;

            if (playerCard == null || opponentCard == null)
            {
                Debug.LogWarning(
                    "BattleController is missing a CardView reference.",
                    this
                );

                return;
            }

            if (playerCard.data == null || opponentCard.data == null)
            {
                Debug.LogWarning(
                    "BattleController cards are missing CardData.",
                    this
                );

                return;
            }

            if (!Enum.IsDefined(typeof(CardStat), stat))
            {
                Debug.LogWarning(
                    "BattleController received an invalid CardStat.",
                    this
                );

                return;
            }

            if (!round.TryResolve(
                    playerCard.data,
                    opponentCard.data,
                    stat
                ))
            {
                return;
            }

            SetStatButtonsInteractable(false);

            if (replayButton != null)
            {
                replayButton.interactable = false;
            }

            BattleResult result = round.Result;

            /*
             * Tell the presentation layer that the comparison
             * has started.
             */
            ComparisonStarted?.Invoke(result);

            resolution = StartCoroutine(
                ShowResult(result)
            );
        }

        IEnumerator ShowResult(BattleResult result)
        {
            string playerName = GetCardName(
                playerCard,
                "PLAYER"
            );

            string opponentName = GetCardName(
                opponentCard,
                "OPPONENT"
            );

            string statName =
                result.Stat
                    .ToString()
                    .ToUpperInvariant();

            if (resultTitle != null)
            {
                resultTitle.text =
                    "COMPARING " + statName;
            }

            if (resultDetail != null)
            {
                resultDetail.text =
                    BuildComparisonText(
                        playerName,
                        opponentName,
                        result
                    );
            }

            float delay =
                GameSettings.ReducedMotion
                    ? 0.05f
                    : comparisonDuration;

            yield return new WaitForSecondsRealtime(delay);

            bool playerWon =
                result.Winner ==
                BattleWinner.Player;

            bool draw =
                result.Winner ==
                BattleWinner.Draw;

            if (playerWon)
            {
                wins++;
            }
            else if (draw)
            {
                draws++;
            }
            else
            {
                losses++;
            }

            Color accent =
                playerWon
                    ? UITheme.Mint
                    : draw
                        ? UITheme.Lavender
                        : UITheme.Pink;

            if (resultTitle != null)
            {
                resultTitle.text =
                    draw
                        ? "A PERFECT TIE"
                        : playerWon
                            ? playerName.ToUpperInvariant() + " WINS"
                            : opponentName.ToUpperInvariant() + " WINS";

                resultTitle.color = accent;
            }

            if (resultDetail != null)
            {
                resultDetail.text =
                    statName +
                    "\n" +
                    BuildComparisonText(
                        playerName,
                        opponentName,
                        result
                    );
            }

            UpdateScoreLabel();

            if (winnerHighlight != null)
            {
                winnerHighlight.alpha = 1f;
            }

            if (playerWon && victory != null)
            {
                victory.Play(accent);
            }

            /*
             * Tell BattleHudPresentation that the result is now known.
             *
             * This restores the API that the existing HUD script
             * already expects.
             */
            WinnerRevealed?.Invoke(result);

            /*
             * The Top Trumps comparison decides which 3D card moves.
             *
             * No alternating fake attack turns.
             */
            if (!draw && cardAnimator != null)
            {
                bool boosted =
                    playerWon
                        ? result.PlayerBoost > 0
                        : result.OpponentBoost > 0;

                bool animationStarted;

                if (playerWon)
                {
                    animationStarted =
                        cardAnimator.PlayPlayerResultAttack(
                            result.Stat,
                            boosted
                        );
                }
                else
                {
                    animationStarted =
                        cardAnimator.PlayRivalResultAttack(
                            result.Stat,
                            boosted
                        );
                }

                if (animationStarted)
                {
                    while (cardAnimator.IsAttacking)
                    {
                        yield return null;
                    }
                }
            }

            /*
             * Small winner pulse after the arena animation.
             */
            if (!draw && !GameSettings.ReducedMotion)
            {
                Transform winner =
                    playerWon
                        ? playerCard.transform
                        : opponentCard.transform;

                if (winner != null)
                {
                    Vector3 originalScale =
                        winner.localScale;

                    float elapsed = 0f;
                    const float pulseDuration = 0.55f;

                    while (elapsed < pulseDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;

                        float t =
                            Mathf.Clamp01(
                                elapsed /
                                pulseDuration
                            );

                        winner.localScale =
                            originalScale *
                            (
                                1f +
                                Mathf.Sin(
                                    t * Mathf.PI
                                ) *
                                0.07f
                            );

                        yield return null;
                    }

                    winner.localScale =
                        originalScale;
                }
            }

            if (replayButton != null)
            {
                replayButton.interactable = true;
            }

            resolution = null;
        }

        public void Replay()
        {
            if (resolution != null)
                return;

            round.Reset();

            if (cardAnimator != null &&
                cardAnimator.IsAttacking)
            {
                cardAnimator.CancelAttack();
            }

            if (victory != null)
            {
                victory.Clear();
            }

            ResetUI();
        }

        void ResetUI()
        {
            if (resultTitle != null)
            {
                resultTitle.text =
                    "CHOOSE YOUR EDGE";

                resultTitle.color =
                    Color.white;
            }

            if (resultDetail != null)
            {
                resultDetail.text =
                    "Pick a statistic below. Highest total wins.";
            }

            if (winnerHighlight != null)
            {
                winnerHighlight.alpha = 0f;
            }

            SetStatButtonsInteractable(true);

            if (replayButton != null)
            {
                replayButton.interactable = false;
            }

            UpdateScoreLabel();

            /*
             * Existing BattleHudPresentation listens for this.
             */
            SelectionReady?.Invoke();
        }

        void SetStatButtonsInteractable(bool interactable)
        {
            if (statButtons == null)
                return;

            foreach (Button button in statButtons)
            {
                if (button != null)
                {
                    button.interactable =
                        interactable;
                }
            }
        }

        void UpdateScoreLabel()
        {
            if (scoreLabel == null)
                return;

            scoreLabel.text =
                wins +
                " WINS    " +
                losses +
                " LOSSES    " +
                draws +
                " DRAWS";
        }

        static string GetCardName(
            CardView card,
            string fallback
        )
        {
            if (card == null ||
                card.data == null ||
                string.IsNullOrWhiteSpace(
                    card.data.displayName
                ))
            {
                return fallback;
            }

            return card.data.displayName;
        }

        static string BuildComparisonText(
            string playerName,
            string opponentName,
            BattleResult result
        )
        {
            return
                playerName +
                ": " +
                result.PlayerBase +
                " + " +
                result.PlayerBoost +
                " = " +
                result.PlayerTotal +
                "\n" +
                opponentName +
                ": " +
                result.OpponentBase +
                " + " +
                result.OpponentBoost +
                " = " +
                result.OpponentTotal;
        }
    }
}
