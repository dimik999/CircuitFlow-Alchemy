using UnityEngine;
using BInject;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Composition.Installers;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Контекст для сцены Game с поддержкой BInject
    /// Использует ApplicationContextBInject как родительский контейнер
    /// </summary>
    public class GameContextBInject : MonoBehaviour
    {
        private static GameContextBInject _gameInstance;
        private DiContainer _container;
        
        [SerializeField] private GameInstaller gameInstaller;
        
        public static GameContextBInject Instance => _gameInstance;
        public DiContainer Container => _container;
        
        // Публичные свойства для обратной совместимости
        public IFactoryManager FactoryManager => _container.Resolve<IFactoryManager>();
        
        private void Awake()
        {
            if (_gameInstance != null && _gameInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _gameInstance = this;
            
            InitializeContainer();
        }
        
        /// <summary>
        /// Инициализация контейнера с использованием ApplicationContext как родителя
        /// </summary>
        private void InitializeContainer()
        {
            // Получаем ApplicationContext
            var appContext = ApplicationContextBInject.Instance;
            if (appContext == null)
            {
                Debug.LogError("GameContextBInject: ApplicationContextBInject не найден! Убедитесь, что он инициализирован в Bootstrap сцене.");
                return;
            }
            
            // Создаём sub-контейнер, наследующий зависимости от ApplicationContext
            _container = appContext.Container.CreateSubContainer();
            
            // Устанавливаем GameInstaller
            if (gameInstaller != null)
            {
                gameInstaller.InstallBindings(_container);
            }
            else
            {
                // Если installer не назначен, создаём его программно
                var installer = gameObject.AddComponent<GameInstaller>();
                installer.InstallBindings(_container);
            }
            
            Debug.Log("GameContextBInject: Контейнер инициализирован");
        }
        
        /// <summary>
        /// Получить сервис из контейнера
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _container.Resolve<T>();
        }
        
        private void OnDestroy()
        {
            if (_gameInstance == this)
            {
                _gameInstance = null;
            }
        }
    }
}
