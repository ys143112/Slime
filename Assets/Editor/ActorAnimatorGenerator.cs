using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 플레이어·슬라임용 AnimatorController 를 만든다.
    ///
    /// 클립이 아직 없어도 상태·파라미터·전이가 전부 깔린 컨트롤러를 미리
    /// 만들어 두는 것이 목적이다. 사용자는 각 블렌드트리의 Motion 칸에
    /// 스프라이트 애니메이션 클립만 끼우면 된다.
    ///
    /// 손으로 만들지 않고 스크립트로 두는 이유: 8방향 × 3상태 = 24칸을 손으로
    /// 깔면 좌표를 하나 잘못 넣어도 티가 안 나고, 나중에 캐릭터가 늘 때마다
    /// 같은 실수를 반복한다.
    /// </summary>
    public static class ActorAnimatorGenerator
    {
        private const string OutputFolder = "Assets/Animations";

        // 8방향의 이름과 좌표를 한 곳에서 정한다. 둘이 어긋나면 사용자가 클립을
        // 엉뚱한 칸에 끼우게 된다.
        private static readonly (string Name, Vector2 Position)[] Directions =
        {
            ("S", new Vector2(0f, -1f)),
            ("SW", new Vector2(-0.7071f, -0.7071f)),
            ("W", new Vector2(-1f, 0f)),
            ("NW", new Vector2(-0.7071f, 0.7071f)),
            ("N", new Vector2(0f, 1f)),
            ("NE", new Vector2(0.7071f, 0.7071f)),
            ("E", new Vector2(1f, 0f)),
            ("SE", new Vector2(0.7071f, -0.7071f)),
        };

        [MenuItem("SlimeRanch/Generate Actor Animators")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            Build(Path.Combine(OutputFolder, "PlayerAnimator.controller"));
            Build(Path.Combine(OutputFolder, "SlimeAnimator.controller"));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("actor_animators_generated: Motion 칸은 비어 있습니다. 클립을 끼우세요.");
        }

        private static void Build(string path)
        {
            path = path.Replace('\\', '/');

            // 이미 있으면 건드리지 않는다 — 사용자가 끼운 클립을 지우지 않기
            // 위해서다. 다시 만들려면 파일을 먼저 지운다.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                Debug.Log($"actor_animator_skipped: 이미 있습니다 — {path}");
                return;
            }

            var controller = new AnimatorController { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(controller, path);

            // AddLayer 가 상태머신을 자산에 함께 넣는다. 여기서 또
            // AddObjectToAsset 하면 "already an asset" 으로 죽는다.
            controller.AddLayer("Base Layer");

            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine root = controller.layers[0].stateMachine;

            // Idle 도 8방향이다 — 어느 쪽을 보고 멈췄는지가 남아야 이동에서
            // 정지로 넘어갈 때 홱 돌지 않는다.
            AnimatorState idle = AddDirectionalState(controller, root, "Idle", new Vector3(280f, 0f, 0f));
            AnimatorState move = AddDirectionalState(controller, root, "Move", new Vector3(280f, 140f, 0f));
            AnimatorState attack = AddDirectionalState(controller, root, "Attack", new Vector3(280f, 280f, 0f));

            // 피격·사망은 방향이 없다. 한 장이면 된다.
            AnimatorState hit = root.AddState("Hit", new Vector3(600f, 140f, 0f));
            AnimatorState dead = root.AddState("Dead", new Vector3(600f, 280f, 0f));

            root.defaultState = idle;

            Connect(idle, move, "Speed", AnimatorConditionMode.Greater, 0.01f);
            Connect(move, idle, "Speed", AnimatorConditionMode.Less, 0.01f);

            Connect(idle, attack, "Attack", AnimatorConditionMode.If, 0f);
            Connect(move, attack, "Attack", AnimatorConditionMode.If, 0f);

            // 공격은 클립이 끝나야 돌아온다 — 즉시 빠지면 한 프레임만 보인다.
            AnimatorStateTransition attackDone = attack.AddTransition(idle);
            attackDone.hasExitTime = true;
            attackDone.exitTime = 0.9f;
            attackDone.duration = 0.05f;

            // 피격은 Any State 에서 들어간다. 이동 중이든 공격 중이든 맞을 수 있다.
            AnimatorStateTransition anyToHit = root.AddAnyStateTransition(hit);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0f;
            anyToHit.canTransitionToSelf = false;

            AnimatorStateTransition hitDone = hit.AddTransition(idle);
            hitDone.hasExitTime = true;
            hitDone.exitTime = 0.9f;
            hitDone.duration = 0.05f;

            // 사망은 Bool 이다. 트리거로 두면 한 번 지나가고 되살아난 것처럼 보인다.
            AnimatorStateTransition anyToDead = root.AddAnyStateTransition(dead);
            anyToDead.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            anyToDead.hasExitTime = false;
            anyToDead.duration = 0f;
            anyToDead.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            Debug.Log($"actor_animator_built: {path} params={controller.parameters.Length} states={root.states.Length}");
        }

        private static void Connect(
            AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.AddCondition(mode, threshold, parameter);
            transition.hasExitTime = false;
            transition.duration = 0.05f;
        }

        private static AnimatorState AddDirectionalState(
            AnimatorController controller, AnimatorStateMachine root, string name, Vector3 position)
        {
            var tree = new BlendTree
            {
                name = name + "Tree",
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY",
                useAutomaticThresholds = false,
            };

            // motion 을 null 로 넣으면 빈 칸만 생긴다 — 사용자가 여기에 클립을 끼운다.
            foreach ((string _, Vector2 dirPosition) in Directions)
            {
                tree.AddChild(null, dirPosition);
            }

            AssetDatabase.AddObjectToAsset(tree, controller);

            AnimatorState state = root.AddState(name, position);
            state.motion = tree;
            return state;
        }
    }
}
