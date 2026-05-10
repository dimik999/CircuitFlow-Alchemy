using System;
using System.Collections.Generic;
using CircuitFlowAlchemy.Core.Models;
using UnityEngine;

namespace CircuitFlowAlchemy.Game.FactoryAlchemy
{
    public partial class WorldGridSystem : MonoBehaviour
    {
        private void RefreshAllPipeVisuals()
        {
            foreach (var pair in _buildings)
            {
                if (pair.Value.Type == BuildingType.Pipe || pair.Value.Type == BuildingType.PipeCorner)
                {
                    RefreshPipeVisualAt(pair.Key);
                }
            }
        }

        private void AutoOrientPipesAfterLoad()
        {
            foreach (var pair in _buildings)
            {
                if (pair.Value.Type != BuildingType.Pipe)
                {
                    continue;
                }

                int mask = BuildPipeConnectionMask(pair.Key, pair.Value);
                if (mask == 0)
                {
                    continue;
                }

                int dirMask = DirToMaskBit(pair.Value.Direction);
                if ((mask & dirMask) != 0)
                {
                    continue;
                }

                if ((mask & 2) != 0) pair.Value.Direction = Vector2Int.right;
                else if ((mask & 1) != 0) pair.Value.Direction = Vector2Int.up;
                else if ((mask & 8) != 0) pair.Value.Direction = Vector2Int.left;
                else pair.Value.Direction = Vector2Int.down;
            }
        }

        private void RefreshPipeVisualsAround(Vector2Int center)
        {
            RefreshPipeVisualAt(center);
            foreach (var n in Neighbors(center))
            {
                RefreshPipeVisualAt(n);
            }
        }

        private void RefreshPipeVisualAt(Vector2Int cell)
        {
            if (!_buildings.TryGetValue(cell, out var data) || data.View == null)
            {
                return;
            }

            if (data.Type == BuildingType.PipeCorner)
            {
                var srCorner = data.View.GetComponent<SpriteRenderer>();
                if (srCorner != null)
                {
                    srCorner.sprite = BuildingSpriteFactory.GetWorldSprite(BuildingType.PipeCorner);
                }
                UpdateRotation(data);
                return;
            }

            if (data.Type != BuildingType.Pipe)
            {
                return;
            }

            int mask = BuildPipeConnectionMask(cell, data);

            var sr = data.View.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (BuildingSpriteFactory.TryResolvePipeSpriteAndAngle(mask, out var customSprite, out float angle))
                {
                    sr.sprite = customSprite;
                    data.View.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                else
                {
                    sr.sprite = BuildingSpriteFactory.GetPipeWorldSprite(mask);
                    data.View.transform.rotation = Quaternion.identity;
                }
            }
        }

        private int BuildPipeConnectionMask(Vector2Int cell, BuildingData data)
        {
            int raw = 0;
            if (CanPipeConnect(cell, Vector2Int.up)) raw |= 1;
            if (CanPipeConnect(cell, Vector2Int.right)) raw |= 2;
            if (CanPipeConnect(cell, Vector2Int.down)) raw |= 4;
            if (CanPipeConnect(cell, Vector2Int.left)) raw |= 8;

            // Regular pipe is straight-only: no auto-corners from nearby neighbors.
            // If placed direction doesn't match actual neighbors, auto-pick the axis
            // that currently has valid connections.
            bool preferredHorizontal = Mathf.Abs(data.Direction.x) >= Mathf.Abs(data.Direction.y);
            int horizontalMask = raw & (2 | 8);
            int verticalMask = raw & (1 | 4);
            int preferredMask = preferredHorizontal ? horizontalMask : verticalMask;
            if (preferredMask != 0)
            {
                return preferredMask;
            }

            return horizontalMask != 0 ? horizontalMask : verticalMask;
        }

        private static bool PipeCornerHasPort(BuildingData corner, Vector2Int dir)
        {
            Vector2Int a = corner.Direction == Vector2Int.zero ? Vector2Int.right : corner.Direction;
            Vector2Int b = new Vector2Int(a.y, -a.x);
            // Two-way elbow: either side can be input or output.
            return dir == a || dir == b;
        }

        private static bool IsPipeCompatibleWithDirection(BuildingData pipe, Vector2Int dir)
        {
            bool horizontal = Mathf.Abs(pipe.Direction.x) >= Mathf.Abs(pipe.Direction.y);
            int allowed = horizontal ? (2 | 8) : (1 | 4);
            return (allowed & DirToMaskBit(dir)) != 0;
        }

        private bool CanPipeConnect(Vector2Int fromPipeCell, Vector2Int dir)
        {
            Vector2Int cell = fromPipeCell + dir;
            if (!_buildings.TryGetValue(cell, out var b))
            {
                return false;
            }

            if (b.Type == BuildingType.Extractor)
            {
                // Extractor output follows its placed direction.
                return fromPipeCell == cell + b.Direction;
            }

            if (b.Type == BuildingType.Pipe)
            {
                // Neighbor pipe must expose a compatible side back to us.
                return IsPipeCompatibleWithDirection(b, -dir);
            }

            if (b.Type == BuildingType.PipeCorner)
            {
                // Explicit compatibility path: straight pipe can connect to a corner
                // whenever it points to the corner port side.
                return PipeCornerHasPort(b, -dir);
            }

            if (b.Type == BuildingType.Storage)
            {
                // Storage: ports on opposite sides along Direction (input -Direction, output +Direction); rotates with building.
                return dir == b.Direction || dir == -b.Direction;
            }

            if (b.Type == BuildingType.Mixer)
            {
                // Mixer: no connection on output(front) side.
                return dir != -b.Direction;
            }

            if (b.Type == BuildingType.MarketTerminal)
            {
                // Market: only one input side (back).
                return dir == b.Direction;
            }

            if (b.Type == BuildingType.PipeConnector)
            {
                // Connector is physically connectable from all sides.
                // Transfer direction rules are handled in CanAccept/TryTransferForward.
                return true;
            }

            if (b.Type == BuildingType.PipeSplitter)
            {
                // Splitter: one input(back), three outputs.
                return dir == b.Direction || dir == -b.Direction
                    || dir == new Vector2Int(-b.Direction.y, b.Direction.x)
                    || dir == new Vector2Int(b.Direction.y, -b.Direction.x);
            }

            return false;
        }

        public bool TryGetPreferredPipeFlowDirection(Vector2Int pipeCell, Vector2Int requestedDirection, out Vector2Int preferredDirection, out bool hasConnection)
        {
            preferredDirection = NormalizeDir(requestedDirection);
            hasConnection = false;

            int raw = 0;
            if (CanPipeConnect(pipeCell, Vector2Int.up)) raw |= 1;
            if (CanPipeConnect(pipeCell, Vector2Int.right)) raw |= 2;
            if (CanPipeConnect(pipeCell, Vector2Int.down)) raw |= 4;
            if (CanPipeConnect(pipeCell, Vector2Int.left)) raw |= 8;

            int horizontalMask = raw & (2 | 8);
            int verticalMask = raw & (1 | 4);
            hasConnection = raw != 0;

            bool requestedHorizontal = Mathf.Abs(preferredDirection.x) >= Mathf.Abs(preferredDirection.y);
            int requestedMask = requestedHorizontal ? horizontalMask : verticalMask;
            if (requestedMask != 0)
            {
                return true;
            }

            if (horizontalMask != 0)
            {
                preferredDirection = Vector2Int.right;
                return true;
            }

            if (verticalMask != 0)
            {
                preferredDirection = Vector2Int.up;
                return true;
            }

            return true;
        }
    }
}
