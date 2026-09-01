using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BunsKun.Game;
using BunsKun.Ingredients;
using BunsKun.Buns;

namespace BunsKun.UI
{
    /// <summary>
    /// The Tab inventory / ingredient-setup screen: view and reorder the equipped
    /// ingredient sequence, equip newly found ingredients, and switch buns.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        private RunController runController;
        private GameObject panelRoot;
        private Transform slotsContainer;
        private Transform ownedContainer;
        private Transform bunsContainer;
        private Text detailsText;

        private int selectedSlot = -1;
        private bool isOpen;

        public void Initialize(RunController controller, Transform canvasRoot)
        {
            runController = controller;
            BuildLayout(canvasRoot);
            panelRoot.SetActive(false);
        }

        private void BuildLayout(Transform canvasRoot)
        {
            panelRoot = new GameObject("InventoryPanel", typeof(RectTransform));
            panelRoot.transform.SetParent(canvasRoot, false);
            var bgImg = panelRoot.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.07f, 0.92f);
            UIFactory.Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var title = UIFactory.CreateText(panelRoot.transform, "Title", "INVENTORY  (Tab to close)", 26, Color.white, TextAnchor.MiddleCenter);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(700, 50);

            // Equipped slots column (left)
            var slotsHeader = UIFactory.CreateText(panelRoot.transform, "SlotsHeader", "Equipped Sequence (click a slot, then an ingredient)", 18, new Color(1, 0.9f, 0.6f));
            PlaceHeader(slotsHeader.GetComponent<RectTransform>(), new Vector2(60, -100));
            var slotsPanel = UIFactory.CreatePanel(panelRoot.transform, "Slots", new Color(0, 0, 0, 0));
            slotsPanel.anchorMin = new Vector2(0, 1);
            slotsPanel.anchorMax = new Vector2(0, 1);
            slotsPanel.pivot = new Vector2(0, 1);
            slotsPanel.anchoredPosition = new Vector2(60, -140);
            slotsPanel.sizeDelta = new Vector2(430, 620);
            slotsContainer = slotsPanel;

            // Owned ingredients column (middle)
            var ownedHeader = UIFactory.CreateText(panelRoot.transform, "OwnedHeader", "Owned Ingredients", 18, new Color(1, 0.9f, 0.6f));
            PlaceHeader(ownedHeader.GetComponent<RectTransform>(), new Vector2(540, -100));
            var ownedPanel = UIFactory.CreatePanel(panelRoot.transform, "Owned", new Color(0, 0, 0, 0));
            ownedPanel.anchorMin = new Vector2(0, 1);
            ownedPanel.anchorMax = new Vector2(0, 1);
            ownedPanel.pivot = new Vector2(0, 1);
            ownedPanel.anchoredPosition = new Vector2(540, -140);
            ownedPanel.sizeDelta = new Vector2(430, 620);
            ownedContainer = ownedPanel;

            // Buns column (right)
            var bunHeader = UIFactory.CreateText(panelRoot.transform, "BunHeader", "Buns", 18, new Color(1, 0.9f, 0.6f));
            PlaceHeader(bunHeader.GetComponent<RectTransform>(), new Vector2(1020, -100));
            var bunPanel = UIFactory.CreatePanel(panelRoot.transform, "Buns", new Color(0, 0, 0, 0));
            bunPanel.anchorMin = new Vector2(0, 1);
            bunPanel.anchorMax = new Vector2(0, 1);
            bunPanel.pivot = new Vector2(0, 1);
            bunPanel.anchoredPosition = new Vector2(1020, -140);
            bunPanel.sizeDelta = new Vector2(430, 620);
            bunsContainer = bunPanel;

