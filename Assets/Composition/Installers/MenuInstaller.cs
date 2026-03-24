using UnityEngine;
using BInject;

namespace CircuitFlowAlchemy.Composition.Installers
{
    /// <summary>
    /// Установщик зависимостей для MenuContext - специфичные для меню сервисы
    /// </summary>
    public class MenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Здесь можно добавить специфичные для меню сервисы
            // Например: IMenuService, ISaveService, ISettingsService и т.д.
            
            // Пример:
            // Container.Bind<IMenuService>().To<MenuService>().AsSingle();
            
            Debug.Log("MenuInstaller: Сервисы меню зарегистрированы");
        }
    }
}
