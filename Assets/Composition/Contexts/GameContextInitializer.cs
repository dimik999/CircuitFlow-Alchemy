using UnityEngine;
using CircuitFlowAlchemy.Composition.Contexts;
using CircuitFlowAlchemy.Composition.Installers;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Инициализатор игрового контекста с BInject - добавляется на сцену Game
    /// </summary>
    public class GameContextInitializer : MonoBehaviour
    {
        [SerializeField] private GameContextBInject gameContextPrefab;
        
        private void Awake()
        {
            InitializeGameContext();
        }
        
        private void InitializeGameContext()
        {
            // Убеждаемся, что ApplicationContextBInject существует
            if (ApplicationContextBInject.Instance == null)
            {
                Debug.LogError("GameContextInitializer: ApplicationContextBInject не найден! Убедитесь, что GameBootstrap инициализирован.");
                return;
            }
            
            // Создать GameContextBInject если его нет
            if (GameContextBInject.Instance == null)
            {
                if (gameContextPrefab != null)
                {
                    Instantiate(gameContextPrefab);
                }
                else
                {
                    var contextGO = new GameObject("GameContextBInject");
                    var context = contextGO.AddComponent<GameContextBInject>();
                    
                    // Добавляем GameInstaller если его нет
                    if (contextGO.GetComponent<GameInstaller>() == null)
                    {
                        contextGO.AddComponent<GameInstaller>();
                    }
                }
            }
            
            Debug.Log("GameContextInitializer: GameContextBInject инициализирован");
        }
    }
}
