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
        RectTransform root,hero,modal;
        Text title,tagline,wallet,caption,modalTitle,modalBody,motionLabel;
        Button battle,cards,packs,settings,help,back,motion;
        PackFoilPresentation heroStage;
        Font font;
        bool built;
        static readonly Color Cyan=new Color(.25f,.93f,1);
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public bool SettingsOpen => IsOpen && modal.gameObject.activeSelf && motion.gameObject.activeSelf;
        void Start()
        {
            match=GetComponent<BattleMatchController>();hand=GetComponent<BattleHandSelection>();progression=GetComponent<BattleProgression>();shop=GetComponent<CardPackShop>();hud=hand.hud;
            hand.home=this;GameSettings.Initialize();font=match.Battle.resultTitle.font;
            Build();built=true;match.MatchStarted+=Hide;
            if(progression.Collection!=null)progression.Collection.Changed+=RefreshWallet;
            ShowHome();
        }
        void Build()
        {
            root=Rect("PupVerse home",hud.layout.controlsRoot.parent);Stretch(root);
            root.gameObject.AddComponent<Image>().color=new Color(.012f,.022f,.058f,.91f);
            title=Label("Home title",root,"PUPVERSE",38,Color.white);
            tagline=Label("Home tagline",root,"COLLECT.  COMPETE.  EVOLVE.",11,Cyan);
            wallet=Label("Home wallet",root,"",12,Cyan);wallet.alignment=TextAnchor.MiddleRight;
            hero=Rect("Home 3D card",root);hero.gameObject.AddComponent<RawImage>().raycastTarget=false;
            heroStage=hero.gameObject.AddComponent<PackFoilPresentation>();heroStage.Initialize(shop.foilShader,font);
            heroStage.Prepare(Cyan,match.StartingHand.Take(4).ToArray());heroStage.Reveal(0,1);
            caption=Label("Home invitation",root,"BUILD YOUR SIX. TAKE THE STACK.",12,Color.white);caption.alignment=TextAnchor.MiddleCenter;
            battle=Button("BATTLE ARENA",root,OpenHand,19);
            cards=Button("MY CARDS",root,OpenHand,14);packs=Button("OPEN PACKS",root,OpenPacks,14);
            settings=Button("SETTINGS",root,OpenSettings,12);help=Button("HOW TO PLAY",root,OpenHelp,12);
            modal=Rect("Home detail page",root);Stretch(modal);modal.gameObject.AddComponent<Image>().color=new Color(.015f,.028f,.07f,.99f);
            modalTitle=Label("Page title",modal,"",28,Color.white);modalBody=Label("Page explanation",modal,"",15,new Color(.72f,.81f,.93f));
            motion=Button("",modal,ToggleMotion,15);motionLabel=motion.GetComponentInChildren<Text>();
            back=Button("BACK HOME",modal,ShowHome,14);
        }
        public void ShowHome()
        {
            if(!built || match.IsMatchRunning)return;
            hand.HideForHome();hud.SetVisible(false);root.gameObject.SetActive(true);root.SetAsLastSibling();
            modal.gameObject.SetActive(false);hero.gameObject.SetActive(true);RefreshWallet();Canvas.ForceUpdateCanvases();LateUpdate();
        }
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
        void RefreshWallet(){wallet.text=(progression.Collection?.Coins??0)+" COINS";}
        void Hide(){if(root!=null)root.gameObject.SetActive(false);}
        void OnDestroy()
        {
            if(match!=null)match.MatchStarted-=Hide;
            if(progression?.Collection!=null)progression.Collection.Changed-=RefreshWallet;
            if(root!=null)Destroy(root.gameObject);
        }
        void LateUpdate()
        {
            if(!IsOpen)return;
            float w=root.rect.width,h=root.rect.height;bool wide=w>h;
            Place(title.rectTransform,20,h-64,w-40,48);Place(tagline.rectTransform,22,h-86,w-44,22);
            Place(wallet.rectTransform,20,h-112,w-40,22);
            float hh=wide?Mathf.Min(270,h-100):Mathf.Max(120,Mathf.Min(400,h-420)),hw=hh*.75f;
            float cx=wide?w*.25f:w*.5f;
            Place(hero,cx-hw/2,wide?62:h-140-hh,hw,hh);
            float x=wide?w*.52f:20,bw=wide?w*.45f:w-40;
            Place(caption.rectTransform,wide?20:x,wide?16:242,wide?w*.45f:bw,28);
            Place((RectTransform)battle.transform,x,164,bw,64);
            Place((RectTransform)cards.transform,x,88,(bw-10)/2,64);Place((RectTransform)packs.transform,x+(bw+10)/2,88,(bw-10)/2,64);
            Place((RectTransform)settings.transform,x,20,(bw-10)/2,48);Place((RectTransform)help.transform,x+(bw+10)/2,20,(bw-10)/2,48);
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
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=surface;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(action);
            var label=Label("Label",r,text,size,Color.white);Stretch(label.rectTransform);label.alignment=TextAnchor.MiddleCenter;return b;
        }
        static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h)=>Battle3DController.Place(r,x,y,w,h);
    }
}
