using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Settings;
using Characters;
using GameManagers;
using ApplicationManagers;

namespace UI
{
    class ItemHandler : MonoBehaviour
    {
        private BasePopup _itemWheelPopup;
        private List<FieldInfo> _itemLists = new List<FieldInfo>();
        private int _currentItemWheelIndex = 0;
        public bool IsActive;
        private InGameManager _inGameManager;

        private void Awake()
        {
            _itemWheelPopup = ElementFactory.InstantiateAndSetupPanel<WheelPopup>(transform, "Prefabs/InGame/WheelMenu").GetComponent<BasePopup>();
            _inGameManager = (InGameManager)SceneLoader.CurrentGameManager;
        }

        private void Start()
        {
            StartCoroutine(UpdateForever(1f));
        }

        private void Update()
        {
            if (IsActive && Input.GetKeyDown(KeyCode.Space))
            {
                NextItemWheel();
            }

            if (IsActive && Input.GetKeyDown(KeyCode.Escape))
            {
                SetItemWheel(false);
            }
        }

        public void ToggleItemWheel()
        {
            SetItemWheel(!IsActive);
        }

        public void SetItemWheel(bool enable)
        {
            if (!InGameMenu.InMenu())
                ScanItemLists();

            if (enable)
            {
                if (_itemLists.Count > 0)
                {
                    ShowItemWheel(_currentItemWheelIndex);
                    IsActive = true;
                }
            }
            else
            {
                _itemWheelPopup.Hide();
                IsActive = false;
            }
        }

        public void NextItemWheel()
        {
            if (!_itemWheelPopup.gameObject.activeSelf || !IsActive || _itemLists.Count == 0)
                return;

            _currentItemWheelIndex++;
            if (_currentItemWheelIndex >= _itemLists.Count)
                _currentItemWheelIndex = 0;

            ShowItemWheel(_currentItemWheelIndex);
        }

        private void ShowItemWheel(int index)
        {
            BaseCharacter character = _inGameManager.CurrentCharacter;
            if (character is not Human human || _itemLists.Count == 0)
                return;

            FieldInfo field = _itemLists[index];

            // Naming
            string wheelName = human.ItemListDisplayNames != null && human.ItemListDisplayNames.ContainsKey(field.Name)
                ? human.ItemListDisplayNames[field.Name]
                : field.Name;

            List<SimpleUseable> list = (List<SimpleUseable>)field.GetValue(human);
            List<string> itemNames = new List<string>();

            foreach (var item in list)
            {
                string name = item.Name;
                if (item.MaxUses != -1)
                    name += $" ({item.UsesLeft})";
                else if (item.GetCooldownLeft() > 0f)
                    name += $" ({(int)item.GetCooldownLeft()})";
                itemNames.Add(name);
            }

            ((WheelPopup)_itemWheelPopup).Show(wheelName, itemNames, () => OnItemSelect(list));
        }

        private void OnItemSelect(List<SimpleUseable> list)
        {
            BaseCharacter character = _inGameManager.CurrentCharacter;
            if (character is not Human)
                return;

            int selected = ((WheelPopup)_itemWheelPopup).SelectedItem;
            if (selected >= 0 && selected < list.Count)
                list[selected].SetInput(true);

            _itemWheelPopup.Hide();
            IsActive = false;
            ((InGameMenu)UIManager.CurrentMenu).SkipAHSSInput = true;
        }

        private void ScanItemLists()
        {
            _itemLists.Clear();
            BaseCharacter character = _inGameManager.CurrentCharacter;
            if (character is not Human human)
                return;

            var fields = typeof(Human).GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name.StartsWith("itemList") && field.FieldType == typeof(List<SimpleUseable>))
                    _itemLists.Add(field);
            }

            _itemLists.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        private IEnumerator UpdateForever(float delay)
        {
            while (true)
            {
                yield return new WaitForSeconds(delay);
                if (IsActive && _itemLists.Count > 0)
                    ShowItemWheel(_currentItemWheelIndex);
            }
        }
    }
}
