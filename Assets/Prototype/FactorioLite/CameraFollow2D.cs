using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Prototype.FactorioLite
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private float smooth = 8f;
        [SerializeField] private float minZoom = 6f;
        [SerializeField] private float maxZoom = 16f;
        [SerializeField] private float zoomSpeed = 2f;

        private Transform _target;
        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target != null)
            {
                Vector3 desired = new Vector3(_target.position.x, _target.position.y, -10f);
                transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
            }

            float wheel = ReadMouseWheel();
            if (_cam != null && Mathf.Abs(wheel) > 0.001f)
            {
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - wheel * zoomSpeed, minZoom, maxZoom);
            }
        }

        private static float ReadMouseWheel()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y * 0.01f : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#else
            return 0f;
#endif
        }
    }
}
