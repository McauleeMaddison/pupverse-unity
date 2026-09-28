using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pupverse
{
    /*
     * Adds premium physical presentation to the
     * existing 3D battle cards.
     *
     * Presentation only.
     *
     * Does NOT change:
     * - stats
     * - battle rules
     * - turns
     * - captures
     * - deck ownership
     */
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleMatchController))]
    public sealed class BattlePremiumCardPresentation : MonoBehaviour
    {
        [Header("Automatic References")]
        [SerializeField]
        BattleMatchController match;

        [SerializeField]
        BattleCardDisplay cardDisplay;

        [Header("Physical Card")]
        [Range(0.02f, 0.12f)]
        public float frameWidth = 0.055f;

        [Range(0.01f, 0.10f)]
        public float frameDepth = 0.035f;

        [Range(0.01f, 0.08f)]
        public float backDepth = 0.028f;

        [Header("Materials")]
        [Range(0f, 1f)]
        public float bodyMetallic = 0.82f;

        [Range(0f, 1f)]
        public float bodySmoothness = 0.88f;

        [Range(0f, 1f)]
        public float frameMetallic = 0.92f;

        [Range(0f, 1f)]
        public float frameSmoothness = 0.94f;

        [Header("Rarity Glow")]
        [Range(0f, 5f)]
        public float normalGlow = 1.15f;

        [Range(0f, 7f)]
        public float legendaryGlow = 2.1f;

        [Range(0f, 8f)]
        public float mythicGlow = 2.65f;

        CardShell playerShell;
        CardShell rivalShell;

        bool subscribed;
        bool built;

        Shader standardShader;

        sealed class CardShell
        {
            public Transform root;

            public Renderer body;
            public Renderer front;

            public GameObject generatedRoot;

            public Material bodyMaterial;
            public Material backMaterial;

            public readonly List<Material> frameMaterials =
                new List<Material>();

            public CardRarity rarity;

            public Color rarityColour;

            public float glowStrength;
        }

        static readonly Color CommonColour =
            new Color(
                0.72f,
                0.78f,
                0.88f
            );

        static readonly Color UncommonColour =
            new Color(
                0.20f,
                1.00f,
                0.62f
            );

        static readonly Color RareColour =
            new Color(
                0.10f,
                0.62f,
                1.00f
            );

        static readonly Color EpicColour =
            new Color(
                0.68f,
                0.25f,
                1.00f
            );

        static readonly Color LegendaryColour =
            new Color(
                1.00f,
                0.69f,
                0.12f
            );

        static readonly Color MythicColour =
            new Color(
                1.00f,
                0.18f,
                0.48f
            );

        static readonly Color MysticColour =
            new Color(
                0.25f,
                0.95f,
                1.00f
            );

        static readonly Color DarkBodyColour =
            new Color(
                0.018f,
                0.025f,
                0.055f
            );

        static readonly Color DarkBackColour =
            new Color(
                0.010f,
                0.015f,
                0.032f
            );

        void Awake()
        {
            if (match == null)
            {
                match =
                    GetComponent<
                        BattleMatchController
                    >();
            }

            if (cardDisplay == null)
            {
                cardDisplay =
                    GetComponent<
                        BattleCardDisplay
                    >();
            }

            standardShader =
                Shader.Find(
                    "Standard"
                );
        }

        void Start()
        {
            if (match == null ||
                cardDisplay == null)
            {
                Debug.LogError(
                    "BattlePremiumCardPresentation requires BattleMatchController and BattleCardDisplay.",
                    this
                );

                enabled = false;
                return;
            }

            if (standardShader == null)
            {
                Debug.LogError(
                    "Standard shader could not be found.",
                    this
                );

                enabled = false;
                return;
            }

            BuildCards();

            Subscribe();

            ApplyCurrentCards();

            built = true;
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
        }

        void OnDestroy()
        {
            CleanupShell(
                playerShell
            );

            CleanupShell(
                rivalShell
            );
        }

        void Subscribe()
        {
            if (subscribed ||
                match == null)
            {
                return;
            }

            /*
             * CardCountsChanged fires immediately after
             * each new pair has been drawn but BEFORE the
             * existing deal/reveal animation.
             *
             * Perfect moment to update rarity visuals.
             */
            match.CardCountsChanged +=
                OnCardCountsChanged;

            match.RoundPrepared +=
                OnRoundPrepared;

            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed ||
                match == null)
            {
                return;
            }

            match.CardCountsChanged -=
                OnCardCountsChanged;

            match.RoundPrepared -=
                OnRoundPrepared;

            subscribed = false;
        }

        void OnCardCountsChanged(
            int player,
            int rival,
            int pot
        )
        {
            ApplyCurrentCards();
        }

        void OnRoundPrepared(
            CardData player,
            CardData rival,
            BattleTurnOwner owner
        )
        {
            ApplyCard(
                playerShell,
                player
            );

            ApplyCard(
                rivalShell,
                rival
            );
        }

        void ApplyCurrentCards()
        {
            if (match == null)
                return;

            ApplyCard(
                playerShell,
                match.PlayerActiveCard
            );

            ApplyCard(
                rivalShell,
                match.RivalActiveCard
            );
        }

        void BuildCards()
        {
            playerShell =
                BuildShell(
                    cardDisplay.playerRoot,
                    cardDisplay.playerFront,
                    "PlayerCard"
                );

            rivalShell =
                BuildShell(
                    cardDisplay.rivalRoot,
                    cardDisplay.rivalFront,
                    "RivalCard"
                );
        }

        CardShell BuildShell(
            Transform root,
            Renderer front,
            string bodyName
        )
        {
            if (root == null ||
                front == null)
            {
                Debug.LogError(
                    "Premium card shell could not find card root/front.",
                    this
                );

                return null;
            }

            Transform bodyTransform =
                root.Find(
                    bodyName
                );

            if (bodyTransform == null)
            {
                Debug.LogError(
                    bodyName +
                    " body was not found below " +
                    root.name,
                    this
                );

                return null;
            }

            Renderer body =
                bodyTransform
                    .GetComponent<Renderer>();

            if (body == null)
            {
                Debug.LogError(
                    bodyName +
                    " has no Renderer.",
                    this
                );

                return null;
            }

            CardShell shell =
                new CardShell
                {
                    root = root,
                    body = body,
                    front = front
                };

            StyleBody(
                shell
            );

            BuildPhysicalShell(
                shell
            );

            return shell;
        }

        void StyleBody(
            CardShell shell
        )
        {
            Material material =
                CreateMaterial(
                    DarkBodyColour,
                    bodyMetallic,
                    bodySmoothness,
                    Color.black
                );

            shell.bodyMaterial =
                material;

            shell.body.sharedMaterial =
                material;
        }

        void BuildPhysicalShell(
            CardShell shell
        )
        {
            GameObject generated =
                new GameObject(
                    "Premium Card Shell"
                );

            generated.transform.SetParent(
                shell.root,
                false
            );

            shell.generatedRoot =
                generated;

            Transform frontTransform =
                shell.front.transform;

            Vector3 frontPosition =
                frontTransform.localPosition;

            Vector3 frontScale =
                frontTransform.localScale;

            float width =
                Mathf.Abs(
                    frontScale.x
                );

            float height =
                Mathf.Abs(
                    frontScale.y
                );

            /*
             * Existing card fronts sit on the side
             * facing the battle camera.
             */
            float frontZ =
                frontPosition.z -
                0.018f;

            /*
             * Metallic outer frame.
             */
            CreateFramePiece(
                shell,
                "Frame Top",
                new Vector3(
                    frontPosition.x,
                    frontPosition.y +
                    height * 0.5f +
                    frameWidth * 0.5f,
                    frontZ
                ),
                new Vector3(
                    width +
                    frameWidth * 2f,
                    frameWidth,
                    frameDepth
                )
            );

            CreateFramePiece(
                shell,
                "Frame Bottom",
                new Vector3(
                    frontPosition.x,
                    frontPosition.y -
                    height * 0.5f -
                    frameWidth * 0.5f,
                    frontZ
                ),
                new Vector3(
                    width +
                    frameWidth * 2f,
                    frameWidth,
                    frameDepth
                )
            );

            CreateFramePiece(
                shell,
                "Frame Left",
                new Vector3(
                    frontPosition.x -
                    width * 0.5f -
                    frameWidth * 0.5f,
                    frontPosition.y,
                    frontZ
                ),
                new Vector3(
                    frameWidth,
                    height,
                    frameDepth
                )
            );

            CreateFramePiece(
                shell,
                "Frame Right",
                new Vector3(
                    frontPosition.x +
                    width * 0.5f +
                    frameWidth * 0.5f,
                    frontPosition.y,
                    frontZ
                ),
                new Vector3(
                    frameWidth,
                    height,
                    frameDepth
                )
            );

            /*
             * Small corner blocks give the frame more
             * physical depth and stop it reading as
             * four disconnected bars.
             */
            float cornerSize =
                frameWidth * 1.45f;

            CreateFramePiece(
                shell,
                "Corner TL",
                new Vector3(
                    frontPosition.x -
                    width * 0.5f,
                    frontPosition.y +
                    height * 0.5f,
                    frontZ -
                    0.008f
                ),
                new Vector3(
                    cornerSize,
                    cornerSize,
                    frameDepth * 1.35f
                )
            );

            CreateFramePiece(
                shell,
                "Corner TR",
                new Vector3(
                    frontPosition.x +
                    width * 0.5f,
                    frontPosition.y +
                    height * 0.5f,
                    frontZ -
                    0.008f
                ),
                new Vector3(
                    cornerSize,
                    cornerSize,
                    frameDepth * 1.35f
                )
            );

            CreateFramePiece(
                shell,
                "Corner BL",
                new Vector3(
                    frontPosition.x -
                    width * 0.5f,
                    frontPosition.y -
                    height * 0.5f,
                    frontZ -
                    0.008f
                ),
                new Vector3(
                    cornerSize,
                    cornerSize,
                    frameDepth * 1.35f
                )
            );

            CreateFramePiece(
                shell,
                "Corner BR",
                new Vector3(
                    frontPosition.x +
                    width * 0.5f,
                    frontPosition.y -
                    height * 0.5f,
                    frontZ -
                    0.008f
                ),
                new Vector3(
                    cornerSize,
                    cornerSize,
                    frameDepth * 1.35f
                )
            );

            /*
             * Dark metallic rear plate.
             *
             * The existing PlayerCard/RivalCard cube
             * provides the thickness. This plate makes
             * the rear feel intentionally designed.
             */
            Vector3 bodyPosition =
                shell.body.transform
                    .localPosition;

            Vector3 bodyScale =
                shell.body.transform
                    .localScale;

            float rearZ =
                bodyPosition.z +
                Mathf.Abs(
                    bodyScale.z
                ) *
                0.5f +
                backDepth *
                0.5f +
                0.004f;

            GameObject back =
                CreateCube(
                    "Premium Card Back",
                    shell.generatedRoot.transform,
                    new Vector3(
                        bodyPosition.x,
                        bodyPosition.y,
                        rearZ
                    ),
                    new Vector3(
                        width * 0.94f,
                        height * 0.94f,
                        backDepth
                    )
                );

            Material backMaterial =
                CreateMaterial(
                    DarkBackColour,
                    0.88f,
                    0.90f,
                    new Color(
                        0.01f,
                        0.025f,
                        0.05f
                    )
                );

            shell.backMaterial =
                backMaterial;

            back.GetComponent<Renderer>()
                .sharedMaterial =
                backMaterial;

            /*
             * Rear energy spine.
             */
            GameObject spine =
                CreateCube(
                    "Rear Energy Spine",
                    shell.generatedRoot.transform,
                    new Vector3(
                        bodyPosition.x,
                        bodyPosition.y,
                        rearZ +
                        backDepth *
                        0.6f
                    ),
                    new Vector3(
                        width * 0.10f,
                        height * 0.72f,
                        backDepth * 0.30f
                    )
                );

            Material spineMaterial =
                CreateFrameMaterial();

            shell.frameMaterials.Add(
                spineMaterial
            );

            spine.GetComponent<Renderer>()
                .sharedMaterial =
                spineMaterial;
        }

        void CreateFramePiece(
            CardShell shell,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale
        )
        {
            GameObject piece =
                CreateCube(
                    objectName,
                    shell.generatedRoot.transform,
                    localPosition,
                    localScale
                );

            Material material =
                CreateFrameMaterial();

            shell.frameMaterials.Add(
                material
            );

            piece.GetComponent<Renderer>()
                .sharedMaterial =
                material;
        }

        GameObject CreateCube(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale
        )
        {
            GameObject cube =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            cube.name =
                objectName;

            cube.transform.SetParent(
                parent,
                false
            );

            cube.transform.localPosition =
                localPosition;

            cube.transform.localRotation =
                Quaternion.identity;

            cube.transform.localScale =
                localScale;

            Collider collider =
                cube.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(
                    collider
                );
            }

            Renderer renderer =
                cube.GetComponent<Renderer>();

            renderer.shadowCastingMode =
                ShadowCastingMode.Off;

            renderer.receiveShadows =
                false;

            return cube;
        }

        Material CreateFrameMaterial()
        {
            return CreateMaterial(
                CommonColour,
                frameMetallic,
                frameSmoothness,
                CommonColour *
                normalGlow
            );
        }

        Material CreateMaterial(
            Color baseColour,
            float metallic,
            float smoothness,
            Color emission
        )
        {
            Material material =
                new Material(
                    standardShader
                );

            material.name =
                "PupVerse Premium Card Runtime";

            if (material.HasProperty(
                    "_Color"
                ))
            {
                material.SetColor(
                    "_Color",
                    baseColour
                );
            }

            if (material.HasProperty(
                    "_Metallic"
                ))
            {
                material.SetFloat(
                    "_Metallic",
                    metallic
                );
            }

            if (material.HasProperty(
                    "_Glossiness"
                ))
            {
                material.SetFloat(
                    "_Glossiness",
                    smoothness
                );
            }

            if (material.HasProperty(
                    "_EmissionColor"
                ))
            {
                material.EnableKeyword(
                    "_EMISSION"
                );

                material.SetColor(
                    "_EmissionColor",
                    emission
                );
            }

            return material;
        }

        void ApplyCard(
            CardShell shell,
            CardData card
        )
        {
            if (shell == null ||
                card == null)
            {
                return;
            }

            shell.rarity =
                card.rarity;

            shell.rarityColour =
                ColourForRarity(
                    card.rarity
                );

            shell.glowStrength =
                GlowForRarity(
                    card.rarity
                );

            UpdateShellMaterials(
                shell,
                shell.rarityColour,
                shell.glowStrength
            );
        }

        void UpdateShellMaterials(
            CardShell shell,
            Color colour,
            float glow
        )
        {
            if (shell == null)
                return;

            /*
             * Keep body mostly black metal, with just
             * enough rarity reflection to tie the shell
             * to the card.
             */
            if (shell.bodyMaterial != null)
            {
                Color bodyTint =
                    Color.Lerp(
                        DarkBodyColour,
                        colour,
                        0.075f
                    );

                SetMaterialColour(
                    shell.bodyMaterial,
                    bodyTint,
                    colour *
                    glow *
                    0.045f
                );
            }

            if (shell.backMaterial != null)
            {
                Color backTint =
                    Color.Lerp(
                        DarkBackColour,
                        colour,
                        0.055f
                    );

                SetMaterialColour(
                    shell.backMaterial,
                    backTint,
                    colour *
                    glow *
                    0.035f
                );
            }

            foreach (
                Material material
                in shell.frameMaterials
            )
            {
                if (material == null)
                    continue;

                Color metalColour =
                    Color.Lerp(
                        new Color(
                            0.42f,
                            0.48f,
                            0.58f
                        ),
                        colour,
                        0.62f
                    );

                SetMaterialColour(
                    material,
                    metalColour,
                    colour *
                    glow
                );
            }
        }

        static void SetMaterialColour(
            Material material,
            Color colour,
            Color emission
        )
        {
            if (material == null)
                return;

            if (material.HasProperty(
                    "_Color"
                ))
            {
                material.SetColor(
                    "_Color",
                    colour
                );
            }

            if (material.HasProperty(
                    "_EmissionColor"
                ))
            {
                material.EnableKeyword(
                    "_EMISSION"
                );

                material.SetColor(
                    "_EmissionColor",
                    emission
                );
            }
        }

        void Update()
        {
            if (!built ||
                GameSettings.ReducedMotion)
            {
                return;
            }

            AnimateRarity(
                playerShell,
                0f
            );

            AnimateRarity(
                rivalShell,
                1.7f
            );
        }

        void AnimateRarity(
            CardShell shell,
            float offset
        )
        {
            if (shell == null)
                return;

            /*
             * Common through Legendary remain mostly
             * restrained. Mythic/Mystic get premium
             * living-energy treatment.
             */
            if (shell.rarity !=
                    CardRarity.Mythic &&
                shell.rarity !=
                    CardRarity.Mystic)
            {
                return;
            }

            float pulse =
                0.78f +
                Mathf.Sin(
                    Time.unscaledTime *
                    2.4f +
                    offset
                ) *
                0.22f;

            Color animatedColour =
                shell.rarityColour;

            /*
             * Mystic shifts gently between cyan,
             * violet and pink instead of one static
             * colour.
             */
            if (shell.rarity ==
                CardRarity.Mystic)
            {
                float hue =
                    Mathf.Repeat(
                        Time.unscaledTime *
                        0.055f +
                        offset *
                        0.02f,
                        1f
                    );

                animatedColour =
                    Color.HSVToRGB(
                        hue,
                        0.72f,
                        1f
                    );
            }

            UpdateShellMaterials(
                shell,
                animatedColour,
                shell.glowStrength *
                pulse
            );
        }

        float GlowForRarity(
            CardRarity rarity
        )
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return normalGlow *
                        0.55f;

                case CardRarity.Uncommon:
                    return normalGlow *
                        0.75f;

                case CardRarity.Rare:
                    return normalGlow;

                case CardRarity.Epic:
                    return normalGlow *
                        1.25f;

                case CardRarity.Legendary:
                    return legendaryGlow;

                case CardRarity.Mythic:
                    return mythicGlow;

                case CardRarity.Mystic:
                    return mythicGlow *
                        1.12f;

                default:
                    return normalGlow;
            }
        }

        static Color ColourForRarity(
            CardRarity rarity
        )
        {
            switch (rarity)
            {
                case CardRarity.Common:
                    return CommonColour;

                case CardRarity.Uncommon:
                    return UncommonColour;

                case CardRarity.Rare:
                    return RareColour;

                case CardRarity.Epic:
                    return EpicColour;

                case CardRarity.Legendary:
                    return LegendaryColour;

                case CardRarity.Mythic:
                    return MythicColour;

                case CardRarity.Mystic:
                    return MysticColour;

                default:
                    return CommonColour;
            }
        }

        void CleanupShell(
            CardShell shell
        )
        {
            if (shell == null)
                return;

            if (shell.generatedRoot != null)
            {
                Destroy(
                    shell.generatedRoot
                );
            }

            if (shell.bodyMaterial != null)
            {
                Destroy(
                    shell.bodyMaterial
                );
            }

            if (shell.backMaterial != null)
            {
                Destroy(
                    shell.backMaterial
                );
            }

            foreach (
                Material material
                in shell.frameMaterials
            )
            {
                if (material != null)
                {
                    Destroy(
                        material
                    );
                }
            }

            shell.frameMaterials.Clear();
        }
    }
}
