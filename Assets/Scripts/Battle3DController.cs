using System;
using System.Collections;
using UnityEngine;

namespace Pupverse
{
    [DisallowMultipleComponent]
    public sealed class Battle3DController : MonoBehaviour
    {
        [SerializeField] BattleCardAnimator animator = null;
        [SerializeField] CardData playerCard = null;
        [SerializeField] CardData rivalCard = null;
        [SerializeField] Camera battleCamera = null;
        Rect originalCameraRect;
        bool ownsViewport;
        readonly BattleRound round = new BattleRound();
        CardStat selectedStat;

        public bool IsResolving { get; private set; }
        public bool IsResolved => round.IsResolved;
        public BattleResult Result => round.Result;
        public bool IsReady => animator != null && animator.isActiveAndEnabled && playerCard != null && rivalCard != null;

        public bool Choose(CardStat stat)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !IsReady || IsResolving || IsResolved || animator.IsAttacking) return false;
            if (!Enum.IsDefined(typeof(CardStat), stat)) return false;
            animator.ResetTurns();
            int completedBefore = animator.CompletedAttacks;
            if (!animator.TryAttackPlayer(stat, playerCard.abilityBoosts.Get(stat) > 0)) return false;
            selectedStat = stat;
            IsResolving = true;
            StartCoroutine(ResolveAfterAttack(stat, completedBefore));
            return true;
        }

        IEnumerator ResolveAfterAttack(CardStat stat, int completedBefore)
        {
            while (animator != null && animator.IsAttacking) yield return null;
            if (animator != null && animator.CompletedAttacks > completedBefore)
                round.TryResolve(playerCard, rivalCard, stat);
            IsResolving = false;
        }

        public void NextRound()
        {
            if (IsResolving || (animator != null && animator.IsAttacking)) return;
            round.Reset();
            if (animator != null) animator.ResetTurns();
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (IsResolving && animator != null) animator.CancelAttack();
            IsResolving = false;
            if (ownsViewport && battleCamera != null) battleCamera.rect = originalCameraRect;
            ownsViewport = false;
        }

        // Layout uses phone-width logical units so touch targets remain large in portrait.
        public static float LayoutScale(Rect safe, bool portrait) =>
            Mathf.Max(0.1f, portrait ? safe.width / 420f : Mathf.Min(safe.width / 900f, safe.height / 540f));

        void LateUpdate()
        {
            if (battleCamera == null || Screen.width == 0 || Screen.height == 0) return;
            if (!ownsViewport)
            {
                originalCameraRect = battleCamera.rect;
                ownsViewport = true;
            }
            Rect safe = Screen.safeArea;
            bool portrait = safe.height > safe.width;
            float panelHeight = (portrait ? 304f : 188f) * LayoutScale(safe, portrait);
            // Reserve the controls' space without changing the camera or arena transforms.
            battleCamera.rect = new Rect(safe.x / Screen.width, (safe.y + panelHeight) / Screen.height,
                safe.width / Screen.width, Mathf.Max(1f, safe.height - panelHeight) / Screen.height);
        }

        // Mouse/touch events come from Unity GUI; no Input Manager polling or hover controls.
        void OnGUI()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            Color previousBackground = GUI.backgroundColor;
            Color previousColor = GUI.color;
            Rect safe = Screen.safeArea;
            bool portrait = safe.height > safe.width;
            float scale = LayoutScale(safe, portrait);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = safe.width / scale - 24f;
            float left = safe.x / scale + 12f;
            float panelHeight = portrait ? 304f : 188f;
            float top = (Screen.height - safe.y) / scale - panelHeight;
            GUI.color = new Color(0.025f, 0.04f, 0.10f, 0.98f);
            GUI.DrawTexture(new Rect(left - 12f, top, width + 24f, panelHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var label = new GUIStyle(GUI.skin.label) { fontSize = portrait ? 15 : 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var titleStyle = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true,
                border = new RectOffset(), padding = new RectOffset(6, 6, 3, 3) };
            foreach (var state in new[] { button.normal, button.hover, button.active, button.focused })
            {
                state.background = Texture2D.whiteTexture;
                state.textColor = new Color(0.025f, 0.04f, 0.1f);
            }
            if (!IsReady)
            {
                GUI.Label(new Rect(left, top, width, panelHeight), "Connect both cards to start the battle.", label);
            }
            else
            {
                string title = IsResolving ? selectedStat + " attack" : "Choose your stat";
                if (IsResolved) title = Result.Winner == BattleWinner.Draw ? "Draw" :
                    (Result.Winner == BattleWinner.Player ? playerCard.displayName : rivalCard.displayName) + " wins";
                GUI.Label(new Rect(left, top + 6f, width, 28f), title, titleStyle);
                if (!IsResolved)
                {
                    float buttonWidth = portrait ? (width - 8f) * 0.5f : (width - 32f) / 5f;
                    for (int i = 0; i < 5; i++)
                    {
                        CardStat stat = (CardStat)i;
                        int bonus = playerCard.abilityBoosts.Get(stat);
                        string value = playerCard.stats.Get(stat) + (bonus > 0 ? " + " + bonus : "");
                        float x = left + (portrait ? i % 2 : i) * (buttonWidth + 8f);
                        float y = top + 40f + (portrait ? i / 2 : 0) * 58f;
                        float w = portrait && i == 4 ? width : buttonWidth;
                        GUI.backgroundColor = BattleCardEffects.StatColor(stat);
                        GUI.enabled = previousEnabled && !IsResolving && !animator.IsAttacking;
                        if (GUI.Button(new Rect(x, y, w, 50f), stat + "  " + value, button)) Choose(stat);
                    }
                    GUI.enabled = previousEnabled;
                    GUI.backgroundColor = previousBackground;
                    string abilities = IsResolving ? AbilityLine(playerCard, selectedStat) + "\n" + AbilityLine(rivalCard, selectedStat) :
                        playerCard.displayName + " · " + playerCard.abilityName + "\n" + playerCard.abilityDescription;
                    GUI.Label(new Rect(left, top + (portrait ? 214f : 96f), width, 80f), abilities, label);
                }
                else
                {
                    string totals = Result.Stat + " · Base + ability bonus\n" + playerCard.displayName + " " + Result.PlayerBase + " + " + Result.PlayerBoost + " = " + Result.PlayerTotal +
                        "   |   " + rivalCard.displayName + " " + Result.OpponentBase + " + " + Result.OpponentBoost + " = " + Result.OpponentTotal;
                    GUI.Label(new Rect(left, top + 36f, width, portrait ? 68f : 44f), totals, label);
                    GUI.Label(new Rect(left, top + (portrait ? 114f : 80f), width, 52f), AbilityLine(playerCard, Result.Stat) + "\n" + AbilityLine(rivalCard, Result.Stat), label);
                    GUI.backgroundColor = BattleCardEffects.StatColor(CardStat.Speed);
                    if (GUI.Button(new Rect(left, top + panelHeight - 62f, width, 50f), "Next round", button)) NextRound();
                }
            }
            GUI.backgroundColor = previousBackground;
            GUI.color = previousColor;
            GUI.enabled = previousEnabled;
            GUI.matrix = previousMatrix;
        }

        static string AbilityLine(CardData card, CardStat stat)
        {
            int bonus = card.abilityBoosts.Get(stat);
            return card.displayName + ": " + (bonus > 0 ? card.abilityName + " +" + bonus + " " + stat : "no " + stat + " bonus");
        }
    }
}
