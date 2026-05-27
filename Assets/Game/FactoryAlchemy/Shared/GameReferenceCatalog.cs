using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public enum ReferenceTab
    {
        Essences,
        Mixer,
        Buildings,
        Logistics,
        Power
    }

    public readonly struct ResourceAmount
    {
        public readonly string Resource;
        public readonly float Amount;

        public ResourceAmount(string resource, float amount)
        {
            Resource = resource;
            Amount = amount;
        }
    }

    public readonly struct EssenceReference
    {
        public readonly EssenceType Type;
        public readonly string Title;
        public readonly string Description;
        public readonly string HowToObtain;
        public readonly bool InCurrentBuild;

        public EssenceReference(EssenceType type, string title, string description, string howToObtain, bool inCurrentBuild)
        {
            Type = type;
            Title = title;
            Description = description;
            HowToObtain = howToObtain;
            InCurrentBuild = inCurrentBuild;
        }
    }

    public readonly struct MixerRecipeReference
    {
        public readonly string Name;
        public readonly ResourceAmount[] Inputs;
        public readonly string Output;
        public readonly float OutputAmount;
        public readonly string Conditions;
        public readonly string Ports;

        public MixerRecipeReference(
            string name,
            ResourceAmount[] inputs,
            string output,
            float outputAmount,
            string conditions,
            string ports)
        {
            Name = name;
            Inputs = inputs;
            Output = output;
            OutputAmount = outputAmount;
            Conditions = conditions;
            Ports = ports;
        }
    }

    public readonly struct BuildingReference
    {
        public readonly BuildingType Type;
        public readonly string Title;
        public readonly string Role;
        public readonly string UnlockHint;
        public readonly ResourceAmount[] CraftCost;

        public BuildingReference(
            BuildingType type,
            string title,
            string role,
            string unlockHint,
            ResourceAmount[] craftCost)
        {
            Type = type;
            Title = title;
            Role = role;
            UnlockHint = unlockHint;
            CraftCost = craftCost;
        }
    }

    public readonly struct ReferenceNote
    {
        public readonly string Title;
        public readonly string[] Lines;

        public ReferenceNote(string title, string[] lines)
        {
            Title = title;
            Lines = lines;
        }
    }

    /// <summary>Справочник рецептов и механик для UI (H).</summary>
    public static class GameReferenceCatalog
    {
        public static string GetTabTitle(ReferenceTab tab)
        {
            switch (tab)
            {
                case ReferenceTab.Essences: return "Эссенции";
                case ReferenceTab.Mixer: return "Смеситель";
                case ReferenceTab.Buildings: return "Постройки";
                case ReferenceTab.Logistics: return "Логистика";
                case ReferenceTab.Power: return "Энергия";
                default: return tab.ToString();
            }
        }

        public static string GetEssenceTitle(EssenceType type)
        {
            switch (type)
            {
                case EssenceType.Ignis: return "Ignis (огонь)";
                case EssenceType.Aqua: return "Aqua (вода)";
                case EssenceType.Terra: return "Terra (земля)";
                case EssenceType.Aeris: return "Aeris (воздух / пар)";
                case EssenceType.Vitus: return "Vitus (жизнь)";
                case EssenceType.Fulgar: return "Fulgar (молния)";
                default: return type.ToString();
            }
        }

        public static IReadOnlyList<EssenceReference> Essences { get; } = new[]
        {
            new EssenceReference(EssenceType.Aqua, "Aqua — вода",
                "Базовая жидкая эссенция для труб и смешения.",
                "Узлы на карте (синие), экстрактор, ручной сбор (E) в радиусе.",
                true),
            new EssenceReference(EssenceType.Ignis, "Ignis — огонь",
                "Тепловая эссенция; нужна для смесителя и крафта.",
                "Узлы на карте (красные), экстрактор, ручной сбор (E).",
                true),
            new EssenceReference(EssenceType.Terra, "Terra — земля",
                "Строительный ресурс инвентаря (не течёт по трубам как эссенция узла).",
                "Узлы на карте (коричневые), экстрактор, цели акта 1.",
                true),
            new EssenceReference(EssenceType.Aeris, "Aeris — воздух / пар",
                "Продукт смесителя; используется в заказах гильдии.",
                "Смеситель: Aqua + Ignis (см. вкладку «Смеситель»).",
                true),
            new EssenceReference(EssenceType.Vitus, "Vitus — жизнь",
                "Запланирована для дерева технологий 2-го акта.",
                "Пока недоступна в прототипе.",
                false),
            new EssenceReference(EssenceType.Fulgar, "Fulgar — молния",
                "Запланирована для цепочек энергии и редких рецептов.",
                "Пока недоступна в прототипе.",
                false),
        };

        public static IReadOnlyList<MixerRecipeReference> MixerRecipes { get; } = new[]
        {
            new MixerRecipeReference(
                "Пар (базовый рецепт)",
                new[]
                {
                    new ResourceAmount("Aqua", 1f),
                    new ResourceAmount("Ignis", 1f),
                },
                "Aeris",
                1f,
                "Нужно питание; буфер выхода ≤ 4; улучшение «Смеситель» на рынке умножает выход.",
                "Вход: стороны и тыл (не лицевая сторона). Выход: по направлению постройки (R)."),
        };

        public static IReadOnlyList<BuildingReference> Buildings { get; } = new[]
        {
            new BuildingReference(BuildingType.Pipe, "Труба", "Прямой транспорт между клетками.", "С начала", BuildCostTable(BuildingType.Pipe)),
            new BuildingReference(BuildingType.PipeCorner, "Угол трубы", "Поворот потока на 90°.", "С начала", BuildCostTable(BuildingType.PipeCorner)),
            new BuildingReference(BuildingType.PipeConnector, "Соединитель", "Узел с несколькими портами.", "С начала", BuildCostTable(BuildingType.PipeConnector)),
            new BuildingReference(BuildingType.PipeSplitter, "Разделитель", "1 вход, до 3 выходов.", "С начала", BuildCostTable(BuildingType.PipeSplitter)),
            new BuildingReference(BuildingType.Extractor, "Экстрактор", "Добыча с ресурсного узла клетки.", "С начала", BuildCostTable(BuildingType.Extractor)),
            new BuildingReference(BuildingType.Storage, "Хранилище", "Буфер с ручным забором (ПКМ).", "С начала", BuildCostTable(BuildingType.Storage)),
            new BuildingReference(BuildingType.Mixer, "Смеситель", "Aqua + Ignis → Aeris.", "Заказ гильдии №1", BuildCostTable(BuildingType.Mixer)),
            new BuildingReference(BuildingType.Generator, "Генератор", "Источник энергии для сети.", "Заказ гильдии №2 (энергия)", BuildCostTable(BuildingType.Generator)),
            new BuildingReference(BuildingType.PowerPole, "Столб", "Передаёт энергию в радиусе.", "Заказ гильдии №2", BuildCostTable(BuildingType.PowerPole)),
            new BuildingReference(BuildingType.MarketTerminal, "Рынок", "Заказы, улучшения, торговля.", "Заказ гильдии №2", BuildCostTable(BuildingType.MarketTerminal)),
        };

        public static IReadOnlyList<ReferenceNote> LogisticsNotes { get; } = new[]
        {
            new ReferenceNote("Трубы",
                new[]
                {
                    "Труба — только прямой участок; направление задаётся при установке.",
                    "Угол (PipeCorner) соединяет два перпендикулярных направления.",
                    "Соединитель и разделитель — узлы с несколькими портами.",
                    "Поток идёт от источника (экстрактор, хранилище, смеситель) по направлению стрелки.",
                }),
            new ReferenceNote("Экстрактор и хранилище",
                new[]
                {
                    "Экстрактор ставится только на цветной узел карты.",
                    "Выход — в сторону, куда смотрит стрелка (клавиша R).",
                    "Хранилище: вход с тыла, выход вперёд; ПКМ — окно ручного забора.",
                }),
            new ReferenceNote("Смеситель",
                new[]
                {
                    "Принимает Aqua и Ignis с боков и тыла.",
                    "Выход Aeris — только в сторону стрелки; не подключайте трубу на выход сзади.",
                    "Без питания (генератор/столб в радиусе) производство останавливается.",
                }),
        };

        public static IReadOnlyList<ReferenceNote> PowerNotes { get; } = new[]
        {
            new ReferenceNote("Источники",
                new[]
                {
                    "Генератор — постоянный источник в своей клетке.",
                    "Столб передаёт энергию другим столбам и постройкам в радиусе (Манхэттен).",
                    "Базовый радиус: 4 клетки; улучшение «Энергосеть» на рынке — до 8.",
                }),
            new ReferenceNote("Потребители",
                new[]
                {
                    "Экстрактор и смеситель требуют питания (серый спрайт = нет питания).",
                    "Трубы, хранилище и рынок питание не потребляют.",
                }),
        };

        public static List<(string resource, float amount)> GetBuildCosts(BuildingType type)
        {
            var costs = BuildCostTable(type);
            var list = new List<(string resource, float amount)>(costs.Length);
            for (int i = 0; i < costs.Length; i++)
            {
                list.Add((costs[i].Resource, costs[i].Amount));
            }

            return list;
        }

        private static ResourceAmount[] BuildCostTable(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Pipe:
                    return new[] { new ResourceAmount("Terra", 1f) };
                case BuildingType.PipeCorner:
                    return new[] { new ResourceAmount("Terra", 1f), new ResourceAmount("Aqua", 1f) };
                case BuildingType.PipeConnector:
                    return new[] { new ResourceAmount("Terra", 2f), new ResourceAmount("Ignis", 1f) };
                case BuildingType.PipeSplitter:
                    return new[] { new ResourceAmount("Terra", 2f), new ResourceAmount("Aqua", 1f) };
                case BuildingType.Extractor:
                    return new[] { new ResourceAmount("Terra", 3f), new ResourceAmount("Ignis", 1f) };
                case BuildingType.Storage:
                    return new[] { new ResourceAmount("Terra", 2f), new ResourceAmount("Aqua", 1f) };
                case BuildingType.Mixer:
                    return new[]
                    {
                        new ResourceAmount("Terra", 3f),
                        new ResourceAmount("Aqua", 2f),
                        new ResourceAmount("Ignis", 1f),
                    };
                case BuildingType.Generator:
                    return new[] { new ResourceAmount("Terra", 4f), new ResourceAmount("Ignis", 2f) };
                case BuildingType.PowerPole:
                    return new[] { new ResourceAmount("Terra", 2f) };
                case BuildingType.MarketTerminal:
                    return new[]
                    {
                        new ResourceAmount("Terra", 4f),
                        new ResourceAmount("Aqua", 2f),
                        new ResourceAmount("Ignis", 2f),
                    };
                default:
                    return System.Array.Empty<ResourceAmount>();
            }
        }

        public static string FormatCostList(ResourceAmount[] costs)
        {
            if (costs == null || costs.Length == 0)
            {
                return "без затрат";
            }

            var parts = new List<string>(costs.Length);
            for (int i = 0; i < costs.Length; i++)
            {
                parts.Add($"{costs[i].Resource} ×{costs[i].Amount:0.##}");
            }

            return string.Join(", ", parts);
        }

    }
}
