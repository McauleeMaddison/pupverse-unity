using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // Navigation stays in Battle3D so the wallet, collection and arena share one session.
    [DefaultExecutionOrder(50),DisallowMultipleComponent]
    public sealed class BattleHomeScreen : MonoBehaviour
    {
        BattleMatchController match;
        BattleHandSelection hand;
        BattleProgression progression;
        CardPackShop shop;
        BattleHudPresentation hud;
        RectTransform root,hero,modal,walletPanel;
        Text eyebrow,battleSubtitle,cardsSubtitle,packsSubtitle,showcaseHint;
        CanvasGroup menuGroup;
        float openedAt;
        Text title,tagline,wallet,modalTitle,modalBody,motionLabel;
        Button battle,cards,packs,settings,help,back,motion,featured;
        FeaturedCardSelection featuredPicker;
        HomeCardShowcase heroStage;
        Font font;
        bool built;
        static readonly Color Cyan=new Color(.25f,.93f,1);
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public bool SettingsOpen => IsOpen && modal.gameObject.activeSelf && motion.gameObject.activeSelf;
        void Start()
        {
            match=GetComponent<BattleMatchController>();hand=GetComponent<BattleHandSelection>();progression=GetComponent<BattleProgression>();shop=GetComponent<CardPackShop>();hud=hand.hud;
            hand.home=this;
            if(match.cardDisplay!=null && match.cardDisplay.cardFoilShader==null)match.cardDisplay.cardFoilShader=shop.cardFoilShader;
            GameSettings.Initialize();font=match.Battle.resultTitle.font;
            Build();built=true;match.MatchStarted+=Hide;
            if(progression.Collection!=null)progression.Collection.Changed+=RefreshWallet;
            ShowHome();
        }
        void Build()
        {
            root=Rect("PupVerse home",hud.layout.controlsRoot.parent);Stretch(root);
            root.gameObject.AddComponent<BattleMenuBackdrop>();root.GetComponent<BattleMenuBackdrop>().raycastTarget=true;
            menuGroup=root.gameObject.AddComponent<CanvasGroup>();
            eyebrow=Label("Home eyebrow",root,"T H E   C A R D   B A T T L E   U N I V E R S E",9,Cyan);
            title=Label("Home title",root,"PUPVERSE",36,Color.white);
            tagline=Label("Home tagline",root,"YOUR NEXT LEGEND STARTS HERE",10,Cyan);
            walletPanel=Rect("Coin capsule",root);var walletSurface=walletPanel.gameObject.AddComponent<BattleHudGraphic>();walletSurface.shape=BattleHudGraphic.Shape.Panel;walletSurface.accent=new Color(1,.76f,.3f);walletSurface.surfaceOpacity=.65f;walletSurface.raycastTarget=false;
            wallet=Label("Home wallet",walletPanel,"",12,new Color(1,.85f,.5f));Stretch(wallet.rectTransform);wallet.alignment=TextAnchor.MiddleCenter;
            hero=Rect("Home 3D card",root);hero.gameObject.AddComponent<RawImage>().raycastTarget=true;
            heroStage=hero.gameObject.AddComponent<HomeCardShowcase>();heroStage.Initialize(FeaturedCards(),shop.foilShader,shop.cardFoilShader);
            showcaseHint=Label("Showcase hint",root,"",9,new Color(.45f,.64f,.77f));showcaseHint.alignment=TextAnchor.MiddleCenter;
            featured=Button("EDIT FEATURED CARDS   ›",root,OpenFeatured,11);
            featured.GetComponent<BattleHudGraphic>().surfaceOpacity=.22f;
            featuredPicker=gameObject.AddComponent<FeaturedCardSelection>();featuredPicker.Initialize(root.parent,match,progression,font,ShowHome);
            battle=Button("BATTLE ARENA     ›",root,OpenHand,21);battle.GetComponent<BattleMenuMotion>().featured=true;
            battle.GetComponent<BattleHudGraphic>().surfaceOpacity=.86f;
            battleSubtitle=Label("Battle invitation",battle.transform,"BUILD YOUR SIX  /  TAKE THE STACK",9,Cyan);
            battleSubtitle.alignment=TextAnchor.MiddleCenter;
            cards=Button("MY CARDS",root,OpenHand,14);packs=Button("OPEN PACKS",root,OpenPacks,14);
            packs.GetComponent<BattleHudGraphic>().Tint(new Color(.77f,.48f,1));
            cardsSubtitle=Label("Owned count",cards.transform,"",10,Cyan);cardsSubtitle.alignment=TextAnchor.MiddleCenter;
            packsSubtitle=Label("Pack contents",packs.transform,"4 CARDS · 3 COLLECTIONS",9,new Color(.78f,.63f,1));packsSubtitle.alignment=TextAnchor.MiddleCenter;
            settings=Button("SETTINGS",root,OpenSettings,12);help=Button("HOW TO PLAY",root,OpenHelp,12);
            modal=Rect("Home detail page",root);Stretch(modal);modal.gameObject.AddComponent<BattleMenuBackdrop>().opacity=.995f;modal.GetComponent<BattleMenuBackdrop>().raycastTarget=true;
            modalTitle=Label("Page title",modal,"",28,Color.white);modalBody=Label("Page explanation",modal,"",15,new Color(.72f,.81f,.93f));
            motion=Button("",modal,ToggleMotion,15);motionLabel=motion.GetComponentInChildren<Text>();
            back=Button("BACK HOME",modal,ShowHome,14);
        }
        public void ShowHome()
        {
            if(!built || match.IsMatchRunning)return;
            hand.HideForHome();hud.SetVisible(false);root.gameObject.SetActive(true);root.SetAsLastSibling();
            modal.gameObject.SetActive(false);hero.gameObject.SetActive(true);heroStage.SetCards(FeaturedCards());openedAt=Time.unscaledTime;RefreshWallet();Canvas.ForceUpdateCanvases();LateUpdate();
        }
        CardData[] FeaturedCards() => progression.Collection?.Featured(match.availableCards,match.StartingHand)??System.Array.Empty<CardData>();
        public void OpenFeatured(){if(!built || match.IsMatchRunning)return;Hide();featuredPicker.Open(FeaturedCards());}
        public void OpenHand(){if(!built || match.IsMatchRunning)return;Hide();match.EditHand();}
        public void OpenPacks(){if(!built || match.IsMatchRunning)return;Hide();shop.Open(ShowHome);}
        public void OpenSettings()
        {
            modal.gameObject.SetActive(true);hero.gameObject.SetActive(false);motion.gameObject.SetActive(true);
            modalTitle.text="SETTINGS";modalBody.text="Choose how much movement you want.\n\nReduced motion skips card flights and shortens reveals. Your choice is saved on this device.";RefreshMotion();
        }
        public void ToggleMotion(){GameSettings.ReducedMotion=!GameSettings.ReducedMotion;RefreshMotion();}
        void RefreshMotion(){motionLabel.text="REDUCED MOTION  /  "+(GameSettings.ReducedMotion?"ON":"OFF");}
        void OpenHelp()
        {
            modal.gameObject.SetActive(true);hero.gameObject.SetActive(false);motion.gameObject.SetActive(false);
            modalTitle.text="HOW TO PLAY";
            modalBody.text="1  Build a hand of six owned cards.\n\n2  Choose a stat on your turn. Highest total wins.\n\n3  Both played cards join the back of the winner's stack. A draw builds the shared pot.\n\n4  Capture all twelve cards to win coins.\n\n5  Open four-card packs and improve your next hand.";
        }
        void RefreshWallet(){wallet.text=(progression.Collection?.Coins??0)+"  COINS";cardsSubtitle.text=match.availableCards.Count(progression.Owns)+" CARDS OWNED";}
        void Hide(){if(root!=null)root.gameObject.SetActive(false);}
        void OnDestroy()
        {
            if(match!=null)match.MatchStarted-=Hide;
            if(progression?.Collection!=null)progression.Collection.Changed-=RefreshWallet;
            if(featuredPicker!=null)Destroy(featuredPicker);
            if(root!=null)Destroy(root.gameObject);
        }
        void LateUpdate()
        {
            if(!IsOpen)return;
            float w=root.rect.width,h=root.rect.height;bool wide=w>h;
            menuGroup.alpha=GameSettings.ReducedMotion?1:Mathf.Lerp(.2f,1,Mathf.Clamp01((Time.unscaledTime-openedAt)/.28f));
            Place(eyebrow.rectTransform,22,h-28,w-44,16);
            Place(title.rectTransform,20,h-76,w-40,44);Place(tagline.rectTransform,22,h-96,w-44,20);
            Place(walletPanel,w-124,h-136,104,32);
            // The stage keeps its render aspect, with three cards framed above navigation.
            float hh=wide?Mathf.Min(290,h-95):Mathf.Min((w+16)/1.125f,h-426),hw=hh*1.125f;
            float cx=wide?w*.25f:w*.5f;
            Place(hero,cx-hw/2,wide?42:Mathf.Max(286,h-142-hh),hw,hh);
            showcaseHint.gameObject.SetActive(!wide);showcaseHint.text=GameSettings.ReducedMotion?"YOUR FEATURED CARDS":"DRAG TO EXPLORE";Place(showcaseHint.rectTransform,20,hero.anchoredPosition.y+2,w-40,18);
            float x=wide?w*.52f:20,bw=wide?w*.45f:w-40;
            Place((RectTransform)featured.transform,wide?20:x,wide?7:240,wide?w*.45f:bw,44);
            Place((RectTransform)battle.transform,x,154,bw,80);
            Place((RectTransform)cards.transform,x,72,(bw-10)/2,72);Place((RectTransform)packs.transform,x+(bw+10)/2,72,(bw-10)/2,72);
            Place((RectTransform)settings.transform,x,16,(bw-10)/2,44);Place((RectTransform)help.transform,x+(bw+10)/2,16,(bw-10)/2,44);
            Place(battle.GetComponentInChildren<Text>().rectTransform,8,30,bw-16,34);Place(battleSubtitle.rectTransform,8,13,bw-16,18);
            Place(cards.GetComponentInChildren<Text>().rectTransform,5,32,(bw-10)/2-10,28);Place(packs.GetComponentInChildren<Text>().rectTransform,5,32,(bw-10)/2-10,28);
            Place(cardsSubtitle.rectTransform,5,12,(bw-10)/2-10,18);Place(packsSubtitle.rectTransform,5,12,(bw-10)/2-10,18);
            Place(modalTitle.rectTransform,24,h-72,w-48,44);
            Place(modalBody.rectTransform,24,wide?118:180,w-48,h-(wide?208:280));
            Place((RectTransform)motion.transform,24,92,w-48,60);Place((RectTransform)back.transform,24,20,w-48,52);
        }
        Text Label(string name,Transform parent,string text,int size,Color colour)
        {
            var t=Rect(name,parent).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.color=colour;t.raycastTarget=false;return t;
        }
        Button Button(string text,Transform parent,UnityEngine.Events.UnityAction action,int size)
        {
            var r=Rect(text,parent);var surface=r.gameObject.AddComponent<BattleHudGraphic>();surface.shape=BattleHudGraphic.Shape.Panel;surface.accent=Cyan;surface.surfaceOpacity=.5f;
            r.gameObject.AddComponent<BattleMenuMotion>();
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=surface;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(action);
            var label=Label("Label",r,text,size,Color.white);Stretch(label.rectTransform);label.alignment=TextAnchor.MiddleCenter;return b;
        }
        static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h)=>Battle3DController.Place(r,x,y,w,h);
    }
}
