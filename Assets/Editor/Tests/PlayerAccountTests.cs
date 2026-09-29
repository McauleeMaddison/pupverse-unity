using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
namespace Pupverse.Tests
{
    public class PlayerAccountTests
    {
        sealed class Backend : IPlayerAccountBackend
        {
            public bool EmailAvailable=>true;public bool AppleAvailable=>true;public int Calls;
            public TaskCompletionSource<AccountReply> Completion=new TaskCompletionSource<AccountReply>();
            public Task<AccountReply> Execute(AccountAction a,string e,string p,CancellationToken c){Calls++;return Completion.Task;}
        }
        sealed class Transport : IAccountHttpTransport
        {
            public int Calls;public string Url,Json;
            public AccountHttpReply Reply=new AccountHttpReply(200,"{\"localId\":\"test-player\",\"email\":\"player@example.com\",\"idToken\":\"test-token\",\"expiresIn\":\"3600\"}");
            public Task<AccountHttpReply> Post(string url,string json,CancellationToken cancellation){Calls++;Url=url;Json=json;return Task.FromResult(Reply);}
        }
        sealed class Apple : IAppleAccountProvider
        {
            public bool Available=>true;
            public Task<AppleAccountCredential> SignIn(CancellationToken c)=>Task.FromResult(new AppleAccountCredential("token+&=","nonce+&="));
        }
        [Serializable] sealed class Posted {public string email=null,password=null,requestType=null,postBody=null,requestUri=null;public bool returnSecureToken=false;}
        PlayerAccountConfig config;Transport transport;
        [SetUp] public void Setup(){config=ScriptableObject.CreateInstance<PlayerAccountConfig>();config.firebaseProjectId="test-project";config.firebaseWebApiKey="public-test-key";config.appleEnabled=true;transport=new Transport();}
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(config);}
        FirebaseAccountBackend Firebase()=>new FirebaseAccountBackend(config,transport,new Apple());
        [Test] public async Task RepeatedTapAndLateCancelledCompletionCannotSignIn()
        {
            var backend=new Backend();var service=new PlayerAccountService(backend);
            var first=service.Submit(AccountAction.SignIn,"player@example.com","private-password");
            Assert.That(service.Busy,Is.True);Assert.That(await service.Submit(AccountAction.SignIn,"player@example.com","private-password"),Is.False);Assert.That(backend.Calls,Is.EqualTo(1));
            service.ContinueAsGuest();backend.Completion.SetResult(new AccountReply(true,"Signed in",new AccountIdentity("id","player@example.com","Email",DateTime.UtcNow.AddHours(1))));
            Assert.That(await first,Is.False);Assert.That(service.IsSignedIn,Is.False);Assert.That(service.Busy,Is.False);
        }
        [Test] public async Task UnknownBackendErrorsNeverExposeCredentials()
        {
            var backend=new Backend();var service=new PlayerAccountService(backend);var task=service.Submit(AccountAction.SignIn,"player@example.com","private-password");
            backend.Completion.SetException(new Exception("private-password secret-token"));Assert.That(await task,Is.False);
            Assert.That(service.Status,Does.Not.Contain("private-password"));Assert.That(service.Status,Does.Not.Contain("secret-token"));Assert.That(service.Busy,Is.False);
        }
        [TestCase(AccountAction.SignIn,"bad","password","",false)]
        [TestCase(AccountAction.SignIn,"player@example.com","","",false)]
        [TestCase(AccountAction.CreateAccount,"player@example.com","short","short",false)]
        [TestCase(AccountAction.CreateAccount,"player@example.com","long-password","mismatch",false)]
        [TestCase(AccountAction.CreateAccount,"player@example.com","long-password","long-password",true)]
        [TestCase(AccountAction.ResetPassword,"player@example.com","","",true)]
        public void FormValidation(AccountAction action,string email,string password,string confirmation,bool valid)
        {Assert.That(string.IsNullOrEmpty(PlayerAccountService.Validate(action,email,password,confirmation)),Is.EqualTo(valid));}
        [Test] public async Task UnconfiguredBuildDoesNotMakeNetworkRequestsOrFakeLogin()
        {
            config.firebaseWebApiKey="";var service=new PlayerAccountService(Firebase());
            Assert.That(await service.Submit(AccountAction.SignIn,"player@example.com","private-password"),Is.False);
            Assert.That(await service.Submit(AccountAction.Apple,"",""),Is.False);Assert.That(transport.Calls,Is.Zero);Assert.That(service.IsSignedIn,Is.False);
        }
        [TestCase(AccountAction.SignIn,"signInWithPassword")]
        [TestCase(AccountAction.CreateAccount,"signUp")]
        public async Task EmailUsesFirebaseHttpsAndOnlyAcceptsAValidServerIdentity(AccountAction action,string endpoint)
        {
            var service=new PlayerAccountService(Firebase());Assert.That(await service.Submit(action," player@example.com ","private-password","private-password"),Is.True);
            Assert.That(transport.Url,Is.EqualTo("https://identitytoolkit.googleapis.com/v1/accounts:"+endpoint+"?key=public-test-key"));
            var body=JsonUtility.FromJson<Posted>(transport.Json);Assert.That(body.email,Is.EqualTo("player@example.com"));Assert.That(body.password,Is.EqualTo("private-password"));Assert.That(body.returnSecureToken,Is.True);
            Assert.That(service.Identity.UserId,Is.EqualTo("test-player"));Assert.That(service.IsSignedIn,Is.True);string status=service.Status;service.Cancel();Assert.That(service.IsSignedIn,Is.True);Assert.That(service.Status,Is.EqualTo(status));service.ContinueAsGuest();Assert.That(service.Identity,Is.Null);
        }
        [Test] public async Task ResetResponseDoesNotRevealWhetherEmailExists()
        {
            var backend=Firebase();transport.Reply=new AccountHttpReply(200,"{}");var found=await backend.Execute(AccountAction.ResetPassword,"player@example.com","",CancellationToken.None);
            Assert.That(JsonUtility.FromJson<Posted>(transport.Json).requestType,Is.EqualTo("PASSWORD_RESET"));
            transport.Reply=new AccountHttpReply(400,"{\"error\":{\"message\":\"EMAIL_NOT_FOUND\"}}");var missing=await backend.Execute(AccountAction.ResetPassword,"player@example.com","",CancellationToken.None);
            Assert.That(missing.Success,Is.True);Assert.That(missing.Message,Is.EqualTo(found.Message));
        }
        [Test] public async Task AppleExchangesTokenAndRawNonceWithoutAnEmptyAccessToken()
        {
            Assert.That((await Firebase().Execute(AccountAction.Apple,"","",CancellationToken.None)).Success,Is.True);
            var body=JsonUtility.FromJson<Posted>(transport.Json);Assert.That(body.postBody,Is.EqualTo("providerId=apple.com&id_token=token%2B%26%3D&nonce=nonce%2B%26%3D"));
            Assert.That(body.requestUri,Is.EqualTo("https://test-project.firebaseapp.com/__/auth/handler"));Assert.That(body.postBody,Does.Not.Contain("access_token"));
        }
        [TestCase("{}")]
        [TestCase("{\"localId\":\"id\",\"idToken\":\"token\",\"expiresIn\":\"0\"}")]
        [TestCase("{\"localId\":\"id\",\"idToken\":\"token\",\"expiresIn\":\"3600\",\"needConfirmation\":true}")]
        public async Task IncompleteOrExpiredCredentialsDoNotCreateASession(string response)
        {
            transport.Reply=new AccountHttpReply(200,response);var service=new PlayerAccountService(Firebase());
            Assert.That(await service.Submit(AccountAction.SignIn,"player@example.com","private-password"),Is.False);Assert.That(service.IsSignedIn,Is.False);
        }
        [Test] public void AppleNonceIsFreshAndUsesSha256()
        {
            string a=AppleAccountProvider.NewNonce(),b=AppleAccountProvider.NewNonce();Assert.That(a.Length,Is.EqualTo(64));Assert.That(a,Is.Not.EqualTo(b));
            Assert.That(AppleAccountProvider.HashNonce("abc"),Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        }
    }
}
