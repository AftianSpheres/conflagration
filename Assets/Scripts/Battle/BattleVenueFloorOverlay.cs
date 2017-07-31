using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour that provides the generated texture for the battle venue floor overlay
/// used to preview move ranges, attack AOE, etc.
/// </summary>
public class BattleVenueFloorOverlay : MonoBehaviour
{
    /// <summary>
    /// A node used by the pathfinding algorithm.
    /// </summary>
    private class PFNode
    {
        public readonly Cell cell;
        public readonly Vector2 vector;
        public PFNode above { get { if (y + 1 < map.GetLength(1)) return map[x, y + 1]; else return null; } }
        public PFNode below { get { if (y > 0) return map[x, y - 1]; else return null; } }
        public PFNode left { get { if (x > 0) return map[x - 1, y]; else return null; } }
        public PFNode right { get { if (x + 1 < map.GetLength(0)) return map[x + 1, y]; else return null; } }
        public PFNode cameFrom { get; private set; }
        public int fScore { get; private set; }
        public int gScore { get; private set; }
        private readonly PFNode[,] map;
        /// <summary>
        /// The local x coordinate within the pathfinding area.
        /// </summary>
        private readonly int x;
        /// <summary>
        /// The local y coordinate within the pathfinding area.
        /// </summary>
        private readonly int y;

        public PFNode (Cell _cell, PFNode[,] _map, int _x, int _y)
        {
            cell = _cell;
            map = _map;
            x = _x;
            y = _y;
            vector = new Vector2(x, y);
            fScore = int.MaxValue;
            gScore = int.MaxValue;
        }

        /// <summary>
        /// This node optimally paths from the given one.
        /// </summary>
        public void PathFrom (PFNode _cameFrom, int _gScore, int estimate)
        {
            cameFrom = _cameFrom;
            gScore = _gScore;
            fScore = gScore + estimate;
        }

        /// <summary>
        /// This node is the start node.
        /// </summary>
        public void StartNode ()
        {
            gScore = 0;
            fScore = 0;
        }
    }

    /// <summary>
    /// Represents one pixel in the texture to be built.
    /// </summary>
    private struct Cell : IEquatable<Cell>
    {
        public readonly int x;
        public readonly int y;
        public readonly Vector2 realPos;
        const float halfFieldRadius = BattleOverseer.fieldRadius / 2;

        public Cell (int _x, int _y)
        {
            x = _x;
            y = _y;
            Func<int, float> normalize = (i) => { return (((i / resolution) * BattleOverseer.fieldRadius) - halfFieldRadius); };
            realPos = new Vector2(normalize(x), normalize(y));
        }

