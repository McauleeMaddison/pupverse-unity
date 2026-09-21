using UnityEngine;
namespace Pupverse
{
    public static class GameSettings
    {
        public static bool ReducedMotion { get => PlayerPrefs.GetInt("Pupverse.ReducedMotion", 0) == 1;
            set { PlayerPrefs.SetInt("Pupverse.ReducedMotion", value ? 1 : 0); PlayerPrefs.Save(); } }
        public static void Initialize() { Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; }
        public static Vector2 MotionInput
        {
            get
            {
                if (ReducedMotion) return Vector2.zero;
                if (Application.isMobilePlatform)
                {
                    if (Input.touchCount > 0) return Normalize(Input.GetTouch(0).position);
                    var acceleration = Input.acceleration;
                    return Vector2.ClampMagnitude(new Vector2(acceleration.x * 2, (acceleration.y + 0.55f) * 2), 1);
                }
                return Normalize(Input.mousePosition);
            }
        }
        static Vector2 Normalize(Vector2 p) => new Vector2(
            Mathf.Clamp(p.x / Mathf.Max(1, Screen.width) * 2 - 1, -1, 1),
            Mathf.Clamp(p.y / Mathf.Max(1, Screen.height) * 2 - 1, -1, 1));
    }
}
