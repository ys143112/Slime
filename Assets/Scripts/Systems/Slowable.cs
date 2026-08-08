using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// 둔화를 받을 수 있는 것. 늪지대 슬라임의 패시브가 이걸 건다.
    /// </summary>
    public interface ISlowable
    {
        void ApplySlow(float scale, float seconds);
    }

    /// <summary>
    /// 둔화 상태를 세는 작은 저울. 이동을 가진 셋(플레이어·동행·야생)이 같은
    /// 규칙을 쓰도록 값 하나로 묶어 둔다 — 각자 타이머를 들면 "가장 센 둔화가
    /// 이긴다" 같은 규칙이 조금씩 어긋난다.
    /// </summary>
    public struct SlowTimer
    {
        private float _scale;
        private float _until;

        /// <summary>지금 걸린 이동 배율. 아무것도 없으면 1.</summary>
        public float Scale => Time.time < _until ? _scale : 1f;

        /// <summary>
        /// 더 센 둔화가 이긴다. 같은 세기면 시간만 늘린다 — 늪지대 슬라임 둘이
        /// 겹쳐 있다고 두 배로 느려지면 붙잡힌 채 아무것도 못 한다.
        /// </summary>
        public void Apply(float scale, float seconds)
        {
            float next = Time.time + seconds;
            if (Time.time >= _until || scale < _scale)
            {
                _scale = scale;
                _until = next;
                return;
            }

            if (Mathf.Approximately(scale, _scale) && next > _until)
            {
                _until = next;
            }
        }
    }
}
