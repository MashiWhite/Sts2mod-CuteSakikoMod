using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Map;

public class ScaledActMap : ActMap
{
    // 原版约束（完全照搬 StandardActMap）
    private static readonly HashSet<MapPointType> _lowerRestrictions = new() { MapPointType.RestSite, MapPointType.Elite };
    private static readonly HashSet<MapPointType> _upperRestrictions = new() { MapPointType.RestSite };
    private static readonly HashSet<MapPointType> _parentRestrictions = new() { MapPointType.Elite, MapPointType.RestSite, MapPointType.Treasure, MapPointType.Shop };
    private static readonly HashSet<MapPointType> _childRestrictions = new() { MapPointType.Elite, MapPointType.RestSite, MapPointType.Treasure, MapPointType.Shop };
    private static readonly HashSet<MapPointType> _siblingRestrictions = new() { MapPointType.RestSite, MapPointType.Monster, MapPointType.Unknown, MapPointType.Elite, MapPointType.Shop };

    private const int MapWidth = 7;   // 恢复原版宽度
    private readonly int _mapLength;
    private readonly Rng _rng;
    private MapPoint?[,] _grid;
    private readonly MapPointTypeCounts _pointTypeCounts;

    public override MapPoint BossMapPoint { get; }
    public override MapPoint StartingMapPoint { get; }
    public override MapPoint? SecondBossMapPoint { get; }
    protected override MapPoint?[,] Grid => _grid;

    public ScaledActMap(RunState runState, ActMap originalMap, double scaleFactor)
    {
        // 和原版一样用 act 索引生成种子
        _rng = new Rng(runState.Rng.Seed, $"scaled_{scaleFactor}_{runState.CurrentActIndex + 1}_map");
        bool hasSecondBoss = runState.Act.HasSecondBoss;

        int originalValidRows = originalMap.GetRowCount() - 2;
        _mapLength = Math.Max(3, (int)Math.Round(originalValidRows * scaleFactor));

        // 只缩放未知和休息数量（精英/商店数量保持不变）
        var baseCounts = runState.Act.GetMapPointTypes(_rng);
        _pointTypeCounts = new MapPointTypeCounts(
            Math.Max(0, (int)Math.Round(baseCounts.NumOfUnknowns * scaleFactor)),
            Math.Max(1, (int)Math.Round(baseCounts.NumOfRests * scaleFactor))
        );

        _grid = new MapPoint[MapWidth, _mapLength + 2]; // +2 是起始行和 Boss 行
        StartingMapPoint = new MapPoint(MapWidth / 2, 0) { PointType = MapPointType.Ancient };
        BossMapPoint = new MapPoint(MapWidth / 2, _mapLength + 1) { PointType = MapPointType.Boss };
        if (hasSecondBoss)
            SecondBossMapPoint = new MapPoint(MapWidth / 2, _mapLength + 2) { PointType = MapPointType.Boss };

        // 1. 先创建固定行（全部铺满）
        CreateFixedRows();

        // 2. 生成随机路径（路径上的格子才生成节点，其余留空）
        GeneratePaths();

        // 3. 确保第一行和最后一行全部连接
        FinalizeConnections();

        // 4. 分配类型
        AssignTypes();

        // 5. 后处理
        ApplyPostProcessing();
    }

    private void CreateFixedRows()
    {
        // 第一行 (row = 1)：全战斗
        for (int c = 0; c < MapWidth; c++)
        {
            var p = GetOrCreatePoint(c, 1);
            p.PointType = MapPointType.Monster;
            p.CanBeModified = false;
        }

        // 精英/宝箱行（倒数第7行），若行号>1 则全铺
        int eliteRow = _mapLength - 6;
        if (eliteRow > 1)
        {
            for (int c = 0; c < MapWidth; c++)
            {
                var p = GetOrCreatePoint(c, eliteRow);
                p.PointType = MapPointType.Elite; // 你也可改为 Treasure
                p.CanBeModified = false;
            }
        }

        // 最后一行 (row = _mapLength)：全火堆
        for (int c = 0; c < MapWidth; c++)
        {
            var p = GetOrCreatePoint(c, _mapLength);
            p.PointType = MapPointType.RestSite;
            p.CanBeModified = false;
        }
    }

    private void GeneratePaths()
    {
        // 生成 4 条路径（和原版类似，但起点可以来自第一行的任意列）
        int pathCount = 4;
        for (int i = 0; i < pathCount; i++)
        {
            // 随机选择一个第一行的节点作为起点
            var startCandidates = Enumerable.Range(0, MapWidth)
                .Select(c => GetPoint(c, 1))
                .Where(p => p != null)
                .ToList();
            if (startCandidates.Count == 0) break;
            var start = startCandidates[_rng.NextInt(0, startCandidates.Count)];
            // 避免完全相同的起点被重复添加（如果已经存在就跳过，因为我们只要路径延伸）
            if (startMapPoints.Contains(start))
                continue;
            startMapPoints.Add(start);
            BuildPath(start);
        }
    }

