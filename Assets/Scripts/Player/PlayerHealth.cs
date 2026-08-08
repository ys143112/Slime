using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHp = 30;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private DamageFlash damageFlash;

        private int _currentHp;
        private bool _dead;
        private ActorAnimation _animation;

        // 테스트가 체력을 읽을 자리. 지금까지 남은 체력을 밖에서 볼 방법이
        // 하나도 없어 spec-007 의 "피격 1프레임 안에 감소" 를 잴 수 없었다.
        public int CurrentHp => _currentHp;

        public int MaxHp => maxHp;

        public Faction Faction => Faction.Player;

        private void Awake()
        {
            _currentHp = maxHp;
            _animation = GetComponent<ActorAnimation>();
            UpdateHealthBar();
        }

        public void ApplyDamage(int amount, object source)
        {
            if (_dead || Factions.IsFriendlyFire(source, this))
            {
                return;
            }

            _currentHp = Mathf.Max(0, _currentHp - amount);
            Debug.Log($"player_damaged hp={_currentHp}/{maxHp}");

            if (damageFlash != null)
            {
                damageFlash.Play();
            }

            if (_animation != null)
            {
                _animation.PlayHit();
            }

            UpdateHealthBar();

            if (_currentHp == 0)
            {
                _dead = true;
                if (_animation != null)
                {
                    _animation.PlayDeath();
                }

                if (GameManager.Instance == null)
                {
                    Debug.LogError("PlayerHealth: GameManager 인스턴스가 없어 사망 처리를 알릴 수 없습니다.");
                    return;
                }

                GameManager.Instance.EndRun(RunEndCause.Death);
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBar == null)
            {
                return;
            }

            healthBar.SetRatio((float)_currentHp / Mathf.Max(1, maxHp));
        }
    }
}