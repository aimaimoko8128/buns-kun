using System.Collections.Generic;
using UnityEngine;
using BunsKun.Rooms;

namespace BunsKun.ProceduralGeneration
{
    /// <summary>
    /// One node in the generated dungeon graph. Grid coordinates place it in world space
    /// (see MapGenerator.RoomWidth/RoomHeight); connections record which sides have a door
    /// to a neighboring room.
    /// </summary>
    public class RoomNode
    {
        public Vector2Int GridPos;
        public RoomType Type;
        public readonly Dictionary<Direction, RoomNode> Connections = new Dictionary<Direction, RoomNode>();
        public bool IsElite; // Optional rooms that chose the "tougher enemies, better reward" branch

        public RoomNode(Vector2Int gridPos, RoomType type)
        {
            GridPos = gridPos;
            Type = type;
        }

        public bool RequiresClearLock()
        {
            return Type == RoomType.Combat || Type == RoomType.Boss || Type == RoomType.Optional;
        }

        public void Connect(Direction dir, RoomNode other)
        {
            Connections[dir] = other;
            other.Connections[dir.Opposite()] = this;
        }
    }
}
