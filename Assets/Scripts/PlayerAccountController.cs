using UnityEngine;
namespace Pupverse
{
    [DisallowMultipleComponent]
    public sealed class PlayerAccountController : MonoBehaviour
    {
        public PlayerAccountService Service {get;private set;}
        GameObject appleObject;
        void Awake()
        {
            var config=Resources.Load<PlayerAccountConfig>("PupverseAccountConfig");
            appleObject=new GameObject("PupVerse Apple account "+System.Guid.NewGuid().ToString("N"));appleObject.transform.SetParent(transform,false);
            var apple=appleObject.AddComponent<AppleAccountProvider>();
            Service=new PlayerAccountService(new FirebaseAccountBackend(config,new UnityAccountHttpTransport(),apple));
        }
        void Update(){Service?.Tick();}
        void OnDestroy(){Service?.Cancel();if(appleObject!=null)Destroy(appleObject);}
    }
}
