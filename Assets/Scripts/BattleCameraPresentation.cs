using UnityEngine;

namespace Pupverse
{
    /*
     * Premium cinematic camera feedback.
     *
     * Presentation only.
     *
     * Does NOT change:
     * - battle rules
     * - card ownership
     * - selected stats
     * - match sequencing
     * - card movement
     */
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(30)]
    public sealed class BattleCameraPresentation
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        BattleController battle;

        [SerializeField]
        Camera battleCamera;

        [Header("Cinematic Movement")]
        [Tooltip("Small push toward the arena when a stat is selected.")]
        [SerializeField, Min(0f)]
        float comparisonPush = 0.24f;

        [Tooltip("Extra push when the winner is revealed.")]
        [SerializeField, Min(0f)]
        float impactPush = 0.36f;

        [Tooltip("How quickly positional camera movement eases.")]
        [SerializeField, Min(0.01f)]
        float positionSmoothTime = 0.16f;

        [Tooltip("How quickly camera rotation eases.")]
        [SerializeField, Min(0.01f)]
        float rotationSmoothTime = 0.18f;

        [Tooltip("How long the camera favours the winner before returning.")]
        [SerializeField, Min(0f)]
        float winnerHoldDuration = 0.72f;

        [Header("Winner Focus")]
        [Tooltip("Very small vertical framing bias toward the winning side.")]
        [SerializeField, Range(0f, 3f)]
        float winnerTiltDegrees = 1.0f;

        [Tooltip("Small cinematic roll toward the winning side.")]
        [SerializeField, Range(0f, 2f)]
        float winnerRollDegrees = 0.45f;

        [Header("Impact Shake")]
        [Tooltip("Maximum positional shake for a Power-style impact.")]
        [SerializeField, Range(0f, 0.15f)]
        float shakeStrength = 0.038f;

        [SerializeField, Min(0.01f)]
        float shakeDuration = 0.24f;

        [SerializeField, Min(1f)]
        float shakeFrequency = 28f;

        [Tooltip("Maximum rotational kick during impact.")]
        [SerializeField, Range(0f, 2f)]
        float shakeRotation = 0.38f;

        Vector3 baseLocalPosition;
        Quaternion baseLocalRotation;

        float currentPush;
        float targetPush;
        float pushVelocity;

        float currentPitch;
        float targetPitch;
        float pitchVelocity;

        float currentRoll;
        float targetRoll;
        float rollVelocity;

        float shakeStartedAt = -100f;
        float activeShakeStrength;

        float returnAt = -1f;

        CardStat activeStat;

        bool initialized;
        bool subscribed;

        void Start()
        {
            ResolveReferences();

            if (battle == null ||
                battleCamera == null)
            {
                Debug.LogError(
                    "BattleCameraPresentation could not find the BattleController or battle Camera.",
                    this
                );

                enabled = false;
                return;
            }

            CaptureBasePose();

            Subscribe();

            initialized = true;
        }

        void OnEnable()
        {
            if (initialized)
            {
                Subscribe();
            }
        }

        void OnDisable()
        {
            Unsubscribe();

            if (initialized)
            {
                RestoreImmediately();
            }
        }

        void ResolveReferences()
        {
            if (battleCamera == null)
            {
                battleCamera =
                    GetComponent<Camera>();
            }

            if (battleCamera == null)
            {
                battleCamera =
                    Camera.main;
            }

            if (battle == null)
            {
                battle =
                    Object.FindAnyObjectByType<
                        BattleController
                    >();
            }
        }

        void CaptureBasePose()
        {
            Transform cameraTransform =
                battleCamera.transform;

            baseLocalPosition =
                cameraTransform.localPosition;

            baseLocalRotation =
                cameraTransform.localRotation;
        }

        void Subscribe()
        {
            if (subscribed ||
                battle == null)
            {
                return;
            }

            battle.SelectionReady +=
                OnSelectionReady;

            battle.ComparisonStarted +=
                OnComparisonStarted;

            battle.WinnerRevealed +=
                OnWinnerRevealed;

            subscribed = true;
        }

        void Unsubscribe()
        {
            if (!subscribed ||
                battle == null)
            {
                return;
            }

            battle.SelectionReady -=
                OnSelectionReady;

            battle.ComparisonStarted -=
                OnComparisonStarted;

            battle.WinnerRevealed -=
                OnWinnerRevealed;

            subscribed = false;
        }

        void OnSelectionReady()
        {
            /*
             * New round.
             * Return to the clean standard arena view.
             */
            targetPush = 0f;

            targetPitch = 0f;

            targetRoll = 0f;

            returnAt = -1f;

            activeShakeStrength = 0f;
        }

        void OnComparisonStarted(
            BattleResult result
        )
        {
            activeStat =
                result.Stat;

            /*
             * Stat selected:
             * gently move into the arena.
             */
            targetPush =
                comparisonPush *
                GetPushMultiplier(
                    result.Stat
                );

            targetPitch =
                GetStatPitch(
                    result.Stat
                );

            targetRoll =
                GetStatRoll(
                    result.Stat
                );

            returnAt = -1f;
        }

        void OnWinnerRevealed(
            BattleResult result
        )
        {
            activeStat =
                result.Stat;

            targetPush =
                impactPush *
                GetPushMultiplier(
                    result.Stat
                );

            /*
             * Player is nearer the camera.
             * Rival is farther up the arena.
             *
             * Tiny pitch differences make the
             * camera acknowledge who won without
             * becoming seasickness simulator 2026.
             */
            if (result.Winner ==
                BattleWinner.Player)
            {
                targetPitch =
                    winnerTiltDegrees;

                targetRoll =
                    -winnerRollDegrees;
            }
            else if (result.Winner ==
                     BattleWinner.Opponent)
            {
                targetPitch =
                    -winnerTiltDegrees;

                targetRoll =
                    winnerRollDegrees;
            }
            else
            {
                targetPitch = 0f;
                targetRoll = 0f;
            }

            StartImpactShake(
                result
            );

            returnAt =
                Time.unscaledTime +
                winnerHoldDuration;
        }

        void StartImpactShake(
            BattleResult result
        )
        {
            if (GameSettings.ReducedMotion)
            {
                activeShakeStrength = 0f;
                return;
            }

            float multiplier =
                GetShakeMultiplier(
                    result.Stat
                );

            /*
             * Draws get feedback but not a
             * winner-sized impact.
             */
            if (result.Winner ==
                BattleWinner.Draw)
            {
                multiplier *= 0.4f;
            }

            bool abilityBoosted =
                result.Winner ==
                BattleWinner.Player
                    ? result.PlayerBoost > 0
                    : result.Winner ==
                      BattleWinner.Opponent
                        ? result.OpponentBoost > 0
                        : false;

            if (abilityBoosted)
            {
                multiplier *= 1.15f;
            }

            activeShakeStrength =
                shakeStrength *
                multiplier;

            shakeStartedAt =
                Time.unscaledTime;
        }

        void LateUpdate()
        {
            if (!initialized ||
                battleCamera == null)
            {
                return;
            }

            /*
             * Accessibility:
             * reduced motion means absolutely
             * no camera drift or shake.
             */
            if (GameSettings.ReducedMotion)
            {
                RestoreImmediately();
                return;
            }

            if (returnAt > 0f &&
                Time.unscaledTime >= returnAt)
            {
                targetPush = 0f;
                targetPitch = 0f;
                targetRoll = 0f;

                returnAt = -1f;
            }

            float delta =
                Mathf.Max(
                    0.0001f,
                    Time.unscaledDeltaTime
                );

            currentPush =
                Mathf.SmoothDamp(
                    currentPush,
                    targetPush,
                    ref pushVelocity,
                    positionSmoothTime,
                    Mathf.Infinity,
                    delta
                );

            currentPitch =
                Mathf.SmoothDampAngle(
                    currentPitch,
                    targetPitch,
                    ref pitchVelocity,
                    rotationSmoothTime,
                    Mathf.Infinity,
                    delta
                );

            currentRoll =
                Mathf.SmoothDampAngle(
                    currentRoll,
                    targetRoll,
                    ref rollVelocity,
                    rotationSmoothTime,
                    Mathf.Infinity,
                    delta
                );

            ApplyCameraPose();
        }

        void ApplyCameraPose()
        {
            Transform cameraTransform =
                battleCamera.transform;

            /*
             * These vectors are based on the original
             * scene rotation, not the animated rotation.
             * That prevents cumulative drifting.
             */
            Vector3 forward =
                baseLocalRotation *
                Vector3.forward;

            Vector3 right =
                baseLocalRotation *
                Vector3.right;

            Vector3 up =
                baseLocalRotation *
                Vector3.up;

            Vector3 shakeOffset =
                Vector3.zero;

            float shakePitch =
                0f;

            float shakeRoll =
                0f;

            GetShake(
                out float horizontal,
                out float vertical,
                out float rotation
            );

            shakeOffset +=
                right *
                horizontal;

            shakeOffset +=
                up *
                vertical;

            shakePitch =
                rotation *
                0.45f;

            shakeRoll =
                rotation;

            cameraTransform.localPosition =
                baseLocalPosition +
                forward *
                currentPush +
                shakeOffset;

            cameraTransform.localRotation =
                baseLocalRotation *
                Quaternion.Euler(
                    currentPitch +
                    shakePitch,
                    0f,
                    currentRoll +
                    shakeRoll
                );
        }

        void GetShake(
            out float horizontal,
            out float vertical,
            out float rotation
        )
        {
            horizontal = 0f;
            vertical = 0f;
            rotation = 0f;

            if (activeShakeStrength <= 0f)
                return;

            float elapsed =
                Time.unscaledTime -
                shakeStartedAt;

            if (elapsed >= shakeDuration)
            {
                activeShakeStrength = 0f;
                return;
            }

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        0.01f,
                        shakeDuration
                    )
                );

            /*
             * Fast attack, smooth decay.
             */
            float envelope =
                1f -
                normalized;

            envelope *= envelope;

            float sampleTime =
                Time.unscaledTime *
                shakeFrequency;

            float xNoise =
                Mathf.PerlinNoise(
                    17.13f,
                    sampleTime
                ) *
                2f -
                1f;

            float yNoise =
                Mathf.PerlinNoise(
                    41.77f,
                    sampleTime
                ) *
                2f -
                1f;

            float rotationNoise =
                Mathf.PerlinNoise(
                    73.21f,
                    sampleTime
                ) *
                2f -
                1f;

            horizontal =
                xNoise *
                activeShakeStrength *
                envelope;

            vertical =
                yNoise *
                activeShakeStrength *
                0.65f *
                envelope;

            rotation =
                rotationNoise *
                shakeRotation *
                envelope;
        }

        static float GetPushMultiplier(
            CardStat stat
        )
        {
            switch (stat)
            {
                case CardStat.Power:
                    return 1.08f;

                case CardStat.Speed:
                    return 0.92f;

                case CardStat.Intelligence:
                    return 0.86f;

                case CardStat.Defence:
                    return 0.76f;

                case CardStat.Luck:
                    return 0.90f;

                default:
                    return 1f;
            }
        }

        static float GetShakeMultiplier(
            CardStat stat
        )
        {
            switch (stat)
            {
                case CardStat.Power:
                    return 1f;

                case CardStat.Speed:
                    return 0.55f;

                case CardStat.Intelligence:
                    return 0.28f;

                case CardStat.Defence:
                    return 0.32f;

                case CardStat.Luck:
                    return 0.44f;

                default:
                    return 0.5f;
            }
        }

        static float GetStatPitch(
            CardStat stat
        )
        {
            switch (stat)
            {
                case CardStat.Power:
                    return 0.20f;

                case CardStat.Speed:
                    return -0.12f;

                case CardStat.Intelligence:
                    return 0.30f;

                case CardStat.Defence:
                    return 0.40f;

                case CardStat.Luck:
                    return -0.18f;

                default:
                    return 0f;
            }
        }

        static float GetStatRoll(
            CardStat stat
        )
        {
            switch (stat)
            {
                case CardStat.Power:
                    return 0f;

                case CardStat.Speed:
                    return -0.20f;

                case CardStat.Intelligence:
                    return 0.15f;

                case CardStat.Defence:
                    return 0f;

                case CardStat.Luck:
                    return 0.22f;

                default:
                    return 0f;
            }
        }

        void RestoreImmediately()
        {
            if (battleCamera == null)
                return;

            currentPush = 0f;
            targetPush = 0f;
            pushVelocity = 0f;

            currentPitch = 0f;
            targetPitch = 0f;
            pitchVelocity = 0f;

            currentRoll = 0f;
            targetRoll = 0f;
            rollVelocity = 0f;

            activeShakeStrength = 0f;

            returnAt = -1f;

            battleCamera.transform
                .localPosition =
                baseLocalPosition;

            battleCamera.transform
                .localRotation =
                baseLocalRotation;
        }
    }
}
