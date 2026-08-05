using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001. 다이브 씬에서 탈출 지점 방향을 가리키는 화살표.
    public sealed class ExtractionDirectionArrow : MonoBehaviour
    {
        [SerializeField] private RectTransform arrowRect;

        private Transform _player;
        private Transform _extractionPoint;

        private void OnEnable()
        {
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
