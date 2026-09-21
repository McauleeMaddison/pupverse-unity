using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Pupverse
{
    // Opt-in standalone validation only; never starts during ordinary play.
    public sealed class SmokeTestRunner : MonoBehaviour
    {
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (!Debug.isDebugBuild) return;
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--smoke-test");
            if(index<0 || index+1>=args.Length)return;
            var go=new GameObject("Smoke Test Runner");DontDestroyOnLoad(go);
            go.AddComponent<SmokeTestRunner>().output=args[index+1];
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(2);
            if(!FindAnyObjectByType<MainMenuController>()) {Fail("MainMenu missing");yield break;}
            yield return Capture("01-main-menu.png");
            FindAnyObjectByType<MainMenuController>().battleButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(2);
            var battle=FindAnyObjectByType<BattleController>();
            if(!battle) {Fail("Battle navigation failed");yield break;}
            yield return Capture("02-battle-ready.png");
            battle.statButtons[0].onClick.Invoke();battle.statButtons[0].onClick.Invoke();
            yield return new WaitForSecondsRealtime(.85f);
            yield return Capture("03-victory.png");
            yield return new WaitForSecondsRealtime(1);
            if(battle.scoreLabel.text!="1 WINS    0 LOSSES    0 DRAWS") {Fail("Duplicate win or wrong result");yield break;}
            battle.replayButton.onClick.Invoke();battle.statButtons[1].onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.5f);
            if(battle.scoreLabel.text!="1 WINS    1 LOSSES    0 DRAWS") {Fail("Replay or loss failed");yield break;}
            yield return Capture("04-defeat.png");
            battle.homeButton.onClick.Invoke();yield return new WaitForSecondsRealtime(1);
            if(!FindAnyObjectByType<MainMenuController>()) {Fail("Return home failed");yield break;}
            File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"PASS: menu, scene navigation, card binding, victory, duplicate-click guard, replay, defeat, return home.");
            Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name));
            yield return new WaitForSecondsRealtime(.25f);
        }
        void Fail(string message) {File.WriteAllText(Path.Combine(output,"smoke-result.txt"),"FAIL: "+message);Application.Quit(1);}
    }
}
