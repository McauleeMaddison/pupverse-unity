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
        public struct StatButtonBinding
        {
            public CardStat stat;
            public Button button;
        }

        public CardView playerCard, opponentCard;
        public Button[] statButtons;
        [Tooltip("Explicit mapping. Older scenes may continue to use statButtons in enum order.")]
        public StatButtonBinding[] statBindings;
        public Button replayButton, homeButton;
        public Text resultTitle, resultDetail, scoreLabel;
        public Image resultPanel;
        public VictoryEffect victory;
        public CanvasGroup winnerHighlight;
        [Header("Optional 3D presentation")]
        public BattleCardAnimator cardAnimator;
        public bool autoNextRound;
        [Min(0)] public float comparisonDuration = 0.7f;
        [Min(0)] public float winnerReadDuration = 0.45f;
        [Min(0)] public float resultHoldDuration = 1.3f;
        readonly BattleRound round = new BattleRound();
        bool initialized;
        Transform animatedUiCard;
        Vector3 originalUiScale;
        public bool IsResolved => round.IsResolved;
        public bool IsResolving { get; private set; }
        public BattleResult Result => round.Result;
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Draws { get; private set; }

        public event Action SelectionReady;
        public event Action<BattleResult> ComparisonStarted;
        public event Action<BattleResult> WinnerRevealed;

        void Awake()
        {
            GameSettings.Initialize();
            if (statBindings != null && statBindings.Length > 0)
            {
                if (statBindings.Length != 5 || statBindings.Any(b => b.button == null || !Enum.IsDefined(typeof(CardStat), b.stat)) ||
                    statBindings.Select(b => b.stat).Distinct().Count() != 5 || statBindings.Select(b => b.button).Distinct().Count() != 5)
                {
                    Debug.LogError("BattleController needs one distinct button for each of the five stats.", this);
                    enabled = false;
                    return;
                }
                statBindings = statBindings.OrderBy(b => (int)b.stat).ToArray();
                statButtons = statBindings.Select(b => b.button).ToArray();
            }
            if (playerCard == null || playerCard.data == null || opponentCard == null || opponentCard.data == null ||
                resultTitle == null || resultDetail == null || scoreLabel == null || statButtons == null ||
                statButtons.Length != 5 || statButtons.Any(b => b == null))
            {
                Debug.LogError("BattleController is missing card data, result text, score, or one of its five stat buttons.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < statButtons.Length; i++)
            {
                CardStat stat = (CardStat)i;
                statButtons[i].onClick.AddListener(() => Choose(stat));
            }
            if (replayButton != null) replayButton.onClick.AddListener(Replay);
            if (homeButton != null) homeButton.onClick.AddListener(() => SceneManager.LoadSceneAsync("MainMenu"));
            initialized = true;
            ResetUI();
        }

        public void Choose(CardStat stat)
        {
            if (!initialized || !isActiveAndEnabled || IsResolving || !Enum.IsDefined(typeof(CardStat), stat) ||
                (cardAnimator != null && cardAnimator.IsAttacking)) return;
            if (!round.TryResolve(playerCard.data, opponentCard.data, stat)) return;
            IsResolving = true;
            foreach (var button in statButtons) button.interactable = false;
            if (replayButton != null) replayButton.interactable = false;
            StartCoroutine(ShowResult(round.Result));
        }

        IEnumerator ShowResult(BattleResult result)
        {
            // Resolve and reveal the exact same stat for both cards BEFORE any movement.
            resultTitle.text = "COMPARING " + result.Stat.ToString().ToUpperInvariant();
            resultDetail.text = playerCard.data.displayName + ": " + result.PlayerBase + " + " + result.PlayerBoost + " = " + result.PlayerTotal +
                (cardAnimator != null ? "\n" : "    /    ") + opponentCard.data.displayName + ": " + result.OpponentBase + " + " + result.OpponentBoost + " = " + result.OpponentTotal;
            ComparisonStarted?.Invoke(result);
            yield return new WaitForSecondsRealtime(comparisonDuration);
            bool won = result.Winner == BattleWinner.Player;
            bool draw = result.Winner == BattleWinner.Draw;
            if (won) Wins++; else if (draw) Draws++; else Losses++;
            Color accent = won ? UITheme.Mint : draw ? UITheme.Lavender : UITheme.Pink;
            resultTitle.text = draw ? "DRAW" : (won ? playerCard.data : opponentCard.data).displayName.ToUpperInvariant() + " WINS";
            resultTitle.color = accent;
            scoreLabel.text = Wins + " WINS    " + Losses + " LOSSES    " + Draws + " DRAWS";
            if (winnerHighlight != null) winnerHighlight.alpha = 1;
            WinnerRevealed?.Invoke(result);
            yield return new WaitForSecondsRealtime(winnerReadDuration);
            if (!draw && cardAnimator != null)
            {
                bool boosted = (won ? result.PlayerBoost : result.OpponentBoost) > 0;
                if (cardAnimator.PlayWinner(result.Winner, result.Stat, boosted))
                    while (cardAnimator != null && cardAnimator.IsAttacking) yield return null;
                else Debug.LogWarning("The round resolved, but the winner animation could not start. Check the card root and core references.", this);
            }
            else if (!draw && !GameSettings.ReducedMotion)
            {
                // Preserve the existing 2D battle presentation for scenes without a 3D animator.
                if (won && victory != null) victory.Play(accent);
                animatedUiCard = (won ? playerCard : opponentCard).transform;
                originalUiScale = animatedUiCard.localScale;
                float elapsed = 0;
                while (elapsed < .55f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    animatedUiCard.localScale = originalUiScale * (1 + Mathf.Sin(Mathf.Clamp01(elapsed / .55f) * Mathf.PI) * .07f);
                    yield return null;
                }
                animatedUiCard.localScale = originalUiScale;
                animatedUiCard = null;
            }
            yield return new WaitForSecondsRealtime(resultHoldDuration);
            IsResolving = false;
            if (autoNextRound) Replay();
            else if (replayButton != null) replayButton.interactable = true;
        }

        public void Replay()
        {
            if (!initialized || IsResolving || (cardAnimator != null && cardAnimator.IsAttacking)) return;
            round.Reset();
            if (victory != null) victory.Clear();
            ResetUI();
        }

        void ResetUI()
        {
            resultTitle.text = "CHOOSE YOUR STAT";
            resultTitle.color = Color.white;
            resultDetail.text = cardAnimator != null ? "Same stat. Base + ability bonus.\nHighest total wins." : "Pick a statistic below. Highest total wins.";
            scoreLabel.text = Wins + " WINS    " + Losses + " LOSSES    " + Draws + " DRAWS";
            if (winnerHighlight != null) winnerHighlight.alpha = 0;
            for (int i = 0; i < statButtons.Length; i++)
            {
                statButtons[i].interactable = true;
                if (cardAnimator != null)
                {
                    var label = statButtons[i].GetComponentInChildren<Text>();
                    if (label != null) label.text = ((CardStat)i).ToString().ToUpperInvariant() + "\n" + playerCard.data.EffectiveValue((CardStat)i);
                }
            }
            if (replayButton != null) replayButton.interactable = false;
            SelectionReady?.Invoke();
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (IsResolving && cardAnimator != null) cardAnimator.CancelAttack();
            if (animatedUiCard != null) animatedUiCard.localScale = originalUiScale;
            animatedUiCard = null;
            IsResolving = false;
            round.Reset();
            if (initialized) ResetUI();
        }
    }
}
