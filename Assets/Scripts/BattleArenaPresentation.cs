using System.Collections.Generic;
using UnityEngine;

namespace Pupverse
{
    /*
     * Premium arena presentation only.
     *
     * Does NOT calculate winners, change cards,
     * ownership, turns or battle rules.
     */
    [DisallowMultipleComponent]
    public sealed class BattleArenaPresentation : MonoBehaviour
    {
        [Header("Arena Look")]
        [Range(0f, 1f)]
        public float floorMetallic = 0.72f;

        [Range(0f, 1f)]
        public float floorSmoothness = 0.86f;

        [Range(0.02f, 0.35f)]
        public float gridOpacity = 0.11f;

        [Range(0f, 0.25f)]
        public float scanOpacity = 0.08f;

        [Header("Energy")]
        [Min(0f)]
        public float corePulseSpeed = 1.4f;

        [Min(0f)]
        public float scanSpeed = 0.16f;

        BattleController battle;

        Renderer arenaFloor;
        Renderer battleCore;
        Renderer coreRing;
        Renderer coreOuter;

        Renderer playerPlatform;
        Renderer rivalPlatform;

        readonly List<Renderer> playerNeon =
            new List<Renderer>();

        readonly List<Renderer> rivalNeon =
            new List<Renderer>();

        readonly Dictionary<Renderer, Material[]>
            originalMaterials =
                new Dictionary<Renderer, Material[]>();

        readonly List<Material>
            runtimeMaterials =
                new List<Material>();

        Light coreGlow;

        float originalCoreIntensity;

        Color originalCoreColour;

        GameObject gridObject;
        GameObject scanObject;

        Material gridMaterial;
        Material scanMaterial;

        Bounds floorBounds;

        Color phaseColour;
        float phaseBoost;
        float winnerBias;

        bool ready;

        static readonly Color PlayerCyan =
            new Color(
                0.08f,
                0.88f,
                1f
            );

        static readonly Color RivalViolet =
            new Color(
                0.64f,
                0.28f,
                1f
            );

        static readonly Color FloorColour =
            new Color(
                0.012f,
                0.022f,
                0.065f
            );

        void Start()
        {
            battle =
                Object.FindFirstObjectByType<
                    BattleController
                >();

            CacheSceneObjects();

            BuildRuntimeMaterials();

            BuildFloorGrid();

            Subscribe();

            phaseColour =
                PlayerCyan;

            ready = true;
        }

        void OnEnable()
        {
            if (ready)
            {
                Subscribe();
            }
        }

        void OnDisable()
        {
            Unsubscribe();

            RestoreMaterials();

            RestoreLight();

            DestroyGeneratedObjects();

            ready = false;
        }

        void Subscribe()
        {
            if (battle == null)
                return;

            battle.SelectionReady -=
                OnSelectionReady;

            battle.ComparisonStarted -=
                OnComparisonStarted;

            battle.WinnerRevealed -=
                OnWinnerRevealed;

            battle.SelectionReady +=
                OnSelectionReady;

            battle.ComparisonStarted +=
                OnComparisonStarted;

            battle.WinnerRevealed +=
                OnWinnerRevealed;
        }

        void Unsubscribe()
        {
            if (battle == null)
                return;

            battle.SelectionReady -=
                OnSelectionReady;

            battle.ComparisonStarted -=
                OnComparisonStarted;

            battle.WinnerRevealed -=
                OnWinnerRevealed;
        }

        void CacheSceneObjects()
        {
            Renderer[] renderers =
                Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include
                );

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                switch (renderer.gameObject.name)
                {
                    case "ArenaFloor":
                        arenaFloor = renderer;
                        break;

                    case "BattleCore":
                        battleCore = renderer;
                        break;

                    case "CoreEnergyRing":
                        coreRing = renderer;
                        break;

                    case "CoreEnergyOuter":
                        coreOuter = renderer;
                        break;

                    case "PlayerPlatform":
                        playerPlatform = renderer;
                        break;

                    case "RivalPlatform":
                        rivalPlatform = renderer;
                        break;

                    case "LeftNeonStrip":
                    case "LeftTowerNeon":
                    case "PlayerFloorStripLeft":
                    case "PlayerFloorStripRight":
                        playerNeon.Add(renderer);
                        break;

                    case "RightNeonStrip":
                    case "RightTowerNeon":
                    case "RivalFloorStripLeft":
                    case "RivalFloorStripRight":
                        rivalNeon.Add(renderer);
                        break;
                }
            }

