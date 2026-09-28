using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    /*
     * Premium visual presentation for the 6-vs-6 match.
     *
     * IMPORTANT:
     * This class does NOT own battle rules.
     * It only listens to BattleMatchController events
     * and visualises card ownership, captures and draws.
     */
    [DisallowMultipleComponent]
    public sealed class BattleDeckPresentation : MonoBehaviour
    {
        [Header("References")]
        public BattleMatchController match;
        public Battle3DController layout;

        [Header("Animation")]
        [Min(0f)]
        public float captureDuration = 0.42f;

        [Min(0f)]
        public float stackPulseDuration = 0.28f;

        [Min(0f)]
        public float dealPulseDuration = 0.22f;

        [Header("Visuals")]
        [Range(0.1f, 0.8f)]
        public float panelOpacity = 0.42f;

        readonly List<RectTransform> playerCards =
            new List<RectTransform>();

        readonly List<RectTransform> rivalCards =
            new List<RectTransform>();

        RectTransform root;

        BattleHudGraphic playerPanel;
        BattleHudGraphic rivalPanel;
        BattleHudGraphic potPanel;

        RectTransform playerStack;
        RectTransform rivalStack;
        RectTransform potRoot;

        Text playerLabel;
        Text rivalLabel;
        Text potLabel;

        Canvas canvas;
        Font font;

        CardData lastPlayerCard;
        CardData lastRivalCard;

        Coroutine playerPulse;
        Coroutine rivalPulse;
        Coroutine potPulse;

        bool built;
        bool subscribed;

        static readonly Color PlayerColour =
            new Color(
                0.18f,
                0.92f,
                1f
            );

        static readonly Color RivalColour =
            new Color(
                0.72f,
                0.42f,
                1f
            );

        static readonly Color PotColour =
            new Color(
                1f,
                0.72f,
                0.22f
            );

        static readonly Color Muted =
            new Color(
                0.55f,
                0.65f,
                0.79f
            );

        void Start()
        {
            if (match == null)
            {
                match =
                    Object.FindFirstObjectByType<
                        BattleMatchController
                    >();
            }

            if (layout == null)
            {
                layout =
                    Object.FindFirstObjectByType<
                        Battle3DController
                    >();
            }

            if (match == null ||
                layout == null ||
                layout.controlsRoot == null)
            {
                Debug.LogError(
                    "BattleDeckPresentation could not find the battle match/layout.",
                    this
                );

                enabled = false;
                return;
            }

            canvas =
                layout.controlsRoot
                    .GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                Debug.LogError(
                    "BattleDeckPresentation requires the battle Canvas.",
                    this
                );

                enabled = false;
                return;
            }

            if (match.Battle != null &&
                match.Battle.resultTitle != null)
            {
                font =
                    match.Battle
                        .resultTitle
                        .font;
            }

            Build();

            built = true;

            Subscribe();

            SetVisible(
                match.IsMatchRunning
            );

            RefreshCounts(
                match.PlayerCardCount,
                match.RivalCardCount,
                match.PotCount
            );
        }

        void OnEnable()
        {
            if (built)
            {
                Subscribe();
            }
        }

        void OnDisable()
        {
            Unsubscribe();

            StopAllCoroutines();

            playerPulse = null;
            rivalPulse = null;
            potPulse = null;
        }

        void Subscribe()
        {
            if (subscribed ||
                match == null)
            {
                return;
            }

            match.MatchStarted +=
                MatchStarted;

            match.HandSelectionOpened +=
                HandSelectionOpened;

            match.CardCountsChanged +=
                CountsChanged;

            match.RoundPrepared +=
                RoundPrepared;

            match.RoundSettled +=
                RoundSettled;

            match.MatchEnded +=
                MatchEnded;

            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed ||
                match == null)
            {
                return;
            }

            match.MatchStarted -=
                MatchStarted;

            match.HandSelectionOpened -=
                HandSelectionOpened;

            match.CardCountsChanged -=
                CountsChanged;

            match.RoundPrepared -=
                RoundPrepared;

            match.RoundSettled -=
                RoundSettled;

            match.MatchEnded -=
                MatchEnded;

            subscribed = false;
        }

        void Build()
        {
            RectTransform parent =
                layout.controlsRoot.parent
                    as RectTransform;

            root =
                new GameObject(
                    "Premium deck presentation",
                    typeof(RectTransform)
                )
                .GetComponent<
                    RectTransform
                >();

            root.SetParent(
                parent,
                false
            );

            Stretch(root);

            /*
             * Keep the ownership display above
             * the arena but below interactive HUD.
             */
            root.SetAsLastSibling();

            BuildPlayerStack();
            BuildRivalStack();
            BuildPot();
        }

        void BuildPlayerStack()
        {
            playerPanel =
                CreatePanel(
                    "Player deck panel",
                    root,
                    PlayerColour
                );

            playerPanel.surfaceOpacity =
                panelOpacity;

            playerPanel.openFrame =
                true;

            playerStack =
                CreateRect(
                    "Player deck cards",
                    playerPanel.rectTransform
                );

            playerLabel =
                CreateLabel(
                    "Player deck label",
                    playerPanel.rectTransform,
                    "YOU  •  6",
                    10,
                    PlayerColour
                );

            BuildMiniCards(
                playerStack,
                playerCards,
                PlayerColour,
                false
            );
        }

        void BuildRivalStack()
        {
            rivalPanel =
                CreatePanel(
                    "Rival deck panel",
                    root,
                    RivalColour
                );

            rivalPanel.surfaceOpacity =
                panelOpacity;

            rivalPanel.openFrame =
                true;

            rivalStack =
                CreateRect(
                    "Rival deck cards",
                    rivalPanel.rectTransform
                );

            rivalLabel =
                CreateLabel(
                    "Rival deck label",
                    rivalPanel.rectTransform,
                    "RIVAL  •  6",
                    10,
                    RivalColour
                );

            BuildMiniCards(
                rivalStack,
                rivalCards,
                RivalColour,
                true
            );
        }

        void BuildPot()
        {
            potPanel =
                CreatePanel(
                    "Draw pot panel",
                    root,
                    PotColour
                );

            potPanel.surfaceOpacity =
                0.26f;

            potPanel.openFrame =
                true;

            potRoot =
                potPanel.rectTransform;

            potLabel =
                CreateLabel(
                    "Pot label",
                    potRoot,
                    "POT 0",
                    9,
                    Muted
                );

            potLabel.alignment =
                TextAnchor.MiddleCenter;
        }

        void BuildMiniCards(
            RectTransform stack,
            List<RectTransform> list,
            Color accent,
            bool reverse
        )
        {
            list.Clear();

            /*
             * Maximum ownership is 12,
             * so create all 12 once and just
             * show/hide them as ownership changes.
             */
            for (int i = 0; i < 12; i++)
            {
                RectTransform card =
                    CreateRect(
                        "Card " + (i + 1),
                        stack
                    );

                BattleHudGraphic frame =
                    card.gameObject
                        .AddComponent<
                            BattleHudGraphic
                        >();

                frame.shape =
                    BattleHudGraphic.Shape.Panel;

                frame.accent =
                    accent;

                frame.surfaceOpacity =
                    0.48f;

                frame.openFrame =
                    true;

                /*
                 * Inner energy line.
                 */
                RectTransform energy =
                    CreateRect(
                        "Energy",
                        card
                    );

                BattleHudGraphic line =
                    energy.gameObject
                        .AddComponent<
                            BattleHudGraphic
                        >();

                line.shape =
                    BattleHudGraphic.Shape.Bar;

                line.accent =
                    accent;

                Battle3DController.Place(
                    energy,
                    5,
                    5,
                    12,
                    2
                );

                /*
                 * Slight alternating rotations make
                 * the stack feel physical rather than
                 * like six checkbox icons.
                 */
                float rotation =
                    reverse
                        ? 2.5f
                        : -2.5f;

                rotation +=
                    (i % 3 - 1) *
                    1.1f;

                card.localRotation =
                    Quaternion.Euler(
                        0,
                        0,
                        rotation
                    );

                list.Add(card);
            }
        }

        void MatchStarted()
        {
            SetVisible(true);

            RefreshCounts(
                match.PlayerCardCount,
                match.RivalCardCount,
                match.PotCount
            );

            PulseBothStacks();
        }

        void HandSelectionOpened()
        {
            SetVisible(false);
        }

        void CountsChanged(
            int player,
            int rival,
            int pot
        )
        {
            RefreshCounts(
                player,
                rival,
                pot
            );
        }

        void RoundPrepared(
            CardData player,
            CardData rival,
            BattleTurnOwner owner
        )
        {
            lastPlayerCard =
                player;

            lastRivalCard =
                rival;

            if (GameSettings.ReducedMotion)
                return;

            PulseBothStacks();
        }

        void RoundSettled(
            BattleResult result,
            int collected
        )
        {
            if (result.Winner ==
                BattleWinner.Draw)
            {
                AnimateDrawToPot();

                PulsePot();

                return;
            }

            if (result.Winner ==
                BattleWinner.Player)
            {
                AnimateCapture(
                    lastRivalCard,
                    match.cardDisplay != null
                        ? match.cardDisplay.rivalRoot
                        : null,
                    playerPanel.rectTransform,
                    PlayerColour
                );

                PulsePlayer();
            }
            else
            {
                AnimateCapture(
                    lastPlayerCard,
                    match.cardDisplay != null
                        ? match.cardDisplay.playerRoot
                        : null,
                    rivalPanel.rectTransform,
                    RivalColour
                );

                PulseRival();
            }
        }

        void MatchEnded(
            BattleMatchWinner winner
        )
        {
            RefreshCounts(
                match.PlayerCardCount,
                match.RivalCardCount,
                match.PotCount
            );

            if (winner ==
                BattleMatchWinner.Player)
            {
                PulsePlayer();
            }
            else if (winner ==
                     BattleMatchWinner.Rival)
            {
                PulseRival();
            }
        }

        void RefreshCounts(
            int player,
            int rival,
            int pot
        )
        {
            player =
                Mathf.Clamp(
                    player,
                    0,
                    12
                );

            rival =
                Mathf.Clamp(
                    rival,
                    0,
                    12
                );

            pot =
                Mathf.Clamp(
                    pot,
                    0,
                    12
                );

            playerLabel.text =
                "YOU  •  " +
                player;

            rivalLabel.text =
                "RIVAL  •  " +
                rival;

            potLabel.text =
                "POT " +
                pot;

            for (
                int i = 0;
                i < playerCards.Count;
                i++
            )
            {
                playerCards[i]
                    .gameObject
                    .SetActive(
                        i < player
                    );
            }

            for (
                int i = 0;
                i < rivalCards.Count;
                i++
            )
            {
                rivalCards[i]
                    .gameObject
                    .SetActive(
                        i < rival
                    );
            }

            potPanel.gameObject
                .SetActive(
                    pot > 0
                );
        }

        void AnimateCapture(
            CardData card,
            Transform source,
            RectTransform target,
            Color colour
        )
        {
            if (GameSettings.ReducedMotion ||
                card == null ||
                card.originalCardArt == null)
            {
                return;
            }

            StartCoroutine(
                FlyCard(
                    card,
                    source,
                    target,
                    colour
                )
            );
        }

        void AnimateDrawToPot()
        {
            if (GameSettings.ReducedMotion)
                return;

            if (lastPlayerCard != null &&
                lastPlayerCard.originalCardArt != null)
            {
                StartCoroutine(
                    FlyCard(
                        lastPlayerCard,
                        match.cardDisplay != null
                            ? match.cardDisplay.playerRoot
                            : null,
                        potRoot,
                        PotColour
                    )
                );
            }

            if (lastRivalCard != null &&
                lastRivalCard.originalCardArt != null)
            {
                StartCoroutine(
                    FlyCard(
                        lastRivalCard,
                        match.cardDisplay != null
                            ? match.cardDisplay.rivalRoot
                            : null,
                        potRoot,
                        PotColour
                    )
                );
            }
        }

        IEnumerator FlyCard(
            CardData card,
            Transform source,
            RectTransform target,
            Color colour
        )
        {
            RectTransform ghost =
                CreateRect(
                    "Captured card",
                    root
                );

            ghost.SetAsLastSibling();

            ghost.sizeDelta =
                new Vector2(
                    48,
                    70
                );

            BattleHudGraphic glow =
                ghost.gameObject
                    .AddComponent<
                        BattleHudGraphic
                    >();

            glow.shape =
                BattleHudGraphic.Shape.Panel;

            glow.accent =
                colour;

            glow.surfaceOpacity =
                0.55f;

            glow.openFrame =
                true;

            RectTransform artRect =
                CreateRect(
                    "Artwork",
                    ghost
                );

            artRect.anchorMin =
                new Vector2(
                    0.08f,
                    0.08f
                );

            artRect.anchorMax =
                new Vector2(
                    0.92f,
                    0.92f
                );

            artRect.offsetMin =
                Vector2.zero;

            artRect.offsetMax =
                Vector2.zero;

            RawImage image =
                artRect.gameObject
                    .AddComponent<
                        RawImage
                    >();

            image.texture =
                card.originalCardArt;

            image.raycastTarget =
                false;

            CanvasGroup group =
                ghost.gameObject
                    .AddComponent<
                        CanvasGroup
                    >();

            group.blocksRaycasts =
                false;

            group.interactable =
                false;

            Vector2 from =
                GetSourcePosition(
                    source
                );

            Vector2 to =
                GetTargetPosition(
                    target
                );

            ghost.anchoredPosition =
                from;

            ghost.localScale =
                Vector3.one;

            float duration =
                Mathf.Max(
                    0.01f,
                    captureDuration
                );

            /*
             * Arc away from the centre before
             * snapping into the winning deck.
             */
            Vector2 middle =
                Vector2.Lerp(
                    from,
                    to,
                    0.5f
                );

            middle.y +=
                68f;

            float elapsed =
                0f;

            while (elapsed <
                   duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                float eased =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t
                    );

                Vector2 a =
                    Vector2.Lerp(
                        from,
                        middle,
                        eased
                    );

                Vector2 b =
                    Vector2.Lerp(
                        middle,
                        to,
                        eased
                    );

                ghost.anchoredPosition =
                    Vector2.Lerp(
                        a,
                        b,
                        eased
                    );

                float spin =
                    Mathf.Sin(
                        t *
                        Mathf.PI
                    ) *
                    8f;

                ghost.localRotation =
                    Quaternion.Euler(
                        0,
                        0,
                        spin
                    );

                float scale =
                    Mathf.Lerp(
                        1f,
                        0.42f,
                        eased
                    );

                ghost.localScale =
                    Vector3.one *
                    scale;

                /*
                 * Keep it solid for most of the trip,
                 * then dissolve into the stack.
                 */
                group.alpha =
                    t < 0.72f
                        ? 1f
                        : Mathf.InverseLerp(
                            1f,
                            0.72f,
                            t
                        );

                glow.Animate(
                    Mathf.Sin(
                        t *
                        Mathf.PI
                    )
                );

                yield return null;
            }

            Destroy(
                ghost.gameObject
            );
        }

        Vector2 GetSourcePosition(
            Transform source
        )
        {
            if (source == null ||
                layout.battleCamera == null)
            {
                return Vector2.zero;
            }

            Vector3 screen =
                layout.battleCamera
                    .WorldToScreenPoint(
                        source.position
                    );

            Camera uiCamera =
                canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    root,
                    screen,
                    uiCamera,
                    out Vector2 local
                );

            return local;
        }

        Vector2 GetTargetPosition(
            RectTransform target
        )
        {
            Camera uiCamera =
                canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

            Vector3 world =
                target.TransformPoint(
                    target.rect.center
                );

            Vector2 screen =
                RectTransformUtility
                    .WorldToScreenPoint(
                        uiCamera,
                        world
                    );

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    root,
                    screen,
                    uiCamera,
                    out Vector2 local
                );

            return local;
        }

        void PulsePlayer()
        {
            if (playerPulse != null)
            {
                StopCoroutine(
                    playerPulse
                );
            }

            playerPulse =
                StartCoroutine(
                    Pulse(
                        playerPanel,
                        PlayerColour
                    )
                );
        }

        void PulseRival()
        {
            if (rivalPulse != null)
            {
                StopCoroutine(
                    rivalPulse
                );
            }

            rivalPulse =
                StartCoroutine(
                    Pulse(
                        rivalPanel,
                        RivalColour
                    )
                );
        }

        void PulsePot()
        {
            if (!potPanel.gameObject.activeSelf)
                return;

            if (potPulse != null)
            {
                StopCoroutine(
                    potPulse
                );
            }

            potPulse =
                StartCoroutine(
                    Pulse(
                        potPanel,
                        PotColour
                    )
                );
        }

        void PulseBothStacks()
        {
            PulsePlayer();
            PulseRival();
        }

        IEnumerator Pulse(
            BattleHudGraphic graphic,
            Color colour
        )
        {
            if (graphic == null)
                yield break;

            RectTransform rect =
                graphic.rectTransform;

            Vector3 originalScale =
                Vector3.one;

            float duration =
                Mathf.Max(
                    0.01f,
                    stackPulseDuration
                );

            float elapsed =
                0f;

            while (elapsed <
                   duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                float pulse =
                    Mathf.Sin(
                        t *
                        Mathf.PI
                    );

                rect.localScale =
                    originalScale *
                    (
                        1f +
                        pulse *
                        0.055f
                    );

                graphic.Animate(
                    0.15f +
                    pulse *
                    0.7f
                );

                graphic.Tint(
                    colour
                );

                yield return null;
            }

            rect.localScale =
                originalScale;

            graphic.Animate(
                0.15f
            );
        }

        void SetVisible(
            bool visible
        )
        {
            if (root != null)
            {
                root.gameObject
                    .SetActive(
                        visible
                    );
            }
        }

        void LateUpdate()
        {
            if (!built ||
                root == null ||
                !root.gameObject.activeSelf)
            {
                return;
            }

            Layout();
        }

        void Layout()
        {
            float width =
                root.rect.width;

            float height =
                root.rect.height;

            if (width <= 0f ||
                height <= 0f)
            {
                return;
            }

            const float panelWidth =
                118f;

            const float panelHeight =
                48f;

            float y =
                height -
                126f;

            Battle3DController.Place(
                playerPanel.rectTransform,
                14f,
                y,
                panelWidth,
                panelHeight
            );

            Battle3DController.Place(
                rivalPanel.rectTransform,
                width -
                14f -
                panelWidth,
                y,
                panelWidth,
                panelHeight
            );

            Battle3DController.Place(
                potRoot,
                width *
                0.5f -
                31f,
                y +
                4f,
                62f,
                34f
            );

            Battle3DController.Place(
                playerStack,
                8f,
                14f,
                102f,
                30f
            );

            Battle3DController.Place(
                rivalStack,
                8f,
                14f,
                102f,
                30f
            );

            Battle3DController.Place(
                playerLabel.rectTransform,
                8f,
                2f,
                102f,
                13f
            );

            Battle3DController.Place(
                rivalLabel.rectTransform,
                8f,
                2f,
                102f,
                13f
            );

            playerLabel.alignment =
                TextAnchor.MiddleLeft;

            rivalLabel.alignment =
                TextAnchor.MiddleRight;

            Battle3DController.Place(
                potLabel.rectTransform,
                0f,
                0f,
                62f,
                34f
            );

            /*
             * Stack cards overlap, like real cards
             * being held together rather than six
             * independent square UI boxes.
             */
            for (
                int i = 0;
                i < playerCards.Count;
                i++
            )
            {
                Battle3DController.Place(
                    playerCards[i],
                    5f +
                    i *
                    7.2f,
                    2f +
                    (i % 2) *
                    1.5f,
                    22f,
                    28f
                );
            }

            for (
                int i = 0;
                i < rivalCards.Count;
                i++
            )
            {
                Battle3DController.Place(
                    rivalCards[i],
                    75f -
                    i *
                    7.2f,
                    2f +
                    (i % 2) *
                    1.5f,
                    22f,
                    28f
                );
            }
        }

        static BattleHudGraphic CreatePanel(
            string objectName,
            RectTransform parent,
            Color colour
        )
        {
            RectTransform rect =
                CreateRect(
                    objectName,
                    parent
                );

            BattleHudGraphic graphic =
                rect.gameObject
                    .AddComponent<
                        BattleHudGraphic
                    >();

            graphic.shape =
                BattleHudGraphic.Shape.Panel;

            graphic.accent =
                colour;

            return graphic;
        }

        Text CreateLabel(
            string objectName,
            RectTransform parent,
            string value,
            int size,
            Color colour
        )
        {
            RectTransform rect =
                CreateRect(
                    objectName,
                    parent
                );

            Text text =
                rect.gameObject
                    .AddComponent<
                        Text
                    >();

            text.font =
                font;

            text.text =
                value;

            text.fontSize =
                size;

            text.fontStyle =
                FontStyle.Bold;

            text.color =
                colour;

            text.alignment =
                TextAnchor.MiddleLeft;

            text.raycastTarget =
                false;

            text.horizontalOverflow =
                HorizontalWrapMode.Overflow;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            return text;
        }

        static RectTransform CreateRect(
            string objectName,
            RectTransform parent
        )
        {
            RectTransform rect =
                new GameObject(
                    objectName,
                    typeof(RectTransform)
                )
                .GetComponent<
                    RectTransform
                >();

            rect.SetParent(
                parent,
                false
            );

            return rect;
        }

        static void Stretch(
            RectTransform rect
        )
        {
            rect.anchorMin =
                Vector2.zero;

            rect.anchorMax =
                Vector2.one;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;
        }
    }
}
