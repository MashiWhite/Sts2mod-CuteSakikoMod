
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Map;

public class ScaledActMap : ActMap
{
    // 与原版完全一致的约束规则
    private static readonly HashSet<MapPointType> _lowerRestrictions = new()
        { MapPointType.RestSite, MapPointType.Elite };
    private static readonly HashSet<MapPointType> _upperRestrictions = new()
        { MapPointType.RestSite };
    private static readonly HashSet<MapPointType> _parentRestrictions = new()
        { MapPointType.Elite, MapPointType.RestSite, MapPointType.Treasure, MapPointType.Shop };
    private static readonly HashSet<MapPointType> _childRestrictions = new()
        { MapPointType.Elite, MapPointType.RestSite, MapPointType.Treasure, MapPointType.Shop };
    private static readonly HashSet<MapPointType> _siblingRestrictions = new()
        { MapPointType.RestSite, MapPointType.Monster, MapPointType.Unknown, MapPointType.Elite, MapPointType.Shop };

    private const int MapWidth = 7;
    private const int PathCount = 7;   // 与原版相同，保证覆盖所有列

    private readonly int _mapLength;
    private readonly Rng _rng;
    private readonly MapPointTypeCounts _pointTypeCounts;
    private readonly MapPoint?[,] _grid;

    public override MapPoint BossMapPoint { get; }
    public override MapPoint StartingMapPoint { get; }
    public override MapPoint? SecondBossMapPoint { get; }
    protected override MapPoint?[,] Grid => _grid;

    public ScaledActMap(RunState runState, ActMap originalMap, double scaleFactor)
    {
        _rng = new Rng(runState.Rng.Seed, $"scaled_{scaleFactor}_{runState.CurrentActIndex + 1}_map");

        bool hasSecondBoss = runState.Act.HasSecondBoss;
    
        // ---------- 新计算逻辑 ----------
        // 原地图的房间行数 = originalMap.GetRowCount() - 1
        int totalRooms = originalMap.GetRowCount() - 2;
        int fixedRows = 3;                     // 第1行、宝箱行、最后一行
        int randomRows = totalRooms - fixedRows;
        int scaledRandom = Math.Max(0, (int)Math.Round(randomRows * scaleFactor));
        _mapLength = fixedRows + scaledRandom; // 至少保持3行固定行
    
        // 获取原图类型数量，按比例缩放（只缩放随机部分对应的数量）
        var baseCounts = runState.Act.GetMapPointTypes(_rng);
    
        int scaleCount(int original) => Math.Max(0, (int)Math.Round(original * scaleFactor));
    
        _pointTypeCounts = new MapPointTypeCounts(
            scaleCount(baseCounts.NumOfUnknowns),
            scaleCount(baseCounts.NumOfRests))
        {
            NumOfElites = scaleCount(baseCounts.NumOfElites),   // 精英也按比例缩放（若想保留原数可改为 baseCounts.NumOfElites）
            PointTypesThatIgnoreRules = baseCounts.PointTypesThatIgnoreRules
        };
        // ---------- 新计算结束 ----------

        _grid = new MapPoint[MapWidth, _mapLength + 2];
        StartingMapPoint = new MapPoint(MapWidth / 2, 0);
        BossMapPoint = new MapPoint(MapWidth / 2, _mapLength + 1);
        if (hasSecondBoss)
            SecondBossMapPoint = new MapPoint(MapWidth / 2, _mapLength + 2);

        GenerateMap();
        AssignPointTypes();
        ApplyPostProcessing();
    }

    // ---------- 路径生成 ----------
    private void GenerateMap()
    {
        for (int i = 0; i < PathCount; i++)
        {
            MapPoint start = GetOrCreatePoint(_rng.NextInt(0, MapWidth), 1);
            if (i == 1)
            {
                while (startMapPoints.Contains(start))
                    start = GetOrCreatePoint(_rng.NextInt(0, MapWidth), 1);
            }
            startMapPoints.Add(start);
            PathGenerate(start);
        }

        // 最后一行所有节点连Boss
        ForEachInRow(_grid, _mapLength, x => x.AddChildPoint(BossMapPoint));
        if (SecondBossMapPoint != null)
            BossMapPoint.AddChildPoint(SecondBossMapPoint);

        // 起始点连第一行所有节点
        ForEachInRow(_grid, 1, x => StartingMapPoint.AddChildPoint(x));
    }

    private void PathGenerate(MapPoint current)
    {
        // 原来：current.coord.row < _mapLength - 1
        while (current.coord.row < _mapLength)
        {
            MapCoord nextCoord = GenerateNextCoord(current);
            MapPoint next = GetOrCreatePoint(nextCoord.col, nextCoord.row);
            current.AddChildPoint(next);
            current = next;
        }
    }

    private MapCoord GenerateNextCoord(MapPoint current)
    {
        int col = current.coord.col;
        int minCol = Math.Max(0, col - 1);
        int maxCol = Math.Min(col + 1, MapWidth - 1);

        List<int> offsets = new() { -1, 0, 1 };
        offsets.StableShuffle(_rng);

        foreach (int offset in offsets)
        {
            int targetCol = col + offset;
            if (targetCol < minCol || targetCol > maxCol)
                continue;

            if (!HasInvalidCrossover(current, targetCol))
                return new MapCoord { col = targetCol, row = current.coord.row + 1 };
        }

        throw new InvalidOperationException("Cannot find next node: no valid path found.");
    }

    private bool HasInvalidCrossover(MapPoint current, int targetX)
    {
        int delta = targetX - current.coord.col;
        if (delta == 0)
            return false;

        MapPoint? sibling = _grid[targetX, current.coord.row];
        if (sibling == null)
            return false;

        return sibling.Children.Any(child => child.coord.col - sibling.coord.col == -delta);
    }

