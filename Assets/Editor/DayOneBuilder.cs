using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Pupverse.Editor
{
    public static class DayOneBuilder
    {
        const string Root = "Assets/";
        static Font font;
        static Sprite rounded, disc, glow, ring;
        static CardData raven, brooklyn;
        [Serializable] class WebAbility { public string name, description; }
        [Serializable] class WebCard
        {
            public string id, name, series, element, rarity, frontImage;
            public int year;
            public StatBlock stats, abilityBoosts;
            public WebAbility ability;
        }
        [Serializable] class WebCards { public WebCard[] cards; }

        [MenuItem("Pupverse/Day One/Build starter scenes (replaces generated scenes)")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild starter scenes?",
                "This replaces MainMenu, Battle and generated prefabs. Save custom scenes under new names first.", "Rebuild", "Cancel")) return;
            Directory.CreateDirectory(Root + "Art/Generated");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = Shape("Rounded", 64, (x,y) => {
                float qx = Mathf.Max(Mathf.Abs(x) - .7f, 0), qy = Mathf.Max(Mathf.Abs(y) - .7f, 0);
                return Mathf.Clamp01((.29f - Mathf.Sqrt(qx*qx+qy*qy)) * 64);
            }, new Vector4(16,16,16,16));
            disc = Shape("Disc", 128, (x,y) => Mathf.Clamp01((1-Mathf.Sqrt(x*x+y*y))*64));
            glow = Shape("Glow", 128, (x,y) => Mathf.Pow(Mathf.Clamp01(1-Mathf.Sqrt(x*x+y*y)),2));
            ring = Shape("Ring", 256, (x,y) => Mathf.Clamp01((.012f-Mathf.Abs(Mathf.Sqrt(x*x+y*y)-.88f))*160));
            ImportCards(); Configure();
            BuildMenu(); BuildBattle();
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(Root+"Scenes/MainMenu.unity",true),
                new EditorBuildSettingsScene(Root+"Scenes/Battle.unity",true)
            };
            EditorSceneManager.OpenScene(Root+"Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("PUPVERSE_BUILD_OK: two scenes, real cards, iOS settings and prefabs generated.");
        }

        static Sprite Shape(string name, int size, Func<float,float,float> alpha, Vector4 border = default)
        {
            string path=Root+"Art/Generated/"+name+".png";
            var tex = new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels = new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                pixels[y*size+x]=new Color(1,1,1,alpha((x+.5f)/size*2-1,(y+.5f)/size*2-1));
            tex.SetPixels(pixels);tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex); AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.spriteBorder=border;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void ImportCards()
        {
            string heroPath=Root+"Art/RavenHero.png";
            var heroImporter=(TextureImporter)AssetImporter.GetAtPath(heroPath);
            heroImporter.textureType=TextureImporterType.Sprite; heroImporter.spriteImportMode=SpriteImportMode.Single;
            heroImporter.alphaIsTransparency=true; heroImporter.mipmapEnabled=false; heroImporter.maxTextureSize=2048;
            heroImporter.SaveAndReimport();
            var source=JsonUtility.FromJson<WebCards>(File.ReadAllText(Root+"Cards/WebCards.json"));
            foreach(var web in source.cards)
            {
                string path=Root+"Cards/"+web.name+".asset";
                var card=AssetDatabase.LoadAssetAtPath<CardData>(path);
                if(!card) {card=ScriptableObject.CreateInstance<CardData>();AssetDatabase.CreateAsset(card,path);}
                card.id=web.id;card.displayName=web.name;card.series=web.series;card.year=web.year;
                card.element=web.element;card.rarity=Enum.Parse<CardRarity>(web.rarity,true);
                card.stats=web.stats;card.abilityName=web.ability.name;card.abilityDescription=web.ability.description;
                card.abilityBoosts=web.abilityBoosts;
                card.originalCardArt=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Art/"+Path.GetFileName(web.frontImage));
                if(web.id=="crypto-raven")
                {
                    raven=card;card.heroArt=AssetDatabase.LoadAssetAtPath<Sprite>(heroPath);
                    card.portraitUV=new Rect(.12f,.635f,.25f,.195f);
                    card.fact="Raven channels dark star energy through Nova Howl.";
                }
                else if(web.id=="crypto-brooklyn")
                {
                    brooklyn=card;card.portraitUV=new Rect(.063f,.656f,.432f,.30f);
                    card.fact="Brooklyn rides ocean momentum with Coast Rush.";
                }
                else {card.fact="Jinx raises a burning shield with Plasma Bulwark.";card.portraitUV=new Rect(.08f,.65f,.4f,.28f);}
                EditorUtility.SetDirty(card);
            }
        }
        [MenuItem("Pupverse/Configure iOS and VS Code")]
        public static void Configure()
        {
            PlayerSettings.companyName="Pupverse"; PlayerSettings.productName="Pupverse";
            PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS,"com.pupverse.mobile");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString="15.0";
            PlayerSettings.iOS.targetDevice=iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.sdkVersion=iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.buildNumber="1";
            PlayerSettings.iOS.appleEnableAutomaticSigning=true;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.accelerometerFrequency=60;
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=false;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            // This project intentionally uses Unity's classic input API for uGUI, touch and accelerometer.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler"); if(input!=null)input.intValue=0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            const string code="/Applications/Visual Studio Code.app";
            if(Directory.Exists(code)) Unity.CodeEditor.CodeEditor.SetExternalScriptEditor(code);
        }
        static RectTransform Rect(string name, Transform parent, float x,float y,float w,float h)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            rt.sizeDelta=new Vector2(w,h);rt.anchoredPosition=new Vector2(x,y);return rt;
        }
        static Image Box(string name,Transform parent,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var rt=Rect(name,parent,x,y,w,h);var im=rt.gameObject.AddComponent<Image>();
            im.color=color;im.sprite=sprite;im.raycastTarget=false;
            if(sprite==rounded)im.type=Image.Type.Sliced;return im;
        }
        static Text Label(string name,Transform parent,string text,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter,FontStyle style=FontStyle.Normal)
        {
            var rt=Rect(name,parent,x,y,w,h);var t=rt.gameObject.AddComponent<Text>();
            t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=align;t.fontStyle=style;
            t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        static Button Button(string name,Transform parent,string text,float x,float y,float w,float h,Color color,Color textColor,int size=28)
        {
            var bg=Box(name,parent,x,y,w,h,color,rounded);bg.raycastTarget=true;
            var b=bg.gameObject.AddComponent<Button>();b.targetGraphic=bg;
            var c=b.colors;c.highlightedColor=new Color(.9f,1,1);c.pressedColor=new Color(.65f,.8f,.85f);c.disabledColor=new Color(.5f,.5f,.5f,.45f);b.colors=c;
            Label("Label",bg.transform,text,0,0,w-24,h-8,size,textColor,TextAnchor.MiddleCenter,FontStyle.Bold);
            return b;
        }
        static Transform BaseScene(string name)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);scene.name=name;
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=UITheme.Navy;
            var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var sky=Rect("World / layered background",canvas.transform,0,0,1080,1920);
            sky.anchorMin=Vector2.zero;sky.anchorMax=Vector2.one;sky.sizeDelta=Vector2.zero;
            Background(sky);
            var safe=Rect("Safe Area",canvas.transform,0,0,1080,1920);safe.gameObject.AddComponent<SafeArea>();
            var board=Rect("Portrait Layout",safe,0,0,1080,1920);
            board.gameObject.AddComponent<ReferenceLayout>();
            return board;
        }
        static void Background(Transform parent)
        {
            var far=Rect("01 / distant nebula",parent,0,0,1400,2400);far.gameObject.AddComponent<ParallaxLayer>().depth=8;
            Box("Violet mist",far,-300,330,1600,1600,new Color(.29f,.17f,.65f,.58f),glow);
            Box("Teal mist",far,450,-150,1250,1500,new Color(.1f,.58f,.6f,.25f),glow);
            var stars=Rect("02 / stars",parent,0,0,1080,1920);stars.gameObject.AddComponent<ParallaxLayer>().depth=18;
            var rng=new System.Random(71);
            for(int i=0;i<70;i++)
            {
                float x=(float)rng.NextDouble()*1250-625,y=(float)rng.NextDouble()*2200-1100,s=i%6==0?5:2;
                Box("Star "+i,stars,x,y,s,s,new Color(.7f,.84f,1,.3f+(float)rng.NextDouble()*.5f),disc);
            }
            var near=Rect("03 / orbital garden",parent,0,0,1080,1920);near.gameObject.AddComponent<ParallaxLayer>().depth=30;
            Box("Distant planet",near,460,560,280,280,UITheme.Hex("263050"),disc);
            var orbit=Box("Planet orbit",near,460,560,400,140,new Color(.6f,.55f,1,.25f),ring);orbit.transform.localRotation=Quaternion.Euler(0,0,-27);
            Box("Left foreground",near,-660,-1050,1300,920,UITheme.Hex("101C32"),disc);
            Box("Right foreground",near,580,-1120,1500,1000,UITheme.Hex("14213A"),disc);
        }
        static void Header(Transform board,string kicker,string title)
        {
            Box("Brand accent",board,-470,858,6,60,UITheme.Mint);
            Label("Kicker",board,kicker,-145,880,600,36,23,UITheme.Mint,TextAnchor.MiddleLeft,FontStyle.Bold);
            Label("Page title",board,title,-120,820,650,70,54,Color.white,TextAnchor.MiddleLeft,FontStyle.Bold);
            Box("Header rule",board,0,752,952,2,new Color(.7f,.8f,1,.16f));
        }
        static void BuildMenu()
        {
            var board=BaseScene("MainMenu");Header(board,"PUPVERSE  /  FIRST LIGHT","PUPVERSE");
            var motion=Button("Motion toggle",board,"MOTION: ON",353,850,250,65,UITheme.Panel,UITheme.Muted,22);
            Label("Chapter",board,"01    THE BLOOM NEBULA",0,685,900,44,23,UITheme.Lavender);
            Label("Hero headline",board,"Every legend starts\nwith a little wild.",0,570,950,175,62,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
            Box("Hero aura",board,0,220,950,950,new Color(.5f,.31f,.9f,.7f),glow);
            Box("Hero platform shadow",board,0,-48,530,65,new Color(.04f,.05f,.16f,.8f),disc);
            Box("Hero platform ring",board,0,-48,660,155,new Color(.5f,.73f,1,.55f),ring);
            var hero=Rect("Hero / Raven",board,0,235,620,620);hero.gameObject.AddComponent<IdleMotion>();
            var heroImage=Box("Raven sprite",hero,0,0,620,620,Color.white,raven.heroArt);heroImage.preserveAspect=true;
            PrefabUtility.SaveAsPrefabAsset(hero.gameObject,Root+"Prefabs/RavenHero.prefab");
            Label("Hero tag",board,"RAVEN    /    DARK NOVA",0,-100,800,40,22,UITheme.Muted,TextAnchor.MiddleCenter,FontStyle.Bold);
            var panel=Box("First card / Raven",board,0,-405,948,490,UITheme.Panel,rounded);
            panel.raycastTarget=true;panel.gameObject.AddComponent<CardMotion>().entranceDelay=.2f;
            var summary=panel.gameObject.AddComponent<CardView>();summary.data=raven;summary.showBonuses=false;summary.statValues=new Text[5];
            Box("Card accent",panel.transform,-451,0,4,395,UITheme.Lavender);
            Label("Card eyebrow",panel.transform,"YOUR FIRST COMPANION",-105,188,650,32,21,UITheme.Muted,TextAnchor.MiddleLeft);
            summary.nameLabel=Label("Card name",panel.transform,"Raven",-245,124,380,85,64,Color.white,TextAnchor.MiddleLeft,FontStyle.Bold);
            var badge=Box("Rarity badge",panel.transform,306,131,230,58,new Color(.37f,.21f,.47f,1),rounded);
            summary.rarityLabel=Label("Rarity",badge.transform,"MYTHIC",0,0,215,48,23,UITheme.Pink,TextAnchor.MiddleCenter,FontStyle.Bold);
            summary.elementLabel=Label("Card series",panel.transform,"CryptoPups  /  Dark Nova  /  2025",-50,59,760,42,24,UITheme.Muted,TextAnchor.MiddleLeft);
            for(int i=0;i<5;i++)
            {
                float x=-350+i*175;
                Label("Stat name "+i,panel.transform,((CardStat)i).ToString().ToUpperInvariant(),x,-15,173,34,i==2?18:21,UITheme.Muted);
                summary.statValues[i]=Label("Stat value "+i,panel.transform,raven.stats.Get((CardStat)i).ToString(),x,-68,170,64,44,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
            }
            Box("Card rule",panel.transform,0,-121,840,2,new Color(.7f,.8f,1,.12f));
            Label("Fact label",panel.transform,"PUP FACT",-310,-158,220,34,20,UITheme.Mint,TextAnchor.MiddleLeft,FontStyle.Bold);
            summary.factLabel=Label("Fact",panel.transform,raven.fact,0,-203,840,50,24,UITheme.Muted,TextAnchor.MiddleLeft);
            var enter=Button("Enter the arena",board,"ENTER THE ARENA   >",0,-746,948,110,UITheme.Mint,UITheme.Navy,32);
            Label("Interaction hint",board,"Drag a card to tilt it. On iPhone, gently tilt your device.",0,-838,1000,42,23,UITheme.Muted);
            Label("Footer",board,"PUPVERSE MOBILE     /     DAY ONE",0,-910,950,30,20,UITheme.Muted);
            var controller=board.gameObject.AddComponent<MainMenuController>();controller.battleButton=enter;controller.motionButton=motion;
            controller.motionLabel=motion.GetComponentInChildren<Text>();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/MainMenu.unity");
        }
        static CardView MakeCard(Transform board,CardData data,float x)
        {
            var panel=Box(data.displayName+" / battle card",board,x,174,452,896,UITheme.Panel,rounded);panel.raycastTarget=true;
            panel.gameObject.AddComponent<CardMotion>().entranceDelay=x<0?.08f:.22f;
            var view=panel.gameObject.AddComponent<CardView>();
            Box("Aura",panel.transform,0,235,430,370,new Color(.42f,.3f,.78f,.6f),glow);
            view.heroImage=Box("Character",panel.transform,0,270,330,330,Color.white,data.heroArt);view.heroImage.preserveAspect=true;
            var portraitRect=Rect("Original portrait",panel.transform,0,270,362,278);view.portrait=portraitRect.gameObject.AddComponent<RawImage>();view.portrait.raycastTarget=false;
            view.nameLabel=Label("Name",panel.transform,"",0,64,404,74,45,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
            view.rarityLabel=Label("Rarity",panel.transform,"",0,112,410,38,22,x<0?UITheme.Pink:UITheme.Lavender,TextAnchor.MiddleCenter,FontStyle.Bold);
            view.elementLabel=Label("Element",panel.transform,"",0,9,412,54,20,UITheme.Muted);
            Box("Divider",panel.transform,0,-35,380,2,new Color(.7f,.8f,1,.15f));
            view.statValues=new Text[5];view.statBars=new Image[5];
            for(int i=0;i<5;i++)
            {
                float y=-85-i*54;
                Label("Statistic "+i,panel.transform,((CardStat)i).ToString(),-80,y,205,42,25,UITheme.Muted,TextAnchor.MiddleLeft);
                view.statValues[i]=Label("Value "+i,panel.transform,"",121,y,135,42,27,Color.white,TextAnchor.MiddleRight,FontStyle.Bold);
                Box("Track "+i,panel.transform,0,y-24,376,3,UITheme.Hex("293650"));
                var bar=Box("Bar "+i,panel.transform,0,y-24,376,3,i%2==0?UITheme.Lavender:UITheme.Mint,disc);
                bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;bar.fillOrigin=0;view.statBars[i]=bar;
            }
            view.abilityLabel=Label("Ability",panel.transform,"",0,-357,392,42,22,UITheme.Mint,TextAnchor.MiddleCenter,FontStyle.Bold);
            view.factLabel=Label("Fact",panel.transform,"",0,-405,392,65,20,UITheme.Muted);
            view.Bind(data);return view;
        }
        static void BuildBattle()
        {
            var board=BaseScene("Battle");Header(board,"TRAINING  /  BLOOM NEBULA","The first encounter");
            var home=Button("Home",board,"HOME",384,855,170,66,UITheme.Panel,UITheme.Muted,22);
            Label("You",board,"YOUR COMPANION",-247,687,450,45,24,UITheme.Mint,TextAnchor.MiddleCenter,FontStyle.Bold);
            Label("Opponent",board,"TRAINING RIVAL",247,687,450,45,24,UITheme.Lavender,TextAnchor.MiddleCenter,FontStyle.Bold);
            var player=MakeCard(board,raven,-247);var opponent=MakeCard(board,brooklyn,247);
            PrefabUtility.SaveAsPrefabAsset(player.gameObject,Root+"Prefabs/CompanionCard.prefab");
            var vs=Box("Versus",board,0,232,68,68,UITheme.Navy,disc);Label("VS",vs.transform,"VS",0,0,66,60,22,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
            Label("Bonus legend",board,"MINT + VALUES ARE ABILITY BONUSES",0,-309,1000,42,21,UITheme.Mint);
            var result=Box("Round result",board,0,-430,948,170,UITheme.Panel,rounded);
            var highlight=Box("Result glow",result.transform,0,0,920,160,new Color(.3f,.72f,.65f,.24f),glow);
            var hg=highlight.gameObject.AddComponent<CanvasGroup>();hg.alpha=0;hg.blocksRaycasts=false;
            var title=Label("Result title",result.transform,"CHOOSE YOUR EDGE",0,30,880,57,38,Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
            var detail=Label("Result detail",result.transform,"Pick a statistic below. Highest total wins.",0,-36,890,51,25,UITheme.Muted);
            var stats=new Button[5];
            for(int i=0;i<5;i++)
            {
                float x=i<3?(i-1)*320:(i==3?-240:240);float y=i<3?-594:-690;float width=i<3?300:464;
                stats[i]=Button("Choose "+(CardStat)i,board,((CardStat)i).ToString().ToUpperInvariant(),x,y,width,78,UITheme.Hex("26364F"),UITheme.Mint,23);
            }
            var replay=Button("Replay",board,"PLAY AGAIN",0,-797,510,90,UITheme.Mint,UITheme.Navy,28);
            var score=Label("Session score",board,"0 WINS    0 LOSSES    0 DRAWS",0,-883,980,40,22,UITheme.Muted);
            var fx=Rect("Victory / particles",board,0,-430,1080,1920);var effect=fx.gameObject.AddComponent<VictoryEffect>();
            effect.group=fx.gameObject.AddComponent<CanvasGroup>();effect.group.alpha=0;effect.group.blocksRaycasts=false;
            effect.sparks=new RectTransform[30];
            for(int i=0;i<30;i++) effect.sparks[i]=Box("Spark "+i,fx,0,0,7+i%3*4,14+i%4*3,UITheme.Mint,rounded).rectTransform;
            var controller=board.gameObject.AddComponent<BattleController>();controller.playerCard=player;controller.opponentCard=opponent;
            controller.statButtons=stats;controller.replayButton=replay;controller.homeButton=home;controller.resultTitle=title;
            controller.resultDetail=detail;controller.resultPanel=result;controller.scoreLabel=score;controller.victory=effect;controller.winnerHighlight=hg;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"Scenes/Battle.unity");
        }
        public static void BuildAndMac() { Build(); BuildMac(); }
        public static void BuildMac()
        {
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Builds/Mac/Pupverse.app",BuildTarget.StandaloneOSX,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Mac build failed: "+report.summary.result);
        }
        [MenuItem("Pupverse/Export iOS Xcode project")]
        public static void BuildIOS()
        {
            Configure();
            // Timestamp avoids overwriting a previously edited Xcode export.
            string path="Builds/iOS/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,path,BuildTarget.iOS,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded) throw new Exception("iOS export failed: "+report.summary.result);
            Debug.Log("PUPVERSE_IOS_EXPORT_OK: "+path);
        }
        public static void SyncSolution() { Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll(); }
    }
}
