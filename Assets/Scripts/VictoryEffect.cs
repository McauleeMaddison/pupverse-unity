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
        void Awake() { group.alpha = 0; group.blocksRaycasts = false; }
        public void Play(Color color)
        {
            Clear();
            if (!GameSettings.ReducedMotion) running = StartCoroutine(Burst(color));
        }
        public void Clear()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            group.alpha = 0;
        }
        IEnumerator Burst(Color color)
        {
            float elapsed = 0;
            while (elapsed < 1.8f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / 1.8f;
                group.alpha = 1 - t;
                for (int i = 0; i < sparks.Length; i++)
                {
                    float angle = (i * 137.5f) * Mathf.Deg2Rad;
                    float speed = 180 + (i % 5) * 60;
                    sparks[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * speed * t,
                        Mathf.Sin(angle) * speed * t - t * t * 300);
                    sparks[i].localRotation = Quaternion.Euler(0, 0, i * 31 + t * 240);
                    sparks[i].GetComponent<Image>().color = i % 3 == 0 ? Color.white : color;
                }
                yield return null;
            }
            group.alpha = 0; running = null;
        }
    }
}
