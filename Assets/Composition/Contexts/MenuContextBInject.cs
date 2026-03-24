using UnityEngine;
using BInject;
using CircuitFlowAlchemy.Composition.Installers;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Контекст для сцены Menu с поддержкой BInject
    /// Использует ApplicationContextBInject как родительский контейнер
    /// </summary>
    public class MenuContextBInject : MonoBehaviour
    {
        private static MenuContextBInject _menuInstance;
        private DiContainer _container;
        
        [SerializeField] private MenuInstaller menuInstaller;
        
        public static MenuContextBInject Instance => _menuInstance;
        public DiContainer Container => _container;
        
        private void Awake()
        {
            if (_menuInstance != null && _menuInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _menuInstance = this;
            
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
                Debug.LogError("MenuContextBInject: ApplicationContextBInject не найден! Убедитесь, что он инициализирован в Bootstrap сцене.");
                return;
            }
            
            // Создаём sub-контейнер, наследующий зависимости от ApplicationContext
            _container = appContext.Container.CreateSubContainer();
            
            // Устанавливаем MenuInstaller
            if (menuInstaller != null)
            {
                menuInstaller.InstallBindings(_container);
            }
            else
            {
                // Если installer не назначен, создаём его программно
                var installer = gameObject.AddComponent<MenuInstaller>();
                installer.InstallBindings(_container);
            }
            
            Debug.Log("MenuContextBInject: Контейнер инициализирован");
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
            if (_menuInstance == this)
            {
                _menuInstance = null;
            }
        }
    }
}
