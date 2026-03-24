# Использование BInject в проекте CircuitFlow Alchemy

## Обзор

Проект использует BInject для управления зависимостями через систему контекстов:
- **ApplicationContextBInject** - глобальный контекст (Bootstrap сцена)
- **MenuContextBInject** - контекст меню (Menu сцена)
- **GameContextBInject** - игровой контекст (Game сцена)

## Структура Installers

### ApplicationInstaller
Регистрирует глобальные сервисы, доступные во всех сценах:
- `IResourceService` → `ResourceService`
- `IRecipeManager` → `RecipeManager`
- `IGameStateService` → `GameStateService`

### MenuInstaller
Регистрирует сервисы специфичные для меню (пока пустой, можно расширить).

### GameInstaller
Регистрирует игровые сервисы:
- `IFactoryManager` → `FactoryManager`
- `ProductionMachine.IFactory` → `ProductionMachine.Factory`

## Использование в коде

### Получение сервисов через контекст

```csharp
// В любом месте кода, где доступен ApplicationContextBInject
var resourceService = ApplicationContextBInject.Instance.ResourceService;
var recipeManager = ApplicationContextBInject.Instance.RecipeManager;

// В игровой сцене
var factoryManager = GameContextBInject.Instance.FactoryManager;
```

### Инъекция зависимостей через [Inject]

```csharp
using BInject;
using CircuitFlowAlchemy.Core.Interfaces;

public class SomeComponent : MonoBehaviour
{
    [Inject]
    private IResourceService _resourceService;
    
    [Inject]
    private IRecipeManager _recipeManager;
    
    private void Start()
    {
        // Зависимости уже инъектированы BInject
        _resourceService.AddEssence(new Essence(EssenceType.Ignis, 10f));
    }
}
```

### Использование фабрик

```csharp
using BInject;
using CircuitFlowAlchemy.Features.Production;

public class MachineSpawner : MonoBehaviour
{
    [Inject]
    private ProductionMachine.IFactory _machineFactory;
    
    private void CreateMachine()
    {
        var machine = _machineFactory.Create("machine_001");
        // Использовать машину...
    }
}
```

### Получение сервисов напрямую из контейнера

```csharp
// В MenuContext или GameContext
var service = MenuContextBInject.Instance.GetService<IResourceService>();

// Или напрямую из контейнера
var container = MenuContextBInject.Instance.Container;
var service = container.Resolve<IResourceService>();
```

## Порядок инициализации

1. **Bootstrap сцена** загружается первой
   - `GameBootstrap` создаёт `ApplicationContextBInject`
   - `ApplicationInstaller` регистрирует глобальные сервисы

2. **Menu сцена** загружается после Bootstrap
   - `MenuContextInitializer` создаёт `MenuContextBInject`
   - `MenuContextBInject` создаёт sub-контейнер от `ApplicationContextBInject`
   - `MenuInstaller` регистрирует сервисы меню
   - Все сервисы из ApplicationContext доступны в MenuContext

3. **Game сцена** загружается после Bootstrap
   - `GameContextInitializer` создаёт `GameContextBInject`
   - `GameContextBInject` создаёт sub-контейнер от `ApplicationContextBInject`
   - `GameInstaller` регистрирует игровые сервисы
   - Все сервисы из ApplicationContext доступны в GameContext

## Добавление новых сервисов

### Глобальный сервис (ApplicationContext)

1. Создайте интерфейс в `Core/Interfaces/`
2. Создайте реализацию в соответствующей папке `Features/`
3. Добавьте биндинг в `ApplicationInstaller`:

```csharp
Container.Bind<IMyService>()
    .To<MyService>()
    .AsSingle();
```

### Сервис для меню (MenuContext)

1. Создайте интерфейс и реализацию
2. Добавьте биндинг в `MenuInstaller`

### Сервис для игры (GameContext)

1. Создайте интерфейс и реализацию
2. Добавьте биндинг в `GameInstaller`

## Создание фабрик

Для создания объектов через фабрику:

1. Создайте интерфейс фабрики:

```csharp
public class MyObject
{
    public interface IFactory
    {
        MyObject Create(string id);
    }
    
    public class Factory : IFactory
    {
        public MyObject Create(string id)
        {
            return new MyObject(id);
        }
    }
}
```

2. Зарегистрируйте фабрику в Installer:

```csharp
Container.Bind<MyObject.IFactory>()
    .To<MyObject.Factory>()
    .AsSingle();
```

3. Используйте через инъекцию:

```csharp
[Inject]
private MyObject.IFactory _factory;

var obj = _factory.Create("id_001");
```

## Примечания

- Все глобальные сервисы должны быть зарегистрированы в `ApplicationInstaller`
- Сервисы специфичные для сцены регистрируются в соответствующих Installers
- Sub-контейнеры автоматически наследуют зависимости от родительского контейнера
- Используйте `[Inject]` для автоматической инъекции зависимостей в MonoBehaviour компоненты
