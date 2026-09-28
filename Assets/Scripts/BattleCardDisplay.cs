using System.Collections;
using UnityEngine;

namespace Pupverse
{
    /*
     * Controls only the visual presentation of the
     * two physical cards entering the 3D arena.
     *
     * BattleMatchController still owns:
     * - card ownership
     * - turns
     * - captures
     * - draw pot
     * - match victory
     */
    [DisallowMultipleComponent]
    public sealed class BattleCardDisplay : MonoBehaviour
    {
        [Header("Scene References")]
        public Transform playerRoot;
        public Transform rivalRoot;

        public Renderer playerFront;
        public Renderer rivalFront;

        [Header("Premium Deal Animation")]
        [Min(0f)]
        public float dealDuration = 0.48f;

        [Min(0f)]
        public float revealDuration = 0.30f;

        [Min(0f)]
        public float settleDuration = 0.18f;

        [Header("Deal Motion")]
        [Min(0f)]
        public float sideOffset = 0.85f;

        [Min(0f)]
        public float liftHeight = 0.36f;

        [Range(0f, 20f)]
        public float dealTilt = 8f;

        [Range(0f, 0.2f)]
        public float settleOvershoot = 0.065f;

        [Range(0.2f, 1f)]
        public float startingScale = 0.72f;

        [Header("Reveal")]
        [Range(0.01f, 0.25f)]
        public float minimumFlipWidth = 0.045f;

        MaterialPropertyBlock playerOriginal;
        MaterialPropertyBlock rivalOriginal;
        MaterialPropertyBlock working;

        Vector3 playerOriginalScale;
        Vector3 rivalOriginalScale;

        Vector3 playerOriginalPosition;
        Vector3 rivalOriginalPosition;

        Quaternion playerOriginalRotation;
        Quaternion rivalOriginalRotation;

        bool revealing;
        bool captured;

        int version;

        static readonly int MainTexture =
            Shader.PropertyToID("_MainTex");

        static readonly int BaseMap =
            Shader.PropertyToID("_BaseMap");

        void CaptureMaterials()
        {
            if (captured)
                return;

            playerOriginal =
                new MaterialPropertyBlock();

            rivalOriginal =
                new MaterialPropertyBlock();

            working =
                new MaterialPropertyBlock();

            if (playerFront != null)
            {
                playerFront.GetPropertyBlock(
                    playerOriginal
                );
            }

            if (rivalFront != null)
            {
                rivalFront.GetPropertyBlock(
                    rivalOriginal
                );
            }

            captured = true;
        }

        public IEnumerator Reveal(
            CardData player,
            CardData rival
        )
        {
            /*
             * Stop an unfinished previous reveal and
             * return both cards to their exact scene poses.
             */
            CancelReveal();

            CaptureMaterials();

            int currentVersion =
                version;

            if (playerRoot == null ||
                rivalRoot == null)
            {
                SetArtwork(
                    playerFront,
                    player
                );

                SetArtwork(
                    rivalFront,
                    rival
                );

                yield break;
            }

            CaptureTransforms();

            /*
             * Accessibility setting:
             * immediately swap artwork with no movement.
             */
            if (GameSettings.ReducedMotion)
            {
                SetArtwork(
                    playerFront,
                    player
                );

                SetArtwork(
                    rivalFront,
                    rival
                );

                RestoreTransforms();

                yield break;
            }

            revealing = true;

            Vector3 playerStartPosition =
                playerOriginalPosition +
                new Vector3(
                    -sideOffset,
                    liftHeight,
                    0f
                );

            Vector3 rivalStartPosition =
                rivalOriginalPosition +
                new Vector3(
                    sideOffset,
                    liftHeight,
                    0f
                );

            Quaternion playerStartRotation =
                playerOriginalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    -dealTilt
                );

            Quaternion rivalStartRotation =
                rivalOriginalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    dealTilt
                );

            /*
             * Begin slightly smaller and outside
             * the battle platform.
             */
            playerRoot.localPosition =
                playerStartPosition;

            rivalRoot.localPosition =
                rivalStartPosition;

            playerRoot.localRotation =
                playerStartRotation;

            rivalRoot.localRotation =
                rivalStartRotation;

            playerRoot.localScale =
                playerOriginalScale *
                startingScale;

            rivalRoot.localScale =
                rivalOriginalScale *
                startingScale;

            /*
             * ------------------------------------------------
             * STAGE 1
             * Cards travel from their respective deck sides
             * toward their arena platforms.
             * ------------------------------------------------
             */
            float duration =
                Mathf.Max(
                    0.01f,
                    dealDuration
                );

            float elapsed =
                0f;

            while (elapsed < duration)
            {
                if (currentVersion != version)
                    yield break;

                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                float eased =
                    EaseOutCubic(t);

                playerRoot.localPosition =
                    Vector3.Lerp(
                        playerStartPosition,
                        playerOriginalPosition,
                        eased
                    );

                rivalRoot.localPosition =
                    Vector3.Lerp(
                        rivalStartPosition,
                        rivalOriginalPosition,
                        eased
                    );

                /*
                 * Add a small vertical arc during travel.
                 */
                float arc =
                    Mathf.Sin(
                        t *
                        Mathf.PI
                    ) *
                    liftHeight *
                    0.55f;

                playerRoot.localPosition +=
                    Vector3.up *
                    arc;

                rivalRoot.localPosition +=
                    Vector3.up *
                    arc;

                playerRoot.localRotation =
                    Quaternion.Slerp(
                        playerStartRotation,
                        playerOriginalRotation,
                        eased
                    );

                rivalRoot.localRotation =
                    Quaternion.Slerp(
                        rivalStartRotation,
                        rivalOriginalRotation,
                        eased
                    );

                float scale =
                    Mathf.Lerp(
                        startingScale,
                        1f,
                        eased
                    );

                playerRoot.localScale =
                    playerOriginalScale *
                    scale;

                rivalRoot.localScale =
                    rivalOriginalScale *
                    scale;

                yield return null;
            }

