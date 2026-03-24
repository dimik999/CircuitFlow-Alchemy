# Настройка BInject для проекта CircuitFlow Alchemy

## Шаг 1: Установка BInject

BInject можно установить несколькими способами:

### Вариант 1: Через Package Manager (Git URL)
1. Откройте Unity Package Manager (Window > Package Manager)
2. Нажмите "+" > "Add package from git URL"
3. Введите URL репозитория BInject (например: `https://github.com/your-org/BInject.git?path=/Assets/BInject`)

### Вариант 2: Через Git Submodule
```bash
git submodule add https://github.com/your-org/BInject.git Assets/BInject
```

### Вариант 3: Через UPM (если доступен)
1. Откройте `Packages/manifest.json`
2. Добавьте зависимость:
```json
{
  "dependencies": {
    "com.your-org.binject": "https://github.com/your-org/BInject.git?path=/Assets/BInject"
  }
}
```

## Шаг 2: Обновление ProjectInstaller

После установки BInject, обновите `ProjectInstaller.cs`:

```csharp
using BInject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Биндинг сервисов как синглтонов
        Container.Bind<IResourceService>().To<ResourceService>().AsSingle();
        Container.Bind<IRecipeManager>().To<RecipeManager>().AsSingle();
        Container.Bind<IFactoryManager>().To<FactoryManager>().AsSingle();
        Container.Bind<IGameStateService>().To<GameStateService>().AsSingle();
        
        // Биндинг фабрик
        Container.BindFactory<ProductionMachine, ProductionMachine.Factory>();
    }
}
```

## Шаг 3: Создание SceneInstaller

Создайте `SceneInstaller.cs` для каждой сцены:

```csharp
using BInject;

public class FactorySceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Специфичные для сцены биндинги
        Container.Bind<IFactoryLayout>().To<FactoryLayout>().AsSingle();
    }
}
```

## Шаг 4: Использование в коде

После настройки BInject, используйте инъекцию зависимостей:

```csharp
public class SomeComponent : MonoBehaviour
{
    [Inject]
    private IResourceService _resourceService;
    
    [Inject]
    private IRecipeManager _recipeManager;
    
    private void Start()
    {
        // Зависимости уже инъектированы
        _resourceService.AddEssence(new Essence(EssenceType.Ignis, 10f));
    }
}
```

## Примечание

Текущая реализация `ProjectInstaller` использует простую систему без BInject для начальной работы. После установки BInject замените текущую реализацию на версию с BInject.
