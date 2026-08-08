using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class PlayerHealth : MonoBehaviour, IDamageable, ISlowable
    {
        [SerializeField] private int maxHp = 30;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private DamageFlash damageFlash;

        private int _currentHp;
        private bool _dead;
        private SlowTimer _slow;
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

        public void Heal(int amount)
        {
            // 죽은 뒤에 무지개 슬라임이 살려내면 안 된다.
            if (_dead || amount <= 0 || _currentHp >= maxHp)
            {
                return;
            }

            _currentHp = Mathf.Min(maxHp, _currentHp + amount);
            UpdateHealthBar();
        }

        // 둔화는 체력이 들고 있다가 이동이 읽어 간다. 둘을 한 컴포넌트에 두면
        // 이동 스크립트가 없는 개체(교배장에 세워 둔 슬라임 등)에서 깨진다.
        public void ApplySlow(float scale, float seconds)
        {
            _slow.Apply(scale, seconds);
        }

        public float SpeedScale => _slow.Scale;

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