using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pupverse
{
    // Touch feedback only; the existing Button still owns the battle action.
    public sealed class BattleStatTile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform visual, icon;
        public CanvasGroup group;
        public BattleHudGraphic panel;
        public Button button;
        public int index;
        public Text number, bonus;
        Vector2 previousSize = new Vector2(-1,-1);
        public bool selected, resolving;
        bool pressed;
        float appearedAt, rippleAt = -100;
        bool wasSelected;
        void OnEnable() { appearedAt = Time.unscaledTime; pressed = false; }
        public void OnPointerDown(PointerEventData data) { pressed = button != null && button.interactable; if(pressed) rippleAt=Time.unscaledTime; }
        public void OnPointerUp(PointerEventData data) { pressed = false; }
        public void OnPointerExit(PointerEventData data) { pressed = false; }
        void Update()
        {
            if (visual == null) return;
            float width=visual.rect.width;
            if(previousSize!=visual.rect.size && number!=null && bonus!=null)
            {
                previousSize=visual.rect.size;
                bool shortTile=visual.rect.height<60;
                bool compact=width<160;
                number.fontSize=compact?21:shortTile?22:24;
                number.rectTransform.sizeDelta=new Vector2(62,shortTile?30:34);
                number.rectTransform.anchoredPosition=new Vector2(48,compact?13:shortTile?3:5);
                bonus.fontSize=compact?8:9;
                bonus.rectTransform.anchoredPosition=new Vector2(-12,compact?0:shortTile?11:15);
            }
            bool reduced = GameSettings.ReducedMotion;
            if(selected && !wasSelected) rippleAt=Time.unscaledTime;
            wasSelected=selected;
            panel.Motion(reduced?.5f:Mathf.Repeat(Time.unscaledTime*.15f+index*.19f,1),
                reduced?1:Mathf.Clamp01((Time.unscaledTime-rippleAt)/.65f));
            float entry = reduced ? 1 : Mathf.Clamp01((Time.unscaledTime - appearedAt - index * .045f) / .38f);
            float ease = 1-Mathf.Pow(1-entry,3);
            float desired = pressed && !resolving ? .96f : selected ? 1.018f : 1;
            visual.localScale = Vector3.one * (reduced ? 1 : Mathf.Lerp(visual.localScale.x,desired,1-Mathf.Exp(-22*Time.unscaledDeltaTime)));
            visual.anchoredPosition = new Vector2(0,(1-ease)*-14);
            group.alpha = ease * (resolving && !selected ? .38f : 1);
            if(icon!=null)
            {
                float wave=reduced?0:Mathf.Sin(Time.unscaledTime*(selected?(index==0?10:6):1.8f)+index);
                float strength=selected?1:.35f;
                wave*=strength;
                icon.localScale=Vector3.one*(1+wave*(index==3?.04f:.08f));
                icon.localRotation=Quaternion.Euler(0,0,index==2||index==4?wave*8:0);
                icon.anchoredPosition=new Vector2(12+(index==1?wave*2:0),(visual.rect.height-27)*.5f+2);
            }
            panel.Animate(selected || pressed ? 1 : reduced ? .15f : .14f + .06f*Mathf.Sin(Time.unscaledTime*1.8f + index));
        }
        void OnDisable() { pressed = false; if(visual != null) { visual.localScale = Vector3.one; visual.anchoredPosition=Vector2.zero; } }
    }
}