            detailsText = UIFactory.CreateText(panelRoot.transform, "Details", "", 16, new Color(0.85f, 0.85f, 0.85f));
            var detailsRect = detailsText.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.5f, 0f);
            detailsRect.anchorMax = new Vector2(0.5f, 0f);
            detailsRect.pivot = new Vector2(0.5f, 0f);
            detailsRect.anchoredPosition = new Vector2(0, 30);
            detailsRect.sizeDelta = new Vector2(1300, 60);
        }

        private void PlaceHeader(RectTransform rect, Vector2 pos)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(420, 30);
        }

        private void Update()
        {
            if (runController == null) return;
            if (Input.GetKeyDown(KeyCode.Tab) && (runController.State == RunState.Playing || isOpen))
            {
                SetOpen(!isOpen);
            }
        }

        private void SetOpen(bool open)
        {
            isOpen = open;
            panelRoot.SetActive(open);
            if (open)
            {
                selectedSlot = -1;
                Time.timeScale = 0f;
                Refresh();
            }
            else if (runController.State == RunState.Playing)
            {
                Time.timeScale = 1f;
            }
        }

        private void Refresh()
        {
            ClearChildren(slotsContainer);
            ClearChildren(ownedContainer);
            ClearChildren(bunsContainer);

            var inventory = runController.Inventory;
            var bunInventory = runController.BunInventory;
            if (inventory == null || bunInventory == null) return;

            for (int i = 0; i < inventory.Equipped.Count; i++)
            {
                int slotIndex = i;
                IngredientData data = inventory.Equipped[i];
                string label = (i + 1) + ". " + (data != null ? data.ingredientName : "(empty)");
                Color color = selectedSlot == i ? new Color(0.3f, 0.5f, 0.3f) : (data != null ? data.color * 0.5f + Color.black * 0.5f : new Color(0.2f, 0.2f, 0.2f));
                var row = UIFactory.CreateButton(slotsContainer, "Slot" + i, label, color, out _);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(0, 1);
                rowRect.pivot = new Vector2(0, 1);
                rowRect.anchoredPosition = new Vector2(0, -i * 56);
                rowRect.sizeDelta = new Vector2(300, 48);
                row.onClick.AddListener(() =>
                {
                    selectedSlot = slotIndex;
                    if (data != null) detailsText.text = data.ingredientName + ": " + data.description;
                    Refresh();
                });

                if (data != null)
                {
                    var unequip = UIFactory.CreateButton(slotsContainer, "Unequip" + i, "X", new Color(0.4f, 0.15f, 0.15f), out _);
                    var unequipRect = unequip.GetComponent<RectTransform>();
                    unequipRect.anchorMin = new Vector2(0, 1);
                    unequipRect.anchorMax = new Vector2(0, 1);
                    unequipRect.pivot = new Vector2(0, 1);
                    unequipRect.anchoredPosition = new Vector2(310, -i * 56);
                    unequipRect.sizeDelta = new Vector2(48, 48);
                    unequip.onClick.AddListener(() =>
                    {
                        inventory.UnequipSlot(slotIndex);
                        Refresh();
                    });
                }
            }

            for (int i = 0; i < inventory.Owned.Count; i++)
            {
                IngredientData data = inventory.Owned[i];
                var row = UIFactory.CreateButton(ownedContainer, "Owned" + i, data.ingredientName + " (" + data.kind + ")", data.color * 0.6f, out _);
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(0, 1);
                rowRect.pivot = new Vector2(0, 1);
                rowRect.anchoredPosition = new Vector2(0, -i * 56);
                rowRect.sizeDelta = new Vector2(400, 48);
                row.onClick.AddListener(() =>
                {
                    detailsText.text = data.ingredientName + ": " + data.description;
                    if (selectedSlot >= 0)
                    {
                        inventory.EquipToSlot(selectedSlot, data);
                        Refresh();
                    }
                });
            }

            for (int i = 0; i < bunInventory.Owned.Count; i++)
            {
                BunData bun = bunInventory.Owned[i];
                bool current = bun == bunInventory.CurrentBun;
                string label = bun.bunName + (current ? " [EQUIPPED]" : "") + "\n" + bun.ingredientSlotCount + " slots, " + bun.maxMana + " mana";
                var row = UIFactory.CreateButton(bunsContainer, "Bun" + i, label, current ? new Color(0.25f, 0.45f, 0.25f) : bun.color * 0.5f, out Text label2);
                label2.fontSize = 15;
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(0, 1);
                rowRect.pivot = new Vector2(0, 1);
                rowRect.anchoredPosition = new Vector2(0, -i * 70);
                rowRect.sizeDelta = new Vector2(400, 62);
                row.onClick.AddListener(() =>
                {
                    detailsText.text = bun.bunName + ": " + bun.description;
                    if (!current)
                    {
                        bunInventory.EquipBun(bun);
                        Refresh();
                    }
                });
            }
        }

        private void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Destroy(t.GetChild(i).gameObject);
            }
        }
    }
}
