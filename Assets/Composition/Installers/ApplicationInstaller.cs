using UnityEngine;
using BInject;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Features.ResourceCollection;
using CircuitFlowAlchemy.Features.Production;
using CircuitFlowAlchemy.Features.GameState;

namespace CircuitFlowAlchemy.Composition.Installers
{
    /// <summary>
    /// Установщик зависимостей для ApplicationContext - глобальные сервисы
    /// </summary>
    public class ApplicationInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Биндинг глобальных сервисов как синглтонов
            Container.Bind<IResourceService>()
                .To<ResourceService>()
                .AsSingle();
            
            Container.Bind<IRecipeManager>()
                .To<RecipeManager>()
                .AsSingle();
            
            Container.Bind<IGameStateService>()
                .To<GameStateService>()
                .AsSingle();
            
            Debug.Log("ApplicationInstaller: Глобальные сервисы зарегистрированы");
        }
    }
}
