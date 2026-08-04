using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (교배 UI 패널)
    public sealed class BreedingUIPanel : MonoBehaviour
    {
        public BreedingPen pen;
        public RectTransform gridContent;
        public RosterSlotButton gridSlotTemplate;
        public Text slotAText;
        public Text slotBText;
        public Text eggResultText;
        public Text hatchConditionText;
        public Text hatchTimeText;
        public Button breedButton;

        private SlimeInstance _slotA;
        private SlimeInstance _slotB;
        private readonly List<RosterSlotButton> _spawnedButtons = new List<RosterSlotButton>();
        private SlimeEgg _shownEgg;

        private void OnEnable()
        {
            if (EggIncubator.Instance != null)
            {
                EggIncubator.Instance.EggAdded += OnEggAdded;
            }

            if (breedButton != null)
            {
                breedButton.onClick.AddListener(OnBreedButtonClicked);
            }

            RefreshGrid();
            RefreshSlots();
        }

        private void OnDisable()
        {
            if (EggIncubator.Instance != null)
            {
                EggIncubator.Instance.EggAdded -= OnEggAdded;
            }

            if (breedButton != null)
            {
                breedButton.onClick.RemoveListener(OnBreedButtonClicked);
            }
        }

        private void Update()
        {
            if (_shownEgg == null || hatchTimeText == null)
            {
                return;
            }

            if (EggIncubator.Instance == null || !EggIncubator.Instance.Eggs.Contains(_shownEgg))
            {
                hatchTimeText.text = "부화 완료";
                _shownEgg = null;
                return;
            }

            float remaining = _shownEgg.RemainingSeconds(DateTime.UtcNow);
            hatchTimeText.text = $"부화까지 {remaining:0.0}초";
        }

        // spec-003: 로스터가 바뀔 때마다(교배 완료 포함) 그리드를 다시 그린다.
        public void RefreshGrid()
        {
            foreach (RosterSlotButton button in _spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _spawnedButtons.Clear();

            if (gridSlotTemplate == null || gridContent == null || PlayerRoster.Instance == null)
            {
                return;
            }

            gridSlotTemplate.gameObject.SetActive(false);

            foreach (SlimeInstance instance in PlayerRoster.Instance.Roster)
            {
                RosterSlotButton spawned = Instantiate(gridSlotTemplate, gridContent);
                spawned.gameObject.SetActive(true);
                spawned.Bind(instance, OnRosterSlotClicked);
                _spawnedButtons.Add(spawned);
            }
        }

        private void OnRosterSlotClicked(SlimeInstance instance)
        {
            if (_slotA == null)
            {
                _slotA = instance;
            }
            else if (_slotB == null && instance != _slotA)
            {
                _slotB = instance;
            }

            RefreshSlots();

            if (_slotA != null && _slotB != null)
            {
                Breed();
            }
        }

        private void RefreshSlots()
        {
            if (slotAText != null)
            {
                slotAText.text = _slotA != null ? _slotA.speciesId : "선택 안 됨";
            }

            if (slotBText != null)
            {
                slotBText.text = _slotB != null ? _slotB.speciesId : "선택 안 됨";
            }

            if (breedButton != null)
            {
                breedButton.interactable = _slotA != null && _slotB != null;
            }
        }

        public void OnBreedButtonClicked()
        {
            Breed();
        }

        private void Breed()
        {
            if (pen == null || _slotA == null || _slotB == null)
            {
                return;
            }

            if (!pen.TryPlace(_slotA))
            {
                return;
            }

            if (!pen.TryPlace(_slotB))
            {
                pen.WithdrawLast();
                return;
            }

            _slotA = null;
            _slotB = null;
            RefreshSlots();
            RefreshGrid();
        }

        private void OnEggAdded(SlimeEgg egg)
        {
            _shownEgg = egg;

            if (eggResultText != null)
            {
                eggResultText.text = $"슬라임 알 ({egg.speciesId})";
            }

            if (hatchConditionText != null)
            {
                hatchConditionText.text = $"부화 조건: {egg.hatchConditionLabel}";
            }
        }
    }
}
