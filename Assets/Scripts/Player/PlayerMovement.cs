using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay
{
    // 기능: spec-007
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        // spec-012: 근접 공격이 바라보는 쪽으로 나가야 한다. 멈춰 있어도 마지막
        // 방향을 유지한다 — 정지 중에 공격이 자기 발밑을 때리면 시선이 없다.
        public Vector2 LastDirection { get; private set; } = Vector2.down;

        private Rigidbody2D _body;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (_body == null)
            {
                Debug.LogError("PlayerMovement: Rigidbody2D 컴포넌트가 없어 이동을 비활성화합니다.");
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
            }

            if (input != Vector2.zero)
            {
                LastDirection = input.normalized;
            }

            _body.linearVelocity = input.normalized * moveSpeed;
        }
    }
}