            Light[] lights =
                Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Include
                );

            foreach (Light light in lights)
            {
                if (light != null &&
                    light.gameObject.name ==
                    "CoreGlow")
                {
                    coreGlow = light;

                    originalCoreIntensity =
                        light.intensity;

                    originalCoreColour =
                        light.color;

                    break;
                }
            }
        }

        void BuildRuntimeMaterials()
        {
            SetupFloor();

            SetupRenderer(
                battleCore,
                PlayerCyan,
                1.6f,
                0.2f,
                0.8f
            );

            SetupRenderer(
                coreRing,
                PlayerCyan,
                2.4f,
                0.1f,
                0.9f
            );

            SetupRenderer(
                coreOuter,
                PlayerCyan,
                1.6f,
                0.1f,
                0.85f
            );

            SetupRenderer(
                playerPlatform,
                PlayerCyan,
                0.8f,
                0.48f,
                0.82f
            );

            SetupRenderer(
                rivalPlatform,
                RivalViolet,
                0.8f,
                0.48f,
                0.82f
            );

            foreach (Renderer renderer in playerNeon)
            {
                SetupRenderer(
                    renderer,
                    PlayerCyan,
                    2.1f,
                    0.15f,
                    0.86f
                );
            }

            foreach (Renderer renderer in rivalNeon)
            {
                SetupRenderer(
                    renderer,
                    RivalViolet,
                    2.1f,
                    0.15f,
                    0.86f
                );
            }
        }

        void SetupFloor()
        {
            if (arenaFloor == null)
                return;

            Material material =
                GetRuntimeMaterial(
                    arenaFloor
                );

            if (material == null)
                return;

            if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    FloorColour
                );
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    floorMetallic
                );
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat(
                    "_Glossiness",
                    floorSmoothness
                );
            }

            EnableEmission(
                material,
                new Color(
                    0f,
                    0.05f,
                    0.08f
                )
            );
        }

        void SetupRenderer(
            Renderer renderer,
            Color colour,
            float emissionStrength,
            float metallic,
            float smoothness
        )
        {
            if (renderer == null)
                return;

            Material material =
                GetRuntimeMaterial(
                    renderer
                );

            if (material == null)
                return;

            if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    colour
                );
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    metallic
                );
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat(
                    "_Glossiness",
                    smoothness
                );
            }

            EnableEmission(
                material,
                colour *
                emissionStrength
            );
        }

        Material GetRuntimeMaterial(
            Renderer renderer
        )
        {
            if (renderer == null)
                return null;

            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials.Add(
                    renderer,
                    renderer.sharedMaterials
                );
            }

            Material[] source =
                renderer.sharedMaterials;

            if (source == null ||
                source.Length == 0 ||
                source[0] == null)
            {
                return null;
            }

            Material runtime =
                new Material(source[0])
                {
                    name =
                        source[0].name +
                        " (PupVerse Runtime)"
                };

            runtimeMaterials.Add(
                runtime
            );

            Material[] replacement =
                (Material[])
                source.Clone();

            replacement[0] =
                runtime;

            renderer.sharedMaterials =
                replacement;

            return runtime;
        }

        static void EnableEmission(
            Material material,
            Color colour
        )
        {
            if (material == null ||
                !material.HasProperty(
                    "_EmissionColor"
                ))
            {
                return;
            }

            material.EnableKeyword(
                "_EMISSION"
            );

            material.SetColor(
                "_EmissionColor",
                colour
            );
        }

        void BuildFloorGrid()
        {
            if (arenaFloor == null)
                return;

            MeshFilter floorFilter =
                arenaFloor.GetComponent<MeshFilter>();

            if (floorFilter == null ||
                floorFilter.sharedMesh == null)
            {
                return;
            }

            floorBounds =
                floorFilter.sharedMesh.bounds;

            Shader shader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (shader == null)
                return;

            gridMaterial =
                new Material(shader)
                {
                    name =
                        "PupVerse Floor Grid Runtime"
                };

            scanMaterial =
                new Material(shader)
                {
                    name =
                        "PupVerse Arena Scan Runtime"
                };

            runtimeMaterials.Add(
                gridMaterial
            );

            runtimeMaterials.Add(
                scanMaterial
            );

            gridObject =
                new GameObject(
                    "Premium Floor Grid"
                );

            gridObject.transform.SetParent(
                arenaFloor.transform,
                false
            );

            MeshFilter gridFilter =
                gridObject.AddComponent<MeshFilter>();

            MeshRenderer gridRenderer =
                gridObject.AddComponent<MeshRenderer>();

            gridFilter.sharedMesh =
                CreateGridMesh(
                    floorBounds
                );

            gridRenderer.sharedMaterial =
                gridMaterial;

            gridRenderer.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.Off;

            gridRenderer.receiveShadows =
                false;

            scanObject =
                new GameObject(
                    "Arena Energy Sweep"
                );

            scanObject.transform.SetParent(
                arenaFloor.transform,
                false
            );

            MeshFilter scanFilter =
                scanObject.AddComponent<MeshFilter>();

            MeshRenderer scanRenderer =
                scanObject.AddComponent<MeshRenderer>();

            scanFilter.sharedMesh =
                CreateScanMesh(
                    floorBounds
                );

            scanRenderer.sharedMaterial =
                scanMaterial;

            scanRenderer.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.Off;

            scanRenderer.receiveShadows =
                false;

            UpdateGridColours();
        }

        Mesh CreateGridMesh(
            Bounds bounds
        )
        {
            const int xLines = 9;
            const int zLines = 13;

            float thicknessX =
                bounds.size.x /
                620f;

            float thicknessZ =
                bounds.size.z /
                760f;

            float y =
                bounds.max.y +
                0.012f;

            List<Vector3> vertices =
                new List<Vector3>();

            List<int> triangles =
                new List<int>();

            for (int i = 0; i < xLines; i++)
            {
                float t =
                    i /
                    (float)(xLines - 1);

                float x =
                    Mathf.Lerp(
                        bounds.min.x,
                        bounds.max.x,
                        t
                    );

                AddFloorQuad(
                    vertices,
                    triangles,
                    x - thicknessX,
                    x + thicknessX,
                    bounds.min.z,
                    bounds.max.z,
                    y
                );
            }

            for (int i = 0; i < zLines; i++)
            {
                float t =
                    i /
                    (float)(zLines - 1);

                float z =
                    Mathf.Lerp(
                        bounds.min.z,
                        bounds.max.z,
                        t
                    );

                AddFloorQuad(
                    vertices,
                    triangles,
                    bounds.min.x,
                    bounds.max.x,
                    z - thicknessZ,
                    z + thicknessZ,
                    y
                );
            }

            Mesh mesh =
                new Mesh
                {
                    name =
                        "PupVerse Premium Grid"
                };

            mesh.SetVertices(vertices);
            mesh.SetTriangles(
                triangles,
                0
            );

            mesh.RecalculateBounds();

            return mesh;
        }

        Mesh CreateScanMesh(
            Bounds bounds
        )
        {
            float y =
                bounds.max.y +
                0.018f;

            float depth =
                bounds.size.z *
                0.035f;

            Vector3[] vertices =
            {
                new Vector3(
                    bounds.min.x,
                    y,
                    -depth
                ),

                new Vector3(
                    bounds.min.x,
                    y,
                    depth
                ),

                new Vector3(
                    bounds.max.x,
                    y,
                    depth
                ),

                new Vector3(
                    bounds.max.x,
                    y,
                    -depth
                )
            };

            int[] triangles =
            {
                0, 1, 2,
                0, 2, 3
            };

            Mesh mesh =
                new Mesh
                {
                    name =
                        "PupVerse Arena Sweep"
                };

            mesh.vertices =
                vertices;

            mesh.triangles =
                triangles;

            mesh.RecalculateBounds();

            return mesh;
        }

        static void AddFloorQuad(
            List<Vector3> vertices,
            List<int> triangles,
            float xMin,
            float xMax,
            float zMin,
            float zMax,
            float y
        )
        {
            int start =
                vertices.Count;

            vertices.Add(
                new Vector3(
                    xMin,
                    y,
                    zMin
                )
            );

            vertices.Add(
                new Vector3(
                    xMin,
                    y,
                    zMax
                )
            );

            vertices.Add(
                new Vector3(
                    xMax,
                    y,
                    zMax
                )
            );

            vertices.Add(
                new Vector3(
                    xMax,
                    y,
                    zMin
                )
            );

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        void OnSelectionReady()
        {
            winnerBias =
                0f;

            phaseColour =
                PlayerCyan;

            phaseBoost =
                Mathf.Max(
                    phaseBoost,
                    0.18f
                );
        }

        void OnComparisonStarted(
            BattleResult result
        )
        {
            phaseColour =
                BattleCardEffects
                    .StatColor(
                        result.Stat
                    );

            winnerBias =
                0f;

            phaseBoost =
                1f;
        }

        void OnWinnerRevealed(
            BattleResult result
        )
        {
            if (result.Winner ==
                BattleWinner.Player)
            {
                phaseColour =
                    PlayerCyan;

                winnerBias =
                    1f;
            }
            else if (result.Winner ==
                     BattleWinner.Opponent)
            {
                phaseColour =
                    RivalViolet;

                winnerBias =
                    -1f;
            }
            else
            {
                phaseColour =
                    BattleCardEffects
                        .StatColor(
                            result.Stat
                        );

                winnerBias =
                    0f;
            }

            phaseBoost =
                1.8f;
        }

        void Update()
        {
            if (!ready)
                return;

            float delta =
                Time.unscaledDeltaTime;

            phaseBoost =
                Mathf.MoveTowards(
                    phaseBoost,
                    0f,
                    delta *
                    1.25f
                );

            float pulse =
                GameSettings.ReducedMotion
                    ? 0.5f
                    : 0.5f +
                      Mathf.Sin(
                          Time.unscaledTime *
                          corePulseSpeed *
                          Mathf.PI *
                          2f
                      ) *
                      0.5f;

            AnimateCore(
                pulse
            );

            AnimatePlatforms(
                pulse
            );

            AnimateNeon(
                pulse
            );

            AnimateGrid(
                pulse
            );

            AnimateScan();
        }

        void AnimateCore(
            float pulse
        )
        {
            float intensity =
                1.25f +
                pulse *
                0.75f +
                phaseBoost *
                0.95f;

            SetRendererEmission(
                battleCore,
                phaseColour *
                intensity
            );

            SetRendererEmission(
                coreRing,
                phaseColour *
                (
                    intensity *
                    1.35f
                )
            );

            SetRendererEmission(
                coreOuter,
                phaseColour *
                (
                    intensity *
                    0.85f
                )
            );

            if (coreGlow != null)
            {
                coreGlow.color =
                    Color.Lerp(
                        PlayerCyan,
                        phaseColour,
                        Mathf.Clamp01(
                            phaseBoost
                        )
                    );

                coreGlow.intensity =
                    originalCoreIntensity *
                    (
                        0.75f +
                        pulse *
                        0.18f +
                        phaseBoost *
                        0.22f
                    );
            }
        }

        void AnimatePlatforms(
            float pulse
        )
        {
            float playerStrength =
                0.65f +
                pulse *
                0.18f;

            float rivalStrength =
                0.65f +
                pulse *
                0.18f;

            if (winnerBias > 0f)
            {
                playerStrength +=
                    phaseBoost *
                    0.8f;
            }
            else if (winnerBias < 0f)
            {
                rivalStrength +=
                    phaseBoost *
                    0.8f;
            }

            SetRendererEmission(
                playerPlatform,
                PlayerCyan *
                playerStrength
            );

            SetRendererEmission(
                rivalPlatform,
                RivalViolet *
                rivalStrength
            );
        }

        void AnimateNeon(
            float pulse
        )
        {
            float baseStrength =
                1.35f +
                pulse *
                0.35f;

            foreach (Renderer renderer
                     in playerNeon)
            {
                SetRendererEmission(
                    renderer,
                    PlayerCyan *
                    (
                        baseStrength +
                        Mathf.Max(
                            0f,
                            winnerBias
                        ) *
                        phaseBoost *
                        0.35f
                    )
                );
            }

            foreach (Renderer renderer
                     in rivalNeon)
            {
                SetRendererEmission(
                    renderer,
                    RivalViolet *
                    (
                        baseStrength +
                        Mathf.Max(
                            0f,
                            -winnerBias
                        ) *
                        phaseBoost *
                        0.35f
                    )
                );
            }
        }

        void AnimateGrid(
            float pulse
        )
        {
            UpdateGridColours();

            if (gridMaterial == null)
                return;

            Color colour =
                Color.Lerp(
                    PlayerCyan,
                    phaseColour,
                    Mathf.Clamp01(
                        phaseBoost
                    )
                );

            colour.a =
                gridOpacity *
                (
                    0.76f +
                    pulse *
                    0.24f
                );

            gridMaterial.color =
                colour;
        }

        void UpdateGridColours()
        {
            if (gridMaterial != null)
            {
                Color colour =
                    PlayerCyan;

                colour.a =
                    gridOpacity;

                gridMaterial.color =
                    colour;
            }

            if (scanMaterial != null)
            {
                Color colour =
                    phaseColour;

                colour.a =
                    scanOpacity;

                scanMaterial.color =
                    colour;
            }
        }

        void AnimateScan()
        {
            if (scanObject == null)
                return;

            if (GameSettings.ReducedMotion)
            {
                scanObject.transform
                    .localPosition =
                    Vector3.zero;

                return;
            }

            float normalized =
                Mathf.Repeat(
                    Time.unscaledTime *
                    scanSpeed,
                    1f
                );

            float z =
                Mathf.Lerp(
                    floorBounds.min.z,
                    floorBounds.max.z,
                    normalized
                );

            scanObject.transform
                .localPosition =
                new Vector3(
                    0f,
                    0f,
                    z
                );

            if (scanMaterial != null)
            {
                Color colour =
                    Color.Lerp(
                        PlayerCyan,
                        phaseColour,
                        Mathf.Clamp01(
                            phaseBoost
                        )
                    );

                colour.a =
                    scanOpacity *
                    (
                        1f +
                        phaseBoost *
                        0.4f
                    );

                scanMaterial.color =
                    colour;
            }
        }

        void SetRendererEmission(
            Renderer renderer,
            Color colour
        )
        {
            if (renderer == null)
                return;

            Material material =
                renderer.sharedMaterial;

            if (material == null ||
                !material.HasProperty(
                    "_EmissionColor"
                ))
            {
                return;
            }

            material.SetColor(
                "_EmissionColor",
                colour
            );
        }

        void RestoreMaterials()
        {
            foreach (
                KeyValuePair<
                    Renderer,
                    Material[]
                > pair
                in originalMaterials
            )
            {
                if (pair.Key != null)
                {
                    pair.Key.sharedMaterials =
                        pair.Value;
                }
            }

            originalMaterials.Clear();

            foreach (Material material
                     in runtimeMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            runtimeMaterials.Clear();
        }

        void RestoreLight()
        {
            if (coreGlow == null)
                return;

            coreGlow.intensity =
                originalCoreIntensity;

            coreGlow.color =
                originalCoreColour;
        }

        void DestroyGeneratedObjects()
        {
            if (gridObject != null)
            {
                MeshFilter filter =
                    gridObject
                        .GetComponent<MeshFilter>();

                if (filter != null &&
                    filter.sharedMesh != null)
                {
                    Destroy(
                        filter.sharedMesh
                    );
                }

                Destroy(
                    gridObject
                );
            }

            if (scanObject != null)
            {
                MeshFilter filter =
                    scanObject
                        .GetComponent<MeshFilter>();

                if (filter != null &&
                    filter.sharedMesh != null)
                {
                    Destroy(
                        filter.sharedMesh
                    );
                }

                Destroy(
                    scanObject
                );
            }

            gridObject = null;
            scanObject = null;
        }
    }
}
