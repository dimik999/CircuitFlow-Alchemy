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
        private sealed class Goal
        {
            public string Title { get; }
            public string ResourceKey { get; }
            public float RequiredAmount { get; }
            public int CoinReward { get; }

            public Goal(string title, string resourceKey, float requiredAmount, int coinReward = 0)
            {
                Title = title;
                ResourceKey = resourceKey;
                RequiredAmount = requiredAmount;
                CoinReward = coinReward;
            }
        }

        private sealed class GuildOrder
        {
            public string Title { get; }
            public string ResourceKey { get; }
            public float RequiredAmount { get; }
            public int RewardCoins { get; }

            public GuildOrder(string title, string resourceKey, float requiredAmount, int rewardCoins)
            {
                Title = title;
                ResourceKey = resourceKey;
                RequiredAmount = requiredAmount;
                RewardCoins = rewardCoins;
            }
        }

        private struct InventoryEntry
        {
            public bool IsBuilding;
            public BuildingType BuildingType;
            public string DisplayName;
            public float Count;
            public string CountText;
            public Texture2D Icon;
            public int Group;
        }

        private readonly struct ResourceCost
        {
            public readonly string Key;
            public readonly float Amount;

            public ResourceCost(string key, float amount)
            {
                Key = key;
                Amount = amount;
            }
        }
    }
}
