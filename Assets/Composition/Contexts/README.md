# Система контекстов

Проект использует трёхуровневую систему контекстов для управления зависимостями и жизненным циклом сервисов.

## Структура контекстов

### ApplicationContext (Базовый контекст)
- **Расположение**: Инициализируется в сцене Bootstrap через `GameBootstrap`
- **Жизненный цикл**: `DontDestroyOnLoad` - существует на протяжении всего времени работы приложения
- **Сервисы**:
  - `IResourceService` - управление ресурсами (глобально)
  - `IRecipeManager` - управление рецептами (глобально)
  - `IGameStateService` - управление состоянием игры (глобально)

### MenuContext (Контекст меню)
- **Расположение**: Инициализируется в сцене Menu через `MenuContextInitializer`
- **Наследование**: Наследуется от `ApplicationContext`
- **Жизненный цикл**: Существует только в сцене Menu
- **Сервисы**: 
  - Наследует все сервисы из `ApplicationContext`
  - Может содержать специфичные для меню сервисы (в будущем)

### GameContext (Игровой контекст)
- **Расположение**: Инициализируется в сцене Game через `GameContextInitializer`
- **Наследование**: Наследуется от `ApplicationContext`
- **Жизненный цикл**: Существует только в сцене Game
- **Сервисы**:
  - Наследует все сервисы из `ApplicationContext`
  - `IFactoryManager` - управление фабрикой (только в игре)
  - Может содержать другие игровые сервисы (IIslandService, IWeatherService и т.д.)

## Использование

### В сцене Bootstrap
```csharp
// GameBootstrap автоматически создаёт ApplicationContext
// ApplicationContext будет доступен во всех последующих сценах
```

### В сцене Menu
```csharp
// Добавьте MenuContextInitializer на сцену
// Он автоматически создаст MenuContext при загрузке сцены

// Доступ к сервисам:
var resourceService = ApplicationContext.Instance.ResourceService;
var menuContext = MenuContext.Instance;
```

### В сцене Game
```csharp
// Добавьте GameContextInitializer на сцену
// Он автоматически создаст GameContext при загрузке сцены

// Доступ к сервисам:
var resourceService = ApplicationContext.Instance.ResourceService; // Глобальный
var factoryManager = GameContext.Instance.FactoryManager; // Только в игре
```

## Порядок инициализации

1. **Bootstrap сцена** → `GameBootstrap` создаёт `ApplicationContext`
2. **Menu сцена** → `MenuContextInitializer` создаёт `MenuContext` (требует `ApplicationContext`)
3. **Game сцена** → `GameContextInitializer` создаёт `GameContext` (требует `ApplicationContext`)

## Интеграция с BInject

После подключения BInject, контексты будут заменены на:
- `ProjectContext` (аналог ApplicationContext)
- `SceneContext` для каждой сцены (аналог MenuContext/GameContext)

Текущая реализация является подготовкой к интеграции с BInject и может быть легко заменена.
