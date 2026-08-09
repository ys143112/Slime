using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay
{
    /// <summary>기능: spec-001 — 탈출 지점을 가리키는 나침반.</summary>
    /// <remarks>
    /// 예전에는 씬마다 배선된 작은 화살표가 화면 위쪽 구석에 떠 있었다. 나침반
    /// 그림(<c>Resources/UI/compass</c> + <c>pointer</c>)으로 갈고 왼쪽 아래
    /// 빈자리로 옮겼다(사용자, 2026-08-09).
    ///
    /// 씬(바이옴 셋)을 각각 고치는 대신 코드로 만든다 — 배낭·미니맵과 같은 규칙
    /// (<see cref="HudRoot"/> 밑). 씬에 남은 옛 화살표는 <see cref="OnEnable"/>
    /// 이 끈다.
    /// </remarks>
    public sealed class ExtractionDirectionArrow : MonoBehaviour
    {
        // 씬에 남아 있는 옛 화살표. 이제 끄기만 한다.
        [SerializeField] private RectTransform arrowRect;

        private const float CompassSize = 132f;

        private Transform _player;
        private Transform _extractionPoint;
        private RectTransform _compass;
        private RectTransform _needle;

        private void OnEnable()
        {
            // **이 컴포넌트가 옛 화살표와 같은 GameObject 에 붙어 있다**(씬 배선).
            // 오브젝트를 끄면 자기 Update 까지 멈춰 바늘이 안 돈다 — 그림만 끈다
            // (2026-08-09 실측: 각도가 0 에서 안 움직였다).
            if (arrowRect != null)
            {
                var graphic = arrowRect.GetComponent<Graphic>();
                if (graphic != null)
                {
                    graphic.enabled = false;
                }
            }

            Build();
        }

        private void OnDisable()
        {
            // 나침반은 씬을 넘어가는 캔버스 밑이라, 이 컴포넌트가 씬과 함께
            // 사라질 때 같이 치우지 않으면 목장에 남는다.
            if (_compass != null)
            {
                Destroy(_compass.gameObject);
                _compass = null;
            }
        }

        private void Build()
        {
            if (_compass != null)
            {
                return;
            }

            Sprite dial = Resources.Load<Sprite>("UI/compass");
            Sprite pointer = Resources.Load<Sprite>("UI/pointer");
            if (dial == null || pointer == null)
            {
                Debug.LogWarning("ExtractionDirectionArrow: UI/compass 또는 UI/pointer 를 찾지 못했습니다.");
                return;
            }

            var go = new GameObject("Compass", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(HudRoot.Get(), false);
            _compass = go.GetComponent<RectTransform>();

            // 왼쪽 아래. 공격 쿨다운 막대(y 24)와 동행 체력바(y 76) 위에 앉힌다.
            _compass.anchorMin = Vector2.zero;
            _compass.anchorMax = Vector2.zero;
            _compass.pivot = Vector2.zero;
            _compass.anchoredPosition = new Vector2(24f, 120f);
            _compass.sizeDelta = new Vector2(CompassSize, CompassSize);

            var dialImage = go.GetComponent<Image>();
            dialImage.sprite = dial;
            dialImage.raycastTarget = false;
            dialImage.preserveAspect = true;

            var needleGo = new GameObject("Needle", typeof(RectTransform), typeof(Image));
            needleGo.transform.SetParent(go.transform, false);

            _needle = needleGo.GetComponent<RectTransform>();
            _needle.anchorMin = new Vector2(0.5f, 0.5f);
            _needle.anchorMax = new Vector2(0.5f, 0.5f);
            _needle.pivot = new Vector2(0.5f, 0.5f);
            _needle.sizeDelta = new Vector2(CompassSize * 0.42f, CompassSize * 0.42f);

            var needleImage = needleGo.GetComponent<Image>();
            needleImage.sprite = pointer;
            needleImage.raycastTarget = false;
            needleImage.preserveAspect = true;
        }

        private void Update()
        {
            if (_compass == null)
            {
                return;
            }

            if (_player == null || _extractionPoint == null)
            {
                // 추출구는 지도를 다 그린 뒤에 자리를 잡는다 — 첫 프레임에 못
                // 찾았어도 계속 다시 본다.
                if (_player == null)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    _player = playerObj != null ? playerObj.transform : null;
                }

                if (_extractionPoint == null)
                {
                    ExtractionPoint point = FindAnyObjectByType<ExtractionPoint>();
                    _extractionPoint = point != null ? point.transform : null;
                }

                _compass.gameObject.SetActive(_player != null && _extractionPoint != null);
                return;
            }

            Vector2 toExtraction = (Vector2)(_extractionPoint.position - _player.position);
            if (toExtraction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // 바늘 그림은 위(+Y)를 향해 그려져 있다. **회전만 한다** — 자리를
            // 옮기면 나침반 밖으로 걸어 나간 것처럼 보인다(사용자, 2026-08-09).
            float angle = Mathf.Atan2(toExtraction.y, toExtraction.x) * Mathf.Rad2Deg - 90f;
            _needle.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }
}
