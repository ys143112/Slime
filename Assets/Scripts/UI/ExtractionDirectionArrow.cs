using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001. 다이브 씬에서 탈출 지점 방향을 가리키는 화살표.
    public sealed class ExtractionDirectionArrow : MonoBehaviour
    {
        [SerializeField] private RectTransform arrowRect;

        // 화면 위쪽 구석에 있는 데다 씬에서 잡힌 크기가 작아 눈에 안 들어왔다
        // (팀 QA, 2026-08-09). 씬 셋을 각각 고치는 대신 여기서 곱한다 — 새
        // 바이옴 씬을 만들어도 같은 크기로 나온다.
        [SerializeField] private float arrowScale = 2f;

        private Transform _player;
        private Transform _extractionPoint;

        private void OnEnable()
        {
            // 대입이라 OnEnable 이 여러 번 돌아도 누적되지 않는다.
            if (arrowRect != null && arrowScale > 0f)
            {
                arrowRect.localScale = new Vector3(arrowScale, arrowScale, 1f);
            }

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            _player = playerObj != null ? playerObj.transform : null;

            ExtractionPoint point = FindObjectOfType<ExtractionPoint>();
            _extractionPoint = point != null ? point.transform : null;

            if (arrowRect != null)
            {
                arrowRect.gameObject.SetActive(_player != null && _extractionPoint != null);
            }
        }

        private void Update()
        {
            if (arrowRect == null || _player == null || _extractionPoint == null)
            {
                return;
            }

            Vector2 toExtraction = (Vector2)(_extractionPoint.position - _player.position);
            if (toExtraction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(toExtraction.y, toExtraction.x) * Mathf.Rad2Deg;
            arrowRect.localEulerAngles = new Vector3(0f, 0f, angle - 90f);
        }
    }
}
