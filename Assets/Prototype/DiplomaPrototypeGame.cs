using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Interfaces;
using CircuitFlowAlchemy.Core.Models;
using CircuitFlowAlchemy.Features.Production;
using CircuitFlowAlchemy.Features.ResourceCollection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Prototype
{
    /// <summary>
    /// Emergency playable MVP for diploma demo:
    /// gather essences, craft mixes, complete first orders.
    /// </summary>
    public class DiplomaPrototypeGame : MonoBehaviour
    {
        private IResourceService _resources;
        private IRecipeManager _recipes;

        private readonly Dictionary<EssenceType, float> _manualGatherAmount = new Dictionary<EssenceType, float>
        {
            { EssenceType.Aqua, 1f },
            { EssenceType.Ignis, 1f },
            { EssenceType.Terra, 1f }
        };

        private readonly List<OrderGoal> _orders = new List<OrderGoal>();
        private int _currentOrderIndex;
        private string _status = "Соберите первые эссенции";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<FactorioLite.FactorioLiteBootstrap>() != null)
            {
                return;
            }

            if (FindFirstObjectByType<DiplomaPrototypeGame>() != null)
            {
                return;
            }

            var go = new GameObject("DiplomaPrototypeGame");
            DontDestroyOnLoad(go);
            go.AddComponent<DiplomaPrototypeGame>();
        }

        private void Awake()
        {
            _resources = new ResourceService();
            _recipes = new RecipeManager();
            BuildActOneOrders();
        }

        private void Update()
        {
            if (IsDigit1Pressed())
            {
                Gather(EssenceType.Aqua);
            }

            if (IsDigit2Pressed())
            {
                Gather(EssenceType.Ignis);
            }

            if (IsDigit3Pressed())
            {
                Gather(EssenceType.Terra);
            }

            if (IsQPressed())
            {
                CraftRecipe("vapor");
            }

            if (IsWPressed())
            {
                CraftRecipe("magma");
            }

            if (IsSpacePressed())
            {
                TryCompleteCurrentOrder();
            }
        }

        private static bool IsDigit1Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha1);
#else
            return false;
#endif
        }

        private static bool IsDigit2Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha2);
#else
            return false;
#endif
        }

        private static bool IsDigit3Pressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha3);
#else
            return false;
#endif
        }

        private static bool IsQPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Q);
#else
            return false;
#endif
        }

        private static bool IsWPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.W);
#else
            return false;
#endif
        }

        private static bool IsSpacePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private void Gather(EssenceType type)
        {
            _resources.AddEssence(new Essence(type, _manualGatherAmount[type]));
            _status = $"Добыто: {type} +{_manualGatherAmount[type]}";
        }

        private void CraftRecipe(string recipeId)
        {
            var result = _recipes.ExecuteRecipe(recipeId, _resources);
            if (result == null)
            {
                _status = $"Недостаточно ресурсов для рецепта: {recipeId}";
                return;
            }

            _resources.AddEssence(result);
            _status = $"Создано: {result.Type} x{result.Amount:0.##}";
        }

        private void BuildActOneOrders()
        {
            // These goals map to Act 1 from the diploma concept, simplified for fast demo.
            _orders.Add(new OrderGoal("1-1 Первые шаги", new Essence(EssenceType.Aqua, 5f), new Essence(EssenceType.Ignis, 5f)));
            _orders.Add(new OrderGoal("1-2 Искра жизни", new Essence(EssenceType.Aeris, 3f)));
            _orders.Add(new OrderGoal("1-3 Кристалл туманности (упрощено)", new Essence(EssenceType.Aeris, 2f)));
            _orders.Add(new OrderGoal("1-4 Стабильное пламя (упрощено)", new Essence(EssenceType.Ignis, 4f), new Essence(EssenceType.Aeris, 2f)));
        }

        private void TryCompleteCurrentOrder()
        {
            if (_currentOrderIndex >= _orders.Count)
            {
                _status = "Все demo-заказы выполнены";
                return;
            }

            var order = _orders[_currentOrderIndex];
            if (!order.CanComplete(_resources))
            {
                _status = $"Не хватает ресурсов для заказа: {order.Title}";
                return;
            }

            order.Consume(_resources);
            _currentOrderIndex++;
            _status = _currentOrderIndex >= _orders.Count
                ? "Поздравляю! Act 1 demo выполнен."
                : $"Заказ выполнен: {order.Title}";
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 460, 360), "CircuitFlow Alchemy - Diploma MVP");

            GUI.Label(new Rect(20, 40, 430, 20), "Добыча: [1] Aqua  [2] Ignis  [3] Terra");
            GUI.Label(new Rect(20, 60, 430, 20), "Крафт: [Q] Vapor(Ignis+Aqua)  [W] Magma(Ignis+Terra)");
            GUI.Label(new Rect(20, 80, 430, 20), "Заказ: [Space] Сдать текущий");

            GUI.Label(new Rect(20, 115, 430, 20), "Ресурсы:");
            var y = 140f;
            foreach (EssenceType type in System.Enum.GetValues(typeof(EssenceType)))
            {
                var amount = _resources.GetEssenceAmount(type);
                GUI.Label(new Rect(30, y, 300, 20), $"- {type}: {amount:0.##}");
                y += 20f;
            }

            var orderText = _currentOrderIndex < _orders.Count
                ? _orders[_currentOrderIndex].ToDisplayText()
                : "Все demo-заказы выполнены";
            GUI.Label(new Rect(20, 275, 430, 40), $"Текущий заказ: {orderText}");
            GUI.Label(new Rect(20, 320, 430, 20), $"Статус: {_status}");
        }

        private sealed class OrderGoal
        {
            public string Title { get; }
            private readonly List<Essence> _requirements = new List<Essence>();

            public OrderGoal(string title, params Essence[] requirements)
            {
                Title = title;
                _requirements.AddRange(requirements);
            }

            public bool CanComplete(IResourceService resources)
            {
                foreach (var req in _requirements)
                {
                    if (!resources.HasEnoughEssence(req.Type, req.Amount))
                    {
                        return false;
                    }
                }

                return true;
            }

            public void Consume(IResourceService resources)
            {
                foreach (var req in _requirements)
                {
                    resources.ConsumeEssence(req.Type, req.Amount);
                }
            }

            public string ToDisplayText()
            {
                var chunks = new List<string>();
                foreach (var req in _requirements)
                {
                    chunks.Add($"{req.Type} x{req.Amount:0.##}");
                }

                return $"{Title}: {string.Join(", ", chunks)}";
            }
        }
    }
}
