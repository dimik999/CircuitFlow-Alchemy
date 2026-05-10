using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    internal sealed class BuildingData
    {
        public BuildingType Type;
        public GameObject View;
        public SpriteRenderer FlowOverlay;
        public int FlowPulseTicks;
        public EssenceType FlowPulseType;
        public Vector2Int Direction;
        public Vector2Int LastIncomingDir;
        public EssenceType BufferType;
        public float BufferAmount;
        public List<EssenceChunk> MixInputs = new List<EssenceChunk>();
    }

    internal readonly struct EssenceChunk
    {
        public readonly EssenceType Type;
        public readonly float Amount;

        public EssenceChunk(EssenceType type, float amount)
        {
            Type = type;
            Amount = amount;
        }
    }

    [Serializable]
    internal sealed class SaveState
    {
        public List<BuildingEntry> Buildings = new List<BuildingEntry>();
        public List<InventoryEntry> Inventory = new List<InventoryEntry>();
        public List<StorageEntry> StorageEntries = new List<StorageEntry>();
        public List<ResourceNodeEntry> ResourceNodes = new List<ResourceNodeEntry>();
        public int Width;
        public int Height;
        public int MapSeed;
        public float ExtractRateMultiplier = 1f;
        public float MixerOutputMultiplier = 1f;
        public int PowerReach = 4;
    }

    [Serializable]
    internal sealed class BuildingEntry
    {
        public int X;
        public int Y;
        public string Type;
        public int DirX;
        public int DirY;
        public string BufferType;
        public float BufferAmount;
    }

    [Serializable]
    internal sealed class InventoryEntry
    {
        public string Key;
        public float Value;
    }

    [Serializable]
    internal sealed class StorageEntry
    {
        public int X;
        public int Y;
        public string Key;
        public float Value;
    }

    [Serializable]
    internal sealed class ResourceNodeEntry
    {
        public int X;
        public int Y;
        public string Type;
    }
}
