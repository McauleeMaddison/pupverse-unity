using UnityEngine;
namespace Pupverse
{
    [CreateAssetMenu(menuName="PupVerse/Account connection")]
    public sealed class PlayerAccountConfig : ScriptableObject
    {
        [Tooltip("Firebase project ID for PupVerse, not an unrelated app.")]
        public string firebaseProjectId="";
        [Tooltip("Public Firebase Web API key. Never put service-account keys or Apple private keys here.")]
        public string firebaseWebApiKey="";
        public bool emailEnabled=true;
        [Tooltip("Enable after configuring Apple in Firebase and the Apple Developer portal.")]
        public bool appleEnabled;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(firebaseProjectId) && !string.IsNullOrWhiteSpace(firebaseWebApiKey);
    }
}
