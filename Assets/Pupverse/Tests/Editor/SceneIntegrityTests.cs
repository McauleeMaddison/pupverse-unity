using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Pupverse.Tests
{
    public class SceneIntegrityTests
    {
        [TestCase("MainMenu")]
        [TestCase("Battle")]
        public void SavedScenesRetainScriptsAndBindings(string sceneName)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Pupverse/Scenes/" + sceneName + ".unity");
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
