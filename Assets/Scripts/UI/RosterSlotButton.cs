using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (교배 UI 로스터 그리드 항목)
    public sealed class RosterSlotButton : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Button button;

        private SlimeInstance _instance;
        

        private void Awake()
        {
            if (label == null)
            {
                label = GetComponentInChildren<Text>(true);
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }
private Action<SlimeInstance> _onClicked;

        public void Bind(SlimeInstance instance, Action<SlimeInstance> onClicked)
        {
            _instance = instance;
            _onClicked = onClicked;

            if (label != null)
            {
                label.text = instance.speciesId;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClicked?.Invoke(_instance));
            }
        }
    }
}
