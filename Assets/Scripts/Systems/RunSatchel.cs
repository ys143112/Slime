using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-011
    // 런 중 포획한 슬라임은 여기 담긴다. 추출하면 보유 목록으로 넘어가고,
    // 죽으면 통째로 사라진다 — 이것이 없으면 죽어도 잃는 게 없어 생존이 손해다.
    public static class RunSatchel
    {
        private static readonly List<SlimeInstance> Held = new List<SlimeInstance>();

        public static int Count => Held.Count;

        // 목록 UI 가 읽을 자리. 몇 마리인지만으로는 무엇을 잃게 되는지 알 수 없다.
        public static IReadOnlyList<SlimeInstance> Contents => Held;

        public static void Add(SlimeInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            Held.Add(instance);
        }

        // 추출 지점 도달 — 담아온 전부를 보유 목록에 확정한다.
        public static void Commit()
        {
            int committed = Held.Count;
            if (PlayerRoster.Instance == null)
            {
                Debug.LogError("RunSatchel: PlayerRoster 인스턴스가 없어 전리품을 확정할 수 없습니다.");
                return;
            }

            foreach (SlimeInstance instance in Held)
            {
                PlayerRoster.Instance.Add(instance);
            }

            Held.Clear();
            Debug.Log($"run_payout committed={committed}");
            RunLogWriter.AppendLine($"RunPayout committed={committed}");
        }

        // 사망 — 담아온 것을 전부 몰수한다. 보유 목록은 건드리지 않는다.
        public static void Discard()
        {
            int forfeited = Held.Count;
            Held.Clear();
            Debug.Log($"run_forfeit forfeited={forfeited}");
            RunLogWriter.AppendLine($"RunForfeit forfeited={forfeited}");
        }
    }
}
