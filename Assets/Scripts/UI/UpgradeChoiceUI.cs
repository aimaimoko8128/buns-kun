using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BunsKun.Game;
using BunsKun.Upgrades;

namespace BunsKun.UI
{
    /// <summary>Shown after clearing a combat room: pick one of three run-limited upgrades.</summary>
    public class UpgradeChoiceUI : MonoBehaviour
    {
        private RunController runController;
        private GameObject panelRoot;
        private Transform cardsContainer;

        public void Initialize(RunController controller, Transform canvasRoot)
        {
            runController = controller;
            BuildLayout(canvasRoot);
            panelRoot.SetActive(false);
            runController.OnUpgradeChoiceOffered += ShowChoices;
        }

        private void BuildLayout(Transform canvasRoot)
        {
            panelRoot = new GameObject("UpgradeChoicePanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasRoot, false);
            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.75f);
            UIFactory.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var title = UIFactory.CreateText(panelRoot.transform, "Title", "ROOM CLEAR - Choose an Upgrade", 30, Color.white, TextAnchor.MiddleCenter);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.75f);
            titleRect.anchorMax = new Vector2(0.5f, 0.75f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(1000, 60);

            var container = new GameObject("Cards", typeof(RectTransform));
            container.transform.SetParent(panelRoot.transform, false);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(1200, 300);
            cardsContainer = container.transform;
        }

        private void ShowChoices(List<UpgradeData> choices)
        {
            for (int i = cardsContainer.childCount - 1; i >= 0; i--) Destroy(cardsContainer.GetChild(i).gameObject);

            float cardWidth = 340f;
            float spacing = 30f;
            float startX = -((choices.Count - 1) * (cardWidth + spacing)) / 2f;

            for (int i = 0; i < choices.Count; i++)
            {
                UpgradeData data = choices[i];
                var button = UIFactory.CreateButton(cardsContainer, "Card" + i, "", data.color * 0.45f, out _);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0);
                rect.sizeDelta = new Vector2(cardWidth, 260);

                var nameText = UIFactory.CreateText(button.transform, "Name", data.upgradeName, 24, Color.white, TextAnchor.UpperCenter);
                var nameRect = nameText.GetComponent<RectTransform>();
                nameRect.anchorMin = new Vector2(0, 1);
                nameRect.anchorMax = new Vector2(1, 1);
                nameRect.pivot = new Vector2(0.5f, 1f);
                nameRect.anchoredPosition = new Vector2(0, -20);
                nameRect.sizeDelta = new Vector2(-20, 60);

                var descText = UIFactory.CreateText(button.transform, "Desc", data.description, 18, new Color(1, 1, 1, 0.9f), TextAnchor.MiddleCenter);
                var descRect = descText.GetComponent<RectTransform>();
                descRect.anchorMin = new Vector2(0, 0);
                descRect.anchorMax = new Vector2(1, 1);
                descRect.offsetMin = new Vector2(15, 20);
                descRect.offsetMax = new Vector2(-15, -90);

                button.onClick.AddListener(() =>
                {
                    runController.ApplyUpgradeChoice(data);
                    panelRoot.SetActive(false);
                });
            }

            panelRoot.SetActive(true);
        }
    }
}
