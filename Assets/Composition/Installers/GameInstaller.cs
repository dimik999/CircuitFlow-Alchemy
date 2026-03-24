using UnityEngine;
using BInject;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Features.FactoryBuilding;
using CircuitFlowAlchemy.Features.Production;

namespace CircuitFlowAlchemy.Composition.Installers
{
    /// <summary>
    /// Установщик зависимостей для GameContext - игровые сервисы
    /// </summary>
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Биндинг игровых сервисов
            Container.Bind<IFactoryManager>()
                .To<FactoryManager>()
                .AsSingle();
            
            // Фабрика для создания машин производства
            Container.Bind<ProductionMachine.IFactory>()
                .To<ProductionMachine.Factory>()
                .AsSingle();
            
            // Здесь можно добавить другие игровые сервисы
            // Например: IIslandService, IWeatherService, IEventService и т.д.
            
            Debug.Log("GameInstaller: Игровые сервисы зарегистрированы");
        }
    }
}
