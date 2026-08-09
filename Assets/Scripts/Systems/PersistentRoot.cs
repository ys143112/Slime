using UnityEngine;

namespace Game.Gameplay
{
    // 기능: spec-003 - 씬 전환에도 살아남아야 하는 오브젝트(교배 UI 등)에 붙인다.
    public sealed class PersistentRoot : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
