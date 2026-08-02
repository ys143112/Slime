namespace Game.Gameplay
{
    // 기능: spec-001
    public enum GameEventId
    {
        RunStarted,
        RunEnded,
        SlimeCaptured,
        CorruptionTierChanged,
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