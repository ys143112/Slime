using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    // 기능: spec-011
    public sealed class SatchelCounterUI : MonoBehaviour
    {
        [SerializeField] private Text counterText;

        // 합격 기준: 표시는 포획 1프레임 안에 실제 소지 수와 같아야 한다 —
        // 갱신을 포획·정산 경로마다 부르는 대신 매 프레임 그대로 읽는다.
        private void Update()
        {
            if (counterText == null)
            {
                return;
            }

            counterText.text = $"위험 슬라임 {RunSatchel.Count}마리";
        }
    }
}
