using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Pupverse
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IPointerUpHandler
    {
        public float entranceDelay;
        Vector2 pointer; bool dragging, hovered, entered;
        Vector3 position, scale;
        CanvasGroup group;
        void Awake() { position = transform.localPosition; scale = transform.localScale; group = GetComponent<CanvasGroup>(); }
        IEnumerator Start()
        {
            group.alpha = 0; group.blocksRaycasts = false;
            yield return new WaitForSecondsRealtime(entranceDelay);
            float elapsed = 0, duration = GameSettings.ReducedMotion ? .05f : .65f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration), ease = 1 - Mathf.Pow(1 - t, 3);
                group.alpha = t;
                transform.localPosition = position + Vector3.down * 70 * (1 - ease);
                transform.localScale = scale * Mathf.Lerp(.92f, 1, ease);
                yield return null;
            }
            transform.localPosition = position; transform.localScale = scale;
            group.alpha = 1; group.blocksRaycasts = true; entered = true;
        }
        void Update()
        {
            if (!entered) return;
            Vector2 input = GameSettings.ReducedMotion ? Vector2.zero :
                dragging ? pointer : (hovered || Application.isMobilePlatform ? GameSettings.MotionInput : Vector2.zero);
            var target = Quaternion.Euler(-input.y * 5, input.x * 7, -input.x * 1.5f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, 1 - Mathf.Exp(-9 * Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) => hovered = false;
        public void OnDrag(PointerEventData e)
        {
            dragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var p);
            var size = ((RectTransform)transform).rect.size;
            pointer = new Vector2(Mathf.Clamp(p.x / size.x * 2, -1, 1), Mathf.Clamp(p.y / size.y * 2, -1, 1));
        }
        public void OnPointerUp(PointerEventData e) { dragging = false; pointer = Vector2.zero; }
        void OnApplicationFocus(bool focus) { if (!focus) { dragging = hovered = false; pointer = Vector2.zero; } }
    }
}
