using System.Collections.Generic;
using UnityEngine;
using BunsKun.Rooms;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// Builds a randomized dungeon graph for one area: a mostly-rightward main path of
    /// rooms (with some vertical wiggle) ending in an area-exit or boss room, plus a few
    /// optional dead-end branch rooms for risk/reward exploration.
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>Room size in tiles (one tile is one world unit).</summary>
        public const int RoomWidthTiles = 32;
        public const int RoomHeightTiles = 20;

        public const float RoomWidth = RoomWidthTiles;
        public const float RoomHeight = RoomHeightTiles;

        public struct Edge
        {
            public RoomNode Parent;
            public RoomNode Child;
            public Direction DirFromParent;
        }

        public class GeneratedArea
        {
            public Dictionary<Vector2Int, RoomNode> Nodes = new Dictionary<Vector2Int, RoomNode>();
            public RoomNode Start;
            public RoomNode End;
            public List<RoomNode> MainPath = new List<RoomNode>();
            public List<Edge> Edges = new List<Edge>();
        }

        public static GeneratedArea Generate(System.Random rng, int mainPathLength, bool isFinalArea, int optionalBranchCount)
        {
            var area = new GeneratedArea();
            var startNode = new RoomNode(Vector2Int.zero, RoomType.Start);
            area.Nodes[Vector2Int.zero] = startNode;
            area.MainPath.Add(startNode);

            Vector2Int cur = Vector2Int.zero;
            for (int i = 1; i < mainPathLength; i++)
            {
                Direction chosen = PickFreeDirection(rng, area.Nodes, cur);
                Vector2Int next = cur + chosen.Delta();

                bool isLast = i == mainPathLength - 1;
                RoomType type;
                if (isLast) type = isFinalArea ? RoomType.Boss : RoomType.Combat;
                else type = (i % 3 == 0) ? RoomType.Reward : RoomType.Combat;

                var node = new RoomNode(next, type);
                area.Nodes[next] = node;
                var parent = area.Nodes[cur];
                parent.Connect(chosen, node);
                area.Edges.Add(new Edge { Parent = parent, Child = node, DirFromParent = chosen });
                area.MainPath.Add(node);
                cur = next;
            }
            area.Start = startNode;
            area.End = area.MainPath[area.MainPath.Count - 1];

            for (int b = 0; b < optionalBranchCount; b++)
            {
                AttachOptionalBranch(rng, area);
            }

            return area;
        }

        private static void AttachOptionalBranch(System.Random rng, GeneratedArea area)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int upperExclusive = Mathf.Max(2, area.MainPath.Count - 1);
                var anchor = area.MainPath[rng.Next(1, upperExclusive)];
                var order = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
                Shuffle(rng, order);
                foreach (var dir in order)
                {
                    Vector2Int candidate = anchor.GridPos + dir.Delta();
                    if (area.Nodes.ContainsKey(candidate)) continue;
                    var optionalNode = new RoomNode(candidate, RoomType.Optional)
                    {
                        IsElite = rng.NextDouble() < 0.5
                    };
                    area.Nodes[candidate] = optionalNode;
                    anchor.Connect(dir, optionalNode);
                    area.Edges.Add(new Edge { Parent = anchor, Child = optionalNode, DirFromParent = dir });
                    return;
                }
            }
        }

        private static Direction PickFreeDirection(System.Random rng, Dictionary<Vector2Int, RoomNode> nodes, Vector2Int from)
        {
            // The dungeon is a descent: the main path mostly tunnels downward, with an
            // occasional sideways wiggle for a wider layer. Up is deliberately excluded from
            // the main path so the player is never routed back toward the surface - it is
            // still available to optional branch rooms for a riskier detour.
            var weighted = new List<Direction> { Direction.Down, Direction.Down, Direction.Down, Direction.Left, Direction.Right };
            Shuffle(rng, weighted);
            foreach (var dir in weighted)
            {
                if (!nodes.ContainsKey(from + dir.Delta())) return dir;
            }
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                if (!nodes.ContainsKey(from + dir.Delta())) return dir;
            }
            return Direction.Right;
        }

        private static void Shuffle<T>(System.Random rng, List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
