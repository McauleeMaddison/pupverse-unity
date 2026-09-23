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

        public bool IsAttacking { get; private set; }
        public bool IsPlayerTurn { get; private set; } = true;

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
        public void AttackPlayer() => BeginAttack(playerCardRoot);

        // Raven only attacks on an explicit command, never automatically.
        [ContextMenu("Test Raven Attack (Play Mode)")]
        public void AttackRival() => BeginAttack(rivalCardRoot);

        public void ResetTurns()
        {
            if (!Application.isPlaying || IsAttacking) return;
            previewPending = false;
            IsPlayerTurn = true;
        }

        void BeginAttack(Transform root)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsAttacking) return;

            if (root == null || battleCore == null)
            {
                Debug.LogWarning("Assign the attacking card root and BattleCore before attacking.", this);
                return;
            }

            if (!root.gameObject.activeInHierarchy) return;
            if (playerCardRoot == rivalCardRoot)
            {
                Debug.LogWarning("Player and rival must have different card roots.", this);
                return;
            }
            if (root != (IsPlayerTurn ? playerCardRoot : rivalCardRoot)) return;

            // Battle3D's roots share the arena origin; the visible cards are offset children.
            // Use their combined renderer centre for aim, but only ever move the root.
            Vector3 direction = battleCore.position - GetCardCentre(root);
            direction.y = 0f;
            float distance = Mathf.Min(Mathf.Max(0f, attackDistance), direction.magnitude);
            if (distance <= 0f) return;

            Vector3 targetWorldPosition = root.position + direction.normalized * distance;
            Vector3 targetLocalPosition = root.parent != null
                ? root.parent.InverseTransformPoint(targetWorldPosition)
                : targetWorldPosition;

            activeRoot = root;
            startingLocalPosition = root.localPosition;
            previewPending = false;
            IsAttacking = true;
            StartCoroutine(Attack(root, targetLocalPosition));
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
            yield return Move(root, startingLocalPosition, target, attackDuration);
            if (centrePauseDuration > 0f) yield return new WaitForSeconds(centrePauseDuration);
            yield return Move(root, target, startingLocalPosition, returnDuration);
            bool completed = root != null;
            RestorePosition();
            // Change turns only after a completed return. Cancelling keeps the current turn.
            if (completed) IsPlayerTurn = !wasPlayerTurn;
        }

        static IEnumerator Move(Transform root, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (root != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                root.localPosition = Vector3.LerpUnclamped(from, to, progress);
                yield return null;
            }

            if (root != null) root.localPosition = to;
        }

        void OnDisable()
        {
            previewPending = false;
            StopAllCoroutines();
            RestorePosition();
        }

        void RestorePosition()
        {
            // Explicit assignment prevents accumulated drift, including when interrupted.
            if (activeRoot != null) activeRoot.localPosition = startingLocalPosition;
            activeRoot = null;
            IsAttacking = false;
        }
    }
}
