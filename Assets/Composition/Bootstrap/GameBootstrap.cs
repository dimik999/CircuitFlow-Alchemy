using UnityEngine;
using CircuitFlowAlchemy.Composition.Contexts;
using CircuitFlowAlchemy.Composition.Installers;

namespace CircuitFlowAlchemy.Composition.Bootstrap
{
    /// <summary>
    /// Точка входа в игру - инициализирует ApplicationContextBInject
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private ApplicationContextBInject applicationContextPrefab;
        
        private void Awake()
        {
            InitializeApplication();
        }
        
        private void InitializeApplication()
        {
            // Создать ApplicationContextBInject если его нет
            if (ApplicationContextBInject.Instance == null)
            {
                if (applicationContextPrefab != null)
                {
                    Instantiate(applicationContextPrefab);
                }
                else
                {
                    var contextGO = new GameObject("ApplicationContextBInject");
                    var context = contextGO.AddComponent<ApplicationContextBInject>();
                    
                    // Добавляем ApplicationInstaller если его нет
                    if (context.GetComponent<ApplicationInstaller>() == null)
                    {
                        contextGO.AddComponent<ApplicationInstaller>();
                    }
                }
            }
            
            Debug.Log("Game Bootstrap: ApplicationContextBInject инициализирован");
        }
    }
}
