using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pupverse.Editor
{
    public static class BattleCardAnimationSetup
    {
        [MenuItem("Pupverse/Setup Brooklyn Attack Preview")]
        [MenuItem("Pupverse/Setup Battle Card Preview")]
        [MenuItem("Pupverse/Setup Stat Battle")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "Battle3D")
            {
                Debug.LogWarning("Stop Play mode and open Battle3D before setting up the stat battle.");
                return;
            }
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var battles = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleController>(true)).ToArray();
            Transform player = FindUnique(transforms, "PlayerCardRoot");
            Transform rival = FindUnique(transforms, "RivalCardRoot");
            Transform core = FindUnique(transforms, "BattleCore");
            var playerData = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Brooklyn.asset");
            var rivalData = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Raven.asset");
            if (player == null || rival == null || core == null || playerData == null || rivalData == null || battles.Length != 1)
            {
                Debug.LogError("Battle3D needs its existing roots, both card assets, and exactly one BattleController. No setup changes made.");
                return;
            }
            BattleController battle = battles[0];
            var canvas = battle.GetComponentInParent<Canvas>(true);
            var views = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CardView>(true)).ToArray();
            var playerView = views.SingleOrDefault(v => v.data == playerData);
            var rivalView = views.SingleOrDefault(v => v.data == rivalData);
            var buttons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                Transform button = FindUnique(transforms, "Choose " + (CardStat)i);
                if (button != null) buttons[i] = button.GetComponent<Button>();
            }
            Transform heading = FindUnique(transforms, "Page title");
            var cameraTransform = FindUnique(transforms, "Main Camera");
            var animators = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleCardAnimator>(true)).ToArray();
            if (canvas == null || playerView == null || rivalView == null || buttons.Any(b => b == null) || heading == null ||
                cameraTransform == null || animators.Length != 1 || battle.resultPanel == null || battle.resultTitle == null ||
                battle.resultDetail == null || battle.scoreLabel == null)
            {
                Debug.LogError("Missing existing Canvas, stat buttons, result UI, card views, camera, or animator. No setup changes made.");
                return;
            }
            BattleCardAnimator animator = animators[0];
            var settings = new SerializedObject(animator);
            settings.FindProperty("playerCardRoot").objectReferenceValue = player;
            settings.FindProperty("rivalCardRoot").objectReferenceValue = rival;
            settings.FindProperty("battleCore").objectReferenceValue = core;
            settings.FindProperty("previewOnStart").boolValue = false;
            settings.FindProperty("showAttackButton").boolValue = false;
            settings.ApplyModifiedProperties();
            var effects = animator.GetComponent<BattleCardEffects>();
            if (effects == null) effects = Undo.AddComponent<BattleCardEffects>(animator.gameObject);
            var effectSettings = new SerializedObject(effects);
            effectSettings.FindProperty("effectShader").objectReferenceValue = Shader.Find("Sprites/Default");
            effectSettings.ApplyModifiedProperties();
            Undo.RecordObject(battle, "Connect 3D stat battle");
            battle.playerCard = playerView;
            battle.opponentCard = rivalView;
            battle.statButtons = buttons;
            battle.statBindings = buttons.Select((button, i) => new BattleController.StatButtonBinding { stat = (CardStat)i, button = button }).ToArray();
            battle.cardAnimator = animator;
            battle.autoNextRound = true;
            battle.enabled = true;
            var layout = battle.GetComponent<ReferenceLayout>();
            if (layout != null) { Undo.RecordObject(layout, "Use mobile battle layout"); layout.enabled = false; }
            battle.transform.localScale = Vector3.one;
            // Keep the existing data views, decoration and old controls intact but hidden.
            var keep = buttons.Select(b => b.gameObject).Concat(new[] { heading.gameObject, battle.resultPanel.gameObject, battle.scoreLabel.gameObject }).ToArray();
            foreach (Transform child in battle.transform) child.gameObject.SetActive(keep.Contains(child.gameObject));
            foreach (Transform child in canvas.transform)
                if (!battle.transform.IsChildOf(child)) child.gameObject.SetActive(false);
            playerView.gameObject.SetActive(false);
            rivalView.gameObject.SetActive(false);
            foreach (var motion in canvas.GetComponentsInChildren<CardMotion>(true)) motion.enabled = false;
            foreach (var text in canvas.GetComponentsInChildren<Text>(true)) text.raycastTarget = false;
            var background = battle.GetComponent<Image>();
            if (background == null) background = Undo.AddComponent<Image>(battle.gameObject);
            background.color = UITheme.Navy;
            background.raycastTarget = false;
            battle.resultPanel.color = UITheme.Panel;
            battle.resultPanel.raycastTarget = false;
            if (battle.winnerHighlight != null) battle.winnerHighlight.gameObject.SetActive(false);
            StyleText(heading.GetComponent<Text>(), 18);
            StyleText(battle.resultTitle, 18);
            StyleText(battle.resultDetail, 16);
            StyleText(battle.scoreLabel, 13);
            for (int i = 0; i < buttons.Length; i++)
            {
                var label = buttons[i].GetComponentInChildren<Text>(true);
                label.text = ((CardStat)i).ToString().ToUpperInvariant() + "\n" + playerData.EffectiveValue((CardStat)i);
                StyleText(label, 16);
                label.color = UITheme.Navy;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
                buttons[i].targetGraphic.color = BattleCardEffects.StatColor((CardStat)i);
                buttons[i].targetGraphic.raycastTarget = true;
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };
            }
            battle.resultTitle.text = "CHOOSE YOUR STAT";
            battle.resultDetail.text = "Same stat. Base + ability bonus.\nHighest total wins.";
            battle.scoreLabel.text = "0 WINS    0 LOSSES    0 DRAWS";
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = 0;
            var presentation = animator.GetComponent<Battle3DController>();
            if (presentation == null) presentation = Undo.AddComponent<Battle3DController>(animator.gameObject);
            Undo.RecordObject(presentation, "Connect mobile battle presentation");
            presentation.battleController = battle;
            presentation.controlsRoot = (RectTransform)battle.transform;
            presentation.instruction = heading.GetComponent<Text>();
            presentation.battleCamera = cameraTransform.GetComponent<Camera>();
            var hud = animator.GetComponent<BattleHudPresentation>();
            if (hud == null) hud = Undo.AddComponent<BattleHudPresentation>(animator.gameObject);
            Undo.RecordObject(hud, "Connect neon battle HUD");
            hud.layout = presentation;
            battle.comparisonDuration = 1.05f;
            battle.winnerReadDuration = .65f;
            battle.resultHoldDuration = 1.1f;
            canvas.gameObject.SetActive(true);
            Selection.activeGameObject = battle.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene)) Debug.Log("Battle3D ready: select a stat, compare, animate the winner, then choose again.", battle);
        }

        static void StyleText(Text text, int size)
        {
            text.fontSize = size;
            text.resizeTextForBestFit = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
        }

        static Transform FindUnique(Transform[] transforms, string objectName)
        {
            var matches = transforms.Where(item => item.name == objectName).ToArray();
            if (matches.Length == 1) return matches[0];
            Debug.LogError("Expected exactly one " + objectName + "; found " + matches.Length + ". Setup cancelled.");
            return null;
        }
    }
}
