using UnityEngine;
using UnityEngine.UI;

namespace Pupverse
{
    // Small, texture-free HUD meshes: one shared UI material, no particles or post-processing.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleHudGraphic : MaskableGraphic
    {
        public enum Shape { Panel, Scrim, Icon, Ring, Bar }
        public Shape shape;
        public CardStat stat;
        public Color accent = new Color(.2f, .9f, 1f);
        [Range(0, 1)] public float emphasis;
        [Range(0, 1)] public float progress = 1;
        [Range(0, 1)] public float surfaceOpacity = .34f;
        public bool openFrame;
        float phase, ripple = 1;
        readonly Vector2[] corners = new Vector2[8];

        public void Animate(float value)
        {
            if (Mathf.Abs(emphasis - value) < .015f) return;
            emphasis = value;
            SetVerticesDirty();
        }
        public void Fill(float value)
        {
            if (Mathf.Abs(progress - value) < .003f) return;
            progress = value;
            SetVerticesDirty();
        }
        public void Motion(float sweep, float pulse)
        {
            if(Mathf.Abs(phase-sweep)<.004f && Mathf.Abs(ripple-pulse)<.015f) return;
            phase=sweep; ripple=pulse; SetVerticesDirty();
        }
        public void Tint(Color value) { accent = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (shape == Shape.Scrim)
            {
                Quad(vh, r, new Color(.025f, .045f, .10f, surfaceOpacity), new Color(.025f, .045f, .10f, 0));
                return;
            }
            if (shape == Shape.Bar)
            {
                Quad(vh, r, new Color(.2f,.3f,.45f,.25f), new Color(.2f,.3f,.45f,.25f));
                r.width *= progress;
                Quad(vh, r, accent * new Color(1,1,1,.55f), accent);
                return;
            }
            if (shape == Shape.Panel)
            {
                float c = Mathf.Min(8, r.height * .14f);
                corners[0] = new Vector2(r.xMin+c,r.yMin); corners[1] = new Vector2(r.xMax-c,r.yMin);
                corners[2] = new Vector2(r.xMax,r.yMin+c); corners[3] = new Vector2(r.xMax,r.yMax-c);
                corners[4] = new Vector2(r.xMax-c,r.yMax); corners[5] = new Vector2(r.xMin+c,r.yMax);
                corners[6] = new Vector2(r.xMin,r.yMax-c); corners[7] = new Vector2(r.xMin,r.yMin+c);
                Color bottom = new Color(.025f,.05f,.11f,surfaceOpacity);
                Color top = Color.Lerp(new Color(.06f,.12f,.20f), accent, .10f + emphasis * .12f);
                top.a=surfaceOpacity*.45f;
                vh.AddVert(r.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
                for (int i=0;i<8;i++) vh.AddVert(corners[i], Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,corners[i].y)), Vector2.zero);
                for (int i=0;i<8;i++)
                {
                    vh.AddTriangle(0,i+1,(i+1)%8+1);
                    if(!openFrame || i%2==1)
                        Line(vh,corners[i],corners[(i+1)%8],1,WithAlpha(accent,.24f+emphasis*.5f));
                }
                Line(vh,new Vector2(r.xMin+8,r.yMin+1),new Vector2(r.xMax-8,r.yMin+1),1,WithAlpha(accent,.22f));
                float head=Mathf.Lerp(r.xMin+10,r.xMax-10,phase);
                Line(vh,new Vector2(Mathf.Max(r.xMin+8,head-24),r.yMin+1),new Vector2(head,r.yMin+1),1.5f,WithAlpha(accent,.55f+emphasis*.4f));
                if(openFrame)
                {
                    Vector2 center=new Vector2(r.xMin+25,r.center.y+2);
                    Glow(vh,center,24,WithAlpha(accent,.09f+emphasis*.12f));
                    if(ripple<1)
                        Circle(vh,center,12+ripple*13,WithAlpha(accent,(1-ripple)*.65f));
                }
                return;
            }
            if (shape == Shape.Ring)
            {
                Vector2 center = r.center;
                float radius = Mathf.Min(r.width,r.height)*.44f;
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI*2/48, b=(i+1)*Mathf.PI*2/48;
                    Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,
                        center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,1.5f,
                        WithAlpha(accent, i<48*progress ? .55f : .10f));
                }
                return;
            }
            // Distinct hand-built symbols, readable at small phone sizes.
            switch(stat)
            {
                case CardStat.Power:
                    Stroke(vh,r,.5f,.92f,.22f,.48f); Stroke(vh,r,.22f,.48f,.46f,.48f);
                    Stroke(vh,r,.46f,.48f,.35f,.08f); Stroke(vh,r,.35f,.08f,.80f,.61f);
                    Stroke(vh,r,.80f,.61f,.56f,.61f); Stroke(vh,r,.56f,.61f,.5f,.92f); break;
                case CardStat.Speed:
                    for(int i=0;i<2;i++) { float x=.25f+i*.32f; Stroke(vh,r,x,.22f,x+.22f,.5f); Stroke(vh,r,x+.22f,.5f,x,.78f); }
                    Stroke(vh,r,.03f,.5f,.28f,.5f); break;
                case CardStat.Intelligence:
                    Stroke(vh,r,.3f,.3f,.7f,.3f); Stroke(vh,r,.7f,.3f,.7f,.7f);
                    Stroke(vh,r,.7f,.7f,.3f,.7f); Stroke(vh,r,.3f,.7f,.3f,.3f);
                    for(int i=0;i<3;i++) { float x=.35f+i*.15f; Stroke(vh,r,x,.15f,x,.3f); Stroke(vh,r,x,.7f,x,.85f); Stroke(vh,r,.15f,x,.3f,x); Stroke(vh,r,.7f,x,.85f,x); } break;
                case CardStat.Defence:
                    Stroke(vh,r,.5f,.88f,.82f,.76f); Stroke(vh,r,.82f,.76f,.77f,.37f);
                    Stroke(vh,r,.77f,.37f,.5f,.1f); Stroke(vh,r,.5f,.1f,.23f,.37f);
                    Stroke(vh,r,.23f,.37f,.18f,.76f); Stroke(vh,r,.18f,.76f,.5f,.88f);
                    Stroke(vh,r,.5f,.72f,.5f,.32f); break;
                case CardStat.Luck:
                    for(int i=0;i<8;i++)
                    {
                        float a=i*Mathf.PI/4, b=(i+1)*Mathf.PI/4;
                        float ra=i%2==0?.44f:.16f, rb=i%2==0?.16f:.44f;
                        Stroke(vh,r,.5f+Mathf.Cos(a)*ra,.5f+Mathf.Sin(a)*ra,.5f+Mathf.Cos(b)*rb,.5f+Mathf.Sin(b)*rb);
                    } break;
            }
        }
        static void Glow(VertexHelper vh,Vector2 center,float radius,Color c)
        {
            int start=vh.currentVertCount;
            vh.AddVert(center,c,Vector2.zero);
            for(int i=0;i<=24;i++)
            {
                float angle=i*Mathf.PI*2/24;
                vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,WithAlpha(c,0),Vector2.zero);
                if(i>0) vh.AddTriangle(start,start+i,start+i+1);
            }
        }
        static void Circle(VertexHelper vh,Vector2 center,float radius,Color c)
        {
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24,b=(i+1)*Mathf.PI*2/24;
                Line(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,1,c);
            }
        }
        void Stroke(VertexHelper vh,Rect r,float x,float y,float xx,float yy)
        {
            Line(vh,new Vector2(r.x+r.width*x,r.y+r.height*y),new Vector2(r.x+r.width*xx,r.y+r.height*yy),2,accent);
        }
        static Color WithAlpha(Color c,float a) { c.a=a; return c; }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            int v=vh.currentVertCount;
            vh.AddVert(a-n,c,Vector2.zero); vh.AddVert(a+n,c,Vector2.zero);
            vh.AddVert(b+n,c,Vector2.zero); vh.AddVert(b-n,c,Vector2.zero);
            vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
        }
        static void Quad(VertexHelper vh,Rect r,Color bottom,Color top)
        {
            int v=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),bottom,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),top,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),top,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),bottom,Vector2.zero);
            vh.AddTriangle(v,v+1,v+2); vh.AddTriangle(v,v+2,v+3);
        }
    }
}
