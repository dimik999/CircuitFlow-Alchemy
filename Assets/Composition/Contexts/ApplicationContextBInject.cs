using UnityEngine;
using BInject;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Composition.Installers;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Базовый контекст приложения с поддержкой BInject
    /// Содержит глобальные сервисы, доступные во всех сценах
    /// </summary>
    public class ApplicationContextBInject : MonoBehaviour
    {
        private static ApplicationContextBInject _instance;
        private DiContainer _container;
        
        [SerializeField] private ApplicationInstaller applicationInstaller;
        
        public static ApplicationContextBInject Instance => _instance;
        public DiContainer Container => _container;
        
        // Публичные свойства для обратной совместимости
        public IResourceService ResourceService => _container.Resolve<IResourceService>();
        public IRecipeManager RecipeManager => _container.Resolve<IRecipeManager>();
        public IGameStateService GameStateService => _container.Resolve<IGameStateService>();
        
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeContainer();
        }
        
        /// <summary>
        /// Инициализация BInject контейнера
        /// </summary>
        private void InitializeContainer()
        {
            _container = new DiContainer();
            
            // Установить ApplicationInstaller
            if (applicationInstaller != null)
            {
                applicationInstaller.InstallBindings(_container);
            }
            else
            {
                // Если installer не назначен, создаём его программно
                var installer = gameObject.AddComponent<ApplicationInstaller>();
                installer.InstallBindings(_container);
            }
            
            Debug.Log("ApplicationContextBInject: Контейнер инициализирован");
        }
        
        /// <summary>
        /// Получить сервис из контейнера
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _container.Resolve<T>();
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