    private void BuildPath(MapPoint current)
    {
        while (current.coord.row < _mapLength)
        {
            int nextCol = PickNextColumn(current);
            // 获取或创建下一行的节点（可能为 null，这时创建新节点）
            var next = GetOrCreatePoint(nextCol, current.coord.row + 1);
            current.AddChildPoint(next);
            current = next;
        }
    }

    private int PickNextColumn(MapPoint current)
    {
        int col = current.coord.col;
        int minCol = Math.Max(0, col - 1);
        int maxCol = Math.Min(col + 1, MapWidth - 1);
        var candidates = new List<int> { col };
        if (col > 0) candidates.Add(col - 1);
        if (col < MapWidth - 1) candidates.Add(col + 1);
        candidates.StableShuffle(_rng);
        foreach (int c in candidates)
        {
            if (!HasCross(current, c))
                return c;
        }
        return col; // fallback
    }

    private bool HasCross(MapPoint current, int targetX)
    {
        if (targetX == current.coord.col) return false;
        var sibling = _grid[targetX, current.coord.row];
        if (sibling == null) return false;
        int delta = targetX - current.coord.col;
        return sibling.Children.Any(child => child.coord.col - sibling.coord.col == -delta);
    }

    private void FinalizeConnections()
    {
        // 最后一行全部连接 Boss
        for (int c = 0; c < MapWidth; c++)
        {
            var last = GetPoint(c, _mapLength);
            if (last != null)
                last.AddChildPoint(BossMapPoint);
        }
        if (SecondBossMapPoint != null)
            BossMapPoint.AddChildPoint(SecondBossMapPoint);

        // 起始点连接第一行所有节点
        for (int c = 0; c < MapWidth; c++)
        {
            var first = GetPoint(c, 1);
            if (first != null)
                StartingMapPoint.AddChildPoint(first);
        }
    }

    private MapPoint GetOrCreatePoint(int col, int row)
    {
        var existing = GetPoint(col, row);
        if (existing != null) return existing;
        var point = new MapPoint(col, row);
        _grid[col, row] = point;
        return point;
    }

    // ---------- 类型分配 ----------
    private void AssignTypes()
    {
        var queue = new Queue<MapPointType>();
        for (int i = 0; i < _pointTypeCounts.NumOfRests; i++) queue.Enqueue(MapPointType.RestSite);
        for (int i = 0; i < _pointTypeCounts.NumOfShops; i++) queue.Enqueue(MapPointType.Shop);
        for (int i = 0; i < _pointTypeCounts.NumOfElites; i++) queue.Enqueue(MapPointType.Elite);
        for (int i = 0; i < _pointTypeCounts.NumOfUnknowns; i++) queue.Enqueue(MapPointType.Unknown);

        AssignRemaining(queue);

        // 剩下未分配的填战斗
        foreach (var pt in GetAllMapPoints().Where(p => p.PointType == MapPointType.Unassigned))
            pt.PointType = MapPointType.Monster;
    }

    private void AssignRemaining(Queue<MapPointType> queue)
    {
        for (int attempt = 0; attempt < 3 && queue.Count > 0; attempt++)
        {
            var unassigned = GetAllMapPoints()
                .Where(p => p.PointType == MapPointType.Unassigned && p.CanBeModified)
                .ToList();
            unassigned.StableShuffle(_rng);
            foreach (var pt in unassigned)
            {
                if (queue.Count == 0) break;
                var type = GetNextValid(queue, pt);
                if (type != MapPointType.Unassigned) pt.PointType = type;
            }
        }
    }

    private MapPointType GetNextValid(Queue<MapPointType> queue, MapPoint point)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            var type = queue.Dequeue();
            if (IsValidPointType(type, point)) return type;
            queue.Enqueue(type);
        }
        return MapPointType.Unassigned;
    }

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
        if (!_siblingRestrictions.Contains(type)) return true;
        var siblings = point.parents.SelectMany(p => p.Children).Where(x => x != point);
        return !siblings.Any(p => p.PointType == type);
    }

    private void ApplyPostProcessing()
    {
        _grid = MapPostProcessing.CenterGrid(_grid);
        _grid = MapPostProcessing.SpreadAdjacentMapPoints(_grid);
        _grid = MapPostProcessing.StraightenPaths(_grid);
    }
}