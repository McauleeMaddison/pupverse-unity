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

        readonly List<TransferVisual> transfers = new List<TransferVisual>(3);
        int activeTransfers;
        bool delayCounts;
        public bool IsTransferring => activeTransfers > 0;
        public int ActiveTransferCount => activeTransfers;
        public Vector2 PlayerTransferOrigin => GetSourcePosition(match.cardDisplay != null ? match.cardDisplay.playerFront : null);
        public Vector2 RivalTransferOrigin => GetSourcePosition(match.cardDisplay != null ? match.cardDisplay.rivalFront : null);

        sealed class TransferVisual
        {
            public RectTransform rect;
            public RawImage art;
            public Text label;
            public CanvasGroup group;
            public BattleHudGraphic glow;
        }

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
                    Object.FindAnyObjectByType<
                        BattleMatchController
                    >();
            }

            if (layout == null)
            {
                layout =
                    Object.FindAnyObjectByType<
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
            if (!built) return;
            Subscribe();
            lastPlayerCard=match.PlayerActiveCard;
            lastRivalCard=match.RivalActiveCard;
            RefreshCounts(match.PlayerCardCount,match.RivalCardCount,match.PotCount);
            SetVisible(match.IsMatchRunning);
        }

        void OnDisable()
        {
            Unsubscribe();
            CancelTransfers();
            SetVisible(false);
        }

        void OnDestroy()
        {
            if(root!=null) Destroy(root.gameObject);
        }

        public void CancelTransfers()
        {
            StopAllCoroutines();
            activeTransfers=0; delayCounts=false;
            playerPulse=rivalPulse=potPulse=null;
            foreach(var item in transfers)
                if(item.rect!=null) item.rect.gameObject.SetActive(false);
            foreach(var panel in new[]{playerPanel,rivalPanel,potPanel})
                if(panel!=null) { panel.rectTransform.localScale=Vector3.one; panel.Animate(.15f); }
            if(built && root!=null && playerPanel!=null && rivalPanel!=null && potPanel!=null && match!=null)
                RefreshCounts(match.PlayerCardCount,match.RivalCardCount,match.PotCount);
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
            BuildTransfers();
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

                frame.raycastTarget=false;
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

                line.raycastTarget=false;
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
            CancelTransfers();
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
            CancelTransfers();
            SetVisible(false);
        }

        void CountsChanged(int player,int rival,int pot)
        {
            if(!delayCounts) RefreshCounts(player,rival,pot);
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

        void RoundSettled(BattleResult result,int collected)
        {
            if(!isActiveAndEnabled || !built) return;
            if(GameSettings.ReducedMotion)
            {
                delayCounts=false;
                RefreshCounts(match.PlayerCardCount,match.RivalCardCount,match.PotCount);
                return;
            }
            bool draw=result.Winner==BattleWinner.Draw;
            RectTransform target=draw?potRoot:result.Winner==BattleWinner.Player?playerPanel.rectTransform:rivalPanel.rectTransform;
            Color colour=draw?PotColour:result.Winner==BattleWinner.Player?PlayerColour:RivalColour;
            if(draw) potPanel.gameObject.SetActive(true);
            Layout();
            // Reserve every transfer before starting any coroutine, including the staggered card.
            int count=collected>2?3:2;
            activeTransfers=count;
            StartCoroutine(FlyCard(transfers[0],lastPlayerCard,PlayerTransferOrigin,target,colour,0,null));
            StartCoroutine(FlyCard(transfers[1],lastRivalCard,RivalTransferOrigin,target,colour,.09f,null));
            if(count==3)
                StartCoroutine(FlyCard(transfers[2],null,GetTargetPosition(potRoot),target,PotColour,.18f,"POT\n+"+(collected-2)));
        }

        void MatchEnded(BattleMatchWinner winner)
        {
            CancelTransfers();
            SetVisible(false);
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

        void BuildTransfers()
        {
            for(int i=0;i<3;i++)
            {
                var rect=CreateRect("Card transfer "+i,root);
                rect.sizeDelta=new Vector2(48,70);
                var glow=rect.gameObject.AddComponent<BattleHudGraphic>();
                glow.shape=BattleHudGraphic.Shape.Panel; glow.surfaceOpacity=.55f; glow.openFrame=true;
                glow.raycastTarget=false;
                var artRect=CreateRect("Artwork",rect);
                artRect.anchorMin=new Vector2(.08f,.08f); artRect.anchorMax=new Vector2(.92f,.92f);
                artRect.offsetMin=artRect.offsetMax=Vector2.zero;
                var art=artRect.gameObject.AddComponent<RawImage>(); art.raycastTarget=false;
                var label=CreateLabel("Pot cards",rect,"",12,PotColour);
                Stretch(label.rectTransform); label.alignment=TextAnchor.MiddleCenter;
                var group=rect.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts=group.interactable=false;
                transfers.Add(new TransferVisual {rect=rect,glow=glow,art=art,label=label,group=group});
                rect.gameObject.SetActive(false);
            }
        }

        IEnumerator FlyCard(TransferVisual item,CardData card,Vector2 from,RectTransform target,Color colour,float delay,string label)
        {
            if(delay>0) yield return new WaitForSecondsRealtime(delay);
            item.rect.gameObject.SetActive(true); item.rect.SetAsLastSibling();
            item.art.texture=card!=null?card.originalCardArt:null;
            item.art.enabled=item.art.texture!=null;
            item.label.text=label??""; item.glow.Tint(colour);
            item.group.alpha=1;
            float duration=Mathf.Max(.01f,captureDuration);
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                if(GameSettings.ReducedMotion) break;
                float t=Mathf.Clamp01(elapsed/duration), eased=Mathf.SmoothStep(0,1,t);
                Vector2 to=GetTargetPosition(target);
                Vector2 middle=Vector2.Lerp(from,to,.5f)+Vector2.up*68;
                item.rect.anchoredPosition=Vector2.Lerp(Vector2.Lerp(from,middle,eased),Vector2.Lerp(middle,to,eased),eased);
                item.rect.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI)*8);
                item.rect.localScale=Vector3.one*Mathf.Lerp(1,.42f,eased);
                item.group.alpha=t<.72f?1:Mathf.InverseLerp(1,.72f,t);
                item.glow.Animate(Mathf.Sin(t*Mathf.PI));
                yield return null;
            }
            item.rect.gameObject.SetActive(false);
            activeTransfers--;
            if(activeTransfers==0)
            {
                delayCounts=false;
                RefreshCounts(match.PlayerCardCount,match.RivalCardCount,match.PotCount);
                if(target==playerPanel.rectTransform) PulsePlayer();
                else if(target==rivalPanel.rectTransform) PulseRival();
                else PulsePot();
            }
        }

        Vector2 GetSourcePosition(Renderer source)
        {
            if(source==null || layout.battleCamera==null) return Vector2.zero;
            Vector3 screen=layout.battleCamera.WorldToScreenPoint(source.bounds.center);
            Camera uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,uiCamera,out Vector2 local);
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
            if (graphic == null || GameSettings.ReducedMotion)
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

            if(match==null || !match.isActiveAndEnabled) { CancelTransfers(); SetVisible(false); return; }
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

            graphic.raycastTarget=false;
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
