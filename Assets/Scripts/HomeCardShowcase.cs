using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pupverse
{
    // Home-only stage: generated geometry and private materials; never moves arena objects.
    public sealed class HomeCardShowcase : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerExitHandler
    {
        readonly List<Object> resources=new List<Object>();
        Transform stage,turntable,orbit;
        readonly Transform[] cards=new Transform[3];
        readonly Transform[] sparks=new Transform[14];
        Camera preview;
        RenderTexture target;
        Vector2 touch,tilt;
        int pointer=int.MinValue;
        float openedAt;
        bool initialized;
        public int VisibleCardCount => initialized?cards.Length:0;
        public Vector2 Tilt => tilt;
        public void Initialize(CardData[] collection,Shader metal)
        {
            stage=new GameObject("Home showcase stage").transform;stage.position=new Vector3(5200,5000,5000);
            preview=new GameObject("Home showcase camera",typeof(Camera)).GetComponent<Camera>();preview.transform.SetParent(stage,false);
            preview.transform.localPosition=new Vector3(0,.72f,-7.4f);preview.transform.LookAt(stage.position+new Vector3(0,-.12f,0));
            preview.enabled=false;preview.fieldOfView=34;preview.nearClipPlane=.1f;preview.farClipPlane=15;preview.cullingMask=1<<31;
            preview.clearFlags=CameraClearFlags.SolidColor;preview.backgroundColor=Color.clear;
            target=new RenderTexture(720,640,16,RenderTextureFormat.ARGB32){name="Home card showcase",antiAliasing=4};resources.Add(target);preview.targetTexture=target;GetComponent<RawImage>().texture=target;
            var shell=new Material(metal);shell.color=new Color(.09f,.2f,.32f);resources.Add(shell);
            Material cyan=Colour(new Color(.12f,.8f,.95f)), violet=Colour(new Color(.55f,.24f,.92f)), dark=Colour(new Color(.025f,.05f,.09f));
            Primitive("Floating plinth",PrimitiveType.Cylinder,stage,new Vector3(0,-1.58f,.1f),new Vector3(4.2f,.09f,2.65f),shell);
            Primitive("Inset platform",PrimitiveType.Cylinder,stage,new Vector3(0,-1.47f,.1f),new Vector3(3.85f,.025f,2.4f),dark);
            Ring("Cyan platform rim",stage,2.02f,1.26f,-1.49f,.1f,cyan,.025f,true);
            Ring("Lower violet rim",stage,1.97f,1.24f,-1.67f,.1f,violet,.015f,true);
            Ring("Inner orbit",stage,1.62f,1,-1.43f,.1f,cyan,.012f,true);
            orbit=new GameObject("Orbital light rails").transform;orbit.SetParent(stage,false);orbit.localPosition=new Vector3(0,.12f,.95f);
            Ring("Rear halo",orbit,2.04f,1.86f,0,0,violet,.014f,false);
            Ring("Inset halo",orbit,1.91f,1.73f,0,.04f,cyan,.008f,false);
            turntable=new GameObject("Featured cards").transform;turntable.SetParent(stage,false);
            for(int i=0;i<3;i++)
            {
                cards[i]=new GameObject("Home featured card "+i).transform;cards[i].SetParent(turntable,false);
                var edge=new Material(metal){color=i==0?new Color(.15f,.75f,1):new Color(.48f,.22f,.85f)};resources.Add(edge);
                Primitive("Bevel",PrimitiveType.Cube,cards[i],Vector3.zero,new Vector3(1.64f,2.5f,.07f),edge);
                var face=Primitive("Original artwork",PrimitiveType.Quad,cards[i],new Vector3(0,0,-.043f),new Vector3(1.59f,2.44f,1),null);
                var material=new Material(Shader.Find("Unlit/Texture"));material.mainTexture=collection[i%collection.Length].originalCardArt;resources.Add(material);face.GetComponent<Renderer>().sharedMaterial=material;
            }
            for(int i=0;i<sparks.Length;i++)
                sparks[i]=Primitive("Orbit mote "+i,PrimitiveType.Quad,stage,Vector3.zero,Vector3.one*(i%3==0?.027f:.013f),i%2==0?cyan:violet);
            foreach(var t in stage.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            initialized=true;openedAt=Time.unscaledTime;Pose();RenderPreview();
        }
        void OnEnable(){openedAt=Time.unscaledTime;pointer=int.MinValue;touch=tilt=Vector2.zero;if(stage!=null)stage.gameObject.SetActive(true);}
        void OnDisable(){pointer=int.MinValue;touch=tilt=Vector2.zero;if(stage!=null)stage.gameObject.SetActive(false);}
        void LateUpdate(){RenderPreview();}
        void Pose()
        {
            bool reduced=GameSettings.ReducedMotion;float time=reduced?0:Time.unscaledTime-openedAt;
            float entry=reduced?1:Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.7f));
            tilt=reduced?Vector2.zero:Vector2.Lerp(tilt,touch,1-Mathf.Exp(-Time.unscaledDeltaTime*8));
            turntable.localRotation=Quaternion.Euler(-tilt.y*5,tilt.x*11+(reduced?0:Mathf.Sin(time*.45f)*3),0);
            for(int i=0;i<3;i++)
            {
                bool featured=i==0;float side=i==1?-1:1;
                Vector3 p=featured?new Vector3(0,.18f,-.48f):new Vector3(side*1.17f,-.07f,.46f);
                p.y+=reduced?0:Mathf.Sin(time*.85f+i*.8f)*.055f;
                p.y-=(1-entry)*.55f;
                cards[i].localPosition=p;cards[i].localScale=Vector3.one*(featured?1.04f:.76f)*Mathf.Lerp(.9f,1,entry);
                cards[i].localRotation=Quaternion.Euler(featured?-3:2,featured?-7:side*-20,featured?-3:side*-9+(1-entry)*side*14);
            }
            orbit.localRotation=Quaternion.Euler(0,0,reduced?14:14+time*2.2f);
            for(int i=0;i<sparks.Length;i++)
            {
                float angle=i*Mathf.PI*2/sparks.Length+time*.09f;
                sparks[i].localPosition=new Vector3(Mathf.Cos(angle)*(1.9f+i%3*.12f),Mathf.Sin(angle)*1.6f+.05f,.8f+Mathf.Sin(angle*2)*.3f);
            }
        }
        public void RenderPreview(){if(initialized && stage.gameObject.activeSelf){Pose();preview.Render();}}
        public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;ReadTouch(e);}
        public void OnDrag(PointerEventData e){if(e.pointerId==pointer)ReadTouch(e);}
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer){pointer=int.MinValue;touch=Vector2.zero;}}
        public void OnPointerExit(PointerEventData e){OnPointerUp(e);}
        void ReadTouch(PointerEventData e)
        {
            if(GameSettings.ReducedMotion)return;
            var rect=(RectTransform)transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out var p);
            touch=new Vector2(Mathf.Clamp((p.x-rect.rect.center.x)/rect.rect.width*2,-1,1),Mathf.Clamp((p.y-rect.rect.center.y)/rect.rect.height*2,-1,1));
        }
        Material Colour(Color colour){var m=new Material(Shader.Find("Unlit/Color")){color=colour};resources.Add(m);return m;}
        Transform Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;Destroy(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            if(material!=null)go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static void Ring(string name,Transform parent,float rx,float ry,float y,float z,Material material,float width,bool floor)
        {
            var go=new GameObject(name,typeof(LineRenderer));go.transform.SetParent(parent,false);var line=go.GetComponent<LineRenderer>();
            line.useWorldSpace=false;line.loop=true;line.positionCount=96;line.widthMultiplier=width;line.sharedMaterial=material;
            for(int i=0;i<96;i++){float a=i*Mathf.PI*2/96;line.SetPosition(i,floor?new Vector3(Mathf.Cos(a)*rx,y,Mathf.Sin(a)*ry+z):new Vector3(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry+y,z));}
        }
        void OnDestroy(){if(stage!=null)Destroy(stage.gameObject);if(target!=null)target.Release();foreach(var resource in resources)if(resource!=null)Destroy(resource);}
    }
}
