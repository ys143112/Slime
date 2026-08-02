using System;
using System.Collections.Generic;

namespace Game.Gameplay
{
    // 기능: spec-001
    public static class EventBus
    {
        private static readonly Dictionary<GameEventId, List<Action<GameEvent>>> Subscribers =
            new Dictionary<GameEventId, List<Action<GameEvent>>>();

        public static void Subscribe(GameEventId id, Action<GameEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (!Subscribers.TryGetValue(id, out List<Action<GameEvent>> handlers))
            {
                handlers = new List<Action<GameEvent>>();
                Subscribers[id] = handlers;
            }

            handlers.Add(handler);
        }

        public static void Unsubscribe(GameEventId id, Action<GameEvent> handler)
        {
            if (Subscribers.TryGetValue(id, out List<Action<GameEvent>> handlers))
            {
                handlers.Remove(handler);
            }
        }

        public static void Publish(GameEvent gameEvent)
        {
            if (!Subscribers.TryGetValue(gameEvent.Id, out List<Action<GameEvent>> handlers))
            {
                return;
            }

            // 구독자가 콜백 안에서 구독을 해지할 수 있어 사본을 순회한다.
            List<Action<GameEvent>> snapshot = new List<Action<GameEvent>>(handlers);
            foreach (Action<GameEvent> handler in snapshot)
            {
                handler.Invoke(gameEvent);
            }
        }
    }
}