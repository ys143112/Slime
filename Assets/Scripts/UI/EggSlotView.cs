using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-003 (부화 대기 중인 알 목록 한 칸)
    public sealed class EggSlotView : MonoBehaviour
    {
        [SerializeField] private Text label;

        // 무엇이 부화 중인지 그림으로 먼저 보여준다. 글자만으로는 목록이 길어질수록
        // 어느 줄이 무엇인지 읽는 데 시간이 걸린다(사용자, 2026-08-08).
        [SerializeField] private Image icon;

        private SlimeEgg _egg;

        // 씬의 템플릿은 줄 높이 40 / 아이콘 32 / 기본 글자 크기라 교배창에서
        // 아이콘도 글자도 안 읽혔다(사용자, 2026-08-09). 인벤토리 칸(140)만큼은
        // 아니어도 목록 줄로서는 이 정도가 인벤토리 아이콘과 같은 눈높이다.
        private const float RowHeight = 76f;
        private const float IconSize = 64f;
        private const int LabelFontSize = 22;

        public void Bind(SlimeEgg egg)
        {
            _egg = egg;
            Enlarge();

            if (icon != null)
            {
                Sprite portrait = SlimeSpeciesCatalog.Portrait(
                    new SlimeInstance { speciesId = egg.speciesId, shinyFlag = egg.shinyFlag });
                icon.sprite = portrait;
                icon.preserveAspect = true;
                icon.color = egg.shinyFlag ? egg.shinyTint : Color.white;
                icon.enabled = portrait != null;
            }

            Tick(DateTime.UtcNow);
        }

        /// <summary>줄 높이·아이콘·글자를 키운다. 씬 템플릿 값은 너무 작다.</summary>
        /// <remarks>
        /// 씬(Boot)을 고치는 대신 코드에서 덮는 이유는 인벤토리 격자와 같다 —
        /// Boot 씬은 병합 사고로 UI 를 통째로 잃은 전력이 있다(CLAUDE.md).
        /// 줄 높이는 <see cref="LayoutElement"/> 로 준다: 부모가
        /// <see cref="VerticalLayoutGroup"/> 이라 RectTransform 높이를 직접 넣으면
        /// 레이아웃이 다음 프레임에 도로 덮어쓴다.
        /// </remarks>
        private void Enlarge()
        {
            var element = GetComponent<LayoutElement>();
            if (element == null)
            {
                element = gameObject.AddComponent<LayoutElement>();
            }

            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;

            if (icon != null)
            {
                var iconRect = (RectTransform)icon.transform;
                iconRect.sizeDelta = new Vector2(IconSize, IconSize);
                iconRect.anchoredPosition = new Vector2(10f, 0f);
            }

            if (label != null)
            {
                label.fontSize = LabelFontSize;
                var labelRect = (RectTransform)label.transform;

                // 아이콘 오른쪽으로 비켜 놓는다. 앵커가 부모를 꽉 채우고 있어
                // offsetMin.x 하나로 왼쪽 여백이 정해진다.
                labelRect.offsetMin = new Vector2(IconSize + 20f, 4f);
                labelRect.offsetMax = new Vector2(-12f, -4f);
            }
        }

        public void Tick(DateTime nowUtc)
        {
            if (_egg == null || label == null)
            {
                return;
            }

            string tag = _egg.mutantFlag ? " [돌연변이]" : _egg.corruptedGeneFlag ? " [오염]" : "";
            float remaining = _egg.RemainingSeconds(nowUtc);
            label.text = $"{SlimeSpeciesCatalog.DisplayName(_egg.speciesId)}{tag}  {_egg.hatchConditionLabel} ({remaining:0.0}s)";
        }
    }
}
