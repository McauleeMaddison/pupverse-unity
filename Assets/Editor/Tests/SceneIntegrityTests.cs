using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Pupverse.Tests
{
    public class SceneIntegrityTests
    {
        [Test]
        public void Battle3DReusesOneCanvasAndWiresStatBattle()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle3D.unity");
            var battle = Object.FindAnyObjectByType<BattleController>();
            Assert.That(battle, Is.Not.Null);
            Assert.That(battle.playerCard.data.id, Is.EqualTo("crypto-brooklyn"));
            Assert.That(battle.opponentCard.data.id, Is.EqualTo("crypto-raven"));
            Assert.That(battle.cardAnimator, Is.Not.Null);
            Assert.That(battle.autoNextRound, Is.True);
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(battle.GetComponentInParent<Canvas>().gameObject.activeInHierarchy, Is.True);
            Assert.That(battle.statBindings.Length, Is.EqualTo(5));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(battle.statBindings[i].stat, Is.EqualTo((CardStat)i));
                Assert.That(battle.statBindings[i].button, Is.EqualTo(battle.statButtons[i]));
            }
            var settings = new SerializedObject(battle.cardAnimator);
            foreach (string property in new[] { "playerCardRoot", "rivalCardRoot", "battleCore" })
                Assert.That(settings.FindProperty(property).objectReferenceValue, Is.Not.Null);
            Assert.That(settings.FindProperty("previewOnStart").boolValue, Is.False);
            Assert.That(settings.FindProperty("showAttackButton").boolValue, Is.False);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
        }

        [TestCase("MainMenu")]
        [TestCase("Battle")]
        public void SavedScenesRetainScriptsAndBindings(string sceneName)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject), Is.Zero, child.name);
            }
            if (sceneName == "MainMenu")
            {
                var menu = Object.FindAnyObjectByType<MainMenuController>();
                Assert.That(menu, Is.Not.Null);
                Assert.That(menu.battleButton, Is.Not.Null);
                Assert.That(menu.motionButton, Is.Not.Null);
            }
            else
            {
                var battle = Object.FindAnyObjectByType<BattleController>();
                Assert.That(battle, Is.Not.Null);
                Assert.That(battle.playerCard.data.id, Is.EqualTo("crypto-raven"));
                Assert.That(battle.opponentCard.data.id, Is.EqualTo("crypto-brooklyn"));
                Assert.That(battle.statButtons.Length, Is.EqualTo(5));
                Assert.That(battle.victory, Is.Not.Null);
                Assert.That(battle.replayButton, Is.Not.Null);
                Assert.That(battle.homeButton, Is.Not.Null);
            }
        }
    }
}