        public Cell (Vector2 _realPos)
        {
            realPos = _realPos;
            Func<float, int> denormalize = (f) => { return Mathf.RoundToInt(((f + halfFieldRadius) / BattleOverseer.fieldRadius) * resolution); };
            x = denormalize(realPos.x);
            y = denormalize(realPos.y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        bool IEquatable<Cell>.Equals(Cell other)
        {
            return (x == other.x && y == other.y);
        }

        public static bool operator == (Cell c0, Cell c1)
        {
            return c0.Equals(c1);
        }

        public static bool operator != (Cell c0, Cell c1)
        {
            return !c0.Equals(c1);
        }
    }

    /// <summary>
    /// A line segment connecting two pathfinding nodes.
    /// </summary>
    private struct Segment
    {
        public readonly float length;
        public readonly PFNode start;
        public readonly PFNode end;

        public Segment (PFNode _start, PFNode _end)
        {
            start = _start;
            end = _end;
            length = Vector2.Distance(start.vector, end.vector);
        }
    }

    public Texture2D tex;
    public Color32 allowedColor;
    public Color32 forbiddenColor;
    private const int resolution = 512;
    private const float realSizeOfCell = resolution / BattleOverseer.fieldRadius;

    IEnumerator<float> cr ()
    {
        while (BattleOverseer.currentBattle == null) yield return 0;
        tex = PreviewMoveArea(BattleOverseer.currentBattle.allBattlers[2]);
    }

    void Start()
    {
        MovementEffects.Timing.RunCoroutine(cr());
    }

    /// <summary>
    /// Generate the move preview texture.
    /// </summary>
    public Texture2D PreviewMoveArea (Battler mover)
    {
        int minX;
        int maxX;
        int minY;
        int maxY;
        LinkedList<Cell> moveRadiusCells = GetRadialForMover(mover, out minX, out maxX, out minY, out maxY);
        Battler[] enemies = BattleOverseer.currentBattle.GetBattlersEnemiesTo(mover.side);
        LinkedList<Cell>[] obstructionZones = new LinkedList<Cell>[enemies.Length];
        LinkedList<Cell>[] obstructorFootprints = new LinkedList<Cell>[enemies.Length];
        int[] bounds;
        for (int i = 0; i < obstructorFootprints.Length; i++) obstructorFootprints[i] = GetCellsInCircle(new Cell(enemies[i].logicalPosition), enemies[i].footprintRadius, out bounds);
        for (int i = 0; i < obstructionZones.Length; i++) obstructionZones[i] = GetObstructedBy(moveRadiusCells, mover, enemies[i], obstructorFootprints, minX, maxX, minY, maxY);
        Texture2D tex = new Texture2D(resolution, resolution);
        Color32[] colors = new Color32[resolution * resolution];
        int y = resolution - 1;
        int x = 0;
        for (int c = 0; c < colors.Length; c++)
        {
            if (x >= minX && x <= maxX && y >= minY && y <= maxY)
            {
                Cell cell = new Cell(x, y);
                bool obstructed = false;
                for (int o = 0; o < obstructionZones.Length; o++)
                {
                    if (obstructionZones[o].Contains(cell))
                    {
                        obstructed = true;
                        break;
                    }
                }
                if (moveRadiusCells.Contains(cell) && !obstructed) colors[c] = allowedColor;
                else colors[c] = forbiddenColor;
            }
            else colors[c] = forbiddenColor;
            if (x >= resolution)
            {
                x = 0;
                y--;
            }
            else x++;
        }
        tex.SetPixels32(colors);
        tex.Apply(false, true);
        MovementEffects.Timing.RunCoroutine(Util._WaitOneFrame(() => { Debug.Log(Time.deltaTime); }));
        return tex;
    }

    /// <summary>
    /// Get a list containing all Cells in a filled circle with the given center and radius.
    /// </summary>
    private LinkedList<Cell> GetCellsInCircle (Cell center, float realSpaceRadius, out int[] boundsArray)
    {
        // This is broken!
        int unitsRadius = Mathf.RoundToInt(realSpaceRadius * realSizeOfCell);
        int r2 = unitsRadius * unitsRadius;
        int xMin = center.x - (unitsRadius / 2);
        int xMax = center.x + (unitsRadius / 2);
        int yMin = center.y - (unitsRadius / 2);
        int yMax = center.y + (unitsRadius / 2);
        Debug.Log("min " + yMin + " max " + yMax + " ur " + unitsRadius);
        if (xMin < 0) xMin = 0;
        if (xMax >= resolution) xMax = resolution;
        if (yMin < 0) yMin = 0;
        if (yMax >= resolution) yMax = resolution;
        int rh = unitsRadius / 2;
        LinkedList<Cell> cells = new LinkedList<Cell>();
        for (int y = yMin; y <= yMax; y++)
        {
            int dy = center.y - y;
            for (int x = xMin; x <= xMax; x++)
            {
                int dx = center.x - x;
                if ((dx * dx) + (dy * dy) <= r2)
                {
                    cells.AddLast(new Cell(x, y));
                    Debug.Log(x + ", " + y);
                }
            }
        }
        boundsArray = new int[] { xMin, xMax, yMin, yMax };
        return cells;
    }

    /// <summary>
    /// Get cells in move range of the given battler,
    /// as well as the boundaries of its move range.
    /// </summary>
    private LinkedList<Cell> GetRadialForMover (Battler mover, out int minX, out int maxX, out int minY, out int maxY)
    {
        Cell center = new Cell(mover.logicalPosition);
        int[] bounds;
        LinkedList<Cell> ret = GetCellsInCircle(center, mover.stats.moveDist, out bounds);
        minX = bounds[0];
        maxX = bounds[1];
        minY = bounds[2];
        maxY = bounds[3];
        return ret;
    }

    /// <summary>
    /// Get all cells in move area that are obstructed by a specific Battler.
    /// </summary>
    private LinkedList<Cell> GetObstructedBy (LinkedList<Cell> moveArea, Battler mover, Battler obstruction, LinkedList<Cell>[] obstructorFootprints, int xMin, int xMax, int yMin, int yMax)
    {
        LinkedList<Cell> obstructed = new LinkedList<Cell>();
        Cell moverCell = new Cell(mover.logicalPosition);
        Cell obstructingCell = new Cell(obstruction.logicalPosition);
        int[] bounds;
        LinkedList<Cell> moverFootprint = GetCellsInCircle(moverCell, mover.footprintRadius, out bounds);
        LinkedList<Cell> obstructorFootprint = GetCellsInCircle(obstructingCell, obstruction.footprintRadius, out bounds);
        if (obstruction.logicalPosition.x < mover.logicalPosition.x && bounds[1] < xMax) xMax = bounds[1];
        else if (obstruction.logicalPosition.x > mover.logicalPosition.x && bounds[0] > xMin) xMin = bounds[0];
        if (obstruction.logicalPosition.y < mover.logicalPosition.y && bounds[3] < yMax) yMax = bounds[3];
        else if (obstruction.logicalPosition.y > mover.logicalPosition.y && bounds[2] > yMin) yMin = bounds[2];
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Cell cell = new Cell(x, y);
                if (moverFootprint.Contains(cell) || obstructorFootprint.Contains(cell)) continue;
                if (!SlowPathfinding(xMin, xMax, yMin, yMax, mover.stats.moveDist, moverCell, cell, moveArea, obstructorFootprints)) obstructed.AddLast(cell);
            }
        }
        return obstructed;
    }

