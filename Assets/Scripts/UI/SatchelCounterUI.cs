using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-011
    public sealed class SatchelCounterUI : MonoBehaviour
    {
        [SerializeField] private Text counterText;

        // B 키로 여는 내용물 목록. 스크롤 패널 전체를 켜고 끈다.
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private RectTransform detailListContent;
        [SerializeField] private SatchelSlotView detailSlotTemplate;

        private readonly List<SatchelSlotView> _spawned = new List<SatchelSlotView>();
        private int _lastRenderedCount = -1;

        private void Awake()
        {
            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }
        }

        // 합격 기준: 표시는 포획 1프레임 안에 실제 소지 수와 같아야 한다 —
        // 갱신을 포획·정산 경로마다 부르는 대신 매 프레임 그대로 읽는다.
        private void Update()
        {
            if (counterText != null)
            {
                counterText.text = $"위험 슬라임 {RunSatchel.Count}마리";
            }

            if (detailPanel == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
            {
                bool next = !detailPanel.activeSelf;
                detailPanel.SetActive(next);
                if (next)
                {
                    _lastRenderedCount = -1;
                }
            }

            // 목록을 매 프레임 다시 그리면 스폰/파괴가 반복돼 낭비다 - 마릿수가
            // 실제로 바뀐 프레임에만 다시 그린다.
            if (detailPanel.activeSelf && RunSatchel.Count != _lastRenderedCount)
            {
                RefreshDetailList();
            }
        }

        private void RefreshDetailList()
        {
            foreach (SatchelSlotView slot in _spawned)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _spawned.Clear();
            _lastRenderedCount = RunSatchel.Count;

            if (detailSlotTemplate == null || detailListContent == null)
            {
                return;
            }

            detailSlotTemplate.gameObject.SetActive(false);

            foreach (SlimeInstance instance in RunSatchel.Contents)
            {
                SatchelSlotView spawned = Instantiate(detailSlotTemplate, detailListContent);
                spawned.gameObject.SetActive(true);
                spawned.Bind(instance);
                _spawned.Add(spawned);
            }
        }
    }
}
