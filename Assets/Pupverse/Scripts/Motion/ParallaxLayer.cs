using UnityEngine;
namespace Pupverse
{
    public sealed class ParallaxLayer : MonoBehaviour
    {
        public float depth = 12;
        Vector3 origin;
        void Awake() => origin = transform.localPosition;
        void Update() => transform.localPosition = Vector3.Lerp(transform.localPosition,
            origin + (Vector3)(GameSettings.MotionInput * depth), 1 - Mathf.Exp(-4 * Time.unscaledDeltaTime));
    }
}
