using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BunsKun.Game;
using BunsKun.Ingredients;

namespace BunsKun.UI
{
    /// <summary>
    /// Always-on gameplay HUD: HP, mana, the upcoming ingredient sequence (with the next
    /// one to activate highlighted), and basic run/area info. Built entirely at runtime.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private const int PreviewSlots = 5;

        private RunController runController;
        private Image hpFill;
        private Image manaFill;
        private Image jetpackFill;
        private Text hpText;
        private Text manaText;
        private Text areaText;
        private Text seedText;
        private Text hintText;

        private readonly List<Image> previewBoxes = new List<Image>();
        private readonly List<Text> previewLabels = new List<Text>();

        public void Initialize(RunController controller, Transform canvasRoot)
        {
            runController = controller;
            BuildLayout(canvasRoot);
            runController.OnAreaChanged += depth => UpdateAreaText();
            UpdateAreaText();
        }

        private void BuildLayout(Transform canvasRoot)
        {
            var topLeft = UIFactory.CreatePanel(canvasRoot, "HUD_TopLeft", new Color(0, 0, 0, 0f));
            topLeft.anchorMin = new Vector2(0f, 1f);
            topLeft.anchorMax = new Vector2(0f, 1f);
            topLeft.pivot = new Vector2(0f, 1f);
            topLeft.anchoredPosition = new Vector2(20, -20);
            topLeft.sizeDelta = new Vector2(420, 165);

            var hpBack = UIFactory.CreateFilledBar(topLeft, "HPBar", new Color(0.15f, 0.05f, 0.05f, 0.85f), new Color(0.85f, 0.2f, 0.2f), out hpFill);
            var hpRect = hpBack.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0, 1);
            hpRect.anchorMax = new Vector2(0, 1);
            hpRect.pivot = new Vector2(0, 1);
            hpRect.anchoredPosition = new Vector2(0, 0);
            hpRect.sizeDelta = new Vector2(340, 30);
            hpText = UIFactory.CreateText(hpBack.transform, "HPText", "100/100", 18, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Stretch(hpText.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var manaBack = UIFactory.CreateFilledBar(topLeft, "ManaBar", new Color(0.05f, 0.08f, 0.18f, 0.85f), new Color(0.25f, 0.55f, 0.95f), out manaFill);
            var manaRect = manaBack.GetComponent<RectTransform>();
            manaRect.anchorMin = new Vector2(0, 1);
            manaRect.anchorMax = new Vector2(0, 1);
            manaRect.pivot = new Vector2(0, 1);
            manaRect.anchoredPosition = new Vector2(0, -38);
            manaRect.sizeDelta = new Vector2(340, 24);
            manaText = UIFactory.CreateText(manaBack.transform, "ManaText", "100/100", 15, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Stretch(manaText.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var jetpackBack = UIFactory.CreateFilledBar(topLeft, "JetpackBar", new Color(0.15f, 0.10f, 0.03f, 0.85f), new Color(1f, 0.7f, 0.25f), out jetpackFill);
            var jetpackRect = jetpackBack.GetComponent<RectTransform>();
            jetpackRect.anchorMin = new Vector2(0, 1);
            jetpackRect.anchorMax = new Vector2(0, 1);
            jetpackRect.pivot = new Vector2(0, 1);
            jetpackRect.anchoredPosition = new Vector2(0, -66);
            jetpackRect.sizeDelta = new Vector2(340, 14);

            areaText = UIFactory.CreateText(topLeft, "AreaText", "Layer 1 / 5", 20, Color.white);
            var areaRect = areaText.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0, 1);
            areaRect.anchorMax = new Vector2(0, 1);
            areaRect.pivot = new Vector2(0, 1);
            areaRect.anchoredPosition = new Vector2(0, -90);
            areaRect.sizeDelta = new Vector2(340, 26);

            seedText = UIFactory.CreateText(topLeft, "SeedText", "", 13, new Color(1, 1, 1, 0.55f));
            var seedRect = seedText.GetComponent<RectTransform>();
            seedRect.anchorMin = new Vector2(0, 1);
            seedRect.anchorMax = new Vector2(0, 1);
            seedRect.pivot = new Vector2(0, 1);
            seedRect.anchoredPosition = new Vector2(0, -116);
            seedRect.sizeDelta = new Vector2(340, 20);

            hintText = UIFactory.CreateText(topLeft, "HintText", "Tab: Inventory    F1: Seed", 15, new Color(1, 1, 1, 0.7f));
            var hintRect = hintText.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 1);
            hintRect.anchorMax = new Vector2(0, 1);
            hintRect.pivot = new Vector2(0, 1);
            hintRect.anchoredPosition = new Vector2(0, -138);
            hintRect.sizeDelta = new Vector2(340, 24);

            // Ingredient sequence preview, bottom-center.
            var previewPanel = UIFactory.CreatePanel(canvasRoot, "IngredientPreview", new Color(0, 0, 0, 0f));
            previewPanel.anchorMin = new Vector2(0.5f, 0f);
            previewPanel.anchorMax = new Vector2(0.5f, 0f);
            previewPanel.pivot = new Vector2(0.5f, 0f);
            previewPanel.anchoredPosition = new Vector2(0, 24);
            previewPanel.sizeDelta = new Vector2(70 * PreviewSlots + 20, 90);

            for (int i = 0; i < PreviewSlots; i++)
            {
                var box = UIFactory.CreatePanel(previewPanel, "Slot" + i, new Color(0.15f, 0.15f, 0.18f, 0.85f));
                box.anchorMin = new Vector2(0, 0);
                box.anchorMax = new Vector2(0, 0);
                box.pivot = new Vector2(0, 0);
                box.anchoredPosition = new Vector2(i * 70, i == 0 ? 10 : 20);
                box.sizeDelta = i == 0 ? new Vector2(64, 64) : new Vector2(54, 54);
                var img = box.GetComponent<Image>();
                previewBoxes.Add(img);

                var label = UIFactory.CreateText(box, "Label", "", i == 0 ? 13 : 11, Color.white, TextAnchor.MiddleCenter);
                UIFactory.Stretch(label.GetComponent<RectTransform>(), new Vector2(2, 2), new Vector2(-2, -2));
                previewLabels.Add(label);
            }
        }

        private void UpdateAreaText()
        {
            areaText.text = "Layer " + (runController.LayerDepth + 1) + " / " + RunController.TotalLayers;
            seedText.text = "Seed: " + runController.RunSeed;
        }

        private void Update()
        {
            if (runController == null || runController.CurrentPlayer == null) return;

            var health = runController.PlayerHealth;
            var stats = runController.PlayerStats;
            var combat = runController.PlayerCombat;
            var inventory = runController.Inventory;

            if (health != null)
            {
                hpFill.fillAmount = health.HealthFraction;
                hpText.text = Mathf.CeilToInt(health.CurrentHealth) + " / " + Mathf.CeilToInt(health.MaxHealth);
            }

            if (combat != null && stats != null)
            {
                float manaFraction = stats.MaxMana <= 0 ? 0 : combat.CurrentMana / stats.MaxMana;
                manaFill.fillAmount = manaFraction;
                manaText.text = Mathf.FloorToInt(combat.CurrentMana) + " / " + Mathf.FloorToInt(stats.MaxMana);
            }

            var playerController = runController.PlayerController;
            if (playerController != null)
            {
                jetpackFill.fillAmount = playerController.JetpackFuelFraction;
            }

            if (inventory != null)
            {
                var upcoming = inventory.PreviewUpcoming(PreviewSlots);
                bool onCooldown = combat != null && combat.DelayTimer > 0f;
                for (int i = 0; i < PreviewSlots; i++)
                {
                    if (i < upcoming.Count)
                    {
                        IngredientData data = upcoming[i];
                        Color c = data.color;
                        if (i == 0 && onCooldown) c *= 0.5f;
                        previewBoxes[i].color = c;
                        previewLabels[i].text = data.ingredientName;
                    }
                    else
                    {
                        previewBoxes[i].color = new Color(0.15f, 0.15f, 0.18f, 0.5f);
                        previewLabels[i].text = "";
                    }
                }
            }
        }
    }
}
