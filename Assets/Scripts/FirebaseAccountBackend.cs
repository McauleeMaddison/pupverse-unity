using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Pupverse
{
    public readonly struct AccountHttpReply
    {
        public readonly long Code;public readonly string Body;
        public AccountHttpReply(long code,string body){Code=code;Body=body;}
    }
    public interface IAccountHttpTransport {Task<AccountHttpReply> Post(string url,string json,CancellationToken cancellation);}
    public sealed class UnityAccountHttpTransport : IAccountHttpTransport
    {
        public async Task<AccountHttpReply> Post(string url,string json,CancellationToken cancellation)
        {
            using(var request=new UnityWebRequest(url,"POST"))
            {
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));request.downloadHandler=new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type","application/json");request.timeout=20;request.redirectLimit=0;
                var operation=request.SendWebRequest();
                while(!operation.isDone){if(cancellation.IsCancellationRequested){request.Abort();cancellation.ThrowIfCancellationRequested();}await Task.Yield();}
                cancellation.ThrowIfCancellationRequested();return new AccountHttpReply(request.responseCode,request.downloadHandler.text);
            }
        }
    }
    public sealed class FirebaseAccountBackend : IPlayerAccountBackend
    {
        readonly PlayerAccountConfig config;readonly IAccountHttpTransport transport;readonly IAppleAccountProvider apple;
        public bool EmailAvailable => config!=null && config.IsConfigured && config.emailEnabled;
        public bool AppleAvailable => config!=null && config.IsConfigured && config.appleEnabled && apple!=null && apple.Available;
        public FirebaseAccountBackend(PlayerAccountConfig config,IAccountHttpTransport transport,IAppleAccountProvider apple){this.config=config;this.transport=transport;this.apple=apple;}
        [Serializable] sealed class EmailRequest {public string email,password;public bool returnSecureToken=true;}
        [Serializable] sealed class ResetRequest {public string requestType="PASSWORD_RESET",email;}
        [Serializable] sealed class AppleRequest {public string requestUri,postBody;public bool returnSecureToken=true;}
        [Serializable] sealed class Response {public string localId=null,email=null,idToken=null,expiresIn=null;public bool needConfirmation=false;}
        [Serializable] sealed class ErrorBody {public Error error=null;}
        [Serializable] sealed class Error {public string message=null;}
        public async Task<AccountReply> Execute(AccountAction action,string email,string password,CancellationToken cancellation)
        {
            if(action==AccountAction.Apple?!AppleAvailable:!EmailAvailable)return new AccountReply(false,"Account sign-in is not connected yet. Continue as guest.");
            string endpoint,json;
            if(action==AccountAction.Apple)
            {
                var credential=await apple.SignIn(cancellation);cancellation.ThrowIfCancellationRequested();
                endpoint="signInWithIdp";
                json=JsonUtility.ToJson(new AppleRequest {requestUri="https://"+config.firebaseProjectId+".firebaseapp.com/__/auth/handler",postBody="providerId=apple.com&id_token="+Uri.EscapeDataString(credential.Token)+"&nonce="+Uri.EscapeDataString(credential.RawNonce)});
            }
            else if(action==AccountAction.ResetPassword){endpoint="sendOobCode";json=JsonUtility.ToJson(new ResetRequest{email=email});}
            else {endpoint=action==AccountAction.CreateAccount?"signUp":"signInWithPassword";json=JsonUtility.ToJson(new EmailRequest{email=email,password=password});}
            var result=await transport.Post("https://identitytoolkit.googleapis.com/v1/accounts:"+endpoint+"?key="+Uri.EscapeDataString(config.firebaseWebApiKey),json,cancellation);
            cancellation.ThrowIfCancellationRequested();
            string code="";
            if(result.Code!=200)
            {
                try{code=JsonUtility.FromJson<ErrorBody>(result.Body)?.error?.message??"";}catch(ArgumentException){}
                if(!(action==AccountAction.ResetPassword && code.StartsWith("EMAIL_NOT_FOUND",StringComparison.Ordinal)))return new AccountReply(false,ErrorMessage(code));
            }
            if(action==AccountAction.ResetPassword)return new AccountReply(true,"If this email has an account, a password-reset link will arrive shortly. Check your inbox and spam folder.");
            Response response;
            try{response=JsonUtility.FromJson<Response>(result.Body);}catch(ArgumentException){return new AccountReply(false,"Sign-in could not be completed. Try again.");}
            if(response==null || response.needConfirmation || string.IsNullOrEmpty(response.localId) || string.IsNullOrEmpty(response.idToken) || !int.TryParse(response.expiresIn,out int seconds) || seconds<=0)
                return new AccountReply(false,"Sign-in could not be completed. If you already have an account, use its original sign-in method.");
            // This first pass keeps identity in memory only. Tokens/passwords are never written to PlayerPrefs.
            // Authenticated cloud data and refresh-token storage will be introduced with cloud save.
            var identity=new AccountIdentity(response.localId,response.email??"",action==AccountAction.Apple?"Apple":"Email",DateTime.UtcNow.AddSeconds(Math.Min(seconds,86400)));
            return new AccountReply(true,action==AccountAction.CreateAccount?"Account created. Your cards and coins still stay on this device.":"You're signed in. Cloud progress backup is not connected yet.",identity);
        }
        public static string ErrorMessage(string code)
        {
            code=code??"";
            if(code.StartsWith("INVALID_LOGIN_CREDENTIALS") || code.StartsWith("EMAIL_NOT_FOUND") || code.StartsWith("INVALID_PASSWORD"))return "Email or password wasn't recognised. Check them and try again.";
            if(code.StartsWith("EMAIL_EXISTS"))return "Unable to create that account. Try signing in or resetting your password.";
            if(code.StartsWith("WEAK_PASSWORD") || code.StartsWith("PASSWORD_DOES_NOT_MEET_REQUIREMENTS"))return "Choose a stronger password and try again.";
            if(code.StartsWith("INVALID_EMAIL"))return "Check your email address and try again.";
            if(code.StartsWith("TOO_MANY_ATTEMPTS") || code.StartsWith("TOO_MANY_REQUESTS"))return "Too many attempts. Please wait before trying again.";
            if(code.StartsWith("USER_DISABLED"))return "This account is unavailable. Contact support.";
            if(code.StartsWith("OPERATION_NOT_ALLOWED") || code.StartsWith("CONFIGURATION_NOT_FOUND") || code.StartsWith("API_KEY_INVALID"))return "Account sign-in is not available in this build yet. Continue as guest.";
            return "Unable to sign in. Check your connection and try again.";
        }
    }
}
