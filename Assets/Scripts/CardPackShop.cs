using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    [DisallowMultipleComponent]
    public sealed class CardPackShop : MonoBehaviour
    {
        public BattleProgression progression;
        public BattleHudPresentation hud;
        public Shader foilShader;
        public Shader cardFoilShader;
        PackFoilPresentation foil;
        RectTransform root,offerRoot,revealRoot,packVisual;
        Text wallet,title,subtitle,cardName,cardInfo,status,actionLabel;
        Button back,action;
        Button[] offers;
        Text[] offerLabels,odds;
        RectTransform[] offerFoils;
        BattleHudGraphic ring;
        Font font;
        int revealIndex;
        bool busy,built;
        string currentPack;
        System.Action closed;
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public bool IsAnimating => busy;
        static readonly Color Muted=new Color(.63f,.73f,.86f);
        static readonly Color Cyan=new Color(.2f,.94f,1);
        void Start() {Build();root.gameObject.SetActive(false);}
        void Build()
        {
            if(built || progression==null || hud==null) return;
            font=progression.match.Battle.resultTitle.font;
            root=Rect("Card pack shop",hud.layout.controlsRoot.parent); Stretch(root);
            root.gameObject.AddComponent<BattleMenuBackdrop>().opacity=.95f;root.GetComponent<BattleMenuBackdrop>().raycastTarget=true;
            title=Label("Pack title",root,"PUPVERSE PACKS",24,Color.white);
            wallet=Label("Coin balance",root,"",12,Cyan);
            subtitle=Label("Pack instructions",root,"Four cards. One new possibility.",12,Muted);
            back=Button("BACK TO HAND",root,Close);
            offerRoot=Rect("Pack choices",root);
            int count=progression.packs.Length;
            offers=new Button[count];offerLabels=new Text[count];odds=new Text[count];offerFoils=new RectTransform[count];
            for(int i=0;i<count;i++)
            {
                int index=i; var pack=progression.packs[i];
                offers[i]=Button(pack.title,offerRoot,()=>Buy(progression.packs[index].id));
                offers[i].GetComponent<BattleHudGraphic>().accent=PackColour(pack.id);
                offerLabels[i]=offers[i].GetComponentInChildren<Text>();
                offerLabels[i].alignment=TextAnchor.UpperLeft;
                var foil=Graphic("Collection foil",offers[i].transform,BattleHudGraphic.Shape.Panel,PackColour(pack.id));
                foil.surfaceOpacity=.8f;offerFoils[i]=foil.rectTransform;
                var emblem=Label("Pack emblem",foil.transform,"PV",16,Color.white);Stretch(emblem.rectTransform);emblem.alignment=TextAnchor.MiddleCenter;
                odds[i]=Label("Rarity chances",offers[i].transform,PlayerCollection.Odds(pack,progression.match.availableCards,progression.rarityWeights),10,Muted);
            }
            status=Label("Pack status",root,"",12,Muted);
            revealRoot=Rect("Pack reveal",root);Stretch(revealRoot);
            ring=Graphic("Opening energy",revealRoot,BattleHudGraphic.Shape.Ring,Cyan);
            packVisual=Rect("3D pack stage",revealRoot);
            packVisual.gameObject.AddComponent<RawImage>();foil=packVisual.gameObject.AddComponent<PackFoilPresentation>();
            foil.Initialize(foilShader,font,cardFoilShader);foil.TearCompleted=BeginTear;
            cardName=Label("Pulled name",revealRoot,"",19,Color.white);
            cardInfo=Label("Pulled stats",revealRoot,"",12,Muted);
            action=Button("REVEAL",root,Next); actionLabel=action.GetComponentInChildren<Text>();
            built=true;
        }
        public void Open(System.Action onClosed=null)
        {
            if(progression.match.IsMatchRunning) return;
            Build(); if(!built) return;
            closed=onClosed;back.GetComponentInChildren<Text>().text=closed==null?"BACK TO HAND":"HOME";
            root.gameObject.SetActive(true);root.SetAsLastSibling();busy=false;
            RefreshWallet();
            var pending=progression.Collection?.Pending;
            if(pending!=null) {currentPack=pending.packId;revealIndex=0;PrepareFoil();if(pending.torn)StartCoroutine(RevealCard());else ShowSealed();}
            else ShowOffers();
            Canvas.ForceUpdateCanvases();LateUpdate();
        }
        public void Close()
        {
            if(busy) return;
            root.gameObject.SetActive(false);var callback=closed;closed=null;callback?.Invoke();
        }
        void ShowOffers()
        {
            title.text="PUPVERSE PACKS";subtitle.text="Choose a collection. Each pack contains four cards.";
            offerRoot.gameObject.SetActive(true);revealRoot.gameObject.SetActive(false);action.gameObject.SetActive(false);
            status.text=progression.SaveError??("Win a match: +"+progression.victoryCoins+" coins.\nDuplicates add copies to your collection.");
            for(int i=0;i<offers.Length;i++)
            {
                var p=progression.packs[i]; offerLabels[i].text=p.title+" PUPS\n"+p.price+" COINS   /   "+p.cardCount+" CARDS";
                offers[i].interactable=progression.Collection!=null && progression.Collection.Coins>=p.price;
            }
            back.interactable=true;
        }
        public bool Buy(string id)
        {
            if(!IsOpen || busy) return false;
            if(!progression.OpenPack(id,out string error)) {status.text=error;return false;}
            currentPack=id;revealIndex=0;RefreshWallet();PrepareFoil();ShowSealed();return true;
        }
        void PrepareFoil()
        {
            var pending=progression.Collection.Pending;
            foil.Prepare(PackColour(currentPack),pending.cardIds.Select(id=>progression.match.availableCards.FirstOrDefault(c=>c.id==id)).ToArray());
        }
        void ShowSealed()
        {
            offerRoot.gameObject.SetActive(false);revealRoot.gameObject.SetActive(true);packVisual.gameObject.SetActive(true);
            title.text=currentPack.ToUpperInvariant()+" PACK";subtitle.text="SWIPE ACROSS THE TOP SEAL";
            cardName.text="TEAR. DISCOVER. EVOLVE.";cardInfo.text="Drag across the foil seam to release your cards.";
            status.text=progression.Collection.Pending.cardIds.Length+" CARDS INSIDE  /  Swipe either direction";
            action.gameObject.SetActive(true);actionLabel.text="OPEN WITHOUT SWIPE";back.interactable=true;busy=false;
            ring.Tint(PackColour(currentPack));ring.Fill(1);LateUpdate();
        }
        void BeginTear()
        {
            if(busy)return;
            if(!progression.Collection.TearOpen(out string error)){status.text=error;PrepareFoil();return;}
            StartCoroutine(OpenAnimation());
        }
        IEnumerator OpenAnimation()
        {
            busy=true;back.interactable=false;action.gameObject.SetActive(false);cardName.text=cardInfo.text=status.text="";
            subtitle.text="BREAKING THE SEAL";
            float duration=GameSettings.ReducedMotion?0:1.1f;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.SmoothStep(0,1,elapsed/duration);foil.Open(t);ring.Animate(t);yield return null;
            }
            foil.Open(1);yield return RevealCard();
        }
        IEnumerator RevealCard()
        {
            var receipt=progression.Collection.Pending;
            if(receipt==null) {ShowOffers();yield break;}
            busy=true;back.interactable=false;action.gameObject.SetActive(false);offerRoot.gameObject.SetActive(false);revealRoot.gameObject.SetActive(true);packVisual.gameObject.SetActive(true);
            string id=receipt.cardIds[revealIndex];var card=progression.match.availableCards.FirstOrDefault(c=>c.id==id);
            var colour=card!=null?RarityColour(card.rarity):Cyan;ring.Tint(colour);ring.Fill(1);
            title.text=currentPack.ToUpperInvariant()+" PACK";subtitle.text="CARD "+(revealIndex+1)+" / "+receipt.cardIds.Length;
            cardName.text=card!=null?card.displayName.ToUpperInvariant():id;
            cardInfo.text=card==null?"Saved in your collection.":card.rarity.ToString().ToUpperInvariant()+"   /   "+(receipt.isNew[revealIndex]?"NEW CARD":"DUPLICATE")+"\n"+
                card.abilityName+"\n"+string.Join("   ",Enumerable.Range(0,5).Select(i=>((CardStat)i).ToString().Substring(0,3).ToUpperInvariant()+" "+card.EffectiveValue((CardStat)i)));
            status.text="OWNED ×"+progression.Collection.OwnedCount(id)+"  /  Added to your collection";
            float duration=GameSettings.ReducedMotion?0:.45f;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.unscaledDeltaTime)
            {
                float t=Mathf.SmoothStep(0,1,elapsed/duration);foil.Reveal(revealIndex,t);ring.Animate(1-t);yield return null;
            }
            foil.Reveal(revealIndex,1);
            busy=false;back.interactable=true;action.gameObject.SetActive(true);
            actionLabel.text=revealIndex+1<receipt.cardIds.Length?"REVEAL NEXT CARD":"KEEP CARDS";
        }
        public void Next()
        {
            if(busy || progression.Collection?.Pending==null) return;
            if(!progression.Collection.Pending.torn){BeginTear();return;}
            if(revealIndex+1<progression.Collection.Pending.cardIds.Length) {revealIndex++;StartCoroutine(RevealCard());}
            else if(progression.Collection.FinishOpening(out string error)) Close();
            else status.text=error;
        }
        void RefreshWallet() {wallet.text="COINS  "+(progression.Collection?.Coins??0);}
        void OnDisable() {StopAllCoroutines();busy=false;if(root!=null)root.gameObject.SetActive(false);}
        void OnDestroy() {if(root!=null)Destroy(root.gameObject);}
        void LateUpdate()
        {
            if(!IsOpen)return;
            float w=root.rect.width,h=root.rect.height;
            bool wide=w>h;
            Place(title.rectTransform,18,h-46,w-36,34);Place(wallet.rectTransform,18,h-68,w-36,20);wallet.alignment=TextAnchor.MiddleRight;
            Place(subtitle.rectTransform,18,h-96,w-36,28);
            Place(offerRoot,18,122,w-36,Mathf.Max(wide?120:246,h-230));
            for(int i=0;i<offers.Length;i++)
            {
                float cell=wide?(w-36-8*(offers.Length-1))/offers.Length: w-36;
                Place((RectTransform)offers[i].transform,wide?i*(cell+8):0,wide?0:offerRoot.rect.height-(i+1)*(offerRoot.rect.height/offers.Length),cell,wide?offerRoot.rect.height:offerRoot.rect.height/offers.Length-8);
                float panelHeight=((RectTransform)offers[i].transform).rect.height;
                float foilHeight=wide?Mathf.Max(38,panelHeight-106):Mathf.Min(114,panelHeight-24);
                float foilWidth=foilHeight*.65f;
                Place(offerFoils[i],wide?(cell-foilWidth)/2:12,wide?100:(panelHeight-foilHeight)/2,foilWidth,foilHeight);
                float textX=wide?10:foilWidth+24;
                Place(offerLabels[i].rectTransform,textX,wide?51:panelHeight*.5f+2,cell-textX-10,44);offerLabels[i].fontSize=14;
                Place(odds[i].rectTransform,textX,wide?6:panelHeight*.5f-52,cell-textX-10,wide?42:50);
                if(!GameSettings.ReducedMotion)offerFoils[i].GetComponent<BattleHudGraphic>().Animate(.25f+.15f*Mathf.Sin(Time.unscaledTime*1.4f+i));
            }
            Place(status.rectTransform,18,68,w-36,44);
            Place((RectTransform)back.transform,18,12,wide?150:(w-44)*.35f,48);
            Place((RectTransform)action.transform,wide?180:26+(w-44)*.35f,12,wide?w-198:(w-44)*.65f,48);
            float ch=wide?Mathf.Min(250,h-100):Mathf.Min(440,h-340),cw=ch*.75f;
            float cx=wide?w*.26f:w*.5f, cy=wide?104:h-96-ch;
            Place(packVisual,cx-cw/2,cy,cw,ch);
            Place(ring.rectTransform,cx-ch*.48f,cy,ch*.96f,ch*.96f);
            Place(cardName.rectTransform,wide?w*.51f:18,wide?h-145:cy-38,wide?w*.46f:w-36,34);
            Place(cardInfo.rectTransform,wide?w*.51f:18,wide?148:cy-114,wide?w*.46f:w-36,wide?90:72);
            cardName.alignment=cardInfo.alignment=TextAnchor.MiddleCenter;
            if(!busy && !GameSettings.ReducedMotion && revealRoot.gameObject.activeSelf)ring.Animate(.18f+.08f*Mathf.Sin(Time.unscaledTime*2));
        }
        static Color PackColour(string id)=>id=="alien"?new Color(.78f,.4f,1):id=="cyber"?new Color(.2f,1,.64f):Cyan;
        public static Color RarityColour(CardRarity rarity)=>rarity==CardRarity.Mythic?new Color(1,.25f,.55f):rarity==CardRarity.Legendary?new Color(1,.76f,.23f):rarity==CardRarity.Epic?new Color(.75f,.45f,1):Cyan;
        Button Button(string name,Transform parent,UnityEngine.Events.UnityAction clicked)
        {
            var r=Rect(name,parent);var surface=r.gameObject.AddComponent<BattleHudGraphic>();surface.shape=BattleHudGraphic.Shape.Panel;surface.accent=Cyan;surface.surfaceOpacity=.5f;
            r.gameObject.AddComponent<BattleMenuMotion>();var b=r.gameObject.AddComponent<Button>();b.targetGraphic=surface;b.onClick.AddListener(clicked);b.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=Label("Label",r,name,12,Color.white);Stretch(label.rectTransform);label.rectTransform.offsetMin=new Vector2(7,4);label.rectTransform.offsetMax=new Vector2(-7,-4);label.alignment=TextAnchor.MiddleCenter;return b;
        }
        Text Label(string name,Transform parent,string value,int size,Color colour){var t=Rect(name,parent).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.color=colour;t.raycastTarget=false;return t;}
        static BattleHudGraphic Graphic(string name,Transform parent,BattleHudGraphic.Shape shape,Color colour){var g=Rect(name,parent).gameObject.AddComponent<BattleHudGraphic>();g.shape=shape;g.accent=colour;g.raycastTarget=false;return g;}
        static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h)=>Battle3DController.Place(r,x,y,w,h);
    }
}
