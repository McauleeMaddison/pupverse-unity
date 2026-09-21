using UnityEngine;
namespace Pupverse
{
    public sealed class IdleMotion : MonoBehaviour
    {
        public float amplitude = 12, speed = 1.25f;
        Vector3 origin, scale; float phase;
        void Awake() { origin = transform.localPosition; scale = transform.localScale; phase = Random.value * 3; }
        void Update()
        {
            float wave = GameSettings.ReducedMotion ? 0 : Mathf.Sin(Time.unscaledTime * speed + phase);
            transform.localPosition = origin + Vector3.up * wave * amplitude;
            transform.localScale = Vector3.Scale(scale, new Vector3(1 - wave * .006f, 1 + wave * .008f, 1));
        }
    }
}
