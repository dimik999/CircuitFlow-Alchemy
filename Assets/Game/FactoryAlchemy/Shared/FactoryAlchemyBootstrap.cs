using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public class FactoryAlchemyBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<FactoryAlchemyBootstrap>() != null)
            {
                return;
            }

            var root = new GameObject("FactoryAlchemyBootstrap");
            DontDestroyOnLoad(root);
            root.AddComponent<FactoryAlchemyBootstrap>();
        }

        private void Awake()
        {
            SceneManager.activeSceneChanged += OnSceneChanged;
            SetupForScene(SceneManager.GetActiveScene().name);
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            SetupForScene(next.name);
        }

        private void SetupForScene(string sceneName)
        {
            if (SceneFlow.IsMenuScene(sceneName))
            {
                EnsureMenuSceneUI();
                return;
            }

            var cam = EnsureCamera();
            EnsureWorld();
            var player = EnsurePlayer();
            EnsureInput();
            EnsureCameraFollow(cam, player);
        }

        private static void EnsureMenuSceneUI()
        {
            if (FindFirstObjectByType<MainMenuSceneController>() != null)
            {
                return;
            }

            var go = new GameObject("MainMenuSceneController");
            go.AddComponent<MainMenuSceneController>();
        }

        private static Camera EnsureCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            return cam;
        }

        private static void EnsureWorld()
        {
            if (FindFirstObjectByType<WorldGridSystem>() != null)
            {
                return;
            }

            var go = new GameObject("WorldGridSystem");
            go.AddComponent<WorldGridSystem>();
        }

        private static PlayerController2D EnsurePlayer()
        {
            var existing = FindFirstObjectByType<PlayerController2D>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("Player");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteUtil.MakeSolidSprite(new Color(0.95f, 0.95f, 1f));
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            go.transform.position = WorldGridSystem.CellToWorld(new Vector2Int(0, -2), 0f);
            return go.AddComponent<PlayerController2D>();
        }

        private static void EnsureInput()
        {
            if (FindFirstObjectByType<BuildingInputController>() != null)
            {
                return;
            }

            var go = new GameObject("BuildingInputController");
            go.AddComponent<BuildingInputController>();
        }

        private static void EnsureCameraFollow(Camera cam, PlayerController2D player)
        {
            if (cam == null || player == null)
            {
                return;
            }

            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                follow = cam.gameObject.AddComponent<CameraFollow2D>();
            }

            follow.SetTarget(player.transform);
        }
    }
}
