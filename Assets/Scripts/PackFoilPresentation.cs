using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pupverse
{
    // A small isolated 3D stage. The arena and its lights are never changed.
    public sealed class PackFoilPresentation : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action TearCompleted;
        public bool IsSealed {get;private set;}
        public float TearProgress {get;private set;}
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        Transform stage,wrapper,lid,tearLine;
        Transform[] cards;
        Material[] faces;
        CardData[] contents;
        Texture2D cardBack;
        Shader cardShader;
        Camera previewCamera;
        RenderTexture target;
        int pointer=int.MinValue,shown=-1;
        Vector2 start;
        bool revealing;
        Vector3[] fromPositions,fromScales;
        Quaternion[] fromRotations;
        public void Initialize(Shader foilShader,Font font,Shader cardFoil)
        {
            cardShader=cardFoil;
            stage=new GameObject("Pack preview stage").transform;stage.position=new Vector3(5000,5000,5000);
            previewCamera=new GameObject("Pack preview camera",typeof(Camera)).GetComponent<Camera>();
            previewCamera.transform.SetParent(stage,false);previewCamera.transform.localPosition=new Vector3(0,0,-6);
            previewCamera.enabled=false;previewCamera.orthographic=false;previewCamera.fieldOfView=42;previewCamera.nearClipPlane=.1f;previewCamera.farClipPlane=15;
            previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=Color.clear;previewCamera.cullingMask=1<<31;
            target=new RenderTexture(600,800,16,RenderTextureFormat.ARGB32);target.name="Foil pack preview";owned.Add(target);previewCamera.targetTexture=target;
            GetComponent<RawImage>().texture=target;
            cardBack=new Texture2D(64,96);owned.Add(cardBack);
            for(int y=0;y<96;y++)for(int x=0;x<64;x++)
            {
                bool border=x<3||x>60||y<3||y>92;bool grid=(x+y)%18<2;
                cardBack.SetPixel(x,y,border?new Color(.2f,.7f,.85f):grid?new Color(.08f,.19f,.3f):new Color(.02f,.05f,.12f));
            }
            cardBack.Apply();
            var foil=new Material(foilShader);owned.Add(foil);
            wrapper=new GameObject("Foil wrapper").transform;wrapper.SetParent(stage,false);
            Surface("Crinkled foil",wrapper,-1.45f,1.17f,foil);
            lid=new GameObject("Tear-off seal").transform;lid.SetParent(wrapper,false);Surface("Crimped top",lid,1.17f,1.48f,foil);
            tearLine=Box("Tear edge",wrapper,new Vector3(-1,1.17f,-.15f),new Vector3(0,.018f,.012f),Material(Color.white));
            var canvasObject=new GameObject("Printed pack branding",typeof(RectTransform),typeof(Canvas));
            canvasObject.transform.SetParent(wrapper,false);canvasObject.transform.localPosition=new Vector3(0,-.1f,-.22f);canvasObject.transform.localScale=Vector3.one*.004f;
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=previewCamera;
            var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(410,590);
            Printed("P U P V E R S E",rect,font,31,210);Printed("P V",rect,font,118,25);Printed("COLLECT • BATTLE • EVOLVE",rect,font,17,-124);Printed("4 CARDS",rect,font,28,-212);
            stage.gameObject.SetActive(false);
        }
        public void Prepare(Color colour,CardData[] pulled)
        {
            contents=pulled;stage.gameObject.SetActive(true);wrapper.gameObject.SetActive(true);
            wrapper.localPosition=Vector3.zero;wrapper.localRotation=Quaternion.identity;wrapper.localScale=Vector3.one;
            lid.gameObject.SetActive(true);lid.localPosition=Vector3.zero;lid.localRotation=Quaternion.identity;
            foreach(var r in wrapper.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial.shader.name=="Pupverse/FoilPack")r.sharedMaterial.color=colour;
            if(cards==null || cards.Length!=pulled.Length)
            {
                if(cards!=null)foreach(var c in cards)Destroy(c.gameObject);
                cards=new Transform[pulled.Length];faces=new Material[pulled.Length];
                fromPositions=new Vector3[cards.Length];fromScales=new Vector3[cards.Length];fromRotations=new Quaternion[cards.Length];
                for(int i=0;i<cards.Length;i++)
                {
                    cards[i]=new GameObject("Pack card "+(i+1)).transform;cards[i].SetParent(stage,false);
                    Box("Card edge",cards[i],Vector3.zero,new Vector3(1.65f,2.5f,.045f),Material(new Color(.16f,.24f,.34f)));
                    var front=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(front.GetComponent<Collider>());front.transform.SetParent(cards[i],false);
                    front.transform.localPosition=new Vector3(0,0,-.03f);front.transform.localScale=new Vector3(1.61f,2.46f,1);
                    faces[i]=CardFoilFinish.Create(cardShader);owned.Add(faces[i]);faces[i].mainTexture=cardBack;front.GetComponent<MeshRenderer>().sharedMaterial=faces[i];
                }
            }
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            for(int i=0;i<cards.Length;i++){cards[i].gameObject.SetActive(false);CardFoilFinish.Apply(faces[i],null);faces[i].mainTexture=cardBack;}
            shown=-1;IsSealed=true;pointer=int.MinValue;SetTear(0);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(!IsSealed || pointer!=int.MinValue)return;
            Vector2 p=Normalized(e);if(p.y<.66f || p.y>.9f || p.x<.1f || p.x>.9f)return;
            start=p;pointer=e.pointerId;
        }
        public void OnDrag(PointerEventData e)
        {
            if(!IsSealed || e.pointerId!=pointer)return;
            var p=Normalized(e);
            if(Mathf.Abs(p.y-start.y)>.22f){pointer=int.MinValue;SetTear(0);return;}
            SetTear(Mathf.Abs(p.x-start.x)/.55f);
            if(TearProgress>=1){IsSealed=false;pointer=int.MinValue;TearCompleted?.Invoke();}
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer){pointer=int.MinValue;if(IsSealed)SetTear(0);}}
        Vector2 Normalized(PointerEventData e)
        {
            var r=(RectTransform)transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(r,e.position,e.pressEventCamera,out var p);
            return new Vector2((p.x-r.rect.xMin)/r.rect.width,(p.y-r.rect.yMin)/r.rect.height);
        }
        void SetTear(float progress)
        {
            TearProgress=Mathf.Clamp01(progress);tearLine.localScale=new Vector3(TearProgress*2,.018f,.012f);
            tearLine.localPosition=new Vector3(-1+TearProgress,1.17f,-.15f);lid.localRotation=Quaternion.Euler(0,0,-TearProgress*5);
        }
        public void Open(float t)
        {
            IsSealed=false;SetTear(1);
            lid.localPosition=new Vector3(t*.7f,t*1.7f,0);lid.localRotation=Quaternion.Euler(0,t*60,-t*50);
            wrapper.localPosition=new Vector3(0,-t*2.7f,t);wrapper.localRotation=Quaternion.Euler(t*15,0,-t*12);
            for(int i=0;i<cards.Length;i++)
            {
                float f=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-i*.08f)/.7f));cards[i].gameObject.SetActive(true);
                cards[i].localPosition=new Vector3((i-(cards.Length-1)*.5f)*.36f*f,-1+f*1.3f,-.4f-i*.035f);
                cards[i].localScale=Vector3.one*.74f;cards[i].localRotation=Quaternion.Euler(0,0,((cards.Length-1)*.5f-i)*9*f);
            }
            if(t>=1)wrapper.gameObject.SetActive(false);
        }
        public void Reveal(int index,float t)
        {
            IsSealed=false;revealing=t<1;wrapper.gameObject.SetActive(false);
            if(shown!=index)
            {
                shown=index;
                for(int i=0;i<cards.Length;i++)
                {
                    if(!cards[i].gameObject.activeSelf){cards[i].gameObject.SetActive(true);cards[i].localPosition=new Vector3((i-1.5f)*.35f,0,0);cards[i].localScale=Vector3.one*.7f;}
                    fromPositions[i]=cards[i].localPosition;fromScales[i]=cards[i].localScale;fromRotations[i]=cards[i].localRotation;
                    CardFoilFinish.Apply(faces[i],i==index?contents[i]:null);
                    faces[i].mainTexture=i==index?contents[i]?.originalCardArt:cardBack;
                }
            }
            for(int i=0;i<cards.Length;i++)
            {
                Vector3 p=i==index?new Vector3(0,0,-1):new Vector3((i-(cards.Length-1)*.5f)*.48f,.18f,.1f);
                cards[i].localPosition=Vector3.Lerp(fromPositions[i],p,t);cards[i].localScale=Vector3.Lerp(fromScales[i],Vector3.one*(i==index?1.25f:.78f),t);
                cards[i].localRotation=Quaternion.Slerp(fromRotations[i],Quaternion.Euler(0,i==index?Mathf.Sin(t*Mathf.PI)*65:0,i==index?0:(1.5f-i)*9),t);
            }
        }
        void LateUpdate()
        {
            if(stage==null)return;
            if(!GameSettings.ReducedMotion && IsSealed)wrapper.localRotation=Quaternion.Euler(Mathf.Sin(Time.unscaledTime)*3,Mathf.Sin(Time.unscaledTime*.7f)*13,-2);
            else if(!GameSettings.ReducedMotion && !revealing && shown>=0)cards[shown].localRotation=Quaternion.Euler(Mathf.Sin(Time.unscaledTime)*2,Mathf.Sin(Time.unscaledTime*.9f)*6,0);
            RenderPreview();
        }
        public void RenderPreview(){if(stage!=null && stage.gameObject.activeSelf){if(faces!=null)foreach(var face in faces)CardFoilFinish.Tick(face);previewCamera.Render();}}
        void OnEnable(){if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){if(stage!=null)stage.gameObject.SetActive(false);pointer=int.MinValue;}
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);foreach(var o in owned)if(o!=null)Destroy(o);}
        Material Material(Color colour){var m=new Material(Shader.Find("Unlit/Color"));m.color=colour;owned.Add(m);return m;}
        Transform Box(string name,Transform parent,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;Destroy(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
        void Surface(string name,Transform parent,float bottom,float top,Material material)
        {
            const int cols=32,rows=24;var vertices=new Vector3[(cols+1)*(rows+1)];var uv=new Vector2[vertices.Length];var triangles=new int[cols*rows*6];
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
            {
                float u=x/(float)cols,v=y/(float)rows,py=Mathf.Lerp(bottom,top,v);int i=y*(cols+1)+x;
                float wrinkle=Mathf.Sin(x*2.7f+y*.3f)*.012f+Mathf.Sin(x*.4f+y*.65f)*.022f;
                vertices[i]=new Vector3((u-.5f)*2,py,-.12f+wrinkle+Mathf.Pow(Mathf.Abs(u-.5f)*2,8)*.1f);uv[i]=new Vector2(u,(py+1.45f)/2.93f);
                if(x<cols && y<rows){int j=(y*cols+x)*6;triangles[j]=i;triangles[j+1]=i+cols+1;triangles[j+2]=i+1;triangles[j+3]=i+1;triangles[j+4]=i+cols+1;triangles[j+5]=i+cols+2;}
            }
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();owned.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            Box("Folded backing",parent,new Vector3(0,(top+bottom)/2,.015f),new Vector3(1.98f,top-bottom,.18f),material);
        }
        static void Printed(string text,RectTransform parent,Font font,int size,float y)
        {
            var go=new GameObject(text,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(410,140);r.anchoredPosition=new Vector2(0,y);
            var label=go.GetComponent<Text>();label.font=font;label.fontSize=size;label.fontStyle=FontStyle.Bold;label.alignment=TextAnchor.MiddleCenter;label.text=text;label.color=Color.white;label.raycastTarget=false;
        }
    }
}
