using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pupverse
{
    public enum AccountAction { SignIn, CreateAccount, ResetPassword, Apple }
    public sealed class AccountIdentity
    {
        public readonly string UserId,Email,Provider;
        public readonly DateTime ExpiresAt;
        public AccountIdentity(string id,string email,string provider,DateTime expiresAt){UserId=id;Email=email;Provider=provider;ExpiresAt=expiresAt;}
    }
    public sealed class AccountReply
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly AccountIdentity Identity;
        public AccountReply(bool success,string message,AccountIdentity identity=null){Success=success;Message=message;Identity=identity;}
    }
    public interface IPlayerAccountBackend
    {
        bool EmailAvailable {get;}
        bool AppleAvailable {get;}
        Task<AccountReply> Execute(AccountAction action,string email,string password,CancellationToken cancellation);
    }
    // Authentication identity only. It does not own, merge or upload the device-local collection.
    public sealed class PlayerAccountService
    {
        readonly IPlayerAccountBackend backend;
        CancellationTokenSource pending;
        int generation;
        public event Action Changed;
        public AccountIdentity Identity {get;private set;}
        public bool IsSignedIn => Identity!=null && Identity.ExpiresAt>DateTime.UtcNow;
        public bool Busy {get;private set;}
        public bool EmailAvailable => backend.EmailAvailable;
        public bool AppleAvailable => backend.AppleAvailable;
        public string Status {get;private set;}="Cards and coins are saved on this device. Cloud backup is not connected yet.";
        public PlayerAccountService(IPlayerAccountBackend backend){this.backend=backend;}
        public static string Validate(AccountAction action,string email,string password,string confirmation)
        {
            if(action==AccountAction.Apple)return "";
            email=(email??"").Trim();int at=email.IndexOf('@');int dot=email.LastIndexOf('.');
            if(email.Length>254 || at<1 || at!=email.LastIndexOf('@') || dot<at+2 || dot>=email.Length-1 || Array.Exists(email.ToCharArray(),char.IsWhiteSpace))return "Enter a valid email address.";
            if(action==AccountAction.ResetPassword)return "";
            if(string.IsNullOrEmpty(password))return "Enter your password.";
            if(password.Length>4096)return "That password is too long.";
            if(action==AccountAction.CreateAccount && password.Length<6)return "Use at least 6 characters for your password.";
            if(action==AccountAction.CreateAccount && password!=confirmation)return "Your passwords do not match.";
            return "";
        }
        public async Task<bool> Submit(AccountAction action,string email,string password,string confirmation="")
        {
            if(Busy)return false;
            string validation=Validate(action,email,password,confirmation);
            if(validation.Length>0){Status=validation;Changed?.Invoke();return false;}
            if(action==AccountAction.Apple?!AppleAvailable:!EmailAvailable)
            {Status=action==AccountAction.Apple?"Apple sign-in is not available in this build yet. You can continue as a guest.":"Account sign-in is not connected yet. You can continue as a guest.";Changed?.Invoke();return false;}
            int request=++generation;var cancellation=new CancellationTokenSource();pending=cancellation;Busy=true;Status="Connecting securely…";Changed?.Invoke();
            try
            {
                var reply=await backend.Execute(action,(email??"").Trim(),password,cancellation.Token);
                if(request!=generation || cancellation.IsCancellationRequested)return false;
                bool needsIdentity=action!=AccountAction.ResetPassword;
                if(reply==null || (reply.Success && needsIdentity && (reply.Identity==null || string.IsNullOrEmpty(reply.Identity.UserId) || reply.Identity.ExpiresAt<=DateTime.UtcNow)))
                {Status="Sign-in could not be completed. Please try again.";return false;}
                Status=reply.Message;
                if(reply.Success && needsIdentity)Identity=reply.Identity;
                return reply.Success;
            }
            catch(OperationCanceledException){if(request==generation)Status="Sign-in cancelled.";return false;}
            catch(Exception){if(request==generation)Status="Unable to connect. Please try again.";return false;}
            finally
            {
                if(request==generation){Busy=false;pending=null;Changed?.Invoke();}
                cancellation.Dispose();
            }
        }
        public void Cancel(){bool wasBusy=Busy;generation++;pending?.Cancel();pending=null;Busy=false;if(wasBusy)Status="Sign-in cancelled.";Changed?.Invoke();}
        public void ContinueAsGuest(){Cancel();Identity=null;Status="Playing as guest. Progress stays on this device.";Changed?.Invoke();}
        public void Tick(){if(Identity!=null && !IsSignedIn){Identity=null;Status="Your session ended. Sign in again when you're ready.";Changed?.Invoke();}}
    }
}
