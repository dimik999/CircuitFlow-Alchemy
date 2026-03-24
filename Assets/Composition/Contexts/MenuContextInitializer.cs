using UnityEngine;
using CircuitFlowAlchemy.Composition.Contexts;
using CircuitFlowAlchemy.Composition.Installers;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Инициализатор контекста меню с BInject - добавляется на сцену Menu
    /// </summary>
    public class MenuContextInitializer : MonoBehaviour
    {
        [SerializeField] private MenuContextBInject menuContextPrefab;
        
        private void Awake()
        {
            InitializeMenuContext();
        }
        
        private void InitializeMenuContext()
        {
            // Убеждаемся, что ApplicationContextBInject существует
            if (ApplicationContextBInject.Instance == null)
            {
                Debug.LogError("MenuContextInitializer: ApplicationContextBInject не найден! Убедитесь, что GameBootstrap инициализирован.");
                return;
            }
            
            // Создать MenuContextBInject если его нет
            if (MenuContextBInject.Instance == null)
            {
                if (menuContextPrefab != null)
                {
                    Instantiate(menuContextPrefab);
                }
                else
                {
                    var contextGO = new GameObject("MenuContextBInject");
                    var context = contextGO.AddComponent<MenuContextBInject>();
                    
                    // Добавляем MenuInstaller если его нет
                    if (contextGO.GetComponent<MenuInstaller>() == null)
                    {
                        contextGO.AddComponent<MenuInstaller>();
                    }
                }
            }
            
            Debug.Log("MenuContextInitializer: MenuContextBInject инициализирован");
        }
    }
}
