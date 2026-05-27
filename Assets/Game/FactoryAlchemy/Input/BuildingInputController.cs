using UnityEngine;
using System.Collections.Generic;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class BuildingInputController : MonoBehaviour
    {
        private const float HandGatherRadiusCells = 2.2f;
        private enum HudTab
        {
            Build,
            Orders,
            Upgrades
        }

        private enum InventorySortMode
        {
            NameAsc,
            CountDesc,
            Type
        }

        private enum UiScreen
        {
            MainMenu,
            SaveMenu,
            InGame
        }

        private const string SaveKeyWorld = "CFA_FACTORIO_LITE_WORLD_V1";
        private const string SaveKeyGoal = "CFA_FACTORIO_LITE_GOAL_V1";
        private const string SaveKeyGuildOrder = "CFA_FACTORIO_LITE_GUILD_ORDER_V1";
        private const string SaveKeyCoins = "CFA_FACTORIO_LITE_COINS_V1";
        private const string SaveKeyUpgExtractor = "CFA_FACTORIO_LITE_UPG_EXTRACTOR_V1";
        private const string SaveKeyUpgMixer = "CFA_FACTORIO_LITE_UPG_MIXER_V1";
        private const string SaveKeyUpgPower = "CFA_FACTORIO_LITE_UPG_POWER_V1";
        private const string SaveKeyCrafted = "CFA_FACTORIO_LITE_CRAFTED_V1";
        private const string SaveKeyCraftQueue = "CFA_FACTORIO_LITE_CRAFT_QUEUE_V1";
        private const string SaveKeyHotbar = "CFA_FACTORIO_LITE_HOTBAR_V1";
        private const string SaveKeyName = "CFA_FACTORIO_LITE_SAVE_NAME_V1";
        private const string SaveKeyPlayerX = "CFA_FACTORIO_LITE_PLAYER_X_V1";
        private const string SaveKeyPlayerY = "CFA_FACTORIO_LITE_PLAYER_Y_V1";
        private const string SaveKeyActOneFinale = "CFA_ACT1_FINALE_V1";
        private const int HotbarSize = 8;
        private const int SaveSlotCount = 3;

        private WorldGridSystem _world;
        private UiScreen _screen = UiScreen.InGame;
        private BuildingType _selected = BuildingType.None;
        private Vector2Int _direction = Vector2Int.right;
        private string _hint;
        private int _currentGoalIndex;
        private Goal[] _goals;
        private GuildOrder[] _guildOrders;
        private int _currentGuildOrderIndex;
        private bool _actOneFinaleComplete;
        private int _coins;
        private int _upgExtractor;
        private int _upgMixer;
        private int _upgPower;
        private GameObject _preview;
        private SpriteRenderer _previewRenderer;
        private SpriteRenderer _previewArrowRenderer;
        private bool _showSettings;
        private bool _isPauseMenuOpen;
        private bool _showPauseLoadSlots;
        private bool _showPauseSaveSlots;
        private string _pendingSaveName = string.Empty;
        private int _activeSlot = 1;
        private HudTab _activeTab = HudTab.Build;
        private float _pauseAnim;
        private float _settingsAnim;
        private string _hoverTooltip;
        private readonly Dictionary<BuildingType, Texture2D> _buildIcons = new Dictionary<BuildingType, Texture2D>();
        private readonly Dictionary<string, Texture2D> _resourceIcons = new Dictionary<string, Texture2D>();
        private readonly Dictionary<BuildingType, int> _craftedItems = new Dictionary<BuildingType, int>();
        private readonly List<BuildingType> _craftQueue = new List<BuildingType>();
        private readonly BuildingType?[] _hotbarSlots = new BuildingType?[HotbarSize];
        private readonly Rect[] _hotbarSlotRects = new Rect[HotbarSize];
        private GUIStyle _slotLabelStyle;
        private AudioSource _uiAudioSource;
        private AudioClip _uiHoverClip;
        private AudioClip _uiClickClip;
        private string _lastHoveredControlId;
        private bool _showCraftWindow;
        private bool _showInventoryWindow;
        private bool _showReferenceWindow;
        private ReferenceTab _referenceTab = ReferenceTab.Essences;
        private bool _showBuildingWindow;
        private Vector2Int _openedBuildingCell;
        private BuildingType _openedBuildingType = BuildingType.None;
        private Vector2 _storageWindowScroll;
        private BuildingType? _activeCraftType;
        private float _activeCraftProgress;
        private bool _isCraftQueuePaused;
        private int _selectedCraftQueueIndex = -1;
        private BuildingType? _draggingHotbarType;
        private InventorySortMode _inventorySort = InventorySortMode.Type;
        private float _inventoryScroll;
        private int _tutorialStep;
        private float _tutorialElapsed;
        private bool _tutorialVisible = true;

    }
}
