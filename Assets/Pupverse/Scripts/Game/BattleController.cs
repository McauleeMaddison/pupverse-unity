using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Pupverse
{
    public sealed class BattleController : MonoBehaviour
    {
        public CardView playerCard, opponentCard;
        public Button[] statButtons;
        public Button replayButton, homeButton;
        public Text resultTitle, resultDetail, scoreLabel;
        public Image resultPanel;
        public VictoryEffect victory;
        public CanvasGroup winnerHighlight;
        readonly BattleRound round = new BattleRound();
        int wins, losses, draws;
        Coroutine resolution;
        public bool IsResolved => round.IsResolved;
        void Awake()
        {
            GameSettings.Initialize();
            for (int i = 0; i < statButtons.Length; i++) { var stat = (CardStat)i; statButtons[i].onClick.AddListener(() => Choose(stat)); }
            replayButton.onClick.AddListener(Replay);
            homeButton.onClick.AddListener(() => SceneManager.LoadSceneAsync("MainMenu"));
            ResetUI();
        }
        public void Choose(CardStat stat)
        {
            if (!round.TryResolve(playerCard.data, opponentCard.data, stat)) return;
            foreach (var button in statButtons) button.interactable = false;
            replayButton.interactable = false;
            resolution = StartCoroutine(ShowResult(round.Result));
        }
        IEnumerator ShowResult(BattleResult result)
        {
            resultTitle.text = "COMPARING " + result.Stat.ToString().ToUpperInvariant();
            resultDetail.text = "Base statistic + ability bonus";
            yield return new WaitForSecondsRealtime(GameSettings.ReducedMotion ? .05f : .55f);
            bool won = result.Winner == BattleWinner.Player;
            bool draw = result.Winner == BattleWinner.Draw;
            if (won) wins++; else if (draw) draws++; else losses++;
            Color accent = won ? UITheme.Mint : draw ? UITheme.Lavender : UITheme.Pink;
            resultTitle.text = won ? "VICTORY" : draw ? "A PERFECT TIE" : "BROOKLYN WINS";
            resultTitle.color = accent;
            resultDetail.text = result.Stat + ":  " + result.PlayerBase + " + " + result.PlayerBoost + " = " + result.PlayerTotal +
                "    /    " + result.OpponentBase + " + " + result.OpponentBoost + " = " + result.OpponentTotal;
            scoreLabel.text = wins + " WINS    " + losses + " LOSSES    " + draws + " DRAWS";
            winnerHighlight.alpha = 1;
            if (won) victory.Play(accent);
            var winner = won ? playerCard.transform : opponentCard.transform;
            Vector3 original = winner.localScale;
            if (!draw && !GameSettings.ReducedMotion)
            {
                float elapsed = 0;
                while (elapsed < .55f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    winner.localScale = original * (1 + Mathf.Sin(Mathf.Clamp01(elapsed / .55f) * Mathf.PI) * .07f);
                    yield return null;
                }
                winner.localScale = original;
            }
            replayButton.interactable = true; resolution = null;
        }
        public void Replay()
        {
            if (resolution != null) return;
            round.Reset(); victory.Clear(); ResetUI();
        }
        void ResetUI()
        {
            resultTitle.text = "CHOOSE YOUR EDGE"; resultTitle.color = Color.white;
            resultDetail.text = "Pick a statistic below. Highest total wins.";
            winnerHighlight.alpha = 0;
            foreach (var button in statButtons) button.interactable = true;
            replayButton.interactable = false;
        }
    }
}
