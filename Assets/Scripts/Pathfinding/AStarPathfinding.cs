using UnityEngine;
using System.Collections.Generic;
using MyGame.Managers; 
using MyGame.Core; 
using System.Linq;

namespace MyGame.Pathfinding
{
    /// <summary>
    /// The main manager for all grid-based pathfinding operations using the A* algorithm.
    /// It also contains the Node helper class as a private inner class.
    /// </summary>
    public class AStarPathfinding : MonoBehaviour
    {
        public static AStarPathfinding Instance { get; private set; }

        private GridManager gridManager;

        /// <summary>
        /// A simple class to represent a node (tile) in the grid for pathfinding.
        /// This is a private inner class only accessible by AStarPathfinding.cs.
        /// </summary>
        private class Node
        {
            public Vector2Int gridPosition;
            public Node parent;
            public int gCost;
            public int hCost;
            public int fCost => gCost + hCost;

            public Node(Vector2Int pos)
            {
                gridPosition = pos;
            }

            public override bool Equals(object obj)
            {
                if (obj is Node otherNode)
                {
                    return this.gridPosition == otherNode.gridPosition;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return gridPosition.GetHashCode();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogError("AStarPathfinding: GridManager instance not found. Cannot perform pathfinding.");
            }
        }

        /// <summary>
        /// Finds the shortest walkable path between two grid positions using the A* algorithm.
        /// </summary>
        /// <param name="startPos">The starting grid coordinate (0-indexed).</param>
        /// <param name="targetPos">The target grid coordinate (0-indexed).</param>
        /// <returns>A list of grid coordinates representing the path, or an empty list if no path is found.</returns>
        public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
        {
            if (gridManager == null || !gridManager.IsWalkable(startPos) || !gridManager.IsWalkable(targetPos))
            {
                Debug.LogWarning("AStarPathfinding: Start or target position is not walkable. Cannot find a path.");
                return new List<Vector2Int>();
            }

            Node startNode = new Node(startPos);
            Node endNode = new Node(targetPos);

            List<Node> openList = new List<Node>();
            HashSet<Node> closedList = new HashSet<Node>();

            openList.Add(startNode);

            while (openList.Count > 0)
            {
                Node currentNode = openList.OrderBy(n => n.fCost).First();
                
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                if (currentNode.gridPosition == endNode.gridPosition)
                {
                    return RetracePath(startNode, currentNode);
                }

                foreach (Vector2Int neighborPos in GetNeighboringNodes(currentNode.gridPosition))
                {
                    if (!gridManager.IsWalkable(neighborPos) || closedList.Any(n => n.gridPosition == neighborPos))
                    {
                        continue;
                    }

                    int newGCost = currentNode.gCost + GetDistance(currentNode.gridPosition, neighborPos);
                    Node neighborNode = openList.FirstOrDefault(n => n.gridPosition == neighborPos);

                    if (neighborNode == null)
                    {
                        neighborNode = new Node(neighborPos);
                        neighborNode.gCost = newGCost;
                        neighborNode.hCost = GetDistance(neighborNode.gridPosition, endNode.gridPosition);
                        neighborNode.parent = currentNode;
                        openList.Add(neighborNode);
                    }
                    else if (newGCost < neighborNode.gCost)
                    {
                        neighborNode.gCost = newGCost;
                        neighborNode.parent = currentNode;
                    }
                }
            }

            return new List<Vector2Int>();
        }

        private List<Vector2Int> RetracePath(Node startNode, Node endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode.gridPosition);
                currentNode = currentNode.parent;
            }
            path.Reverse();
            return path;
        }

        private List<Vector2Int> GetNeighboringNodes(Vector2Int nodePos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue; 
                    
                    Vector2Int neighbor = new Vector2Int(nodePos.x + x, nodePos.y + y);
                    neighbors.Add(neighbor);
                }
            }
            return neighbors;
        }

        private int GetDistance(Vector2Int posA, Vector2Int posB)
        {
            int dstX = Mathf.Abs(posA.x - posB.x);
            int dstY = Mathf.Abs(posA.y - posB.y);

            if (dstX > dstY)
            {
                return 14 * dstY + 10 * (dstX - dstY);
            }
            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
}