using NUnit.Framework;
using UnityEngine;

namespace Game.Gameplay.Tests
{
    /// <summary>
    /// 2026-08-09 QA 두 건: 다이브 목적지 추첨(초원 고정 해제)과 슬라임 발밑 그림자.
    /// </summary>
    public sealed class DiveAndShadowTests
    {
        private BiomeCatalog _catalog;
        private GameObject _actor;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<BiomeCatalog>();
            var entries = new System.Collections.Generic.List<BiomeEntry>
            {
                new BiomeEntry { biomeId = "biome_default", sceneName = "Biome" },
                new BiomeEntry { biomeId = "biome_marsh", sceneName = "BiomeMarsh" },
                new BiomeEntry { biomeId = "biome_ashfall", sceneName = "BiomeAshfall" },
            };

            // entries 는 [SerializeField] private 이라 밖에서 못 넣는다 —
            // JsonUtility 로 자산을 통째로 덮어 세 바이옴짜리 표를 만든다.
            JsonUtility.FromJsonOverwrite(
                JsonUtility.ToJson(new CatalogSeed { entries = entries }), _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null) Object.DestroyImmediate(_catalog);
            if (_actor != null) Object.DestroyImmediate(_actor);
        }

        [System.Serializable]
        private sealed class CatalogSeed
        {
            public System.Collections.Generic.List<BiomeEntry> entries;
        }

        [Test]
        public void Rotation_CyclesTableOrder()
        {
            // 표 순서대로 한 바퀴, 그리고 다시 처음으로 돌아온다.
            Assert.That(BiomeDiveTrigger.BiomeInRotation(_catalog, 0, "x"), Is.EqualTo("biome_default"));
            Assert.That(BiomeDiveTrigger.BiomeInRotation(_catalog, 1, "x"), Is.EqualTo("biome_marsh"));
            Assert.That(BiomeDiveTrigger.BiomeInRotation(_catalog, 2, "x"), Is.EqualTo("biome_ashfall"));
            Assert.That(BiomeDiveTrigger.BiomeInRotation(_catalog, 3, "x"), Is.EqualTo("biome_default"),
                "한 바퀴 돌면 처음으로 돌아와야 한다 — 안 그러면 순환이 끊긴다.");
        }

        [Test]
        public void Rotation_HandlesNegativeIndex()
        {
            // PlayerPrefs 는 손으로 고칠 수 있다 — 음수도 표 안으로 접혀야 한다.
            Assert.That(BiomeDiveTrigger.BiomeInRotation(_catalog, -1, "x"), Is.EqualTo("biome_ashfall"));
        }

        [Test]
        public void Rotation_WithoutCatalog_FallsBackToDefault()
        {
            Assert.That(BiomeDiveTrigger.BiomeInRotation(null, 0, "biome_default"), Is.EqualTo("biome_default"));
        }

        [Test]
        public void Shadow_AttachesOnceUnderTheBody()
        {
            _actor = new GameObject("Actor", typeof(SpriteRenderer));
            var body = _actor.GetComponent<SpriteRenderer>();
            body.sortingOrder = 3;

            ActorShadow.Attach(_actor.transform, body);
            ActorShadow.Attach(_actor.transform, body);

            Transform shadow = _actor.transform.Find("Shadow");
            Assert.That(shadow, Is.Not.Null, "그림자가 안 붙으면 슬라임이 바닥에서 떠 보인다.");
            Assert.That(_actor.transform.childCount, Is.EqualTo(1),
                "스폰마다 다시 붙으면 그림자가 겹쳐 쌓인다.");
            Assert.That(shadow.GetComponent<SpriteRenderer>().sortingOrder, Is.EqualTo(2),
                "몸보다 한 단계 뒤여야 그림자가 몸 위로 안 올라온다.");
        }
    }
}
