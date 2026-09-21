using UnityEngine;
namespace Pupverse
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ReferenceLayout : MonoBehaviour
    {
        // Scale the entire composition inside the safe area, including on wider iPads.
        void LateUpdate()
        {
            var parent=transform.parent as RectTransform;
            if (!parent) return;
            var rt=(RectTransform)transform;
            rt.sizeDelta=new Vector2(1080,1920);
            float scale=Mathf.Min(parent.rect.width/1080f,parent.rect.height/1920f);
            transform.localScale=Vector3.one*Mathf.Max(.01f,scale);
        }
    }
}
