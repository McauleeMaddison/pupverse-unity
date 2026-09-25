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

        readonly BattleRound round =
            new BattleRound();

        int wins;
        int losses;
        int draws;

        Coroutine resolution;

        public bool IsResolved =>
            round.IsResolved;

        void Awake()
        {
            GameSettings.Initialize();

            for (
                int i = 0;
                i < statButtons.Length;
                i++
            )
            {
                int index = i;

                statButtons[i]
                    .onClick
                    .AddListener(
                        () =>
                        {
                            Choose(
                                (CardStat)index
                            );
                        }
                    );
            }

            if (replayButton != null)
            {
                replayButton
                    .onClick
                    .AddListener(
                        Replay
                    );
            }

            if (homeButton != null)
            {
                homeButton
                    .onClick
                    .AddListener(
                        () =>
                        {
                            SceneManager
                                .LoadSceneAsync(
                                    "MainMenu"
                                );
                        }
                    );
            }

            ResetUI();
        }

        public void Choose(
            CardStat stat
        )
        {
            if (
                playerCard == null ||
                opponentCard == null
            )
            {
                Debug.LogWarning(
                    "BattleController is missing a CardView reference.",
                    this
                );

                return;
            }

            if (
                playerCard.data == null ||
                opponentCard.data == null
            )
            {
                Debug.LogWarning(
                    "BattleController cards are missing CardData.",
                    this
                );

                return;
            }

            if (
                !round.TryResolve(
                    playerCard.data,
                    opponentCard.data,
                    stat
                )
            )
            {
                return;
            }

            foreach (
                var button in statButtons
            )
            {
                if (button != null)
                    button.interactable =
                        false;
            }

            if (replayButton != null)
            {
                replayButton.interactable =
                    false;
            }

            resolution =
                StartCoroutine(
                    ShowResult(
                        round.Result
                    )
                );
        }

        IEnumerator ShowResult(
            BattleResult result
        )
        {
            string playerName =
                playerCard.data != null &&
                !string.IsNullOrWhiteSpace(
                    playerCard.data.displayName
                )
                    ? playerCard
                        .data
                        .displayName
                    : "PLAYER";

            string opponentName =
                opponentCard.data != null &&
                !string.IsNullOrWhiteSpace(
                    opponentCard
                        .data
                        .displayName
                )
                    ? opponentCard
                        .data
                        .displayName
                    : "OPPONENT";

            string statName =
                result.Stat
                    .ToString()
                    .ToUpperInvariant();

            if (resultTitle != null)
            {
                resultTitle.text =
                    "COMPARING " +
                    statName;
            }

            if (resultDetail != null)
            {
                resultDetail.text =
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

            yield return
                new WaitForSecondsRealtime(
                    GameSettings.ReducedMotion
                        ? 0.05f
                        : 0.55f
                );

            bool won =
                result.Winner ==
                BattleWinner.Player;

            bool draw =
                result.Winner ==
                BattleWinner.Draw;

            if (won)
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
                won
                    ? UITheme.Mint
                    : draw
                        ? UITheme.Lavender
                        : UITheme.Pink;

            if (resultTitle != null)
            {
                resultTitle.text =
                    draw
                        ? "A PERFECT TIE"
                        : won
                            ? playerName
                                  .ToUpperInvariant() +
                              " WINS"
                            : opponentName
                                  .ToUpperInvariant() +
                              " WINS";

                resultTitle.color =
                    accent;
            }

            if (resultDetail != null)
            {
                resultDetail.text =
                    statName +
                    "\n" +
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

            if (scoreLabel != null)
            {
                scoreLabel.text =
                    wins +
                    " WINS    " +
                    losses +
                    " LOSSES    " +
                    draws +
                    " DRAWS";
            }

            if (winnerHighlight != null)
            {
                winnerHighlight.alpha =
                    1f;
            }

            if (
                won &&
                victory != null
            )
            {
                victory.Play(accent);
            }

            // -----------------------------------------
            // Animate ONLY the card that actually won.
            // -----------------------------------------

            if (
                !draw &&
                cardAnimator != null
            )
            {
                bool boosted =
                    won
                        ? result.PlayerBoost > 0
                        : result.OpponentBoost > 0;

                if (won)
                {
                    cardAnimator
                        .PlayPlayerResultAttack(
                            result.Stat,
                            boosted
                        );
                }
                else
                {
                    cardAnimator
                        .PlayRivalResultAttack(
                            result.Stat,
                            boosted
                        );
                }

                while (
                    cardAnimator.IsAttacking
                )
                {
                    yield return null;
                }
            }

            // Small winner emphasis after the 3D movement.
            if (
                !draw &&
                !GameSettings.ReducedMotion
            )
            {
                Transform winner =
                    won
                        ? playerCard.transform
                        : opponentCard.transform;

                Vector3 original =
                    winner.localScale;

                float elapsed = 0f;

                while (
                    elapsed < 0.55f
                )
                {
                    elapsed +=
                        Time.unscaledDeltaTime;

                    winner.localScale =
                        original *
                        (
                            1f +
                            Mathf.Sin(
                                Mathf.Clamp01(
                                    elapsed /
                                    0.55f
                                ) *
                                Mathf.PI
                            ) *
                            0.07f
                        );

                    yield return null;
                }

                winner.localScale =
                    original;
            }

            if (replayButton != null)
            {
                replayButton.interactable =
                    true;
            }

            resolution = null;
        }

        public void Replay()
        {
            if (resolution != null)
                return;

            round.Reset();

            if (victory != null)
                victory.Clear();

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
                winnerHighlight.alpha =
                    0f;
            }

            foreach (
                var button in statButtons
            )
            {
                if (button != null)
                {
                    button.interactable =
                        true;
                }
            }

            if (replayButton != null)
            {
                replayButton.interactable =
                    false;
            }

            if (scoreLabel != null)
            {
                scoreLabel.text =
                    wins +
                    " WINS    " +
                    losses +
                    " LOSSES    " +
                    draws +
                    " DRAWS";
            }
        }
    }
}
