using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public class PlayerController2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;

        private void Update()
        {
            if (!GameUiState.IsGameplayActive)
            {
                return;
            }

            var input = ReadMoveInput();
            transform.position += (Vector3)(input * moveSpeed * Time.deltaTime);
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
