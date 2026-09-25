using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // Canvas and viewport layout only. BattleController owns rules and round sequencing.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class Battle3DController : MonoBehaviour
    {
        public BattleController battleController;
        public RectTransform controlsRoot;
        public Text instruction;
        public Camera battleCamera;
        Rect originalViewport;
        Matrix4x4 originalProjection;
        bool ownsViewport;
        Canvas canvas;
        CanvasScaler scaler;
        readonly RectTransform[] viewportMatte = new RectTransform[4];
        RectTransform matteRoot;
        Rect previousSafeArea;
        Vector2 previousScreen;
        public PhoneLayout CurrentLayout { get; private set; }

        // Units are scaled to the safe width (390), not the full width behind a notch.
        public readonly struct PhoneLayout
        {
            public readonly Vector2 SafeSize;
            public readonly bool Portrait;
            public readonly float Height, TileHeight, ArenaReserve;
            public PhoneLayout(Vector2 safeSize)
            {
                SafeSize=safeSize;
                Portrait=safeSize.y>=safeSize.x;
                Height=Portrait?Mathf.Clamp(safeSize.y*.44f,320,338):208;
                TileHeight=Portrait?(Height-152)/3:62;
                ArenaReserve=Portrait?Height-116:100;
            }
            public Rect StatRect(int i)
            {
                float width=Portrait?(SafeSize.x-40)/2:(SafeSize.x-64)/5;
                return new Rect(16+(Portrait?i%2:i)*(width+8),
                    Portrait?32+(2-i/2)*(TileHeight+8):32,
                    Portrait&&i==4?SafeSize.x-32:width,TileHeight);
            }
            public Rect ComparisonRect => new Rect(16,Height-96,SafeSize.x-32,64);
        }

        void LateUpdate()
        {
            if(controlsRoot==null || battleController==null) return;
            if(canvas==null) { canvas=controlsRoot.GetComponentInParent<Canvas>(); if(canvas!=null) scaler=canvas.GetComponent<CanvasScaler>(); }
            if(canvas==null) return;
            if(matteRoot==null) BuildMatte();
            Rect safe=Screen.safeArea;
            if(safe.width<=0 || safe.height<=0 || Screen.width<=0 || Screen.height<=0) return;
            bool portrait=safe.height>=safe.width;
            if(safe!=previousSafeArea || previousScreen!=new Vector2(Screen.width,Screen.height))
            {
                previousSafeArea=safe; previousScreen=new Vector2(Screen.width,Screen.height);
                if(scaler!=null)
                {
                    scaler.referenceResolution=portrait?new Vector2(390*Screen.width/safe.width,844):new Vector2(844,390*Screen.height/safe.height);
                    scaler.matchWidthOrHeight=portrait?0:1;
                }
            }
            var parent=controlsRoot.parent as RectTransform;
            CurrentLayout=new PhoneLayout(parent.rect.size);
            float height=CurrentLayout.Height, width=CurrentLayout.SafeSize.x;
            controlsRoot.anchorMin=Vector2.zero; controlsRoot.anchorMax=Vector2.right;
            controlsRoot.pivot=new Vector2(.5f,0); controlsRoot.anchoredPosition=Vector2.zero;
            controlsRoot.sizeDelta=new Vector2(0,height);
            Place(battleController.resultPanel.rectTransform,16,height-62,width-32,62);
            Place(battleController.resultTitle.rectTransform,0,34,width-32,26);
            Place(battleController.resultDetail.rectTransform,0,7,width-32,22);
            for(int i=0;i<5;i++)
            {
                Rect r=CurrentLayout.StatRect(i);
                Place((RectTransform)battleController.statButtons[i].transform,r.x,r.y,r.width,r.height);
            }
            Place(battleController.scoreLabel.rectTransform,16,6,145,16);
            battleController.scoreLabel.alignment=TextAnchor.MiddleLeft;
            if(battleCamera==null) return;
            if(!ownsViewport)
            {
                originalViewport=battleCamera.rect;
                originalProjection=battleCamera.projectionMatrix;
                ownsViewport=true;
            }
            battleCamera.rect=new Rect(safe.x/Screen.width,safe.y/Screen.height,safe.width/Screen.width,safe.height/Screen.height);
            float reserve=Mathf.Min(CurrentLayout.ArenaReserve*canvas.scaleFactor,safe.height*.55f);
            battleCamera.projectionMatrix=ExtendArenaProjection(battleCamera.fieldOfView,safe.size,reserve,battleCamera.nearClipPlane,battleCamera.farClipPlane);
            // Only the areas outside the phone safe area need opaque clearing now.
            Rect v=battleCamera.rect;
            Vector2 size=((RectTransform)canvas.transform).rect.size;
            Place(viewportMatte[0],0,0,size.x,v.yMin*size.y);
            Place(viewportMatte[1],0,v.yMax*size.y,size.x,(1-v.yMax)*size.y);
            Place(viewportMatte[2],0,v.yMin*size.y,v.xMin*size.x,v.height*size.y);
            Place(viewportMatte[3],v.xMax*size.x,v.yMin*size.y,(1-v.xMax)*size.x,v.height*size.y);
        }
        // Preserve the upper arena's framing while rendering extra floor beneath the glass HUD.
        // This changes the runtime projection only, never the camera or arena transforms.
        public static Matrix4x4 ExtendArenaProjection(float fieldOfView,Vector2 size,float reserve,float near,float far)
        {
            float visibleHeight=Mathf.Max(1,size.y-reserve);
            Matrix4x4 matrix=Matrix4x4.Perspective(fieldOfView,size.x/visibleHeight,near,far);
            float fraction=visibleHeight/size.y;
            matrix.SetRow(1,matrix.GetRow(1)*fraction+matrix.GetRow(3)*(1-fraction));
            return matrix;
        }
        void BuildMatte()
        {
            matteRoot=new GameObject("Viewport matte",typeof(RectTransform)).GetComponent<RectTransform>();
            matteRoot.SetParent(canvas.transform,false); matteRoot.SetAsFirstSibling();
            matteRoot.anchorMin=Vector2.zero; matteRoot.anchorMax=Vector2.one;
            matteRoot.offsetMin=matteRoot.offsetMax=Vector2.zero;
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("Outside arena "+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                viewportMatte[i]=(RectTransform)go.transform; viewportMatte[i].SetParent(matteRoot,false);
                var image=go.GetComponent<Image>(); image.color=new Color(.025f,.045f,.10f,1); image.raycastTarget=false;
            }
        }
        public static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=Vector2.zero; rect.pivot=Vector2.zero;
            rect.anchoredPosition=new Vector2(x,y); rect.sizeDelta=new Vector2(width,height);
        }
        void OnDisable()
        {
            if(ownsViewport && battleCamera!=null) { battleCamera.rect=originalViewport; battleCamera.projectionMatrix=originalProjection; }
            ownsViewport=false;
            if(matteRoot!=null) Destroy(matteRoot.gameObject);
        }
    }
}
