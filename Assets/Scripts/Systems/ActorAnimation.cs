using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-007/spec-012 표현 계층.
    // 플레이어와 슬라임이 **같은** 컴포넌트를 쓴다 — 둘 다 idle / 8방향 이동 /
    // 8방향 공격 / 피격이라는 같은 상태 집합을 갖기 때문이다. 클립만 다르게
    // 끼우면 되므로 컴포넌트를 둘로 나눌 이유가 없다.
    //
    // 효과음은 애니메이션 이벤트가 아니라 이 스크립트가 낸다. 클립이 아직
    // 없는 상태에서도 배선이 성립해야 하고(이벤트는 클립에 붙는다), 클립을
    // 나중에 갈아 끼워도 효과음 배선이 안 날아간다.
    [DisallowMultipleComponent]
    public sealed class ActorAnimation : MonoBehaviour
    {
        // Animator 파라미터 이름. 문자열을 해시로 바꿔 매 프레임 비교 비용을 없앤다.
        public static readonly int MoveXParam = Animator.StringToHash("MoveX");
        public static readonly int MoveYParam = Animator.StringToHash("MoveY");
        public static readonly int SpeedParam = Animator.StringToHash("Speed");
        public static readonly int AttackParam = Animator.StringToHash("Attack");
        public static readonly int HitParam = Animator.StringToHash("Hit");
        public static readonly int DeadParam = Animator.StringToHash("Dead");

        [Header("애니메이터")]
        [SerializeField] private Animator animator;

        [Header("효과음 — 상태마다 하나씩 끼운다 (비어 있으면 조용히 넘어간다)")]
        [SerializeField] private AudioClip idleSfx;
        [SerializeField] private AudioClip moveSfx;
        [SerializeField] private AudioClip attackSfx;
        [SerializeField] private AudioClip hitSfx;
        [SerializeField] private AudioClip deathSfx;

        [Header("이동 효과음 반복 간격(초). 0 이면 한 번만 낸다")]
        [SerializeField] private float moveSfxInterval = 0.4f;

        private float _nextMoveSfxTime;
        private bool _wasMoving;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // 시작 방향을 남쪽으로 못박는다. MoveX/MoveY 기본값 0,0 은 블렌드트리의
            // 원점이라 8방향이 한꺼번에 섞인다 — 한 번도 안 움직인 개체가 어느 쪽도
            // 아닌 상태로 서 있게 되고, 대각선 칸이 1프레임짜리 정지 그림인 종
            // (용암 슬라임은 원본 GIF 이 4방향뿐이다)은 그 뭉갬이 "멈춘 그림"으로
            // 보인다. 정지 시 방향을 안 덮는 규칙과도 어긋나지 않는다 — 여기서
            // 정하는 건 최초 1회뿐이다.
            SetFacing(Vector2.down);

            // 발밑 그림자는 **플레이어에게만** 붙인다(사용자, 2026-08-09: 슬라임은
            // 빼라). 슬라임은 제자리에서 통통 뛰는 그림이라 그림자가 붙으면 뜀에
            // 따라 같이 움직여 오히려 바닥에서 떠 보였다.
            if (GetComponent<PlayerMovement>() != null)
            {
                ActorShadow.Attach(transform, GetComponent<SpriteRenderer>());
            }
        }

        /// <summary>
        /// 이동 속도를 애니메이터에 넘긴다. 매 FixedUpdate 마다 불러도 된다.
        /// </summary>
        /// <remarks>
        /// 정지하면 MoveX/MoveY 를 0 으로 덮지 <b>않는다.</b> 덮으면 블렌드트리가
        /// 원점으로 돌아가 어느 쪽을 보고 서 있는지 잃어버린다 — 마지막 방향을
        /// 유지해야 idle 도 그 방향으로 선다.
        /// </remarks>
        public void SetMovement(Vector2 velocity)
        {
            if (animator == null)
            {
                return;
            }

            float speed = velocity.magnitude;
            animator.SetFloat(SpeedParam, speed);

            bool moving = speed > 0.01f;
            if (moving)
            {
                Vector2 direction = velocity.normalized;
                animator.SetFloat(MoveXParam, direction.x);
                animator.SetFloat(MoveYParam, direction.y);
                PlayMoveSfx();
            }

            _wasMoving = moving;
        }

        /// <summary>바라보는 방향만 갱신한다(정지 중 공격 방향 등).</summary>
        public void SetFacing(Vector2 facing)
        {
            if (animator == null || facing == Vector2.zero)
            {
                return;
            }

            Vector2 direction = facing.normalized;
            animator.SetFloat(MoveXParam, direction.x);
            animator.SetFloat(MoveYParam, direction.y);
        }

        public void PlayAttack(Vector2 facing)
        {
            SetFacing(facing);
            if (animator != null)
            {
                animator.SetTrigger(AttackParam);
            }

            PlaySfx(attackSfx);
        }

        public void PlayHit()
        {
            if (animator != null)
            {
                animator.SetTrigger(HitParam);
            }

            PlaySfx(hitSfx);
        }

        public void PlayDeath()
        {
            if (animator != null)
            {
                animator.SetBool(DeadParam, true);
            }

            PlaySfx(deathSfx);
        }

        /// <summary>대기 효과음. 상태 진입 시 한 번만 부르는 용도다.</summary>
        public void PlayIdle()
        {
            PlaySfx(idleSfx);
        }

        private void PlayMoveSfx()
        {
            if (moveSfx == null)
            {
                return;
            }

            // 간격이 0 이면 이동을 시작하는 순간에만 낸다 — 매 프레임 내면
            // 발소리가 소음이 된다.
            if (moveSfxInterval <= 0f)
            {
                if (!_wasMoving)
                {
                    PlaySfx(moveSfx);
                }

                return;
            }

            if (Time.time >= _nextMoveSfxTime)
            {
                PlaySfx(moveSfx);
                _nextMoveSfxTime = Time.time + moveSfxInterval;
            }
        }

        // 볼륨 설정(설정 패널의 효과음 슬라이더)이 한 곳에서 걸리도록 전부
        // AudioManager 를 거친다. 여기서 AudioSource 를 직접 두면 그 개체만
        // 슬라이더를 무시하게 된다.
        private static void PlaySfx(AudioClip clip)
        {
            if (clip == null || AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.PlaySfx(clip);
        }
    }
}
