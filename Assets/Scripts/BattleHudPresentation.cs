using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // Presentation subscribes to resolved results. It never calculates or chooses a winner.
    [DisallowMultipleComponent]
    public sealed class BattleHudPresentation : MonoBehaviour
    {
        public Battle3DController layout;
        [Header("Mobile glass HUD")]
        [Range(.15f,.7f)] public float panelOpacity = .34f;
        [Range(.2f,.8f)] public float arenaShadeOpacity = .52f;
        BattleController battle;
        readonly BattleStatTile[] tiles = new BattleStatTile[5];
        readonly Text[] originalLabels = new Text[5];
        readonly Text[] totals = new Text[2];
        readonly Text[] formulas = new Text[2];
        readonly Text[] names = new Text[2];
        readonly BattleHudGraphic[] bars = new BattleHudGraphic[2];
        readonly BattleHudGraphic[] plates = new BattleHudGraphic[2];
        readonly Text[] plateNames = new Text[2];
        readonly Text[] plateRoles = new Text[2];
        RectTransform header, decoration, comparison, divider;
        Text roundLabel, versus, hint;
        BattleHudGraphic sweep, halo, arenaFade;
        CanvasGroup comparisonGroup;
        BattleResult result;
        float comparisonAt, revealAt = -100;
        int lastPlayer = -1, lastRival = -1;
        bool built, comparing, revealed;
        Font font;
        static readonly Color Cyan = new Color(.35f,.93f,1);
        static readonly Color Violet = new Color(.76f,.58f,1);
        static readonly Color Muted = new Color(.55f,.65f,.79f);
        public bool IsBuilt => built;
        public bool ShowingComparison => built && comparison.gameObject.activeSelf;

        void Start()
        {
            if (layout == null || layout.battleController == null) { enabled=false; return; }
            battle = layout.battleController;
            font = battle.resultTitle.font;
            Build();
            built=true;
            Subscribe();
            Ready();
        }
        void OnEnable() { if(built) { Subscribe(); Ready(); } }
        void Subscribe()
        {
            battle.SelectionReady += Ready;
            battle.ComparisonStarted += Compare;
            battle.WinnerRevealed += Reveal;
        }
        void OnDisable()
        {
            if (!built) return;
            battle.SelectionReady -= Ready;
            battle.ComparisonStarted -= Compare;
            battle.WinnerRevealed -= Reveal;
        }
        void Build()
        {
            RectTransform root=layout.controlsRoot;
            var background=root.GetComponent<Image>();
            if(background!=null) background.color=Color.clear;
            battle.resultPanel.color=Color.clear;
            decoration=Rect("Battle HUD decoration",root);
            Stretch(decoration); decoration.SetAsFirstSibling();
            arenaFade=Graphic("Arena fade",decoration,BattleHudGraphic.Shape.Scrim,Cyan);
            arenaFade.surfaceOpacity=arenaShadeOpacity;
            divider=Graphic("Divider",root,BattleHudGraphic.Shape.Bar,Cyan).rectTransform;
            sweep=divider.GetComponent<BattleHudGraphic>();
            hint=Label("Ready hint",root,"BASE + ABILITY BONUS",10,Muted);
            // The old result Text objects remain authoritative for accessibility and existing callers.
            Style(battle.resultTitle,18,Color.white,TextAnchor.MiddleLeft);
            Style(battle.resultDetail,12,Muted,TextAnchor.MiddleLeft);
            Style(battle.scoreLabel,9,Muted,TextAnchor.MiddleCenter);
            layout.instruction.enabled=false;
            comparison=Rect("Stat comparison",root);
            comparisonGroup=comparison.gameObject.AddComponent<CanvasGroup>();
            for(int i=0;i<2;i++)
            {
                Color c=i==0?Cyan:Violet;
                names[i]=Label(i==0?"Player name":"Rival name",comparison,"",11,c);
                totals[i]=Label(i==0?"Player total":"Rival total",comparison,"",26,Color.white);
                formulas[i]=Label(i==0?"Player calculation":"Rival calculation",comparison,"",11,Muted);
                bars[i]=Graphic(i==0?"Player meter":"Rival meter",comparison,BattleHudGraphic.Shape.Bar,c);
            }
            versus=Label("Comparison versus",comparison,"VS",10,Muted);
            header=Rect("Arena identity",root.parent as RectTransform);
            header.anchorMin=Vector2.zero; header.anchorMax=Vector2.one; header.offsetMin=header.offsetMax=Vector2.zero;
            roundLabel=Label("Round",header,"",10,Muted);
            for(int i=0;i<2;i++)
            {
                Color c=i==0?Cyan:Violet;
                plates[i]=Graphic(i==0?"Player badge":"Rival badge",header,BattleHudGraphic.Shape.Panel,c);
                plates[i].surfaceOpacity=panelOpacity;
                plateNames[i]=Label("Name",plates[i].rectTransform,(i==0?battle.playerCard:battle.opponentCard).data.displayName.ToUpperInvariant(),16,Color.white);
                plateRoles[i]=Label("Role",plates[i].rectTransform,i==0?"YOU  /  READY":"RIVAL  /  READY",9,c);
            }
            halo=Graphic("Clash emblem",header,BattleHudGraphic.Shape.Ring,Cyan);
            var emblem=Graphic("Clash symbol",halo.rectTransform,BattleHudGraphic.Shape.Icon,Cyan);
            emblem.stat=CardStat.Power; Stretch(emblem.rectTransform); emblem.rectTransform.offsetMin=new Vector2(10,10); emblem.rectTransform.offsetMax=new Vector2(-10,-10);
            for(int i=0;i<5;i++) BuildTile(i);
        }
        void BuildTile(int i)
        {
            Button button=battle.statButtons[i];
            originalLabels[i]=button.GetComponentInChildren<Text>();
            originalLabels[i].enabled=false;
            button.transition=Selectable.Transition.None;
            button.targetGraphic.color=Color.clear;
            RectTransform visual=Rect("Neon stat tile",button.transform as RectTransform);
            Stretch(visual);
            var tile=button.gameObject.AddComponent<BattleStatTile>();
            tiles[i]=tile; tile.index=i; tile.button=button; tile.visual=visual;
            tile.group=visual.gameObject.AddComponent<CanvasGroup>();
            Color accent=BattleCardEffects.StatColor((CardStat)i);
            tile.panel=Graphic("Tile surface",visual,BattleHudGraphic.Shape.Panel,accent); Stretch(tile.panel.rectTransform);
            tile.panel.openFrame=true; tile.panel.surfaceOpacity=panelOpacity;
            var icon=Graphic("Stat symbol",visual,BattleHudGraphic.Shape.Icon,accent); icon.stat=(CardStat)i; tile.icon=icon.rectTransform;
            Place(icon.rectTransform,12,19,27,27);
            var name=Label("Stat name",visual,((CardStat)i).ToString().ToUpperInvariant(),11,new Color(.85f,.9f,1));
            name.rectTransform.anchorMin=new Vector2(0,1); name.rectTransform.anchorMax=Vector2.one;
            name.rectTransform.pivot=new Vector2(0,1); name.rectTransform.anchoredPosition=new Vector2(48,-8); name.rectTransform.sizeDelta=new Vector2(-56,16);
            int value=battle.playerCard.data.EffectiveValue((CardStat)i), bonus=battle.playerCard.data.abilityBoosts.Get((CardStat)i);
            var number=Label("Stat value",visual,value.ToString(),24,Color.white);
            Place(number.rectTransform,48,5,62,34);
            var boost=Label("Ability bonus",visual,bonus>0?"+"+bonus+" BOOST":"BASE",9,bonus>0?accent:Muted);
            boost.rectTransform.anchorMin=boost.rectTransform.anchorMax=Vector2.right;
            boost.rectTransform.pivot=Vector2.right; boost.rectTransform.anchoredPosition=new Vector2(-12,15); boost.rectTransform.sizeDelta=new Vector2(64,15);
            boost.alignment=TextAnchor.MiddleRight;
            tile.number=number; tile.bonus=boost;
        }
        void Ready()
        {
            comparing=revealed=false;
            comparison.gameObject.SetActive(false);
            battle.resultDetail.enabled=true;
            battle.resultDetail.text="Tap a stat. Highest total takes the round.";
            battle.resultTitle.color=Color.white;
            for(int i=0;i<5;i++) { tiles[i].resolving=tiles[i].selected=false; originalLabels[i].enabled=false; }
            roundLabel.text="P U P V E R S E     /     ROUND "+(battle.Wins+battle.Losses+battle.Draws+1).ToString("00");
            plateRoles[0].text="YOU  /  READY"; plateRoles[1].text="RIVAL  /  READY";
            hint.text="BASE + ABILITY BONUS";
            sweep.Tint(Cyan); sweep.Fill(1);
        }
        void Compare(BattleResult value)
        {
            result=value; comparing=true; revealed=false; comparisonAt=Time.unscaledTime;
            lastPlayer=lastRival=-1;
            comparison.gameObject.SetActive(true); comparisonGroup.alpha=1;
            battle.resultDetail.enabled=false;
            for(int i=0;i<5;i++) { tiles[i].resolving=true; tiles[i].selected=i==(int)value.Stat; }
            names[0].text=battle.playerCard.data.displayName.ToUpperInvariant(); names[1].text=battle.opponentCard.data.displayName.ToUpperInvariant();
            formulas[0].text=value.PlayerBase+" BASE  +  "+value.PlayerBoost+" BONUS";
            formulas[1].text=value.OpponentBase+" BASE  +  "+value.OpponentBoost+" BONUS";
            hint.text=value.Stat.ToString().ToUpperInvariant()+" SELECTED";
            sweep.Tint(BattleCardEffects.StatColor(value.Stat));
            plateRoles[0].text=plateRoles[1].text="COMPARING";
            Count(GameSettings.ReducedMotion ? 1 : 0);
        }
        void Reveal(BattleResult value)
        {
            revealed=true; revealAt=Time.unscaledTime; Count(1);
            Color c=value.Winner==BattleWinner.Opponent?Violet:Cyan;
            sweep.Tint(c); halo.Tint(c);
            plateRoles[0].text=value.Winner==BattleWinner.Draw?"DRAW":value.Winner==BattleWinner.Player?"ROUND WINNER":"ROUND LOST";
            plateRoles[1].text=value.Winner==BattleWinner.Draw?"DRAW":value.Winner==BattleWinner.Opponent?"ROUND WINNER":"ROUND LOST";
            hint.text=value.Winner==BattleWinner.Draw?"MATCHED TOTALS":"NEXT ROUND AFTER THE CLASH";
        }
        void Count(float t)
        {
            int p=Mathf.RoundToInt(result.PlayerTotal*t), r=Mathf.RoundToInt(result.OpponentTotal*t);
            if(p!=lastPlayer) { totals[0].text=p.ToString(); lastPlayer=p; }
            if(r!=lastRival) { totals[1].text=r.ToString(); lastRival=r; }
            float max=Mathf.Max(1,Mathf.Max(result.PlayerTotal,result.OpponentTotal));
            bars[0].Fill(result.PlayerTotal/max*t); bars[1].Fill(result.OpponentTotal/max*t);
        }
        void LateUpdate()
        {
            if(!built) return;
            bool reduced=GameSettings.ReducedMotion;
            float elapsed=Time.unscaledTime-comparisonAt;
            if(comparing && !revealed)
            {
                Count(reduced?1:Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,battle.comparisonDuration*.85f)));
                sweep.Fill(reduced?1:Mathf.Clamp01(elapsed/Mathf.Max(.01f,battle.comparisonDuration)));
            }
            else sweep.Fill(1);
            float pulse=reduced?0:Mathf.Max(0,1-(Time.unscaledTime-revealAt)/.65f);
            comparison.localScale=Vector3.one*(1+pulse*.025f);
            halo.Animate(pulse);
            halo.Fill(reduced?1:.65f+.15f*Mathf.Sin(Time.unscaledTime*1.2f));
            for(int i=0;i<2;i++) plates[i].Animate(revealed && (result.Winner==(i==0?BattleWinner.Player:BattleWinner.Opponent))?.45f+pulse*.5f:.08f);
            Layout();
        }
        void Layout()
        {
            float width=layout.controlsRoot.rect.width, height=layout.controlsRoot.rect.height;
            bool portrait=layout.CurrentLayout.Portrait;
            Place(arenaFade.rectTransform,0,0,width,height+72);
            Place(hint.rectTransform,174,7,width-192,16); hint.alignment=TextAnchor.MiddleRight;
            Place(divider,18,29,width-36,1);
            Rect compareRect=layout.CurrentLayout.ComparisonRect;
            Place(comparison,compareRect.x,compareRect.y,compareRect.width,compareRect.height);
            float half=(width-56)*.5f;
            for(int i=0;i<2;i++)
            {
                float x=i==0?0:width-32-half;
                Place(names[i].rectTransform,x,45,half,14);
                Place(totals[i].rectTransform,x,13,half,33);
                Place(formulas[i].rectTransform,x,1,half,14);
                Place(bars[i].rectTransform,x,0,half,2);
                names[i].alignment=totals[i].alignment=formulas[i].alignment=i==0?TextAnchor.MiddleLeft:TextAnchor.MiddleRight;
            }
            Place(versus.rectTransform,(width-32)*.5f-12,23,24,22); versus.alignment=TextAnchor.MiddleCenter;
            float headerWidth=header.rect.width, headerHeight=header.rect.height;
            float badgeWidth=portrait?(headerWidth-92)/2:Mathf.Min(200,(headerWidth-92)/2);
            Place(roundLabel.rectTransform,18,headerHeight-20,headerWidth-36,18); roundLabel.alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<2;i++)
            {
                Place(plates[i].rectTransform,i==0?18:headerWidth-18-badgeWidth,headerHeight-72,badgeWidth,44);
                Place(plateNames[i].rectTransform,12,19,badgeWidth-24,24);
                Place(plateRoles[i].rectTransform,12,5,badgeWidth-24,13);
                plateNames[i].alignment=plateRoles[i].alignment=i==0?TextAnchor.MiddleLeft:TextAnchor.MiddleRight;
            }
            Place(halo.rectTransform,headerWidth*.5f-21,headerHeight-70,42,42);
        }
        Text Label(string name,RectTransform parent,string value,int size,Color color)
        {
            var rect=Rect(name,parent); var text=rect.gameObject.AddComponent<Text>();
            text.font=font; text.text=value; Style(text,size,color,TextAnchor.MiddleLeft); return text;
        }
        static void Style(Text text,int size,Color color,TextAnchor alignment)
        {
            text.fontSize=size; text.fontStyle=FontStyle.Bold; text.color=color; text.alignment=alignment;
            text.resizeTextForBestFit=false; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
            text.raycastTarget=false;
        }
        static BattleHudGraphic Graphic(string name,RectTransform parent,BattleHudGraphic.Shape shape,Color accent)
        {
            var graphic=Rect(name,parent).gameObject.AddComponent<BattleHudGraphic>();
            graphic.shape=shape; graphic.accent=accent; graphic.raycastTarget=false; return graphic;
        }
        static RectTransform Rect(string name,RectTransform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false); return rect;
        }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        static void Place(RectTransform r,float x,float y,float w,float h) { Battle3DController.Place(r,x,y,w,h); }
    }
}
