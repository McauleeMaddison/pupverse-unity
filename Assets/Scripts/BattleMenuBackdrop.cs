using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // Shared menu atmosphere: a small UI mesh, with no textures, particles or post-processing.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleMenuBackdrop : MaskableGraphic
    {
        [Range(0,1)] public float opacity=.97f;
        float lastRefresh,phase;
        protected override void Awake(){base.Awake();raycastTarget=false;}
        void Update()
        {
            float next=GameSettings.ReducedMotion?0:Time.unscaledTime;
            if(Mathf.Abs(next-lastRefresh)<.09f)return;
            lastRefresh=next;phase=next;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=rectTransform.rect;const int cols=12,rows=18;
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
            {
                float u=x/(float)cols,v=y/(float)rows;
                float cyan=Mathf.Exp(-((u-.12f)*(u-.12f)*7+(v-.6f)*(v-.6f)*14));
                float violet=Mathf.Exp(-((u-.91f)*(u-.91f)*8+(v-.7f)*(v-.7f)*15));
                var c=new Color(.015f+violet*.038f,.024f+cyan*.065f,.055f+cyan*.075f+violet*.09f,opacity);
                vh.AddVert(new Vector3(r.x+u*r.width,r.y+v*r.height),c,Vector2.zero);
                if(x<cols && y<rows){int i=y*(cols+1)+x;vh.AddTriangle(i,i+cols+1,i+1);vh.AddTriangle(i+1,i+cols+1,i+cols+2);}
            }
            var grid=new Color(.2f,.65f,.85f,.055f);
            for(int i=-4;i<=4;i++)Line(vh,Point(r,.5f+i*.11f,.3f),Point(r,.5f+i*.3f,0),.7f,grid);
            for(int i=1;i<=5;i++){float y=.3f-i*i*.012f;Line(vh,Point(r,0,y),Point(r,1,y),.7f,grid);}
            for(int i=0;i<28;i++)
            {
                float u=Mathf.Repeat(Mathf.Sin(i*17.31f)*173.71f,1),v=.35f+Mathf.Repeat(Mathf.Sin(i*9.71f)*123.17f,1)*.64f;
                float a=.06f+(.5f+.5f*Mathf.Sin(phase*.7f+i))* .11f;
                var p=Point(r,u,v);Line(vh,p-Vector2.right,p+Vector2.right,1,new Color(.6f,.82f,1,a));
            }
        }
        static Vector2 Point(Rect r,float x,float y)=>new Vector2(r.x+r.width*x,r.y+r.height*y);
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int i=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
