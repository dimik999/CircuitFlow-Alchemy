using UnityEngine;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Features.ResourceCollection;
using CircuitFlowAlchemy.Features.Production;
using CircuitFlowAlchemy.Features.FactoryBuilding;
using CircuitFlowAlchemy.Features.GameState;

namespace CircuitFlowAlchemy.Composition.Installers
{
    /// <summary>
    /// УСТАРЕЛО: Этот класс заменён на ApplicationContext, MenuContext и GameContext
    /// Оставлен для обратной совместимости. Используйте контексты вместо этого класса.
    /// </summary>
    [System.Obsolete("Используйте ApplicationContext, MenuContext или GameContext вместо ProjectInstaller")]
    public class ProjectInstaller : MonoBehaviour
    {
        // BInject будет использоваться для биндинга
        // Пока создаём простую структуру, которая будет расширена при подключении BInject
        
        private static ProjectInstaller _instance;
        private IResourceService _resourceService;
        private IRecipeManager _recipeManager;
        private IFactoryManager _factoryManager;
        private IGameStateService _gameStateService;
        
        public static ProjectInstaller Instance => _instance;
        
        public IResourceService ResourceService => _resourceService;
        public IRecipeManager RecipeManager => _recipeManager;
        public IFactoryManager FactoryManager => _factoryManager;
        public IGameStateService GameStateService => _gameStateService;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeServices();
        }
        
        private void InitializeServices()
        {
            // Инициализация сервисов (позже будет заменено на BInject)
            _resourceService = new ResourceService();
            _recipeManager = new RecipeManager();
            _factoryManager = new FactoryManager();
            _gameStateService = new GameStateService();
        }
        
        // Методы для получения сервисов (временное решение до подключения BInject)
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IResourceService))
                return _resourceService as T;
            if (typeof(T) == typeof(IRecipeManager))
                return _recipeManager as T;
            if (typeof(T) == typeof(IFactoryManager))
                return _factoryManager as T;
            if (typeof(T) == typeof(IGameStateService))
                return _gameStateService as T;
            
            return null;
        }
    }
}
