using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pupverse
{
    // Touch feedback, without keyboard polling or allocations during animation.
    public sealed class BattleMenuMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public bool featured;
        BattleHudGraphic surface;
        bool pressed;
        void Awake(){surface=GetComponent<BattleHudGraphic>();}
        void OnEnable(){pressed=false;transform.localScale=Vector3.one;}
        public void OnPointerDown(PointerEventData e){var button=GetComponent<Button>();pressed=button!=null && button.IsInteractable();}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnPointerExit(PointerEventData e){pressed=false;}
        void Update()
        {
            bool reduced=GameSettings.ReducedMotion;
            transform.localScale=reduced?Vector3.one:Vector3.Lerp(transform.localScale,Vector3.one*(pressed?.967f:1),1-Mathf.Exp(-Time.unscaledDeltaTime*18));
            if(surface!=null){surface.Animate(pressed?1:featured?.55f:.12f);surface.Motion(reduced?.5f:Mathf.Repeat(Time.unscaledTime*.14f,1),1);}
        }
        void OnDisable(){pressed=false;transform.localScale=Vector3.one;}
    }
}
