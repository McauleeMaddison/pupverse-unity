using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pupverse.Editor
{
    public static class BattleCardAnimationSetup
    {
        [MenuItem("Pupverse/Setup Brooklyn Attack Preview")]
        [MenuItem("Pupverse/Setup Battle Card Preview")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "Battle3D")
            {
                Debug.LogWarning("Stop Play mode and open Battle3D before setting up the attack preview.");
                return;
            }

            var transforms = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform player = FindUnique(transforms, "PlayerCardRoot");
            Transform rival = FindUnique(transforms, "RivalCardRoot");
            Transform core = FindUnique(transforms, "BattleCore");
            if (player == null || rival == null || core == null) return;

            var animators = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BattleCardAnimator>(true)).ToArray();
            if (animators.Length > 1)
            {
                Debug.LogError("More than one BattleCardAnimator exists. Choose one before setting up the preview.");
                return;
            }

            BattleCardAnimator animator;
            if (animators.Length == 1) animator = animators[0];
            else
            {
                var controller = new GameObject("BattleAnimationController");
                SceneManager.MoveGameObjectToScene(controller, scene);
                Undo.RegisterCreatedObjectUndo(controller, "Create battle animation controller");
                animator = Undo.AddComponent<BattleCardAnimator>(controller);
            }

            var settings = new SerializedObject(animator);
            settings.FindProperty("playerCardRoot").objectReferenceValue = player;
            settings.FindProperty("rivalCardRoot").objectReferenceValue = rival;
            settings.FindProperty("battleCore").objectReferenceValue = core;
            settings.FindProperty("previewOnStart").boolValue = true;
            settings.FindProperty("showAttackButton").boolValue = true;
            settings.ApplyModifiedProperties();
            Selection.activeGameObject = animator.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene))
                Debug.Log("Battle card preview connected and saved. Press Play for Brooklyn's preview, then alternate Attack Raven and Attack Brooklyn. Reset turns returns control to Brooklyn.", animator);
        }

        static Transform FindUnique(Transform[] transforms, string objectName)
        {
            var matches = transforms.Where(item => item.name == objectName).ToArray();
            if (matches.Length == 1) return matches[0];
            Debug.LogError("Expected exactly one " + objectName + " in Battle3D; found " + matches.Length + ". Setup cancelled.");
            return null;
        }
    }
}
