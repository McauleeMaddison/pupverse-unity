using System.Collections;
using UnityEngine;

namespace Pupverse
{
    // Runtime artwork overrides preserve the original shared materials and scene poses.
    [DisallowMultipleComponent]
    public sealed class BattleCardDisplay : MonoBehaviour
    {
        public Transform playerRoot, rivalRoot;
        public Renderer playerFront, rivalFront;
        [Min(0)] public float revealDuration = .36f;
        MaterialPropertyBlock playerOriginal;
        MaterialPropertyBlock rivalOriginal;
        MaterialPropertyBlock working;
        Vector3 playerScale, rivalScale;
        bool revealing, captured;
        int version;
        static readonly int MainTexture = Shader.PropertyToID("_MainTex");

        void CaptureMaterials()
        {
            if(captured) return;
            playerOriginal=new MaterialPropertyBlock(); rivalOriginal=new MaterialPropertyBlock(); working=new MaterialPropertyBlock();
            if(playerFront!=null) playerFront.GetPropertyBlock(playerOriginal);
            if(rivalFront!=null) rivalFront.GetPropertyBlock(rivalOriginal);
            captured=true;
        }
        public IEnumerator Reveal(CardData player, CardData rival)
        {
            CancelReveal(); CaptureMaterials();
            int current=version;
            if(playerRoot==null || rivalRoot==null) { SetArtwork(playerFront,player); SetArtwork(rivalFront,rival); yield break; }
            playerScale=playerRoot.localScale; rivalScale=rivalRoot.localScale; revealing=true;
            float duration=GameSettings.ReducedMotion?0:revealDuration;
            bool swapped=false;
            for(float elapsed=0; elapsed<duration; elapsed+=Time.unscaledDeltaTime)
            {
                if(current!=version) yield break;
                float t=elapsed/duration;
                if(t>=.5f && !swapped) { SetArtwork(playerFront,player); SetArtwork(rivalFront,rival); swapped=true; }
                float x=Mathf.Lerp(.04f,1,Mathf.Abs(Mathf.Cos(t*Mathf.PI)));
                playerRoot.localScale=Vector3.Scale(playerScale,new Vector3(x,1,1));
                rivalRoot.localScale=Vector3.Scale(rivalScale,new Vector3(x,1,1));
                yield return null;
            }
            if(current!=version) yield break;
            SetArtwork(playerFront,player); SetArtwork(rivalFront,rival);
            CancelReveal();
        }
        void SetArtwork(Renderer target, CardData card)
        {
            if(target==null || card==null || card.originalCardArt==null) return;
            target.GetPropertyBlock(working); working.SetTexture(MainTexture,card.originalCardArt); target.SetPropertyBlock(working);
        }
        public void CancelReveal()
        {
            version++;
            if(!revealing) return;
            if(playerRoot!=null) playerRoot.localScale=playerScale;
            if(rivalRoot!=null) rivalRoot.localScale=rivalScale;
            revealing=false;
        }
        void OnDisable()
        {
            CancelReveal();
            if(!captured) return;
            if(playerFront!=null) playerFront.SetPropertyBlock(playerOriginal);
            if(rivalFront!=null) rivalFront.SetPropertyBlock(rivalOriginal);
            captured=false;
        }
    }
}
