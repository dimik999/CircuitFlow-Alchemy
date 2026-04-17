using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Prototype.FactoryAlchemy
{
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        private WorldGridSystem _world;

        private void Start()
        {
            _world = FindFirstObjectByType<WorldGridSystem>();
        }

        private void Update()
        {
            if (!GameUiState.IsGameplayActive)
            {
                return;
            }

            var input = ReadMoveInput();
            if (input.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var pos = transform.position;
            float step = moveSpeed * Time.deltaTime;

            // Axis-separated movement keeps sliding smooth while blocking buildings.
            var targetX = pos + new Vector3(input.x * step, 0f, 0f);
            if (CanStandAt(targetX))
            {
                pos.x = targetX.x;
            }

            var targetY = pos + new Vector3(0f, input.y * step, 0f);
            if (CanStandAt(targetY))
            {
                pos.y = targetY.y;
            }

            transform.position = pos;
        }

        private bool CanStandAt(Vector3 worldPos)
        {
            if (_world == null)
            {
                return true;
            }

            var cell = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
            if (!_world.IsInside(cell))
            {
                return false;
            }

            return !_world.HasBuilding(cell) && !_world.IsResourceNode(cell);
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            float x = 0f;
            float y = 0f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) y -= 1f;
            if (Keyboard.current.wKey.isPressed) y += 1f;
            return new Vector2(x, y).normalized;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#else
            return Vector2.zero;
#endif
        }
    }
}
