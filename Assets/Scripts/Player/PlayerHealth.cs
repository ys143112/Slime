using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 30;

        private int _currentHp;
        private bool _dead;

        private void Awake()
        {
            _currentHp = maxHp;
        }

        public void ApplyDamage(int amount, object source)
        {
            if (_dead)
            {
                return;
            }

            _currentHp = Mathf.Max(0, _currentHp - amount);
            if (_currentHp == 0)
            {
                _dead = true;
                if (GameManager.Instance == null)
                {
                    Debug.LogError("PlayerHealth: GameManager 인스턴스가 없어 사망 처리를 알릴 수 없습니다.");
                    return;
                }

                GameManager.Instance.EndRun(RunEndCause.Death);
            }
        }
    }
}