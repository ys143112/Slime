using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Gameplay
{
    public class FogOfWarReveal : MonoBehaviour
    {
        /// <summary>씬에 하나뿐. 미니맵이 "어디까지 걷어냈나" 를 여기서 읽는다.</summary>
        public static FogOfWarReveal Instance { get; private set; }

        /// <summary>안개를 걷어낸 순간. 인자는 방금 밝힌 중심 칸이다.</summary>
        /// <remarks>
        /// 플레이어가 칸을 옮길 때만 발생한다 — 매 프레임이 아니다. 미니맵이 이걸
        /// 듣고 그 주변만 다시 칠하므로 지도 전체를 훑지 않는다.
        /// </remarks>
        public event Action<Vector3Int> Revealed;

        public int OuterRadius => outerRadius;

        /// <summary>이 칸을 한 번이라도 밝혔는가. 가장 짙은 단계로 남아 있으면 아니다.</summary>
        public bool IsExplored(Vector3Int cell)
        {
            return fogLevels != null && fogLevels.Length > 0 &&
                _bestLevel.TryGetValue(cell, out int level) && level < fogLevels.Length - 1;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [SerializeField] private Tilemap fogTilemap;
        [SerializeField] private Transform target;
        [SerializeField] private int innerRadius = 3;
        [SerializeField] private int outerRadius = 6;
        [SerializeField] private TileBase[] fogLevels;

        private readonly Dictionary<Vector3Int, int> _bestLevel = new Dictionary<Vector3Int, int>();
        private Vector3Int _lastCell = new Vector3Int(int.MinValue, 0, 0);

        private void Update()
        {
            if (fogTilemap == null || target == null || fogLevels == null || fogLevels.Length == 0)
            {
                return;
            }

            Vector3Int cell = fogTilemap.WorldToCell(target.position);
            if (cell == _lastCell)
            {
                return;
            }

            _lastCell = cell;
            UpdateAround(cell);
        }

        private void UpdateAround(Vector3Int center)
        {
            int maxLevel = fogLevels.Length - 1;
            for (int dx = -outerRadius; dx <= outerRadius; dx++)
            {
                for (int dy = -outerRadius; dy <= outerRadius; dy++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > outerRadius)
                    {
                        continue;
                    }

                    var pos = new Vector3Int(center.x + dx, center.y + dy, center.z);
                    if (!fogTilemap.HasTile(pos))
                    {
                        continue;
                    }

                    float t = Mathf.InverseLerp(innerRadius, outerRadius, dist);
                    int level = Mathf.RoundToInt(t * maxLevel);

                    int best = _bestLevel.TryGetValue(pos, out int existing) ? Mathf.Min(existing, level) : level;
                    _bestLevel[pos] = best;
                    fogTilemap.SetTile(pos, fogLevels[best]);
                }
            }

            Revealed?.Invoke(center);
        }
    }
}