    // ---------- 辅助方法 ----------
    private static void ForEachInRow(MapPoint?[,] grid, int row, Action<MapPoint> action)
    {
        for (int c = 0; c < grid.GetLength(0); c++)
        {
            MapPoint? point = grid[c, row];
            if (point != null)
                action(point);
        }
    }

    private MapPoint GetOrCreatePoint(int col, int row)
    {
        MapPoint? existing = _grid[col, row];
        if (existing != null)
            return existing;

        MapPoint point = new(col, row);
        _grid[col, row] = point;
        return point;
    }

    // ---------- 类型分配 ----------
    private void AssignPointTypes()
    {
        // 第一行 → 怪物
        ForEachInRow(_grid, 1, p =>
        {
            p.PointType = MapPointType.Monster;
            p.CanBeModified = false;
        });

        // 最后一行 → 篝火
        ForEachInRow(_grid, _mapLength, p =>
        {
            p.PointType = MapPointType.RestSite;
            p.CanBeModified = false;
        });

        // 宝箱动态放在中间行（向上取整）
        int treasureRow = (int)Math.Ceiling((1.0 + _mapLength) / 2.0);
        // 确保宝箱行不会与第一行/最后一行重叠（当地图极短时手动保护）
        if (treasureRow <= 1) treasureRow = 2;
        if (treasureRow >= _mapLength) treasureRow = _mapLength - 1;

        ForEachInRow(_grid, treasureRow, p =>
        {
            p.PointType = MapPointType.Treasure;
            p.CanBeModified = false;
        });

        // ---------- 随机类型分配（保持不变）----------
        Queue<MapPointType> queue = new();
        for (int i = 0; i < _pointTypeCounts.NumOfRests; i++)
            queue.Enqueue(MapPointType.RestSite);
        for (int i = 0; i < _pointTypeCounts.NumOfShops; i++)
            queue.Enqueue(MapPointType.Shop);
        for (int i = 0; i < _pointTypeCounts.NumOfElites; i++)
            queue.Enqueue(MapPointType.Elite);
        for (int i = 0; i < _pointTypeCounts.NumOfUnknowns; i++)
            queue.Enqueue(MapPointType.Unknown);

        AssignRemainingTypesToRandomPoints(queue);

        // 剩余未分配的 → 怪物
        foreach (MapPoint p in GetAllMapPoints().Where(p => p.PointType == MapPointType.Unassigned))
            p.PointType = MapPointType.Monster;

        BossMapPoint.PointType = MapPointType.Boss;
        StartingMapPoint.PointType = MapPointType.Ancient;
        if (SecondBossMapPoint != null)
            SecondBossMapPoint.PointType = MapPointType.Boss;
    }

    private void AssignRemainingTypesToRandomPoints(Queue<MapPointType> queue)
    {
        for (int attempt = 0; attempt < 3 && queue.Count > 0; attempt++)
        {
            List<MapPoint> unassigned = GetAllMapPoints()
                .Where(p => p.PointType == MapPointType.Unassigned && p.CanBeModified)
                .ToList();
            unassigned.StableShuffle(_rng);

            foreach (MapPoint point in unassigned)
            {
                if (queue.Count == 0) break;
                MapPointType type = GetNextValidPointType(queue, point);
                if (type != MapPointType.Unassigned)
                    point.PointType = type;
            }
        }
    }

    private MapPointType GetNextValidPointType(Queue<MapPointType> queue, MapPoint point)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            MapPointType type = queue.Dequeue();
            if (_pointTypeCounts.ShouldIgnoreMapPointRulesForMapPointType(type) || IsValidPointType(type, point))
                return type;
            queue.Enqueue(type);
        }
        return MapPointType.Unassigned;
    }

    // ---------- 规则检查 ----------
    private bool IsValidPointType(MapPointType type, MapPoint point)
    {
        return IsValidForUpper(type, point)
               && IsValidForLower(type, point)
               && IsValidWithParents(type, point)
               && IsValidWithChildren(type, point)
               && IsValidWithSiblings(type, point);
    }

    private bool IsValidForUpper(MapPointType type, MapPoint point) =>
        point.coord.row >= _mapLength - 2 ? !_upperRestrictions.Contains(type) : true;

    private static bool IsValidForLower(MapPointType type, MapPoint point) =>
        point.coord.row < 6 ? !_lowerRestrictions.Contains(type) : true;

    private static bool IsValidWithParents(MapPointType type, MapPoint point) =>
        !_parentRestrictions.Contains(type) || !point.parents.Concat(point.Children).Any(p => p.PointType == type);

    private static bool IsValidWithChildren(MapPointType type, MapPoint point) =>
        !_childRestrictions.Contains(type) || !point.Children.Any(p => p.PointType == type);

    private static bool IsValidWithSiblings(MapPointType type, MapPoint point)
    {
        if (!_siblingRestrictions.Contains(type))
            return true;
        IEnumerable<MapPoint> siblings = point.parents.SelectMany(p => p.Children).Where(x => x != point);
        return !siblings.Any(p => p.PointType == type);
    }

    // ---------- 后处理 ----------
    private void ApplyPostProcessing()
    {
        MapPoint?[,] centered = MapPostProcessing.CenterGrid(_grid);
        centered = MapPostProcessing.SpreadAdjacentMapPoints(centered);
        centered = MapPostProcessing.StraightenPaths(centered);
        // 将处理后的结果写回 _grid（因为 MapPostProcessing 返回新数组）
        Array.Copy(centered, _grid, centered.Length);
    }
}