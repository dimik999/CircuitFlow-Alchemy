using UnityEngine;
using CircuitFlowAlchemy.Composition.Contexts;

namespace CircuitFlowAlchemy.Composition.Contexts
{
    /// <summary>
    /// Контекст для сцены Menu - наследуется от ApplicationContext
    /// Содержит специфичные для меню сервисы и логику
    /// </summary>
    public class MenuContext : ApplicationContext
    {
        private static MenuContext _menuInstance;
        
        public new static MenuContext Instance => _menuInstance;
        
        protected override void Awake()
        {
            // Сначала вызываем базовый Awake для инициализации ApplicationContext
            base.Awake();
            
            if (_menuInstance != null && _menuInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _menuInstance = this;
            
            InitializeMenuServices();
        }
        
        /// <summary>
        /// Инициализация сервисов специфичных для меню
        /// </summary>
        private void InitializeMenuServices()
        {
            // Здесь можно добавить специфичные для меню сервисы
            // Например: IMenuService, ISaveService, ISettingsService и т.д.
            
            Debug.Log("MenuContext: Сервисы меню инициализированы");
        }
        
        protected override void OnDestroy()
        {
            if (_menuInstance == this)
            {
                _menuInstance = null;
            }
            
            base.OnDestroy();
        }
    }
}
