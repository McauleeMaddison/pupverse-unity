using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace Pupverse
{
    public readonly struct AppleAccountCredential
    {
        public readonly string Token,RawNonce;
        public AppleAccountCredential(string token,string rawNonce){Token=token;RawNonce=rawNonce;}
    }
    public interface IAppleAccountProvider {bool Available {get;} Task<AppleAccountCredential> SignIn(CancellationToken cancellation);}
    public sealed class AppleAccountProvider : MonoBehaviour,IAppleAccountProvider
    {
        TaskCompletionSource<AppleAccountCredential> pending;
        string requestId,rawNonce;
        public bool Available {
            get {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PupverseAppleSignIn(string receiver,string nonceHash,string state);
        [DllImport("__Internal")] static extern void PupverseAppleCancel(string state);
#endif
        public static string NewNonce(){var bytes=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(bytes);return Hex(bytes);}
        public static string HashNonce(string value){using(var hash=SHA256.Create())return Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(value)));}
        static string Hex(byte[] bytes)=>BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();
        public async Task<AppleAccountCredential> SignIn(CancellationToken cancellation)
        {
            if(!Available)throw new InvalidOperationException("Apple sign-in requires an iOS device build.");
            if(pending!=null)throw new InvalidOperationException("Apple sign-in already in progress.");
            string id=Guid.NewGuid().ToString("N");requestId=id;rawNonce=NewNonce();var completion=new TaskCompletionSource<AppleAccountCredential>();pending=completion;
            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                PupverseAppleSignIn(gameObject.name,HashNonce(rawNonce),id);
#endif
                var deadline=DateTime.UtcNow.AddSeconds(90);
                while(!completion.Task.IsCompleted)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if(DateTime.UtcNow>=deadline)throw new OperationCanceledException();
                    await Task.Yield();
                }
                cancellation.ThrowIfCancellationRequested();return await completion.Task;
            }
            finally
            {
#if UNITY_IOS && !UNITY_EDITOR
                if(!completion.Task.IsCompleted)PupverseAppleCancel(id);
#endif
                pending=null;requestId=rawNonce=null;
            }
        }
        [Serializable] sealed class NativeReply {public string request=null,state=null,token=null,status=null;}
        [Preserve] public void OnAppleAccountResult(string json)
        {
            if(pending==null)return;
            NativeReply reply;try{reply=JsonUtility.FromJson<NativeReply>(json);}catch(ArgumentException){return;}
            if(reply==null || reply.request!=requestId)return;
            if(reply.status=="cancelled"){pending.TrySetCanceled();return;}
            if(reply.status!="ok" || reply.state!=requestId || string.IsNullOrEmpty(reply.token)){pending.TrySetException(new InvalidOperationException("Apple sign-in could not be completed."));return;}
            pending.TrySetResult(new AppleAccountCredential(reply.token,rawNonce));
        }
        void OnDestroy()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if(pending!=null && !string.IsNullOrEmpty(requestId))PupverseAppleCancel(requestId);
#endif
            pending?.TrySetCanceled();
        }
    }
}
