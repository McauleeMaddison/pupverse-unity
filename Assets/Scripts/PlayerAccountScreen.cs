using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Pupverse
{
    public sealed class PlayerAccountScreen : MonoBehaviour
    {
        PlayerAccountService service;Action closed;Font font;
        RectTransform root,viewport,content;ScrollRect scroll;
        Text title,subtitle,heading,description,status,divider,emailLabel,passwordLabel,confirmLabel;
        Button apple,signInTab,createTab,submit,reset,back,guest;
        InputField email,password,confirmation;AccountAction mode=AccountAction.SignIn;
        public bool IsOpen => root!=null && root.gameObject.activeSelf;
        public AccountAction Mode => mode;
        public void Initialize(Transform parent,PlayerAccountService accounts,Font typeface,Action onClosed)
        {
            service=accounts;closed=onClosed;font=typeface;
            root=Rect("Player account screen",parent);Stretch(root);var background=root.gameObject.AddComponent<BattleMenuBackdrop>();background.opacity=.99f;background.raycastTarget=true;
            title=Label("Account title",root,"YOUR PUPVERSE",27,Color.white);subtitle=Label("Account subtitle",root,"ONE PLAYER. A WORLD OF CARDS.",10,Cyan);
            viewport=Rect("Account scroll",root);viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.001f);viewport.gameObject.AddComponent<RectMask2D>();
            scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            content=Rect("Account form",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);scroll.content=content;
            heading=Label("Form heading",content,"",22,Color.white);description=Label("Form description",content,"",13,Muted);description.supportRichText=false;
            apple=MakeButton("Continue with Apple",content,()=>{_ = SubmitApple();},16);apple.GetComponent<BattleHudGraphic>().accent=Color.white;
            divider=Label("Email divider",content,"OR USE YOUR EMAIL",10,Muted);divider.alignment=TextAnchor.MiddleCenter;
            signInTab=MakeButton("SIGN IN",content,()=>SetMode(AccountAction.SignIn),12);createTab=MakeButton("CREATE ACCOUNT",content,()=>SetMode(AccountAction.CreateAccount),12);
            emailLabel=Label("Email label",content,"EMAIL ADDRESS",10,Cyan);email=Field("Email address",content,"you@example.com",false);
            passwordLabel=Label("Password label",content,"PASSWORD",10,Cyan);password=Field("Password",content,"Your password",true);
            confirmLabel=Label("Confirm label",content,"CONFIRM PASSWORD",10,Cyan);confirmation=Field("Confirm password",content,"Repeat your password",true);
            submit=MakeButton("SIGN IN",content,()=>{_ = Submit();},15);submit.GetComponent<BattleMenuMotion>().featured=true;
            reset=MakeButton("FORGOT PASSWORD?",content,()=>SetMode(AccountAction.ResetPassword),11);
            status=Label("Account status",content,"",12,Muted);status.alignment=TextAnchor.UpperLeft;
            back=MakeButton("BACK HOME",root,Close,12);guest=MakeButton("CONTINUE AS GUEST",root,ContinueAsGuest,12);
            service.Changed+=Refresh;root.gameObject.SetActive(false);
        }
        static readonly Color Cyan=new Color(.3f,.91f,1),Muted=new Color(.65f,.75f,.85f);
        public void Open(){root.gameObject.SetActive(true);root.SetAsLastSibling();SetMode(AccountAction.SignIn);Refresh();LateUpdate();}
        public void SetMode(AccountAction action)
        {
            if(service.Busy || action==AccountAction.Apple)return;mode=action;ClearPasswords();content.anchoredPosition=Vector2.zero;Refresh();
        }
        public void SetCredentials(string address,string secret,string repeat=""){email.text=address;password.text=secret;confirmation.text=repeat;}
        public async Task<bool> Submit()
        {
            if(service.Busy)return false;
            string address=email.text,secret=password.text,repeat=confirmation.text;ClearPasswords();
            return await service.Submit(mode,address,secret,repeat);
        }
        public async Task<bool> SubmitApple(){ClearPasswords();return await service.Submit(AccountAction.Apple,"","");}
        public void ContinueAsGuest(){service.ContinueAsGuest();Close();}
        public void Close(){service.Cancel();email.text="";ClearPasswords();root.gameObject.SetActive(false);closed?.Invoke();}
        void ClearPasswords(){password.text="";confirmation.text="";}
        void Refresh()
        {
            if(root==null)return;bool signed=service.IsSignedIn,busy=service.Busy,creating=mode==AccountAction.CreateAccount,recovering=mode==AccountAction.ResetPassword;
            heading.text=signed?"WELCOME BACK":recovering?"RESET PASSWORD":creating?"JOIN PUPVERSE":"MAKE IT YOURS";
            description.text=signed?"Signed in with "+service.Identity.Provider+".\n"+(string.IsNullOrEmpty(service.Identity.Email)?"Your Apple identity is connected.":service.Identity.Email)+"\n\nCards and coins stay on this device. Cloud backup is not connected yet.":"Play as a guest or connect your identity.\nYour cards and coins stay on this device.";
            apple.gameObject.SetActive(!signed && !recovering);divider.gameObject.SetActive(!signed && !recovering);
            apple.interactable=!busy && service.AppleAvailable;
            signInTab.gameObject.SetActive(!signed);createTab.gameObject.SetActive(!signed);
            signInTab.interactable=createTab.interactable=!busy;
            signInTab.GetComponent<BattleHudGraphic>().surfaceOpacity=mode==AccountAction.SignIn?.8f:.3f;createTab.GetComponent<BattleHudGraphic>().surfaceOpacity=creating?.8f:.3f;
            emailLabel.gameObject.SetActive(!signed);email.gameObject.SetActive(!signed);
            passwordLabel.gameObject.SetActive(!signed && !recovering);password.gameObject.SetActive(!signed && !recovering);
            confirmLabel.gameObject.SetActive(!signed && creating);confirmation.gameObject.SetActive(!signed && creating);
            email.interactable=password.interactable=confirmation.interactable=!busy && service.EmailAvailable;
            submit.gameObject.SetActive(!signed);submit.interactable=!busy && service.EmailAvailable;
            submit.GetComponentInChildren<Text>().text=busy?"CONNECTING…":recovering?"SEND RESET LINK":creating?"CREATE ACCOUNT":"SIGN IN";
            reset.gameObject.SetActive(!signed && !recovering);reset.interactable=!busy;
            guest.GetComponentInChildren<Text>().text=signed?"SIGN OUT / GUEST":"CONTINUE AS GUEST";
            status.text=!service.EmailAvailable && !signed?"Sign-in is being connected for this build.\nContinue as a guest for now.":service.Status;
            if(service.EmailAvailable && !service.AppleAvailable && !signed && !recovering)status.text+="\nApple sign-in requires a configured iPhone build.";
            LateUpdate();
        }
        void LateUpdate()
        {
            if(!IsOpen)return;
            float w=root.rect.width,h=root.rect.height;var canvas=root.GetComponentInParent<Canvas>();
            float keyboard=TouchScreenKeyboard.visible?TouchScreenKeyboard.area.height/Mathf.Max(.01f,canvas.scaleFactor):0;
            keyboard=Mathf.Clamp(keyboard,0,h*.65f);
            Place(title.rectTransform,22,h-60,w-44,38);Place(subtitle.rectTransform,24,h-82,w-48,20);
            Place(viewport,20,82+keyboard,w-40,Mathf.Max(60,h-182-keyboard));
            float width=Mathf.Min(420,w-44),x=(w-40-width)/2;
            bool signed=service.IsSignedIn,recovering=mode==AccountAction.ResetPassword,creating=mode==AccountAction.CreateAccount;
            float height=signed?280:recovering?400:creating?728:640;
            content.sizeDelta=new Vector2(0,height);float y=height;
            Place(heading.rectTransform,x,y-38,width,34);y-=40;float dh=signed?122:50;Place(description.rectTransform,x,y-dh,width,dh);y-=dh+12;
            if(!signed)
            {
                if(!recovering){Place((RectTransform)apple.transform,x,y-54,width,54);y-=66;Place(divider.rectTransform,x,y-20,width,20);y-=34;}
                Place((RectTransform)signInTab.transform,x,y-44,(width-8)/2,44);Place((RectTransform)createTab.transform,x+(width+8)/2,y-44,(width-8)/2,44);y-=58;
                Place(emailLabel.rectTransform,x,y-16,width,16);y-=22;Place((RectTransform)email.transform,x,y-48,width,48);y-=60;
                if(!recovering){Place(passwordLabel.rectTransform,x,y-16,width,16);y-=22;Place((RectTransform)password.transform,x,y-48,width,48);y-=60;}
                if(creating){Place(confirmLabel.rectTransform,x,y-16,width,16);y-=22;Place((RectTransform)confirmation.transform,x,y-48,width,48);y-=60;}
                Place((RectTransform)submit.transform,x,y-52,width,52);y-=62;
                if(!recovering){Place((RectTransform)reset.transform,x,y-44,width,44);y-=52;}
            }
            Place(status.rectTransform,x,y-76,width,76);
            Place((RectTransform)back.transform,20,16,(w-50)*.38f,52);Place((RectTransform)guest.transform,30+(w-50)*.38f,16,(w-50)*.62f,52);
            if(keyboard>0 && EventSystem.current!=null)
            {
                var selected=EventSystem.current.currentSelectedGameObject;
                if(selected==email.gameObject || selected==password.gameObject || selected==confirmation.gameObject)
                {
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,selected.transform);
                    float adjustment=bounds.min.y<viewport.rect.yMin+8?viewport.rect.yMin+8-bounds.min.y:bounds.max.y>viewport.rect.yMax-8?viewport.rect.yMax-8-bounds.max.y:0;
                    content.anchoredPosition=new Vector2(0,Mathf.Clamp(content.anchoredPosition.y+adjustment,0,Mathf.Max(0,height-viewport.rect.height)));
                }
            }
        }
        InputField Field(string name,Transform parent,string placeholder,bool secret)
        {
            var r=Rect(name,parent);var surface=r.gameObject.AddComponent<Image>();surface.color=new Color(.04f,.09f,.15f,.95f);
            var field=r.gameObject.AddComponent<InputField>();field.targetGraphic=surface;field.lineType=InputField.LineType.SingleLine;field.contentType=secret?InputField.ContentType.Password:InputField.ContentType.EmailAddress;
            field.characterLimit=secret?4096:254;field.shouldHideMobileInput=false;
            var text=Label("Input text",r,"",15,Color.white);text.fontStyle=FontStyle.Normal;text.supportRichText=false;Inset(text.rectTransform);field.textComponent=text;
            var hint=Label("Placeholder",r,placeholder,14,Muted);hint.fontStyle=FontStyle.Normal;Inset(hint.rectTransform);field.placeholder=hint;
            return field;
        }
        static void Inset(RectTransform rect){Stretch(rect);rect.offsetMin=new Vector2(14,6);rect.offsetMax=new Vector2(-14,-6);}
        Button MakeButton(string text,Transform parent,UnityEngine.Events.UnityAction action,int size)
        {
            var r=Rect(text,parent);var g=r.gameObject.AddComponent<BattleHudGraphic>();g.shape=BattleHudGraphic.Shape.Panel;g.accent=Cyan;g.surfaceOpacity=.5f;r.gameObject.AddComponent<BattleMenuMotion>();
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=g;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(action);
            var label=Label("Label",r,text,size,Color.white);Stretch(label.rectTransform);label.alignment=TextAnchor.MiddleCenter;return b;
        }
        Text Label(string name,Transform parent,string text,int size,Color colour){var t=Rect(name,parent).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Bold;t.text=text;t.color=colour;t.raycastTarget=false;return t;}
        static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Place(RectTransform r,float x,float y,float w,float h)=>Battle3DController.Place(r,x,y,w,h);
        void OnDestroy(){if(service!=null){service.Changed-=Refresh;service.Cancel();}if(root!=null)Destroy(root.gameObject);}
    }
}
