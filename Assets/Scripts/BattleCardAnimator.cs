using System.Collections;
using UnityEngine;

namespace Pupverse
{
    [DisallowMultipleComponent]
    public sealed class BattleCardAnimator : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] Transform playerCardRoot = null;
        [SerializeField] Transform rivalCardRoot = null;
        [SerializeField] Transform battleCore = null;

        [Header("Attack movement")]
        [Tooltip("World units toward BattleCore, keeping the card at its starting height.")]
        [SerializeField, Min(0f)] float attackDistance = 1.8f;
        [SerializeField, Min(0f)] float attackDuration = 0.35f;
        [SerializeField, Min(0f)] float centrePauseDuration = 0.15f;
        [SerializeField, Min(0f)] float returnDuration = 0.45f;

        [Header("Prototype preview")]
        [Tooltip("Play Brooklyn's attack once when entering Play mode.")]
        [SerializeField] bool previewOnStart = false;
        [SerializeField] bool showAttackButton = false;
        bool previewPending;

        Transform activeRoot;
        Vector3 startingLocalPosition;
        BattleCardEffects effects;
        CardStat activeStat;
        bool abilityActive;

        public bool IsAttacking { get; private set; }
        public bool IsPlayerTurn { get; private set; } = true;
        public int CompletedAttacks { get; private set; }

        IEnumerator Start()
        {
            if (!previewOnStart) yield break;
            previewPending = true;
            yield return new WaitForSeconds(0.75f);
            if (previewPending) AttackPlayer();
        }

        // Temporary mouse/touch preview control. Does not use the old Input Manager.
        void OnGUI()
        {
            if (!showAttackButton) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            float scale = Mathf.Max(0.25f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            var style = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            float left = (Screen.width / scale - 540f) * 0.5f;
            float top = Screen.height / scale - 136f;
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            string cardName = IsPlayerTurn ? "Brooklyn" : "Raven";
            GUI.Label(new Rect(left, top, 540f, 36f), cardName + (IsAttacking ? " is attacking" : "'s turn"), labelStyle);
            bool ready = previousEnabled && !IsAttacking && battleCore != null;
            GUI.enabled = ready && IsPlayerTurn && playerCardRoot != null;
            if (GUI.Button(new Rect(left, top + 40f, 260f, 56f), "Attack Brooklyn", style)) AttackPlayer();
            GUI.enabled = ready && !IsPlayerTurn && rivalCardRoot != null;
            if (GUI.Button(new Rect(left + 280f, top + 40f, 260f, 56f), "Attack Raven", style)) AttackRival();
            GUI.enabled = previousEnabled && !IsAttacking;
            if (GUI.Button(new Rect(left + 190f, top + 100f, 160f, 30f), "Reset turns")) ResetTurns();
            GUI.enabled = previousEnabled;
            GUI.matrix = previousMatrix;
        }

        // Also callable from a UI Button or future turn controller; no input polling.
        [ContextMenu("Test Brooklyn Attack (Play Mode)")]
        public void AttackPlayer() => TryAttackPlayer(CardStat.Power);

        public bool TryAttackPlayer(CardStat stat, bool boosted = false) => BeginAttack(playerCardRoot, stat, boosted);

        // Raven only attacks on an explicit command, never automatically.
        [ContextMenu("Test Raven Attack (Play Mode)")]
        public void AttackRival() => BeginAttack(rivalCardRoot, CardStat.Power, false);

        public void ResetTurns()
        {
            if (!Application.isPlaying || IsAttacking) return;
            previewPending = false;
            IsPlayerTurn = true;
        }

        bool BeginAttack(Transform root, CardStat stat, bool boosted)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsAttacking) return false;
            if (!System.Enum.IsDefined(typeof(CardStat), stat)) return false;

            if (root == null || battleCore == null)
            {
                Debug.LogWarning("Assign the attacking card root and BattleCore before attacking.", this);
                return false;
            }

            if (!root.gameObject.activeInHierarchy) return false;
            if (playerCardRoot == rivalCardRoot)
            {
                Debug.LogWarning("Player and rival must have different card roots.", this);
                return false;
            }
            if (root != (IsPlayerTurn ? playerCardRoot : rivalCardRoot)) return false;

            // Battle3D's roots share the arena origin; the visible cards are offset children.
            // Use their combined renderer centre for aim, but only ever move the root.
            Vector3 direction = battleCore.position - GetCardCentre(root);
            direction.y = 0f;
            float distance = Mathf.Min(Mathf.Max(0f, attackDistance), direction.magnitude);
            if (distance <= 0f) return false;
            if (stat == CardStat.Defence) distance *= 0.35f;
            if (stat == CardStat.Intelligence) distance *= 0.6f;

            Vector3 targetWorldPosition = root.position + direction.normalized * distance;
            Vector3 targetLocalPosition = root.parent != null
                ? root.parent.InverseTransformPoint(targetWorldPosition)
                : targetWorldPosition;

            activeRoot = root;
            startingLocalPosition = root.localPosition;
            previewPending = false;
            activeStat = stat;
            abilityActive = boosted;
            effects = GetComponent<BattleCardEffects>();
            IsAttacking = true;
            StartCoroutine(Attack(root, targetLocalPosition));
            return true;
        }

        static Vector3 GetCardCentre(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return root.position;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.center;
        }

        IEnumerator Attack(Transform root, Vector3 target)
        {
            bool wasPlayerTurn = IsPlayerTurn;
            float speed = activeStat == CardStat.Speed ? 0.45f : activeStat == CardStat.Intelligence ? 1.3f : 1f;
            if (activeStat == CardStat.Power && !GameSettings.ReducedMotion)
                yield return Move(root, startingLocalPosition, startingLocalPosition - (target - startingLocalPosition) * 0.12f, attackDuration * 0.4f, false);
            if (root != null)
                yield return Move(root, root.localPosition, target, attackDuration * speed, true);
            float elapsed = 0f;
            while (root != null && elapsed < centrePauseDuration)
            {
                elapsed += Time.deltaTime;
                ShowEffect(root, Mathf.Clamp01(elapsed / centrePauseDuration));
                yield return null;
            }
            yield return Move(root, target, startingLocalPosition, returnDuration * speed, false);
            bool completed = root != null;
            RestorePosition();
            // Change turns only after a completed return. Cancelling keeps the current turn.
            if (completed)
            {
                IsPlayerTurn = !wasPlayerTurn;
                CompletedAttacks++;
            }
        }

        IEnumerator Move(Transform root, Vector3 from, Vector3 to, float duration, bool outward)
        {
            float elapsed = 0f;
            while (root != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float progress = Mathf.SmoothStep(0f, 1f, t);
                Vector3 offset = Vector3.zero;
                if (outward && !GameSettings.ReducedMotion)
                {
                    float arc = Mathf.Sin(t * Mathf.PI);
                    float height = activeStat == CardStat.Intelligence ? 0.5f : activeStat == CardStat.Luck ? 0.7f : activeStat == CardStat.Power ? 0.12f : 0f;
                    offset = Vector3.up * (arc * height);
                    if (activeStat == CardStat.Speed) offset.x = Mathf.Sin(t * Mathf.PI * 4f) * arc * 0.2f;
                    if (activeStat == CardStat.Luck) offset.x = Mathf.Sin(t * Mathf.PI * 2f) * arc * 0.5f;
                }
                root.localPosition = Vector3.LerpUnclamped(from, to, progress) +
                    (root.parent != null ? root.parent.InverseTransformVector(offset) : offset);
                ShowEffect(root, t);
                yield return null;
            }

            if (root != null) root.localPosition = to;
        }

        void OnDisable()
        {
            CancelAttack();
        }

        public void CancelAttack()
        {
            previewPending = false;
            StopAllCoroutines();
            RestorePosition();
        }

        void ShowEffect(Transform root, float progress)
        {
            if (effects != null && battleCore != null)
                effects.Show(activeStat, GetCardCentre(root), battleCore.position, progress, abilityActive);
        }

        void RestorePosition()
        {
            // Explicit assignment prevents accumulated drift, including when interrupted.
            if (activeRoot != null) activeRoot.localPosition = startingLocalPosition;
            activeRoot = null;
            IsAttacking = false;
            if (effects != null) effects.Clear();
        }
    }
}
