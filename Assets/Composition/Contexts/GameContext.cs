using UnityEngine;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Composition.Contexts;
using CircuitFlowAlchemy.Features.FactoryBuilding;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Контекст для сцены Game - наследуется от ApplicationContext
    /// Содержит специфичные для игрового процесса сервисы
    /// </summary>
    public class GameContext : ApplicationContext
    {
        private static GameContext _gameInstance;
        
        // Игровые сервисы
        private IFactoryManager _factoryManager;
        
        public new static GameContext Instance => _gameInstance;
        
        public IFactoryManager FactoryManager => _factoryManager;
        
        protected override void Awake()
        {
            // Сначала вызываем базовый Awake для инициализации ApplicationContext
            base.Awake();
            
            if (_gameInstance != null && _gameInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _gameInstance = this;
            
            InitializeGameServices();
        }
        
        /// <summary>
        /// Инициализация сервисов специфичных для игрового процесса
        /// </summary>
        private void InitializeGameServices()
        {
            _factoryManager = new FactoryManager();
            
            // Здесь можно добавить другие игровые сервисы
            // Например: IIslandService, IWeatherService, IEventService и т.д.
            
            Debug.Log("GameContext: Игровые сервисы инициализированы");
        }
        
        /// <summary>
        /// Переопределяем GetService для добавления игровых сервисов
        /// </summary>
        public new T GetService<T>() where T : class
        {
            // Сначала проверяем базовые сервисы
            var baseService = base.GetService<T>();
            if (baseService != null)
                return baseService;
            
            // Затем проверяем игровые сервисы
            if (typeof(T) == typeof(IFactoryManager))
                return _factoryManager as T;
            
            return null;
        }
        
        protected override void OnDestroy()
        {
            if (_gameInstance == this)
            {
                _gameInstance = null;
            }
            
            base.OnDestroy();
        }
    }
}