    /// <summary>
    /// Tries to get a path from moverPos to dest within realDist using a* search.
    /// This is slower than is ideal. Be sure to constrain the number of cells you try
    /// to pathfind for as much as possible.
    /// </summary>
    private bool SlowPathfinding (int xMin, int xMax, int yMin, int yMax, float distLimit, Cell moverPos, Cell dest, LinkedList<Cell> moveArea, LinkedList<Cell>[] obstructorFootprints)
    {
        Vector2 destVector = new Vector2(dest.x, dest.y);
        PFNode start = null;
        PFNode[,] pathfindingArea = new PFNode[xMax - xMin, yMax - yMin];
        for (int y = 0; y < pathfindingArea.GetLength(1); y++)
        {
            for (int x = 0; x < pathfindingArea.GetLength(0); x++)
            {
                Cell cell = new Cell(x, y);         
                if (moveArea.Contains(cell))
                {
                    bool inObstructorFootprint = false;
                    for (int i = 0; i < obstructorFootprints.Length; i++)
                    {
                        if (obstructorFootprints[i].Contains(cell))
                        {
                            inObstructorFootprint = true;
                            break;
                        }
                    }
                    if (!inObstructorFootprint | cell == moverPos) pathfindingArea[x, y] = new PFNode(cell, pathfindingArea, x, y);
                    if (cell == moverPos) start = pathfindingArea[x, y];
                }
            }
        }
        LinkedList<PFNode> closed = new LinkedList<PFNode>();
        start.StartNode();
        LinkedList<PFNode> open = new LinkedList<PFNode>();
        open.AddFirst(start);
        while (open.Count > 0)
        {
            // Get the node with the lowest fScore
            PFNode currentNode = null;
            int lowestFScore = int.MaxValue;
            LinkedListNode<PFNode> chk = open.First;
            if (currentNode.cell == dest) return ValidatePathLength(currentNode, distLimit);
            bool searching = true;
            while (searching)
            {
                if (chk.Value.fScore < lowestFScore)
                {
                    currentNode = chk.Value;
                    lowestFScore = currentNode.fScore;
                }
                if (chk.Next == null) searching = false;
            }
            open.Remove(currentNode);
            closed.AddLast(currentNode);
            Func<PFNode, PFNode, int> dist = (nodeA, nodeB) =>
            {
                return Mathf.RoundToInt(Vector2.Distance(nodeA.vector, nodeB.vector));
            };
            Func<PFNode, int> estimate = (node) =>
            {
                return Mathf.RoundToInt(Vector2.Distance(node.vector, destVector));
            };
            Action<PFNode> forNeighbor = (neighbor) =>
            {
                if (!closed.Contains(neighbor))
                {
                    if (!open.Contains(neighbor)) open.AddLast(neighbor);
                    int tentativeGScore = currentNode.gScore + dist(currentNode, neighbor);
                    if (tentativeGScore < neighbor.gScore) neighbor.PathFrom(currentNode, tentativeGScore, estimate(neighbor));
                }
            };
            if (currentNode.above != null) forNeighbor(currentNode.above);
            if (currentNode.below != null) forNeighbor(currentNode.below);
            if (currentNode.left != null) forNeighbor(currentNode.left);
            if (currentNode.right != null) forNeighbor(currentNode.right);
        }
        return false;
    }

    /// <summary>
    /// This takes the final node and reconstructs a path to the origin, ensuring that
    /// it remains within the given limit.
    /// </summary>
    private bool ValidatePathLength (PFNode finalNode, float limit)
    {
        // The number of path nodes between line segments.
        const int segmentLength = 4;
        PFNode currentNode = finalNode;
        LinkedList<Segment> segments = new LinkedList<Segment>();
        while (currentNode.cameFrom != null)
        {
            PFNode first = currentNode;
            PFNode last = currentNode;
            for (int i = 0; i < segmentLength; i++)
            {
                if (currentNode.cameFrom == null) break;
                else
                {
                    currentNode = currentNode.cameFrom;
                    last = currentNode;
                }
            }
            segments.AddLast(new Segment(first, last));
        }
        LinkedListNode<Segment> currentSegment = segments.First;
        float dist = currentSegment.Value.length;
        while (currentSegment.Next != null)
        {
            dist += currentSegment.Value.length;
            if (currentSegment.Next != null) currentSegment = currentSegment.Next;
        }
        return dist <= limit;
    }
}
