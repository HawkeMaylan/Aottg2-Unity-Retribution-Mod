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
        private const int MaxPopups = 5;
        private const float Spacing = 35f;

        private readonly Queue<GameObject> _popupQueue = new Queue<GameObject>();

        private Transform _popupParent;
        private GameObject _popupPrefab;

        private void Awake()
        {
            Instance = this;
            Debug.Log("[ItemPopupManager] Awake called");

            GameObject menu = GameObject.Find("DefaultMenu(Clone)");
            if (menu == null)
            {
                Debug.LogError("[ItemPopupManager] Could not find DefaultMenu(Clone)!");
                return;
            }

            _popupParent = menu.transform.Find("BottomRightPopups");
            if (_popupParent == null)
            {
                Debug.Log("[ItemPopupManager] BottomRightPopups not found, creating manually.");
                _popupParent = new GameObject("BottomRightPopups", typeof(RectTransform)).transform;
                _popupParent.SetParent(menu.transform);
                RectTransform rt = _popupParent.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-20f, 20f);
                rt.sizeDelta = new Vector2(300f, 400f);
            }

            _popupPrefab = (GameObject)ResourceManager.LoadAsset("UI", "ItemNotificationPopup", false);
            if (_popupPrefab == null)
                Debug.LogError("[ItemPopupManager] Failed to load UI/ItemNotificationPopup prefab!");
            else
                Debug.Log("[ItemPopupManager] Prefab loaded successfully.");
        }

        public void ShowPopup(string message)
        {
            Debug.Log("[ItemPopupManager] ShowPopup called with message: " + message);

            if (_popupPrefab == null || _popupParent == null)
            {
                Debug.LogWarning("[ItemPopupManager] ShowPopup failed due to missing prefab or parent.");
                return;
            }

            GameObject popup = Instantiate(_popupPrefab, _popupParent);
            popup.transform.SetAsLastSibling();

            Text text = popup.GetComponentInChildren<Text>();
            if (text != null)
                text.text = message;
            else
                Debug.LogWarning("[ItemPopupManager] No Text component found in popup prefab.");

            _popupQueue.Enqueue(popup);
            UpdatePopupPositions();

            StartCoroutine(DestroyAfter(popup, Duration));
        }

        private IEnumerator DestroyAfter(GameObject popup, float delay)
        {
            yield return new WaitForSeconds(delay);
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
    }
}