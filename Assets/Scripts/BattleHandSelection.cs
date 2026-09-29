using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // A local loadout contains IDs only; match captures never change the saved collection.
    [DisallowMultipleComponent]
    public sealed class BattleHandSelection : MonoBehaviour
    {
        public BattleMatchController match;
        public BattleHudPresentation hud;
        public BattleProgression progression;
        public CardPackShop packShop;
        public BattleHomeScreen home;
        Button homeButton;
        readonly System.Collections.Generic.List<GameObject> collectionEntries=new System.Collections.Generic.List<GameObject>();
        Button packs;
        int ownedTotal;
        const string SaveKey="Pupverse.StartingHand.v1";
        [Serializable] sealed class SavedHand { public string[] ids; }
        CardData[] cards, hand;
        readonly Button[] slots=new Button[6];
        readonly RawImage[] slotArt=new RawImage[6];
        readonly Text[] slotNames=new Text[6];
        RectTransform root, content, viewport, slotArea;
        CanvasGroup group;
        Text title, description, detail, outcome;
        Button start, earlier, later, edit;
        Font font;
        int selected;
        bool built, results;
        float openedAt;
        readonly Color cyan=new Color(.35f,.93f,1);
        public bool IsOpen => built && root.gameObject.activeSelf;
        public CardData[] SelectedHand => hand==null?Array.Empty<CardData>():(CardData[])hand.Clone();

        void Start()
        {
            if(match==null || hud==null || hud.layout==null) { enabled=false; return; }
            cards=(match.availableCards??Array.Empty<CardData>()).Where(c=>c!=null && c.originalCardArt!=null)
                .GroupBy(c=>c.id).Select(g=>g.First()).OrderBy(c=>c.series).ThenBy(c=>c.displayName).ToArray();
            hand=match.StartingHand;
            try
            {
                var saved=JsonUtility.FromJson<SavedHand>(PlayerPrefs.GetString(SaveKey,""));
                if(saved?.ids?.Length==6)
                {
                    var restored=saved.ids.Select(id=>cards.FirstOrDefault(c=>c.id==id)).ToArray();
                    if(Valid(restored)) hand=restored;
                }
            }
            catch(ArgumentException) { /* An old or malformed local save falls back to the scene loadout. */ }
            if(progression!=null && hand.Any(c=>!progression.Owns(c)))
                hand=cards.Where(progression.Owns).Take(6).ToArray();
            font=match.Battle.resultTitle.font;
            Build(); built=true; Subscribe();
            if(match.IsMatchRunning) Hide(); else Show();
        }
        void OnEnable() { if(built) { Subscribe(); if(!match.IsMatchRunning) Show(); } }
        void Subscribe() { if(progression?.Collection!=null) progression.Collection.Changed+=RefreshOwnership; match.MatchStarted+=Hide; match.MatchEnded+=End; match.HandSelectionOpened+=Show; }
        void OnDisable()
        {
            if(!built) return;
            if(progression?.Collection!=null) progression.Collection.Changed-=RefreshOwnership;
            match.MatchStarted-=Hide; match.MatchEnded-=End; match.HandSelectionOpened-=Show;
            if(root!=null) root.gameObject.SetActive(false);
            if(hud!=null) hud.SetVisible(true);
        }
        void OnDestroy() { if(root!=null) Destroy(root.gameObject); }
        public void HideForHome(){if(root!=null)root.gameObject.SetActive(false);}
        public bool SelectCard(CardData card)
        {
            if(hand.Length!=6 || !IsOpen || results || (packShop!=null && packShop.IsOpen) || Array.IndexOf(cards,card)<0 || (progression!=null && !progression.Owns(card))) return false;
            int existing=Array.IndexOf(hand,card);
            if(existing>=0) hand[existing]=hand[selected];
            hand[selected]=card; Refresh(); return true;
        }
        public void SelectSlot(int index) { if(index<0 || index>=6) return; selected=index; Refresh(); }
        public void MoveSelected(int direction)
        {
            if(hand.Length!=6)return;
            int next=selected+direction;
            if(next<0 || next>=6 || match.IsMatchRunning) return;
            var temp=hand[next]; hand[next]=hand[selected]; hand[selected]=temp; selected=next; Refresh();
        }
        public void BeginMatch()
        {
            if(results || (packShop!=null && packShop.IsOpen) || !Valid(hand) || !match.SelectHand(hand)) return;
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new SavedHand { ids=hand.Select(c=>c.id).ToArray() }));
            PlayerPrefs.Save(); match.StartMatch();
        }
        bool Valid(CardData[] value) => value!=null && value.Length==6 && value.All(c=>c!=null && Array.IndexOf(cards,c)>=0 && (progression==null || progression.Owns(c))) && value.Select(c=>c.id).Distinct().Count()==6;
        void Show()
        {
            RefreshOwnership();
            results=false; root.gameObject.SetActive(true); root.SetAsLastSibling(); hud.SetVisible(false); openedAt=Time.unscaledTime;
            title.text="BUILD YOUR SIX"; description.text=progression?.SaveError??"Tap a slot, then an owned card below.\nOpen packs to grow your collection.";
            slotArea.gameObject.SetActive(true); viewport.gameObject.SetActive(true); start.gameObject.SetActive(true);
            earlier.gameObject.SetActive(true); later.gameObject.SetActive(true); detail.gameObject.SetActive(true);
            outcome.gameObject.SetActive(false); edit.gameObject.SetActive(false); Refresh();
        }
        void Hide() { root.gameObject.SetActive(false); hud.SetVisible(true); }
        void End(BattleMatchWinner winner)
        {
            Show(); results=true; title.text=winner==BattleMatchWinner.Player?"MATCH VICTORY":winner==BattleMatchWinner.Rival?"MATCH DEFEAT":"MATCH DRAW";
            description.text="YOU  "+match.PlayerCardCount+" CARDS     /     RIVAL  "+match.RivalCardCount+" CARDS";
            slotArea.gameObject.SetActive(false); viewport.gameObject.SetActive(false); start.gameObject.SetActive(false);
            earlier.gameObject.SetActive(false); later.gameObject.SetActive(false); detail.gameObject.SetActive(false);
            outcome.gameObject.SetActive(true); edit.gameObject.SetActive(true);
            outcome.text=winner==BattleMatchWinner.Draw?"All 12 cards reached the draw pot.\nBuild your hand and try again.":"All 12 cards captured.\nYour saved starting hand is ready for a rematch.";
            if(progression!=null) outcome.text+="\n\n+"+progression.LastReward+" COINS  /  Wallet "+(progression.Collection?.Coins??0);
        }
        void Refresh()
        {
            if(!built) return;
            for(int i=0;i<6;i++)
            {
                if(i>=hand.Length) {slotNames[i].text="EMPTY";continue;}
                slotArt[i].texture=hand[i]?.originalCardArt;
                slotNames[i].text=(i+1)+"  "+(hand[i]!=null?hand[i].displayName:"EMPTY");
                slots[i].GetComponent<BattleHudGraphic>().Animate(i==selected?1:0);
            }
            var c=selected<hand.Length?hand[selected]:null;
            detail.text=c==null?"Select a card":c.displayName+"  /  "+c.abilityName+"\n"+string.Join("   ",Enumerable.Range(0,5).Select(i=>((CardStat)i).ToString().Substring(0,3).ToUpperInvariant()+" "+c.EffectiveValue((CardStat)i)));
            earlier.interactable=selected>0; later.interactable=selected<5; start.interactable=Valid(hand);
        }
        void Build()
        {
            root=Rect("Six-card hand builder",hud.layout.controlsRoot.parent);
            root.anchorMin=Vector2.zero; root.anchorMax=Vector2.one; root.offsetMin=root.offsetMax=Vector2.zero;
            group=root.gameObject.AddComponent<CanvasGroup>();
            var shade=root.gameObject.AddComponent<BattleMenuBackdrop>();shade.opacity=.91f;shade.raycastTarget=true;
            title=Label("Title",root,"",24,Color.white);
            description=Label("Instructions",root,"",12,new Color(.65f,.76f,.86f));
            slotArea=Rect("Starting order",root);
            for(int i=0;i<6;i++)
            {
                int slot=i; slots[i]=Button("Slot "+(i+1),slotArea,()=>SelectSlot(slot));
                slotArt[i]=Rect("Artwork",slots[i].transform).gameObject.AddComponent<RawImage>(); slotArt[i].raycastTarget=false;
                slotNames[i]=Label("Card name",slots[i].transform,"",10,Color.white);
            }
            earlier=TextButton("Move earlier",root,()=>MoveSelected(-1)); later=TextButton("Move later",root,()=>MoveSelected(1));
            detail=Label("Selected card stats",root,"",11,cyan);
            viewport=Rect("Card collection",root); viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>(); hit.color=new Color(0,0,0,.01f);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.viewport=viewport;
            content=Rect("Cards",viewport); content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one; content.pivot=new Vector2(.5f,1); scroll.content=content;
            var grid=content.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=2; grid.spacing=new Vector2(8,8);
            foreach(var card in cards)
            {
                var entry=Button(card.displayName,content,()=>SelectCard(card));
                collectionEntries.Add(entry.gameObject);
                var art=Rect("Artwork",entry.transform).gameObject.AddComponent<RawImage>(); art.texture=card.originalCardArt; art.raycastTarget=false;
                Place(art.rectTransform,8,8,47,72);
                var name=Label("Name",entry.transform,card.displayName,12,Color.white); Place(name.rectTransform,62,52,102,30);
                var series=Label("Series",entry.transform,card.series+"\n"+card.rarity,10,cyan); Place(series.rectTransform,62,14,102,36);
            }
            homeButton=TextButton("HOME",root,()=>home.ShowHome());
            start=TextButton("START MATCH",root,BeginMatch);
            packs=TextButton("PACKS",root,()=>packShop.Open());
            packs.gameObject.SetActive(packShop!=null);
            outcome=Label("Match result",root,"",18,Color.white);
            edit=TextButton("EDIT HAND / PLAY AGAIN",root,()=>match.EditHand());
        }
        void LateUpdate()
        {
            if(!IsOpen) return;
            group.alpha=GameSettings.ReducedMotion?1:Mathf.Clamp01((Time.unscaledTime-openedAt)/.24f);
            float w=Mathf.Min(540,root.rect.width-24), x=(root.rect.width-w)/2, h=root.rect.height;
            homeButton.gameObject.SetActive(home!=null);
            if(root.rect.width>root.rect.height)
            {
                float left=root.rect.width*.46f, right=root.rect.width-left-36;
                Place(title.rectTransform,12,h-38,left-76,32);Place((RectTransform)homeButton.transform,left-56,h-48,68,44); Place(description.rectTransform,12,h-82,left,42);
                float cell=(left-16)/3;
                Place(slotArea,12,h-264,left,176);
                for(int i=0;i<6;i++)
                {
                    Place(slots[i].transform as RectTransform,(i%3)*(cell+8),i<3?92:0,cell,84);
                    Place(slotArt[i].rectTransform,(cell-37)/2,24,37,56); Place(slotNames[i].rectTransform,4,3,cell-8,20);
                }
                Place(earlier.transform as RectTransform,12,70,left/2-4,44); Place(later.transform as RectTransform,16+left/2,70,left/2-4,44);
                Place(detail.rectTransform,12,16,left,48);
                Place(viewport,left+24,70,right,h-82);
                content.GetComponent<GridLayoutGroup>().cellSize=new Vector2(right,88);
                content.GetComponent<GridLayoutGroup>().constraintCount=1;
                content.sizeDelta=new Vector2(0,Mathf.Max(0,ownedTotal*96-8));
                Place(start.transform as RectTransform,left+24,12,right*.55f-4,48);
                Place(packs.transform as RectTransform,left+28+right*.55f,12,right*.45f-4,48);
                Place(outcome.rectTransform,24,h*.42f,root.rect.width-48,100); Place(edit.transform as RectTransform,24,20,root.rect.width*.52f,52);
                if(results)Place(packs.transform as RectTransform,root.rect.width*.56f,20,root.rect.width*.4f,52);
                return;
            }
            content.GetComponent<GridLayoutGroup>().constraintCount=2;
            Place(title.rectTransform,x,h-42,w-80,34);Place((RectTransform)homeButton.transform,x+w-68,h-48,68,44); Place(description.rectTransform,x,h-88,w,42);
            float sw=(w-16)/3;
            Place(slotArea,x,h-270,w,176);
            for(int i=0;i<6;i++)
            {
                Place(slots[i].transform as RectTransform,(i%3)*(sw+8),i<3?92:0,sw,84);
                Place(slotArt[i].rectTransform,(sw-37)/2,24,37,56); Place(slotNames[i].rectTransform,4,3,sw-8,20);
                slotNames[i].alignment=TextAnchor.MiddleCenter;
            }
            Place(earlier.transform as RectTransform,x,h-318,w/2-4,44); Place(later.transform as RectTransform,x+w/2+4,h-318,w/2-4,44);
            Place(detail.rectTransform,x,h-368,w,44);
            Place(viewport,x,66,w,Mathf.Max(44,h-444));
            var grid=content.GetComponent<GridLayoutGroup>(); grid.cellSize=new Vector2((w-8)/2,88);
            content.sizeDelta=new Vector2(0,Mathf.Max(0,Mathf.Ceil(ownedTotal/2f)*96-8));
            Place(start.transform as RectTransform,x,10,w*.55f-4,48);
            Place(packs.transform as RectTransform,x+w*.55f+4,10,w*.45f-4,48);
            Place(outcome.rectTransform,x,h*.42f,w,160); Place(edit.transform as RectTransform,x,h*.3f,w,52);
        }
        void RefreshOwnership()
        {
            if(!built)return;
            ownedTotal=0;
            for(int i=0;i<cards.Length;i++)
            {
                bool owned=progression==null || progression.Owns(cards[i]);
                collectionEntries[i].SetActive(owned);if(owned)ownedTotal++;
            }
            if(progression!=null)
            {
                packs.GetComponentInChildren<Text>().text="PACKS / "+(progression.Collection?.Coins??0)+" COINS";
                if(progression.SaveError!=null) description.text=progression.SaveError;
            }
            Refresh();
        }
        Button TextButton(string name,Transform parent,UnityEngine.Events.UnityAction action)
        {
            var button=Button(name,parent,action); var text=Label("Label",button.transform,name,12,Color.white);
            text.alignment=TextAnchor.MiddleCenter; text.rectTransform.anchorMin=Vector2.zero; text.rectTransform.anchorMax=Vector2.one; text.rectTransform.offsetMin=new Vector2(6,3); text.rectTransform.offsetMax=new Vector2(-6,-3); return button;
        }
        Button Button(string name,Transform parent,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(name,parent); var graphic=r.gameObject.AddComponent<BattleHudGraphic>(); graphic.shape=BattleHudGraphic.Shape.Panel; graphic.accent=cyan; graphic.surfaceOpacity=.45f; graphic.raycastTarget=true;
            r.gameObject.AddComponent<BattleMenuMotion>();var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=graphic; b.onClick.AddListener(action); b.navigation=new Navigation {mode=Navigation.Mode.None}; return b;
        }
        Text Label(string name,Transform parent,string value,int size,Color color)
        {
            var t=Rect(name,parent).gameObject.AddComponent<Text>(); t.text=value; t.font=font; t.fontSize=size; t.color=color; t.fontStyle=FontStyle.Bold; t.raycastTarget=false; return t;
        }
        static RectTransform Rect(string name,Transform parent) { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); return r; }
        static void Place(RectTransform r,float x,float y,float w,float h) => Battle3DController.Place(r,x,y,w,h);
    }
}
