using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using ApplicationManagers;
using Settings;

namespace UI
{
    public class ItemPopupManager : MonoBehaviour
    {
        public static ItemPopupManager Instance;

        private const float Duration = 4f;
        private const float FadeDuration = 2f;
        private const int MaxPopups = 5;
        private const float Spacing = 35f;

        // Dictionary for mapping internal names to display names
        private readonly Dictionary<string, string> _itemDisplayNames = new Dictionary<string, string>()
        {
            {"Wagon1", "Support Wagon"},
            {"Wagon2", "Resupply Wagon"},
            {"Cannon", "Cannon"},
            {"CannonGround", "Ground Cannon"},
            {"WallCannon", "Wall Cannon"},
            {"GasBomb", "Gas Bomb"},
            {"CannonTest", "Cannon"}
            // Add more mappings as needed
        };

        private readonly Queue<GameObject> _popupQueue = new Queue<GameObject>();
        private Transform _popupParent;
        private GameObject _popupPrefab;

        private void Awake()
        {
            Instance = this;

            GameObject menu = GameObject.Find("DefaultMenu(Clone)");
            if (menu == null)
                return;

            _popupParent = menu.transform.Find("BottomRightPopups");
            if (_popupParent == null)
            {
                _popupParent = new GameObject("BottomRightPopups", typeof(RectTransform)).transform;
                _popupParent.SetParent(menu.transform);
                RectTransform rt = _popupParent.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-20f, 20f);
                rt.sizeDelta = new Vector2(300f, 400f);
            }

            _popupPrefab = (GameObject)ResourceManager.LoadAsset("UI", "ItemNotificationPopup", false);
        }

        public void ShowPopup(string message)
        {
            if (_popupPrefab == null || _popupParent == null)
                return;

            // Handle "Not Enough" messages
            if (message.StartsWith("Not Enough "))
            {
                string itemName = message.Substring(11); // Remove "Not Enough "
                if (_itemDisplayNames.TryGetValue(itemName, out string displayName))
                {
                    message = $"Not Enough {displayName}";
                }
            }
            else
            {
                // Handle regular change messages (e.g., "Wagon1 -1")
                string[] parts = message.Split(new[] { ' ' }, 2);
                if (parts.Length == 2)
                {
                    string itemName = parts[0];
                    string change = parts[1];

                    if (_itemDisplayNames.TryGetValue(itemName, out string displayName))
                    {
                        message = $"{displayName} {change}";
                    }
                }
            }

            GameObject popup = Instantiate(_popupPrefab, _popupParent);
            popup.transform.SetAsLastSibling();

            Text text = popup.GetComponentInChildren<Text>();
            if (text != null)
                text.text = message;

            _popupQueue.Enqueue(popup);
            UpdatePopupPositions();

            StartCoroutine(FadeAndDestroy(popup, Duration, FadeDuration));
        }

        private IEnumerator FadeAndDestroy(GameObject popup, float totalDuration, float fadeDuration)
        {
            yield return new WaitForSeconds(totalDuration - fadeDuration);

            CanvasGroup cg = popup.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = popup.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            cg.alpha = 0f;

            if (_popupQueue.Contains(popup))
            {
                _popupQueue.Dequeue();
                Destroy(popup);
                UpdatePopupPositions();
            }
        }

        private void UpdatePopupPositions()
        {
            int index = 0;
            foreach (GameObject popup in _popupQueue)
            {
                RectTransform rt = popup.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition = new Vector2(0f, index * Spacing);
                index++;
            }

            while (_popupQueue.Count > MaxPopups)
            {
                GameObject oldest = _popupQueue.Dequeue();
                Destroy(oldest);
            }
        }

        // Optional: Public method to add or update display names at runtime
        public static void SetDisplayName(string internalName, string displayName)
        {
            if (Instance != null)
            {
                if (Instance._itemDisplayNames.ContainsKey(internalName))
                    Instance._itemDisplayNames[internalName] = displayName;
                else
                    Instance._itemDisplayNames.Add(internalName, displayName);
            }
        }
    }
}