using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-011
    public sealed class SatchelCounterUI : MonoBehaviour
    {
        [SerializeField] private Text counterText;

        // B 키로 여는 내용물 목록. 숫자만 봐서는 지금 무엇을 걸고 있는지 알 수 없다.
        [SerializeField] private Text detailText;

        private readonly StringBuilder _builder = new StringBuilder();

        private void Awake()
        {
            if (detailText != null)
            {
                detailText.gameObject.SetActive(false);
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

            if (detailText == null)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
            {
                detailText.gameObject.SetActive(!detailText.gameObject.activeSelf);
            }

            if (detailText.gameObject.activeSelf)
            {
                detailText.text = BuildContents();
            }
        }

        private string BuildContents()
        {
            _builder.Clear();
            _builder.AppendLine("배낭 (B)");

            if (RunSatchel.Count == 0)
            {
                _builder.Append("비어 있음");
                return _builder.ToString();
            }

            foreach (SlimeInstance held in RunSatchel.Contents)
            {
                _builder.Append(held.speciesId);
                if (held.mutantFlag)
                {
                    _builder.Append(" [돌연변이]");
                }

                _builder.AppendLine($" HP {held.baseStats.maxHp} ATK {held.baseStats.attack}");
            }

            return _builder.ToString();
        }
    }
}
