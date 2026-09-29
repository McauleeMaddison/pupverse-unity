using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Pupverse.Tests
{
    public class HomeStartupTests
    {
        string collection,backup,hand;
        [UnitySetUp] public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Battle3D.unity");
            collection=PlayerPrefs.GetString(BattleProgression.SaveKey,"");backup=PlayerPrefs.GetString(BattleProgression.BackupKey,"");hand=PlayerPrefs.GetString("Pupverse.StartingHand.v1","");
            PlayerPrefs.DeleteKey(BattleProgression.SaveKey);PlayerPrefs.DeleteKey(BattleProgression.BackupKey);PlayerPrefs.DeleteKey("Pupverse.StartingHand.v1");
            yield break;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            PlayerPrefs.SetString(BattleProgression.SaveKey,collection);PlayerPrefs.SetString(BattleProgression.BackupKey,backup);PlayerPrefs.SetString("Pupverse.StartingHand.v1",hand);PlayerPrefs.Save();
            yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator OlderLoadedSceneWithoutHomeOrNewReferencesStillStartsAtHome()
        {
            var selector=Object.FindAnyObjectByType<BattleHandSelection>();
            Object.DestroyImmediate(selector.GetComponent<BattleHomeScreen>());
            selector.home=null;selector.progression=null;selector.packShop=null;
            var shop=selector.GetComponent<CardPackShop>();shop.progression=null;shop.hud=null;shop.cardFoilShader=null;shop.foilShader=null;
            selector.GetComponent<BattleCardDisplay>().cardFoilShader=null;
            yield return new EnterPlayMode();yield return new WaitForSecondsRealtime(.15f);
            var match=Object.FindAnyObjectByType<BattleMatchController>();
            var home=match.GetComponent<BattleHomeScreen>();selector=match.GetComponent<BattleHandSelection>();shop=match.GetComponent<CardPackShop>();
            Assert.That(home,Is.Not.Null);Assert.That(home.IsOpen,Is.True);Assert.That(selector.IsOpen,Is.False);
            Assert.That(match.IsMatchRunning,Is.False);Assert.That(shop.IsOpen,Is.False);
            Assert.That(match.GetComponents<BattleHomeScreen>().Length,Is.EqualTo(1));
            Assert.That(selector.progression,Is.SameAs(match.progression));Assert.That(selector.packShop,Is.SameAs(shop));
            Assert.That(shop.cardFoilShader,Is.Not.Null);Assert.That(match.cardDisplay.cardFoilShader,Is.SameAs(shop.cardFoilShader));
            home.OpenPacks();Assert.That(shop.IsOpen,Is.True);shop.Close();Assert.That(home.IsOpen,Is.True);
            home.OpenHand();Assert.That(selector.IsOpen,Is.True);Assert.That(home.IsOpen,Is.False);
            home.ShowHome();Assert.That(selector.IsOpen,Is.False);Assert.That(home.IsOpen,Is.True);
            selector.enabled=false;selector.enabled=true;Assert.That(selector.IsOpen,Is.False,"Re-enabling the hand component must not cover Home");
        }
        [UnityTest] public IEnumerator DisabledHomeInAnOlderSceneIsRestoredOnLaunch()
        {
            Object.FindAnyObjectByType<BattleHomeScreen>().enabled=false;
            yield return new EnterPlayMode();yield return new WaitForSecondsRealtime(.15f);
            var match=Object.FindAnyObjectByType<BattleMatchController>();
            Assert.That(match.GetComponent<BattleHomeScreen>().IsOpen,Is.True);
            Assert.That(match.GetComponent<BattleHandSelection>().IsOpen,Is.False);Assert.That(match.IsMatchRunning,Is.False);
        }
    }
}
