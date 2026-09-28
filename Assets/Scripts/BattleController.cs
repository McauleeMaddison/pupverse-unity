using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pupverse
{
    public sealed class BattleController : MonoBehaviour
    {
        [Serializable]
        public sealed class StatButtonBinding
        {
            public CardStat stat;
            public Button button;
        }

        [Header("Cards")]
        public CardView playerCard;
        public CardView opponentCard;

        [Header("Stat Selection")]
        public StatButtonBinding[] statBindings;
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

        [Min(0f)]
        public float winnerReadDuration = 0.65f;

        [Min(0f)]
        public float resultHoldDuration = 1.1f;

        public bool autoNextRound = true;
        public BattleMatchController matchController;
        bool resolving;
        Transform animatedUiCard;
        Vector3 originalUiScale;

        public event Action<BattleResult> RoundCompleted;
        public event Action SelectionReady;
        public event Action<BattleResult> ComparisonStarted;
        public event Action<BattleResult> WinnerRevealed;

        readonly BattleRound round = new BattleRound();

        int wins;
        int losses;
        int draws;

        Coroutine resolution;

        public bool IsResolved => round.IsResolved;

        // Exposed for the existing HUD and editor tests.
        public bool IsResolving => resolving;

        // Exposes the most recently resolved Top Trumps result.
        public BattleResult Result => round.Result;

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
            if(statBindings!=null && statBindings.Length>0)
            {
                if(statBindings.Length!=5 || statBindings.Any(b=>b==null || b.button==null || !Enum.IsDefined(typeof(CardStat),b.stat)) ||
                   statBindings.Select(b=>b.stat).Distinct().Count()!=5 || statBindings.Select(b=>b.button).Distinct().Count()!=5)
                { Debug.LogError("Assign one distinct button for each stat.",this); enabled=false; return; }
                statBindings=statBindings.OrderBy(b=>(int)b.stat).ToArray();
                statButtons=statBindings.Select(b=>b.button).ToArray();
            }
            if(statButtons==null) return;
            for(int i=0;i<statButtons.Length;i++)
            {
                CardStat stat=(CardStat)i;
                if(statButtons[i]!=null) statButtons[i].onClick.AddListener(()=>Choose(stat));
            }
        }

        public void Choose(CardStat stat) => ChooseFor(stat,BattleTurnOwner.Player);
        internal void ChooseForRival(CardStat stat) => ChooseFor(stat,BattleTurnOwner.Rival);
        void ChooseFor(CardStat stat,BattleTurnOwner owner)
        {
            if(!isActiveAndEnabled || (matchController!=null && !matchController.CanChoose(owner)) ||
                (cardAnimator!=null && cardAnimator.IsAttacking)) return;
            if (IsResolving)
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

            resolving=true;
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

            float comparisonWait =
                GameSettings.ReducedMotion
                    ? 0.05f
                    : comparisonDuration;

            if (comparisonWait > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    comparisonWait
                );
            }

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

            if (playerWon && victory != null && cardAnimator == null)
            {
                victory.Play(accent);
            }

            WinnerRevealed?.Invoke(result);

            if (!draw && cardAnimator != null)
            {
                bool boosted =
                    playerWon
                        ? result.PlayerBoost > 0
                        : result.OpponentBoost > 0;

                cardAnimator.SetCardIdentity((playerWon?playerCard:opponentCard).data);
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

            if (!draw && cardAnimator==null && !GameSettings.ReducedMotion)
            {
                Transform winner =
                    playerWon
                        ? playerCard.transform
                        : opponentCard.transform;

                if (winner != null)
                {
                    Vector3 originalScale = winner.localScale;
                    animatedUiCard=winner; originalUiScale=originalScale;

                    float elapsed = 0f;
                    const float pulseDuration = 0.55f;

                    while (elapsed < pulseDuration)
                    {
                        elapsed += Time.unscaledDeltaTime;

                        float t =
                            Mathf.Clamp01(
                                elapsed / pulseDuration
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

                    winner.localScale = originalScale;
                    animatedUiCard=null;
                }
            }

            if (winnerReadDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    winnerReadDuration
                );
            }

            if (autoNextRound)
            {
                if (resultHoldDuration > 0f)
                {
                    yield return new WaitForSecondsRealtime(
                        resultHoldDuration
                    );
                }

                round.Reset();

                if (victory != null)
                {
                    victory.Clear();
                }

                resolution = null; resolving=false;

                ResetUI();

                yield break;
            }

            if (replayButton != null)
            {
                replayButton.interactable = true;
            }

            resolution = null; resolving=false;
            RoundCompleted?.Invoke(result);
        }

        public void Replay()
        {
            if(matchController!=null) return;
            PrepareRound();
        }
        internal void PrepareRound()
        {
            if (IsResolving)
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

        public void ResetSession()
        {
            AbortRound(); wins=losses=draws=0; ResetUI();
        }
        public void AbortRound()
        {
            StopAllCoroutines(); resolution=null; resolving=false; round.Reset();
            if(cardAnimator!=null) cardAnimator.CancelAttack();
            if(animatedUiCard!=null) animatedUiCard.localScale=originalUiScale;
            animatedUiCard=null;
        }
        void OnDisable() { AbortRound(); }

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

            if(cardAnimator!=null && playerCard!=null && playerCard.data!=null && statButtons!=null)
                for(int i=0;i<statButtons.Length;i++)
                {
                    var label=statButtons[i]?.GetComponentInChildren<Text>();
                    if(label!=null) label.text=((CardStat)i).ToString().ToUpperInvariant()+"\n"+playerCard.data.EffectiveValue((CardStat)i);
                }
            UpdateScoreLabel();
            SelectionReady?.Invoke();
        }

        void SetStatButtonsInteractable(
            bool interactable
        )
        {
            bool hasBindings =
                statBindings != null &&
                statBindings.Length > 0;

            if (hasBindings)
            {
                foreach (StatButtonBinding binding in statBindings)
                {
                    if (binding != null &&
                        binding.button != null)
                    {
                        binding.button.interactable =
                            interactable;
                    }
                }

                return;
            }

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
