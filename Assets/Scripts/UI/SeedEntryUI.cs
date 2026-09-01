using UnityEngine;
using UnityEngine.UI;
using BunsKun.Game;

namespace BunsKun.UI
{
    /// <summary>
    /// F1 panel: shows the seed the current run was generated from and lets you type a
    /// seed to replay that exact descent. Useful for debugging a bad layout and for
    /// sharing an interesting run.
    /// </summary>
    public class SeedEntryUI : MonoBehaviour
    {
        private RunController runController;
        private GameObject panelRoot;
        private Text currentSeedText;
        private InputField seedInput;
        private bool isOpen;

        public void Initialize(RunController controller, Transform canvasRoot)
        {
            runController = controller;
            BuildLayout(canvasRoot);
            panelRoot.SetActive(false);
        }

        private void BuildLayout(Transform canvasRoot)
        {
            panelRoot = new GameObject("SeedPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasRoot, false);
            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.94f);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720, 330);

            var title = UIFactory.CreateText(panelRoot.transform, "Title", "SEED", 28, Color.white, TextAnchor.MiddleCenter);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -18);
            titleRect.sizeDelta = new Vector2(600, 40);

            currentSeedText = UIFactory.CreateText(panelRoot.transform, "CurrentSeed", "", 22,
                new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
            var currentRect = currentSeedText.GetComponent<RectTransform>();
            currentRect.anchorMin = new Vector2(0.5f, 1f);
            currentRect.anchorMax = new Vector2(0.5f, 1f);
            currentRect.pivot = new Vector2(0.5f, 1f);
            currentRect.anchoredPosition = new Vector2(0, -66);
            currentRect.sizeDelta = new Vector2(640, 36);

            var help = UIFactory.CreateText(panelRoot.transform, "Help",
                "Enter a seed to replay that exact run. Leave empty for a random one.", 16,
                new Color(1, 1, 1, 0.7f), TextAnchor.MiddleCenter);
            var helpRect = help.GetComponent<RectTransform>();
            helpRect.anchorMin = new Vector2(0.5f, 1f);
            helpRect.anchorMax = new Vector2(0.5f, 1f);
            helpRect.pivot = new Vector2(0.5f, 1f);
            helpRect.anchoredPosition = new Vector2(0, -108);
            helpRect.sizeDelta = new Vector2(640, 30);

            seedInput = CreateInputField(panelRoot.transform, new Vector2(0, -150), new Vector2(400, 46));

            var startButton = UIFactory.CreateButton(panelRoot.transform, "StartSeed", "Start Run With Seed",
                new Color(0.2f, 0.45f, 0.25f), out _);
            var startRect = startButton.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 1f);
            startRect.anchorMax = new Vector2(0.5f, 1f);
            startRect.pivot = new Vector2(0.5f, 1f);
            startRect.anchoredPosition = new Vector2(-115, -212);
            startRect.sizeDelta = new Vector2(220, 52);
            startButton.onClick.AddListener(StartWithTypedSeed);

            var randomButton = UIFactory.CreateButton(panelRoot.transform, "RandomSeed", "Random Run",
                new Color(0.28f, 0.28f, 0.35f), out _);
            var randomRect = randomButton.GetComponent<RectTransform>();
            randomRect.anchorMin = new Vector2(0.5f, 1f);
            randomRect.anchorMax = new Vector2(0.5f, 1f);
            randomRect.pivot = new Vector2(0.5f, 1f);
            randomRect.anchoredPosition = new Vector2(115, -212);
            randomRect.sizeDelta = new Vector2(220, 52);
            randomButton.onClick.AddListener(() =>
            {
                SetOpen(false);
                runController.StartNewRun();
            });

            var closeHint = UIFactory.CreateText(panelRoot.transform, "CloseHint", "F1 to close", 15,
                new Color(1, 1, 1, 0.55f), TextAnchor.MiddleCenter);
            var closeRect = closeHint.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 14);
            closeRect.sizeDelta = new Vector2(400, 26);
        }

        private InputField CreateInputField(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject("SeedInput", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var placeholder = UIFactory.CreateText(go.transform, "Placeholder", "e.g. 38291745", 20,
                new Color(1, 1, 1, 0.35f), TextAnchor.MiddleCenter);
            UIFactory.Stretch(placeholder.GetComponent<RectTransform>(), new Vector2(10, 6), new Vector2(-10, -6));

            var text = UIFactory.CreateText(go.transform, "Text", "", 20, Color.white, TextAnchor.MiddleCenter);
            text.supportRichText = false;
            UIFactory.Stretch(text.GetComponent<RectTransform>(), new Vector2(10, 6), new Vector2(-10, -6));

            var input = go.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 10;
            return input;
        }

        private void StartWithTypedSeed()
        {
            string raw = seedInput != null ? seedInput.text : null;
            SetOpen(false);
            if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out int seed) && seed != 0)
            {
                runController.StartNewRun(Mathf.Abs(seed));
            }
            else
            {
                runController.StartNewRun();
            }
        }

        private void Update()
        {
            if (runController == null) return;
            if (Input.GetKeyDown(KeyCode.F1)) SetOpen(!isOpen);
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            panelRoot.SetActive(open);
            if (!open) return;

            currentSeedText.text = "Current run seed: " + runController.RunSeed;
            if (seedInput != null) seedInput.text = runController.RunSeed.ToString();
        }
    }
}
