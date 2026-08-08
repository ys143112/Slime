namespace Game.Gameplay
{
    // 기능: spec-001
    public enum GameEventId
    {
        RunStarted,
        RunEnded,
        SlimeCaptured,
        SlimeCaptureFailed,
        CorruptionTierChanged,

        // 주말 작업(스테이지·동행)에서 쓸 값을 미리 잡아 둔다 — enum 한 줄
        // 추가마다 두 브랜치가 같은 자리에서 충돌하는 걸 피하려는 것이다.
        // 아직 발행처가 없는 값도 있다.
        StageGenerated,
        StageDepthChanged,
        SlimeSpawned,
        CompanionAssigned,
        CompanionDismissed,
        CompanionDied,
    }

    // 기능: spec-001
    public readonly struct GameEvent
    {
        public GameEventId Id { get; }
        public string BiomeId { get; }
        public object Payload { get; }

        public GameEvent(GameEventId id, string biomeId, object payload = null)
        {
            Id = id;
            BiomeId = biomeId;
            Payload = payload;
        }
    }
}