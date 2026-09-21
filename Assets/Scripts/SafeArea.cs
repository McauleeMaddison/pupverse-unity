using UnityEngine;
namespace Pupverse
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect last; Vector2Int size;
        void OnEnable() => Refresh();
        void Update() { if (last != Screen.safeArea || size.x != Screen.width || size.y != Screen.height) Refresh(); }
        void Refresh()
        {
            if (Screen.width == 0 || Screen.height == 0) return;
            last = Screen.safeArea; size = new Vector2Int(Screen.width, Screen.height);
            var r = (RectTransform)transform;
            r.anchorMin = last.position / new Vector2(Screen.width, Screen.height);
            r.anchorMax = (last.position + last.size) / new Vector2(Screen.width, Screen.height);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
