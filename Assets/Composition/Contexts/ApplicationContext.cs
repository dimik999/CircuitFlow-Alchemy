using UnityEngine;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Features.ResourceCollection;
using CircuitFlowAlchemy.Features.Production;
using CircuitFlowAlchemy.Features.GameState;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Базовый контекст приложения - содержит глобальные сервисы, доступные во всех сценах
    /// </summary>
    public class ApplicationContext : MonoBehaviour
    {
        private static ApplicationContext _instance;
        
        // Глобальные сервисы
        protected IResourceService _resourceService;
        protected IRecipeManager _recipeManager;
        protected IGameStateService _gameStateService;
        
        public static ApplicationContext Instance => _instance;
        
        public IResourceService ResourceService => _resourceService;
        public IRecipeManager RecipeManager => _recipeManager;
        public IGameStateService GameStateService => _gameStateService;
        
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeApplicationServices();
        }
        
        /// <summary>
        /// Инициализация глобальных сервисов приложения
        /// </summary>
        protected virtual void InitializeApplicationServices()
        {
            _resourceService = new ResourceService();
            _recipeManager = new RecipeManager();
            _gameStateService = new GameStateService();
            
            Debug.Log("ApplicationContext: Глобальные сервисы инициализированы");
        }
        
        /// <summary>
        /// Получить сервис по типу (для совместимости)
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IResourceService))
                return _resourceService as T;
            if (typeof(T) == typeof(IRecipeManager))
                return _recipeManager as T;
            if (typeof(T) == typeof(IGameStateService))
                return _gameStateService as T;
            
            return null;
        }
        
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
