using System.Collections.Generic;
using UnityEngine;

namespace FinalProject
{
    public sealed class AStarPathfinder : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Vector2 gridCenter = Vector2.zero;
        [SerializeField] private Vector2 gridSize = new Vector2(7f, 7f);
        [SerializeField] private float cellSize = 0.35f;
        [SerializeField] private float pathHeight = 0.9f;
        [SerializeField] private bool allowDiagonals = true;

        [Header("Obstacle Sampling")]
        [SerializeField] private LayerMask obstacleMask = 1;
        [SerializeField] private float clearanceRadius = 0.2f;
        [SerializeField] private float obstacleSampleHeight = 0.65f;
        [SerializeField] private float obstacleSampleHalfHeight = 0.35f;

        private readonly List<int> openIndices = new List<int>(256);
        private readonly List<Vector3> lastPath = new List<Vector3>(64);
        private Node[] nodes;
        private int gridWidth;
        private int gridDepth;

        private static readonly Vector2Int[] CardinalNeighbours =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        private static readonly Vector2Int[] DiagonalNeighbours =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1),
        };

        public IReadOnlyList<Vector3> LastPath => lastPath;

        public bool IsSegmentClear(Vector3 from, Vector3 to, float radius = -1f)
        {
            if (obstacleMask.value == 0)
                return true;

            var start = new Vector3(from.x, pathHeight, from.z);
            var end = new Vector3(to.x, pathHeight, to.z);
            var direction = end - start;
            var distance = direction.magnitude;

            if (distance <= 0.001f)
                return true;

            var probeRadius = radius > 0f ? radius : clearanceRadius;
            return !Physics.SphereCast(start, probeRadius, direction / distance, out _, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        public void Configure(Vector2 center, Vector2 size, float newCellSize, float newPathHeight, LayerMask newObstacleMask)
        {
            gridCenter = center;
            gridSize = size;
            cellSize = Mathf.Max(0.1f, newCellSize);
            pathHeight = newPathHeight;
            obstacleMask = newObstacleMask;
        }

        public bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> result)
        {
            result.Clear();
            lastPath.Clear();
            BuildGrid();

            if (!TryGetClosestWalkableCell(start, out var startCell))
                return false;

            if (!TryGetClosestWalkableCell(end, out var endCell))
                return false;

            var startIndex = ToIndex(startCell.x, startCell.y);
            var endIndex = ToIndex(endCell.x, endCell.y);

            var startNode = nodes[startIndex];
            startNode.gCost = 0f;
            startNode.hCost = GetHeuristic(startCell, endCell);
            startNode.parentIndex = -1;
            startNode.isOpen = true;
            nodes[startIndex] = startNode;

            openIndices.Clear();
            openIndices.Add(startIndex);

            while (openIndices.Count > 0)
            {
                var currentIndex = PopBestOpenIndex();
                var current = nodes[currentIndex];
                current.isClosed = true;
                nodes[currentIndex] = current;

                if (currentIndex == endIndex)
                {
                    ReconstructPath(endIndex, result);
                    lastPath.AddRange(result);
                    return true;
                }

                VisitNeighbours(currentIndex, endCell);
            }

            return false;
        }

        private void BuildGrid()
        {
            gridWidth = Mathf.Max(2, Mathf.CeilToInt(gridSize.x / cellSize));
            gridDepth = Mathf.Max(2, Mathf.CeilToInt(gridSize.y / cellSize));
            var requiredLength = gridWidth * gridDepth;

            if (nodes == null || nodes.Length != requiredLength)
                nodes = new Node[requiredLength];

            for (var z = 0; z < gridDepth; z++)
            {
                for (var x = 0; x < gridWidth; x++)
                {
                    var index = ToIndex(x, z);
                    nodes[index] = new Node
                    {
                        x = x,
                        z = z,
                        parentIndex = -1,
                        gCost = float.PositiveInfinity,
                        isBlocked = IsCellBlocked(x, z),
                    };
                }
            }
        }

        private void VisitNeighbours(int currentIndex, Vector2Int endCell)
        {
            var current = nodes[currentIndex];
            VisitNeighbourSet(currentIndex, current, endCell, CardinalNeighbours, cellSize);

            if (allowDiagonals)
                VisitNeighbourSet(currentIndex, current, endCell, DiagonalNeighbours, cellSize * 1.41421356f);
        }

        private void VisitNeighbourSet(int currentIndex, Node current, Vector2Int endCell, IReadOnlyList<Vector2Int> offsets, float movementCost)
        {
            for (var i = 0; i < offsets.Count; i++)
            {
                var neighbourX = current.x + offsets[i].x;
                var neighbourZ = current.z + offsets[i].y;

                if (!IsInside(neighbourX, neighbourZ))
                    continue;

                var neighbourIndex = ToIndex(neighbourX, neighbourZ);
                var neighbour = nodes[neighbourIndex];

                if (neighbour.isBlocked || neighbour.isClosed)
                    continue;

                if (Mathf.Abs(offsets[i].x) == 1 && Mathf.Abs(offsets[i].y) == 1 && CutsBlockedCorner(current.x, current.z, offsets[i]))
                    continue;

                if (!IsSegmentClear(CellToWorld(current.x, current.z), CellToWorld(neighbourX, neighbourZ)))
                    continue;

                var nextCost = current.gCost + movementCost;
                if (neighbour.isOpen && nextCost >= neighbour.gCost)
                    continue;

                neighbour.gCost = nextCost;
                neighbour.hCost = GetHeuristic(new Vector2Int(neighbourX, neighbourZ), endCell);
                neighbour.parentIndex = currentIndex;

                if (!neighbour.isOpen)
                {
                    neighbour.isOpen = true;
                    openIndices.Add(neighbourIndex);
                }

                nodes[neighbourIndex] = neighbour;
            }
        }

        private int PopBestOpenIndex()
        {
            var bestListIndex = 0;
            var bestNode = nodes[openIndices[0]];

            for (var i = 1; i < openIndices.Count; i++)
            {
                var candidate = nodes[openIndices[i]];
                if (candidate.FCost < bestNode.FCost || Mathf.Approximately(candidate.FCost, bestNode.FCost) && candidate.hCost < bestNode.hCost)
                {
                    bestListIndex = i;
                    bestNode = candidate;
                }
            }

            var bestIndex = openIndices[bestListIndex];
            openIndices.RemoveAt(bestListIndex);
            return bestIndex;
        }

        private void ReconstructPath(int endIndex, List<Vector3> result)
        {
            var currentIndex = endIndex;
            var guard = nodes.Length;

            while (currentIndex >= 0 && guard-- > 0)
            {
                var node = nodes[currentIndex];
                result.Add(CellToWorld(node.x, node.z));
                currentIndex = node.parentIndex;
            }

            result.Reverse();
        }

        private bool TryGetClosestWalkableCell(Vector3 worldPosition, out Vector2Int cell)
        {
            cell = WorldToCell(worldPosition);

            if (IsInside(cell.x, cell.y) && !nodes[ToIndex(cell.x, cell.y)].isBlocked)
                return true;

            var maxRadius = Mathf.Max(gridWidth, gridDepth);
            for (var radius = 1; radius < maxRadius; radius++)
            {
                for (var z = -radius; z <= radius; z++)
                {
                    for (var x = -radius; x <= radius; x++)
                    {
                        if (Mathf.Abs(x) != radius && Mathf.Abs(z) != radius)
                            continue;

                        var candidate = new Vector2Int(cell.x + x, cell.y + z);
                        if (!IsInside(candidate.x, candidate.y))
                            continue;

                        if (!nodes[ToIndex(candidate.x, candidate.y)].isBlocked)
                        {
                            cell = candidate;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool CutsBlockedCorner(int x, int z, Vector2Int offset)
        {
            var horizontalX = x + offset.x;
            var verticalZ = z + offset.y;

            if (!IsInside(horizontalX, z) || !IsInside(x, verticalZ))
                return true;

            return nodes[ToIndex(horizontalX, z)].isBlocked || nodes[ToIndex(x, verticalZ)].isBlocked;
        }

        private bool IsCellBlocked(int x, int z)
        {
            if (obstacleMask.value == 0)
                return false;

            var center = CellToWorld(x, z);
            center.y = obstacleSampleHeight;
            var horizontalProbe = Mathf.Max(cellSize * 0.45f, clearanceRadius);
            var halfExtents = new Vector3(horizontalProbe, obstacleSampleHalfHeight, horizontalProbe);
            return Physics.CheckBox(center, halfExtents, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private Vector2Int WorldToCell(Vector3 worldPosition)
        {
            var minX = gridCenter.x - gridSize.x * 0.5f;
            var minZ = gridCenter.y - gridSize.y * 0.5f;
            var x = Mathf.FloorToInt((worldPosition.x - minX) / cellSize);
            var z = Mathf.FloorToInt((worldPosition.z - minZ) / cellSize);
            return new Vector2Int(Mathf.Clamp(x, 0, gridWidth - 1), Mathf.Clamp(z, 0, gridDepth - 1));
        }

        private Vector3 CellToWorld(int x, int z)
        {
            var minX = gridCenter.x - gridSize.x * 0.5f;
            var minZ = gridCenter.y - gridSize.y * 0.5f;
            return new Vector3(minX + (x + 0.5f) * cellSize, pathHeight, minZ + (z + 0.5f) * cellSize);
        }

        private bool IsInside(int x, int z)
        {
            return x >= 0 && x < gridWidth && z >= 0 && z < gridDepth;
        }

        private int ToIndex(int x, int z)
        {
            return z * gridWidth + x;
        }

        private float GetHeuristic(Vector2Int from, Vector2Int to)
        {
            var dx = Mathf.Abs(from.x - to.x);
            var dz = Mathf.Abs(from.y - to.y);
            return allowDiagonals ? Mathf.Max(dx, dz) * cellSize : (dx + dz) * cellSize;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.28f);
            Gizmos.DrawWireCube(new Vector3(gridCenter.x, pathHeight, gridCenter.y), new Vector3(gridSize.x, 0.05f, gridSize.y));

            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.8f);
            for (var i = 0; i < lastPath.Count; i++)
                Gizmos.DrawSphere(lastPath[i], 0.04f);
        }

        private struct Node
        {
            public int x;
            public int z;
            public int parentIndex;
            public float gCost;
            public float hCost;
            public bool isBlocked;
            public bool isOpen;
            public bool isClosed;

            public float FCost => gCost + hCost;
        }
    }
}
