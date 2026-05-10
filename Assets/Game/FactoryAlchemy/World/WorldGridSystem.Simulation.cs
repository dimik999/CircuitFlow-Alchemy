using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        private void MarkFlowActivity(BuildingData data, EssenceType type, float movedAmount)
        {
            if (data == null || movedAmount <= 0f || !IsTransportCarrier(data.Type))
            {
                return;
            }

            data.FlowPulseType = type;
            data.FlowPulseTicks = 2;
        }

        private void SimulateExtractionAndFlow()
        {
            RebuildPowerNetwork();

            foreach (var pair in _buildings)
            {
                if (pair.Value.FlowPulseTicks > 0)
                {
                    pair.Value.FlowPulseTicks--;
                }
            }

            foreach (var pair in _buildings)
            {
                if (pair.Value.Type != BuildingType.Extractor)
                {
                    continue;
                }

                if (!IsBuildingPowered(pair.Key, pair.Value))
                {
                    continue;
                }

                if (_resourceNodes.TryGetValue(pair.Key, out var nodeType))
                {
                    pair.Value.BufferType = nodeType;
                    pair.Value.BufferAmount += _extractRateMultiplier;
                }
            }

            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Mixer)
                {
                    if (!IsBuildingPowered(pair.Key, pair.Value))
                    {
                        continue;
                    }

                    TryMix(pair.Value);
                }
            }

            var cells = new List<Vector2Int>(_buildings.Keys);
            foreach (var cell in cells)
            {
                if (!_buildings.TryGetValue(cell, out var source))
                {
                    continue;
                }

                if ((source.Type == BuildingType.Extractor
                     || source.Type == BuildingType.Pipe
                     || source.Type == BuildingType.PipeCorner
                     || source.Type == BuildingType.Storage
                     || source.Type == BuildingType.Mixer
                     || source.Type == BuildingType.PipeConnector
                     || source.Type == BuildingType.PipeSplitter)
                    && (source.BufferAmount > 0f || (source.Type == BuildingType.Storage && HasStorageContent(cell))))
                {
                    TryTransferForward(cell, source);
                }
            }

            UpdateTransportFlowVisuals();
        }
        private void TryMix(BuildingData mixer)
        {
            if (mixer.MixInputs.Count == 0)
            {
                return;
            }

            float aqua = 0f;
            float ignis = 0f;
            for (int i = mixer.MixInputs.Count - 1; i >= 0; i--)
            {
                var chunk = mixer.MixInputs[i];
                if (chunk.Type == EssenceType.Aqua) aqua += chunk.Amount;
                if (chunk.Type == EssenceType.Ignis) ignis += chunk.Amount;
            }

            if (aqua < 1f || ignis < 1f || mixer.BufferAmount > 4f)
            {
                return;
            }

            ConsumeMixInput(mixer, EssenceType.Aqua, 1f);
            ConsumeMixInput(mixer, EssenceType.Ignis, 1f);
            mixer.BufferType = EssenceType.Aeris;
            mixer.BufferAmount += _mixerOutputMultiplier;
        }

        private static void ConsumeMixInput(BuildingData mixer, EssenceType type, float amount)
        {
            float remain = amount;
            for (int i = 0; i < mixer.MixInputs.Count && remain > 0f; i++)
            {
                if (mixer.MixInputs[i].Type != type)
                {
                    continue;
                }

                float used = Mathf.Min(remain, mixer.MixInputs[i].Amount);
                mixer.MixInputs[i] = new EssenceChunk(type, mixer.MixInputs[i].Amount - used);
                remain -= used;
            }

            mixer.MixInputs.RemoveAll(x => x.Amount <= 0.001f);
        }

        private void TryTransferForward(Vector2Int sourcePos, BuildingData source)
        {
            if (source.Type == BuildingType.PipeSplitter)
            {
                TryTransferFromSplitter(sourcePos, source);
                return;
            }

            if (source.Type == BuildingType.Pipe)
            {
                if (TryTransferFromPipeAuto(sourcePos, source))
                {
                    return;
                }
            }

            if (source.Type == BuildingType.PipeCorner)
            {
                if (TryTransferFromPipeCorner(sourcePos, source))
                {
                    return;
                }
            }

            if (source.Type == BuildingType.Storage)
            {
                if (TryTransferFromStorage(sourcePos, source))
                {
                    return;
                }
            }

            var targetPos = sourcePos + source.Direction;
            if (!_buildings.TryGetValue(targetPos, out var target))
            {
                return;
            }

            if (!CanAccept(targetPos, target, source.Direction, source.BufferType))
            {
                return;
            }

            float moved = Mathf.Min(1f, source.BufferAmount);
            source.BufferAmount -= moved;
            MarkFlowActivity(source, source.BufferType, moved);

            if (target.Type == BuildingType.Storage)
            {
                AddToStorage(targetPos, source.BufferType.ToString(), moved);
                return;
            }

            if (target.Type == BuildingType.Mixer)
            {
                target.MixInputs.Add(new EssenceChunk(source.BufferType, moved));
                return;
            }

            target.BufferType = source.BufferType;
            target.BufferAmount += moved;
            MarkFlowActivity(target, target.BufferType, moved);
            if (target.Type == BuildingType.PipeCorner)
            {
                target.LastIncomingDir = -source.Direction;
            }
        }

        private bool HasStorageContent(Vector2Int storagePos)
        {
            if (!_storageInventories.TryGetValue(storagePos, out var storage))
            {
                return false;
            }

            foreach (var kv in storage)
            {
                if (kv.Value > 0.001f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryPopStorageResource(Vector2Int storagePos, float amount, out EssenceType type, out float moved)
        {
            type = EssenceType.Aqua;
            moved = 0f;
            if (!_storageInventories.TryGetValue(storagePos, out var storage))
            {
                return false;
            }

            string bestKey = null;
            float bestAmount = 0f;
            foreach (var kv in storage)
            {
                if (kv.Value > bestAmount)
                {
                    bestKey = kv.Key;
                    bestAmount = kv.Value;
                }
            }

            if (string.IsNullOrEmpty(bestKey) || bestAmount <= 0.001f)
            {
                return false;
            }

            if (!Enum.TryParse(bestKey, out type))
            {
                return false;
            }

            moved = Mathf.Min(amount, bestAmount);
            float left = bestAmount - moved;
            if (left <= 0.001f) storage.Remove(bestKey);
            else storage[bestKey] = left;
            return true;
        }

        private bool TryTransferFromStorage(Vector2Int sourcePos, BuildingData source)
        {
            Vector2Int outDir = source.Direction;
            Vector2Int targetPos = sourcePos + outDir;
            if (!_buildings.TryGetValue(targetPos, out var target))
            {
                return false;
            }

            if (!TryPopStorageResource(sourcePos, 1f, out var resourceType, out float moved) || moved <= 0f)
            {
                return false;
            }

            if (!CanAccept(targetPos, target, outDir, resourceType))
            {
                AddToStorage(sourcePos, resourceType.ToString(), moved);
                return false;
            }

            if (target.Type == BuildingType.Storage)
            {
                AddToStorage(targetPos, resourceType.ToString(), moved);
                return true;
            }

            if (target.Type == BuildingType.Mixer)
            {
                target.MixInputs.Add(new EssenceChunk(resourceType, moved));
                return true;
            }

            target.BufferType = resourceType;
            target.BufferAmount += moved;
            MarkFlowActivity(target, target.BufferType, moved);
            if (target.Type == BuildingType.PipeCorner)
            {
                target.LastIncomingDir = -outDir;
            }
            return true;
        }

        private bool TryTransferFromPipeAuto(Vector2Int sourcePos, BuildingData source)
        {
            int mask = BuildPipeConnectionMask(sourcePos, source);
            if (mask == 0)
            {
                return false;
            }

            var orderedDirs = new List<Vector2Int>(4);
            void addDir(Vector2Int d)
            {
                int bit = DirToMaskBit(d);
                if ((mask & bit) == 0 || orderedDirs.Contains(d))
                {
                    return;
                }
                orderedDirs.Add(d);
            }

            // Prefer current visual direction first, then all other connected sides.
            addDir(source.Direction);
            addDir(Vector2Int.up);
            addDir(Vector2Int.right);
            addDir(Vector2Int.down);
            addDir(Vector2Int.left);

            for (int i = 0; i < orderedDirs.Count; i++)
            {
                var dir = orderedDirs[i];
                var targetPos = sourcePos + dir;
                if (!_buildings.TryGetValue(targetPos, out var target))
                {
                    continue;
                }

                if (!CanAccept(targetPos, target, dir, source.BufferType))
                {
                    continue;
                }

                float moved = Mathf.Min(1f, source.BufferAmount);
                source.BufferAmount -= moved;
                MarkFlowActivity(source, source.BufferType, moved);

                if (target.Type == BuildingType.Storage)
                {
                    AddToStorage(targetPos, source.BufferType.ToString(), moved);
                    return true;
                }

                if (target.Type == BuildingType.Mixer)
                {
                    target.MixInputs.Add(new EssenceChunk(source.BufferType, moved));
                    return true;
                }

                target.BufferType = source.BufferType;
                target.BufferAmount += moved;
                MarkFlowActivity(target, target.BufferType, moved);
                if (target.Type == BuildingType.PipeCorner)
                {
                    target.LastIncomingDir = -dir;
                }
                return true;
            }

            return false;
        }

        private bool TryTransferFromPipeCorner(Vector2Int sourcePos, BuildingData source)
        {
            Vector2Int a = source.Direction == Vector2Int.zero ? Vector2Int.right : source.Direction;
            Vector2Int b = new Vector2Int(a.y, -a.x);
            Vector2Int[] dirs;
            if (source.LastIncomingDir == a)
            {
                dirs = new[] { b };
            }
            else if (source.LastIncomingDir == b)
            {
                dirs = new[] { a };
            }
            else
            {
                dirs = new[] { a, b };
            }

            for (int i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                var targetPos = sourcePos + dir;
                if (!_buildings.TryGetValue(targetPos, out var target))
                {
                    continue;
                }

                if (!CanAccept(targetPos, target, dir, source.BufferType))
                {
                    continue;
                }

                float moved = Mathf.Min(1f, source.BufferAmount);
                source.BufferAmount -= moved;
                MarkFlowActivity(source, source.BufferType, moved);

                if (target.Type == BuildingType.Storage)
                {
                    AddToStorage(targetPos, source.BufferType.ToString(), moved);
                    return true;
                }

                if (target.Type == BuildingType.Mixer)
                {
                    target.MixInputs.Add(new EssenceChunk(source.BufferType, moved));
                    return true;
                }

                target.BufferType = source.BufferType;
                target.BufferAmount += moved;
                MarkFlowActivity(target, target.BufferType, moved);
                if (target.Type == BuildingType.PipeCorner)
                {
                    target.LastIncomingDir = -dir;
                }
                return true;
            }

            return false;
        }

        private void TryTransferFromSplitter(Vector2Int sourcePos, BuildingData source)
        {
            var outDirs = new[]
            {
                source.Direction,
                new Vector2Int(-source.Direction.y, source.Direction.x),
                new Vector2Int(source.Direction.y, -source.Direction.x)
            };

            int available = 0;
            for (int i = 0; i < outDirs.Length; i++)
            {
                var p = sourcePos + outDirs[i];
                if (_buildings.TryGetValue(p, out var t) && CanAccept(p, t, outDirs[i], source.BufferType))
                {
                    available++;
                }
            }

            if (available == 0)
            {
                return;
            }

            float totalMoved = Mathf.Min(1f, source.BufferAmount);
            float part = totalMoved / available;
            float moved = 0f;

            for (int i = 0; i < outDirs.Length; i++)
            {
                var p = sourcePos + outDirs[i];
                if (!_buildings.TryGetValue(p, out var t) || !CanAccept(p, t, outDirs[i], source.BufferType))
                {
                    continue;
                }

                if (t.Type == BuildingType.Storage)
                {
                    AddToStorage(p, source.BufferType.ToString(), part);
                }
                else if (t.Type == BuildingType.Mixer)
                {
                    t.MixInputs.Add(new EssenceChunk(source.BufferType, part));
                }
                else
                {
                    t.BufferType = source.BufferType;
                    t.BufferAmount += part;
                    MarkFlowActivity(t, t.BufferType, part);
                    if (t.Type == BuildingType.PipeCorner)
                    {
                        t.LastIncomingDir = -outDirs[i];
                    }
                }

                moved += part;
            }

            source.BufferAmount = Mathf.Max(0f, source.BufferAmount - moved);
            MarkFlowActivity(source, source.BufferType, moved);
        }

        private bool CanAccept(Vector2Int targetPos, BuildingData target, Vector2Int incomingDir, EssenceType incomingType)
        {
            // Storage: input on -Direction, output on +Direction (e.g. left in / right out when Direction is east).
            // incomingDir is the flow vector source→target (same as a pipe/extractor output into this cell).
            // That equals +Direction only when the sender sits on the input face (target - Direction).
            if (target.Type == BuildingType.Storage) return incomingDir == target.Direction;

            if (target.Type == BuildingType.Pipe)
            {
                int mask = BuildPipeConnectionMask(targetPos, target);
                // incomingDir is source->target, while target port is opposite-facing.
                return (mask & DirToMaskBit(-incomingDir)) != 0;
            }

            if (target.Type == BuildingType.PipeCorner)
            {
                return PipeCornerHasPort(target, -incomingDir);
            }

            if (target.Type == BuildingType.PipeConnector)
            {
                // Connector merges any side except its output side.
                return incomingDir != -target.Direction;
            }

            if (target.Type == BuildingType.PipeSplitter)
            {
                // Splitter accepts only from back side and emits to 3 directions.
                return target.Direction == incomingDir;
            }

            if (target.Type == BuildingType.Mixer)
            {
                // Mixer receives from side/back, not from output side.
                if (target.Direction == -incomingDir)
                {
                    return false;
                }

                return incomingType == EssenceType.Aqua || incomingType == EssenceType.Ignis;
            }

            if (target.Type == BuildingType.MarketTerminal) return incomingDir == target.Direction;

            return false;
        }

        private static int DirToMaskBit(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return 1;
            if (dir == Vector2Int.right) return 2;
            if (dir == Vector2Int.down) return 4;
            return 8;
        }

        private static IEnumerable<Vector2Int> Neighbors(Vector2Int c)
        {
            yield return c + Vector2Int.up;
            yield return c + Vector2Int.down;
            yield return c + Vector2Int.left;
            yield return c + Vector2Int.right;
        }

        private void AddToInventory(string key, float amount)
        {
            if (_inventory.ContainsKey(key))
            {
                _inventory[key] += amount;
            }
            else
            {
                _inventory[key] = amount;
            }
        }

        private void AddToStorage(Vector2Int storagePos, string key, float amount)
        {
            if (amount <= 0f || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_storageInventories.TryGetValue(storagePos, out var storage))
            {
                storage = new Dictionary<string, float>();
                _storageInventories[storagePos] = storage;
            }

            if (storage.ContainsKey(key))
            {
                storage[key] += amount;
            }
            else
            {
                storage[key] = amount;
            }
        }
    }
}