            /*
             * ------------------------------------------------
             * STAGE 2
             * Holographic card flip / artwork reveal.
             * ------------------------------------------------
             */
            duration =
                Mathf.Max(
                    0.01f,
                    revealDuration
                );

            elapsed =
                0f;

            bool swapped =
                false;

            while (elapsed < duration)
            {
                if (currentVersion != version)
                    yield break;

                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                float widthScale;

                if (t < 0.5f)
                {
                    float firstHalf =
                        t /
                        0.5f;

                    widthScale =
                        Mathf.Lerp(
                            1f,
                            minimumFlipWidth,
                            Mathf.SmoothStep(
                                0f,
                                1f,
                                firstHalf
                            )
                        );
                }
                else
                {
                    if (!swapped)
                    {
                        SetArtwork(
                            playerFront,
                            player
                        );

                        SetArtwork(
                            rivalFront,
                            rival
                        );

                        swapped = true;
                    }

                    float secondHalf =
                        (t - 0.5f) /
                        0.5f;

                    widthScale =
                        Mathf.Lerp(
                            minimumFlipWidth,
                            1f,
                            Mathf.SmoothStep(
                                0f,
                                1f,
                                secondHalf
                            )
                        );
                }

                playerRoot.localScale =
                    Vector3.Scale(
                        playerOriginalScale,
                        new Vector3(
                            widthScale,
                            1f,
                            1f
                        )
                    );

                rivalRoot.localScale =
                    Vector3.Scale(
                        rivalOriginalScale,
                        new Vector3(
                            widthScale,
                            1f,
                            1f
                        )
                    );

                yield return null;
            }

            /*
             * Ensure the correct textures are applied
             * even if the framerate skipped over the
             * exact midpoint.
             */
            SetArtwork(
                playerFront,
                player
            );

            SetArtwork(
                rivalFront,
                rival
            );

            /*
             * ------------------------------------------------
             * STAGE 3
             * Small premium settle / overshoot.
             * ------------------------------------------------
             */
            duration =
                Mathf.Max(
                    0.01f,
                    settleDuration
                );

            elapsed =
                0f;

            while (elapsed < duration)
            {
                if (currentVersion != version)
                    yield break;

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

                float scale =
                    1f +
                    pulse *
                    settleOvershoot;

                playerRoot.localScale =
                    playerOriginalScale *
                    scale;

                rivalRoot.localScale =
                    rivalOriginalScale *
                    scale;

                /*
                 * Tiny opposite card tilts stop the
                 * arrival from feeling robotic.
                 */
                float settleTilt =
                    pulse *
                    dealTilt *
                    0.18f;

                playerRoot.localRotation =
                    playerOriginalRotation *
                    Quaternion.Euler(
                        0f,
                        0f,
                        settleTilt
                    );

                rivalRoot.localRotation =
                    rivalOriginalRotation *
                    Quaternion.Euler(
                        0f,
                        0f,
                        -settleTilt
                    );

                yield return null;
            }

            if (currentVersion != version)
                yield break;

            RestoreTransforms();

            revealing = false;
        }

        void CaptureTransforms()
        {
            playerOriginalScale =
                playerRoot.localScale;

            rivalOriginalScale =
                rivalRoot.localScale;

            playerOriginalPosition =
                playerRoot.localPosition;

            rivalOriginalPosition =
                rivalRoot.localPosition;

            playerOriginalRotation =
                playerRoot.localRotation;

            rivalOriginalRotation =
                rivalRoot.localRotation;
        }

        void SetArtwork(
            Renderer target,
            CardData card
        )
        {
            if (target == null ||
                card == null ||
                card.originalCardArt == null)
            {
                return;
            }

            target.GetPropertyBlock(
                working
            );

            Material material =
                target.sharedMaterial;

            /*
             * Current project uses _MainTex.
             */
            if (material == null ||
                material.HasProperty(
                    MainTexture
                ))
            {
                working.SetTexture(
                    MainTexture,
                    card.originalCardArt
                );
            }

            /*
             * Future-proof this for a possible URP
             * material upgrade later.
             */
            if (material != null &&
                material.HasProperty(
                    BaseMap
                ))
            {
                working.SetTexture(
                    BaseMap,
                    card.originalCardArt
                );
            }

            target.SetPropertyBlock(
                working
            );
        }

        static float EaseOutCubic(
            float t
        )
        {
            t =
                Mathf.Clamp01(t);

            float inverse =
                1f - t;

            return
                1f -
                inverse *
                inverse *
                inverse;
        }

        void RestoreTransforms()
        {
            if (playerRoot != null)
            {
                playerRoot.localPosition =
                    playerOriginalPosition;

                playerRoot.localRotation =
                    playerOriginalRotation;

                playerRoot.localScale =
                    playerOriginalScale;
            }

            if (rivalRoot != null)
            {
                rivalRoot.localPosition =
                    rivalOriginalPosition;

                rivalRoot.localRotation =
                    rivalOriginalRotation;

                rivalRoot.localScale =
                    rivalOriginalScale;
            }
        }

        public void CancelReveal()
        {
            version++;

            if (!revealing)
                return;

            RestoreTransforms();

            revealing =
                false;
        }

        void OnDisable()
        {
            CancelReveal();

            if (!captured)
                return;

            if (playerFront != null)
            {
                playerFront.SetPropertyBlock(
                    playerOriginal
                );
            }

            if (rivalFront != null)
            {
                rivalFront.SetPropertyBlock(
                    rivalOriginal
                );
            }

            captured =
                false;
        }
    }
}
