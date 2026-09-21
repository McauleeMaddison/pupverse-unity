using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Pupverse
{
    public sealed class MainMenuController : MonoBehaviour
    {
        public Button battleButton, motionButton;
        public Text motionLabel;
        bool loading;
        void Awake()
        {
            GameSettings.Initialize();
            battleButton.onClick.AddListener(EnterBattle);
            motionButton.onClick.AddListener(ToggleMotion);
            RefreshLabel();
        }
        void EnterBattle()
        {
            if (loading) return;
            loading = true; battleButton.interactable = false;
            SceneManager.LoadSceneAsync("Battle");
        }
        void ToggleMotion() { GameSettings.ReducedMotion = !GameSettings.ReducedMotion; RefreshLabel(); }
        void RefreshLabel() => motionLabel.text = GameSettings.ReducedMotion ? "MOTION: OFF" : "MOTION: ON";
    }
}
