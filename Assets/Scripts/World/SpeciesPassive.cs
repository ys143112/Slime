using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>종의 특수 능력. 넷 다 "주기마다 반경 안을 훑어 뭔가 한다" 라는 같은 모양이다.</summary>
    public enum SpeciesPassiveKind
    {
        None,

        /// <summary>방어형: 근처 적이 나를 먼저 노린다.</summary>
        Taunt,

        /// <summary>무지개: 반경 안을 <b>진영 가리지 않고</b> 회복시킨다.</summary>
        AreaHeal,

        /// <summary>늪지대: 반경 안의 적을 느리게 만든다.</summary>
        Slow,

        /// <summary>용암: 반경 안의 적에게 지속 피해.</summary>
        Burn,
    }

    /// <summary>
    /// 종 패시브를 도는 컴포넌트. 야생이든 동행이든 같은 것을 붙인다.
    /// </summary>
    /// <remarks>
    /// 네 패시브가 전부 "주기 + 반경 + 대상 고르기" 라 클래스를 넷으로 나눌 이유가
    /// 없다. 값은 <see cref="SlimeSpecies"/> 가 들고 있으므로 밸런스는 재컴파일
    /// 없이 SO 에서 만진다.
    ///
    /// 붙이는 쪽이 <see cref="Attach"/> 를 부른다 — 프리팹마다 컴포넌트를 손으로
    /// 얹으면 종이 늘 때마다 프리팹을 고쳐야 하고, 동행·야생 프리팹이 따로라
    /// 두 번씩 고쳐야 한다.
    /// </remarks>
    public sealed class SpeciesPassive : MonoBehaviour
    {
        // 도발 중인 개체들. 야생 슬라임이 대상을 고를 때 이 목록을 본다.
        // 씬을 넘나들며 파괴되므로 OnDisable 에서 반드시 뺀다.
        private static readonly List<SpeciesPassive> Taunters = new List<SpeciesPassive>();

        private IFactionMember _owner;
        private SlimeSpecies _species;
        private PassiveRangeRing _ring;
        private float _nextTick;

        // 무슨 패시브인지 색으로 구분한다 — 회복은 초록, 지속 피해는 주황,
        // 둔화는 보라, 도발은 파랑.
        private static Color RingColorFor(SpeciesPassiveKind kind)
        {
            switch (kind)
            {
                case SpeciesPassiveKind.AreaHeal: return new Color(0.45f, 0.95f, 0.45f);
                case SpeciesPassiveKind.Burn: return new Color(1f, 0.55f, 0.2f);
                case SpeciesPassiveKind.Slow: return new Color(0.65f, 0.5f, 0.95f);
                default: return new Color(0.4f, 0.7f, 1f);
            }
        }

        public static void Attach(GameObject host, SlimeInstance instance)
        {
            if (host == null || instance == null)
            {
                return;
            }

            SlimeSpecies species = SlimeSpeciesCatalog.Lookup(instance.speciesId);
            var existing = host.GetComponent<SpeciesPassive>();

            // 패시브가 없는 종으로 바뀌었으면 예전 것을 떼야 한다 — 동행은 같은
            // 오브젝트를 갈아 끼우지 않지만, 야생은 Initialize 로 종이 바뀐다.
            if (species == null || species.passive == SpeciesPassiveKind.None)
            {
                if (existing != null)
                {
                    Destroy(existing);
                }

                return;
            }

            SpeciesPassive passive = existing != null ? existing : host.AddComponent<SpeciesPassive>();
            passive._species = species;
            passive._owner = host.GetComponent<IFactionMember>();
            passive.RegisterTaunt();
            passive._ring = PassiveRangeRing.Attach(host, species.passiveRadius, RingColorFor(species.passive));
        }

        /// <summary>
        /// 이 진영을 노리는 쪽이 우선해서 때려야 할 도발 대상. 없으면 null.
        /// </summary>
        public static Transform FindTauntTarget(Faction attacker, Vector2 from)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;

            foreach (SpeciesPassive taunter in Taunters)
            {
                if (taunter == null || taunter._owner == null || taunter._owner.Faction == attacker)
                {
                    continue;
                }

                float distance = Vector2.Distance(taunter.transform.position, from);
                if (distance <= taunter._species.passiveRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = taunter.transform;
                }
            }

            return best;
        }

        private void RegisterTaunt()
        {
            bool taunts = _species.passive == SpeciesPassiveKind.Taunt;
            if (taunts && !Taunters.Contains(this))
            {
                Taunters.Add(this);
            }
            else if (!taunts)
            {
                Taunters.Remove(this);
            }
        }

        private void OnEnable()
        {
            if (_species != null)
            {
                RegisterTaunt();
            }
        }

        private void OnDisable()
        {
            Taunters.Remove(this);
        }

        private void Update()
        {
            // 도발은 주기 없이 목록에 있는 것만으로 성립한다.
            if (_species == null || _species.passive == SpeciesPassiveKind.Taunt)
            {
                return;
            }

            if (Time.time < _nextTick)
            {
                return;
            }

            _nextTick = Time.time + _species.passiveInterval;
            Tick();
        }

        private void Tick()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _species.passiveRadius);
            int affected = 0;

            foreach (Collider2D hit in hits)
            {
                var target = hit.GetComponent<IDamageable>();
                if (target == null)
                {
                    continue;
                }

                bool friendly = _owner != null && target.Faction == _owner.Faction;

                switch (_species.passive)
                {
                    case SpeciesPassiveKind.AreaHeal:
                        // 기획대로 진영을 안 가린다 — 적까지 같이 낫는다.
                        target.Heal(Mathf.RoundToInt(_species.passiveAmount));
                        affected++;
                        break;

                    case SpeciesPassiveKind.Slow:
                        if (!friendly && hit.GetComponent<ISlowable>() is ISlowable slowable)
                        {
                            slowable.ApplySlow(_species.passiveAmount, _species.passiveInterval * 1.5f);
                            affected++;
                        }

                        break;

                    case SpeciesPassiveKind.Burn:
                        if (!friendly)
                        {
                            target.ApplyDamage(Mathf.Max(1, Mathf.RoundToInt(_species.passiveAmount)), _owner);
                            affected++;
                        }

                        break;
                }
            }

            if (affected > 0)
            {
                if (_ring != null)
                {
                    _ring.Pulse();
                }

                Debug.Log($"species_passive kind={_species.passive} species={_species.speciesId} targets={affected}");
            }
        }
    }
}
