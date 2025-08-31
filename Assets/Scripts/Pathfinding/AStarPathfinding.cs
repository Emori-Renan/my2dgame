using UnityEngine;
using System.Collections.Generic;
using MyGame.Managers;
using MyGame.Core;
using System.Linq;

namespace MyGame.Pathfinding
{
    public class AStarPathfinding : MonoBehaviour
    {
        public static AStarPathfinding Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GridManager gridManager;

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
                return gridPosition.x.GetHashCode() ^ gridPosition.y.GetHashCode();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
            }

            if (gridManager == null)
            {
                Debug.LogError("AStarPathfinding: GridManager instance not found. Pathfinding will not work!");
                this.enabled = false;
                return;
            }
            Debug.Log("AStarPathfinding Manager Initialized.");
        }

        private void Start()
        {
        }

        public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
        {
            if (gridManager == null)
            {
                Debug.LogError("AStarPathfinding: GridManager is null. Cannot find path.");
                return new List<Vector2Int>();
            }
            if (!gridManager.IsGridDataInitialized)
            {
                Debug.LogWarning("AStarPathfinding: GridManager's grid data is not initialized for the current scene. Cannot find path.");
                return new List<Vector2Int>();
            }
            if (!gridManager.IsWalkable(startPos) || !gridManager.IsWalkable(targetPos))
            {
                Debug.LogWarning($"AStarPathfinding: Start ({startPos}) or target ({targetPos}) position is not walkable. Cannot find a path.");
                return new List<Vector2Int>();
            }

            Node startNode = new Node(startPos);
            Node endNode = new Node(targetPos);

            List<Node> openList = new List<Node>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Node> allNodesInOpen = new Dictionary<Vector2Int, Node>();

            startNode.gCost = 0;
            startNode.hCost = GetDistance(startNode.gridPosition, endNode.gridPosition);
            openList.Add(startNode);
            allNodesInOpen.Add(startNode.gridPosition, startNode);

            while (openList.Count > 0)
            {
                Node currentNode = openList.OrderBy(n => n.fCost).First();
                
                openList.Remove(currentNode);
                allNodesInOpen.Remove(currentNode.gridPosition);
                closedSet.Add(currentNode.gridPosition);

                if (currentNode.gridPosition == endNode.gridPosition)
                {
                    return RetracePath(startNode, currentNode);
                }

                foreach (Vector2Int neighborPos in GetNeighboringNodes(currentNode.gridPosition))
                {
                    BoundsInt gridBounds = gridManager.GetMapGridBounds();
                    if (neighborPos.x < 0 || neighborPos.x >= gridBounds.size.x || 
                        neighborPos.y < 0 || neighborPos.y >= gridBounds.size.y)
                    {
                        continue;
                    }

                    if (!gridManager.IsWalkable(neighborPos) || closedSet.Contains(neighborPos))
                    {
                        continue;
                    }

                    int newGCost = currentNode.gCost + GetDistance(currentNode.gridPosition, neighborPos);
                    
                    Node neighborNode;
                    if (allNodesInOpen.TryGetValue(neighborPos, out neighborNode))
                    {
                        if (newGCost < neighborNode.gCost)
                        {
                            neighborNode.gCost = newGCost;
                            neighborNode.parent = currentNode;
                        }
                    }
                    else
                    {
                        neighborNode = new Node(neighborPos);
                        neighborNode.gCost = newGCost;
                        neighborNode.hCost = GetDistance(neighborNode.gridPosition, endNode.gridPosition);
                        neighborNode.parent = currentNode;
                        openList.Add(neighborNode);
                        allNodesInOpen.Add(neighborNode.gridPosition, neighborNode);
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
