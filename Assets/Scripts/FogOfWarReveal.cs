using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Gameplay
{
    public class FogOfWarReveal : MonoBehaviour
    {
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
        }
    }
}