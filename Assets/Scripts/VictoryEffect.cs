using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    public sealed class VictoryEffect : MonoBehaviour
    {
        public RectTransform[] sparks;
        public CanvasGroup group;

        Coroutine running;

        void Awake()
        {
            Hide();
        }

        public void Play(Color color)
        {
            Clear();

            if (GameSettings.ReducedMotion)
                return;

            // The existing Battle3D scene may have this object
            // disabled between effects. Reactivate it safely.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (group != null && !group.gameObject.activeSelf)
            {
                group.gameObject.SetActive(true);
            }

            // A disabled parent can still leave this component
            // inactive. In that case simply skip the visual effect
            // instead of producing a Unity runtime error.
            if (!isActiveAndEnabled)
                return;

            running = StartCoroutine(
                Burst(color)
            );
        }

        public void Clear()
        {
            if (running != null && isActiveAndEnabled)
            {
                StopCoroutine(running);
            }

            running = null;

            Hide();
        }

        void Hide()
        {
            if (group == null)
                return;

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        IEnumerator Burst(Color color)
        {
            if (group == null)
            {
                running = null;
                yield break;
            }

            group.alpha = 1f;

            float elapsed = 0f;
            const float duration = 1.8f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                group.alpha = 1f - t;

                if (sparks != null)
                {
                    for (int i = 0; i < sparks.Length; i++)
                    {
                        RectTransform spark = sparks[i];

                        if (spark == null)
                            continue;

                        float angle =
                            (i * 137.5f) *
                            Mathf.Deg2Rad;

                        float speed =
                            180f +
                            (i % 5) * 60f;

                        spark.anchoredPosition =
                            new Vector2(
                                Mathf.Cos(angle) *
                                speed *
                                t,

                                Mathf.Sin(angle) *
                                speed *
                                t -
                                t * t * 300f
                            );

                        spark.localRotation =
                            Quaternion.Euler(
                                0f,
                                0f,
                                i * 31f +
                                t * 240f
                            );

                        Image image =
                            spark.GetComponent<Image>();

                        if (image != null)
                        {
                            image.color =
                                i % 3 == 0
                                    ? Color.white
                                    : color;
                        }
                    }
                }

                yield return null;
            }

            Hide();

            running = null;
        }

        void OnDisable()
        {
            running = null;
        }
    }
}
