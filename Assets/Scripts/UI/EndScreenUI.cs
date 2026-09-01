using UnityEngine;
using UnityEngine.UI;
using BunsKun.Game;

namespace BunsKun.UI
{
    /// <summary>Full-screen Game Over / Victory overlay with a button to start a new run.</summary>
    public class EndScreenUI : MonoBehaviour
    {
        private RunController runController;
        private GameObject panelRoot;
        private Text titleText;

        public void Initialize(RunController controller, Transform canvasRoot)
        {
            runController = controller;
            BuildLayout(canvasRoot);
            panelRoot.SetActive(false);
            runController.OnGameOver += () => Show("GAME OVER", new Color(0.5f, 0.05f, 0.05f, 0.92f));
            runController.OnVictory += () => Show("VICTORY!", new Color(0.15f, 0.4f, 0.15f, 0.92f));
        }

        private void BuildLayout(Transform canvasRoot)
        {
            panelRoot = new GameObject("EndScreenPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasRoot, false);
            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);
            UIFactory.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            titleText = UIFactory.CreateText(panelRoot.transform, "Title", "GAME OVER", 64, Color.white, TextAnchor.MiddleCenter);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.6f);
            titleRect.anchorMax = new Vector2(0.5f, 0.6f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(900, 100);

            var button = UIFactory.CreateButton(panelRoot.transform, "NewRunButton", "Start New Run", new Color(0.2f, 0.5f, 0.2f), out _);
            var btnRect = button.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.4f);
            btnRect.anchorMax = new Vector2(0.5f, 0.4f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = new Vector2(300, 70);
            button.onClick.AddListener(() =>
            {
                panelRoot.SetActive(false);
                runController.StartNewRun();
            });
        }

        private void Show(string title, Color bgColor)
        {
            titleText.text = title;
            panelRoot.GetComponent<Image>().color = bgColor;
            panelRoot.SetActive(true);
        }
    }
}
