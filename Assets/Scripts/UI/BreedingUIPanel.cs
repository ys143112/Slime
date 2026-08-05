using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (교배 UI 패널)
    public sealed class BreedingUIPanel : MonoBehaviour
    {
        public BreedingPen pen;
        public CanvasGroup canvasGroup;
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
        private bool _visible;

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

            SetVisible(false);
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
            if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            {
                // spec-003: 패널은 씬을 넘나드는 영구 오브젝트라 Hub 밖에서는
                // BreedingPen 이 없다 - 그럴 땐 U 를 눌러도 열리지 않는다.
                if (_visible || ResolvePen() != null)
                {
                    SetVisible(!_visible);
                }
            }

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

        // spec-003: U 키로 패널을 켜고 끈다. 이 오브젝트 자체는 계속 활성 상태로
        // 둬야 Update() 가 다음 U 입력과 부화 카운트다운을 계속 감지한다.
        private void SetVisible(bool visible)
        {
            _visible = visible;

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
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

        // spec-003: pen 은 Hub 씬에만 있는 오브젝트다. 이 패널은 DontDestroyOnLoad
        // 로 씬을 넘나들며 살아남으므로, Hub 가 다시 로드될 때마다 새 인스턴스를
        // 다시 찾아야 한다 - 캐시된 참조는 씬이 바뀌면 죽은 참조가 된다.
        private BreedingPen ResolvePen()
        {
            if (pen == null)
            {
                GameObject go = GameObject.Find("BreedingPen");
                pen = go != null ? go.GetComponent<BreedingPen>() : null;
            }

            return pen;
        }

        private void Breed()
        {
            BreedingPen activePen = ResolvePen();
            if (activePen == null || _slotA == null || _slotB == null)
            {
                return;
            }

            if (!activePen.TryPlace(_slotA))
            {
                return;
            }

            if (!activePen.TryPlace(_slotB))
            {
                activePen.WithdrawLast();
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
