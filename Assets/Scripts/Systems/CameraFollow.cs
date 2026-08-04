using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-001 (맵을 넓히면서 필요해진 카메라 추적 - 이전엔 카메라가 고정이라
    // 플레이어가 화면 밖으로 나갈 수 있었다)
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Player";

        private Transform _target;

        private void LateUpdate()
        {
            if (_target == null)
            {
                var found = GameObject.FindWithTag(targetTag);
                if (found == null)
                {
                    return;
                }

                _target = found.transform;
            }

            Vector3 position = _target.position;
            position.z = transform.position.z;
            transform.position = position;
        }
    }
}
