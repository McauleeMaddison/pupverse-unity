using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // A showcase preference, independent of the six-card battle hand.
    public sealed class FeaturedCardSelection : MonoBehaviour
    {
        BattleMatchController match;
        BattleProgression progression;
        Action closed;
        Font font;
        RectTransform root,viewport,content;
        GridLayoutGroup grid;
        Text title,instruction,status;
        Button save,cancel;
        readonly Button[] slots=new Button[3];
        readonly RawImage[] slotArt=new RawImage[3];
        readonly Text[] slotLabels=new Text[3];
        Button[] choices;
        CardData[] owned,draft=Array.Empty<CardData>();
        int selectedSlot;
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public CardData[] Draft => (CardData[])draft.Clone();
        public void Initialize(Transform parent,BattleMatchController controller,BattleProgression collection,Font typeface,Action onClosed)
        {
            match=controller;progression=collection;font=typeface;closed=onClosed;
            root=Rect("Featured card selection",parent);Stretch(root);var backdrop=root.gameObject.AddComponent<BattleMenuBackdrop>();backdrop.opacity=.98f;backdrop.raycastTarget=true;
            title=Label("Title",root,"YOUR SHOWCASE",24,Color.white);
            instruction=Label("Instruction",root,"Choose a position, then tap an owned card.",12,new Color(.6f,.8f,.9f));
            for(int i=0;i<3;i++)
            {
                int index=i;slots[i]=MakeButton("Showcase slot "+i,root,()=>SelectSlot(index));
                slotArt[i]=Rect("Artwork",slots[i].transform).gameObject.AddComponent<RawImage>();slotArt[i].raycastTarget=false;
                slotLabels[i]=Label("Position",slots[i].transform,"",10,Color.white);slotLabels[i].alignment=TextAnchor.MiddleCenter;
            }
            var scroll=Rect("Owned cards scroll",root).gameObject.AddComponent<ScrollRect>();viewport=(RectTransform)scroll.transform;
            viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.01f);viewport.gameObject.AddComponent<RectMask2D>();
            content=Rect("Owned cards",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
            grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.spacing=new Vector2(10,10);grid.padding=new RectOffset(2,2,2,2);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;
            scroll.content=content;scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            status=Label("Status",root,"Your battle hand stays unchanged.",11,new Color(.6f,.8f,.9f));status.alignment=TextAnchor.MiddleCenter;
            cancel=MakeButton("Cancel",root,Cancel);var ct=Label("Label",cancel.transform,"CANCEL",13,Color.white);Stretch(ct.rectTransform);ct.alignment=TextAnchor.MiddleCenter;
            save=MakeButton("Save showcase",root,()=>Save());var st=Label("Label",save.transform,"SAVE SHOWCASE",13,Color.white);Stretch(st.rectTransform);st.alignment=TextAnchor.MiddleCenter;
            root.gameObject.SetActive(false);
        }
        public void Open(CardData[] current)
        {
            draft=(CardData[])current.Clone();selectedSlot=0;root.gameObject.SetActive(true);root.SetAsLastSibling();
            foreach(Transform child in content)Destroy(child.gameObject);
            owned=match.availableCards.Where(c=>progression.Owns(c) && c.originalCardArt!=null).GroupBy(c=>c.id).Select(g=>g.First()).ToArray();
            choices=new Button[owned.Length];
            for(int i=0;i<owned.Length;i++)
            {
                var card=owned[i];choices[i]=MakeButton(card.displayName,content,()=>ChooseCard(card.id));
                var art=Rect("Artwork",choices[i].transform).gameObject.AddComponent<RawImage>();art.texture=card.originalCardArt;art.raycastTarget=false;
                art.rectTransform.anchorMin=art.rectTransform.anchorMax=new Vector2(.5f,1);art.rectTransform.pivot=new Vector2(.5f,1);art.rectTransform.anchoredPosition=new Vector2(0,-9);art.rectTransform.sizeDelta=new Vector2(68,104);
                var label=Label("Card name",choices[i].transform,card.displayName,12,Color.white);label.alignment=TextAnchor.MiddleCenter;label.rectTransform.anchorMax=Vector2.right;label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.offsetMin=new Vector2(4,31);label.rectTransform.offsetMax=new Vector2(-4,59);
                var rarity=Label("Finish",choices[i].transform,card.rarity.ToString().ToUpper()+"\n"+CardFoilFinish.Label(card.rarity),9,new Color(.62f,.84f,1));rarity.alignment=TextAnchor.MiddleCenter;rarity.rectTransform.anchorMax=Vector2.right;rarity.rectTransform.anchorMin=Vector2.zero;rarity.rectTransform.offsetMin=new Vector2(3,7);rarity.rectTransform.offsetMax=new Vector2(-3,33);
            }
            content.anchoredPosition=Vector2.zero;status.text="Your battle hand stays unchanged.";Refresh();LateUpdate();
        }
        public void SelectSlot(int index){if(index<0 || index>=draft.Length)return;selectedSlot=index;Refresh();}
        public void ChooseCard(string id)
        {
            if(!IsOpen || draft.Length!=3)return;
            var card=owned.FirstOrDefault(c=>c.id==id);if(card==null)return;
            int existing=Array.FindIndex(draft,c=>c.id==id);
            if(existing>=0)draft[existing]=draft[selectedSlot];
            draft[selectedSlot]=card;Refresh();
        }
        public bool Save()
        {
            if(!IsOpen || progression.Collection==null)return false;
            if(!progression.Collection.SetFeatured(draft.Select(c=>c.id).ToArray(),match.availableCards,out string error)){status.text=error;return false;}
            root.gameObject.SetActive(false);closed?.Invoke();return true;
        }
        public void Cancel(){if(root==null)return;root.gameObject.SetActive(false);closed?.Invoke();}
        void Refresh()
        {
            for(int i=0;i<3;i++)
            {
                slotArt[i].texture=i<draft.Length?draft[i].originalCardArt:null;
                slotLabels[i].text=(i==0?"CENTRE":i==1?"LEFT":"RIGHT")+(i==selectedSlot?"  •":"");
                slots[i].GetComponent<BattleHudGraphic>().surfaceOpacity=i==selectedSlot?.9f:.35f;
                slots[i].GetComponent<BattleHudGraphic>().Tint(i==selectedSlot?new Color(.35f,1,1):new Color(.25f,.4f,.6f));
            }
            for(int i=0;i<choices.Length;i++)
            {
                bool featured=draft.Any(c=>c.id==owned[i].id);choices[i].GetComponent<BattleHudGraphic>().Tint(featured?new Color(.7f,.52f,1):new Color(.2f,.38f,.52f));
            }
            save.interactable=draft.Length==3 && progression.Collection!=null;
        }
        void LateUpdate()
        {
            if(!IsOpen)return;float w=root.rect.width,h=root.rect.height;
            Place(title.rectTransform,20,h-56,w-40,34);Place(instruction.rectTransform,20,h-80,w-40,22);
            bool wide=w>h;float slotX=20,slotWidth=wide?w*.38f:w-40,slotY=wide?110:h-184;
            for(int i=0;i<3;i++)
            {
                float sw=(slotWidth-16)/3;Place((RectTransform)slots[i].transform,slotX+i*(sw+8),slotY,sw,94);
                Place(slotArt[i].rectTransform,(sw-40)/2,25,40,61);Place(slotLabels[i].rectTransform,1,3,sw-2,20);
            }
            float gx=wide?w*.44f:20,gw=wide?w*.56f-20:w-40,gh=wide?h-172:h-288;
            Place(viewport,gx,92,gw,Mathf.Max(80,gh));
            int columns=wide?3:2;grid.constraintCount=columns;grid.cellSize=new Vector2((gw-4-(columns-1)*10)/columns,178);
            content.sizeDelta=new Vector2(0,Mathf.CeilToInt(owned.Length/(float)columns)*188+4);content.offsetMin=new Vector2(0,content.offsetMin.y);content.offsetMax=new Vector2(0,content.offsetMax.y);
            Place(status.rectTransform,16,70,w-32,20);Place((RectTransform)cancel.transform,20,16,(w-50)*.4f,48);Place((RectTransform)save.transform,30+(w-50)*.4f,16,(w-50)*.6f,48);
        }
        Button MakeButton(string name,Transform parent,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(name,parent);var surface=r.gameObject.AddComponent<BattleHudGraphic>();surface.shape=BattleHudGraphic.Shape.Panel;surface.accent=new Color(.3f,.8f,1);surface.surfaceOpacity=.55f;
            r.gameObject.AddComponent<BattleMenuMotion>();var b=r.gameObject.AddComponent<Button>();b.targetGraphic=surface;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(action);return b;
        }
        Text Label(string name,Transform parent,string text,int size,Color colour){var label=Rect(name,parent).gameObject.AddComponent<Text>();label.font=font;label.fontSize=size;label.fontStyle=FontStyle.Bold;label.text=text;label.color=colour;label.raycastTarget=false;return label;}
        static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h)=>Battle3DController.Place(r,x,y,w,h);
        void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
    }
}
