using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class RelicSwapCoordinator
{
    private static readonly Dictionary<ulong, List<string>> _choices = new();
    private static readonly Dictionary<ulong, List<string>> _pendingBeforeCollect = new();
    private static TaskCompletionSource? _tcs;
    private static int _expectedCount;
    private static bool _collecting;
    private static bool _registered;

    // ★ 执行防重
    private static bool _executeReceived;

    public static void Init(INetGameService netService)
    {
        if (_registered) return;
        netService.RegisterMessageHandler(
            new MessageHandlerDelegate<RelicSwapChoiceMessage>(OnChoiceMessage));
        netService.RegisterMessageHandler(
            new MessageHandlerDelegate<RelicSwapExecuteMessage>(OnExecuteMessage));
        _registered = true;
    }

    public static void ResetForNewExchange()
    {
        _choices.Clear();
        _pendingBeforeCollect.Clear();
        _expectedCount = 0;
        _tcs = null;
        _collecting = false;
        _executeReceived = false;
    }

    public static void BroadcastChoice(ulong netId, List<string> relicIds)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        netService.SendMessage(new RelicSwapChoiceMessage
        {
            SourceNetId = netId,
            RelicIds = relicIds,
        });
    }

    public static async Task<Dictionary<ulong, List<string>>> CollectAllAsync(
        ulong localNetId, List<string> localRelicIds,
        int expectedCount, int timeoutMs = 20000)
    {
        _choices[localNetId] = localRelicIds;
        _expectedCount = expectedCount;
        _tcs = new TaskCompletionSource();
        _collecting = true;

        foreach (var (netId, ids) in _pendingBeforeCollect)
            if (!_choices.ContainsKey(netId))
                _choices[netId] = ids;
        _pendingBeforeCollect.Clear();

        if (_choices.Count < _expectedCount)
        {
            var timeout = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(_tcs.Task, timeout);
            if (completed != _tcs.Task)
                Entry.Logger.Warn(
                    $"[RelicSwap] 收集选择超时（{_choices.Count}/{_expectedCount}），将按已收到的继续");
        }

        _collecting = false;
        var result = new Dictionary<ulong, List<string>>(_choices);
        _choices.Clear();
        _tcs = null;
        return result;
    }

    // ★ host 调用：本地执行 + 广播（SendMessage 会发给所有客户端，包括自己）
    public static async Task ExecuteAndBroadcast(List<string> moveStrings)
    {
        // 1. 本地执行
        await ExecuteMoves(moveStrings);
        _executeReceived = true;

        // 2. 广播
        var netService = RunManager.Instance?.NetService;
        netService?.SendMessage(new RelicSwapExecuteMessage { Moves = moveStrings });
    }

    private static void OnChoiceMessage(RelicSwapChoiceMessage msg, ulong senderId)
        => OnChoiceReceived(msg.SourceNetId, msg.RelicIds);

    private static void OnChoiceReceived(ulong netId, List<string> relicIds)
    {
        if (!_collecting)
        {
            _pendingBeforeCollect[netId] = relicIds;
            return;
        }

        if (_choices.ContainsKey(netId)) return;
        _choices[netId] = relicIds;

        if (_choices.Count >= _expectedCount)
            _tcs?.TrySetResult();
    }

    private static async void OnExecuteMessage(RelicSwapExecuteMessage msg, ulong senderId)
    {
        // 防重（host 已经本地执行过）
        if (_executeReceived) return;
        _executeReceived = true;

        try
        {
            await ExecuteMoves(msg.Moves);
        }
        catch (Exception ex)
        {
            // async void 里异常不会有人接，必须自己兜住；失败后复位标志，允许重发
            _executeReceived = false;
            Entry.Logger.Warn($"[RelicSwap] 执行交换失败: {ex}");
        }
    }

    private static async Task ExecuteMoves(List<string> moveStrings)
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return;

        var players = runState.Players.ToList();

        // 解析 moves
        var parsedMoves = new List<(RelicModel Relic, MegaCrit.Sts2.Core.Entities.Players.Player Target)>();
        foreach (var s in moveStrings)
        {
            var parts = s.Split('|');
            if (parts.Length != 3) continue;
            if (!ulong.TryParse(parts[0], out var ownerNetId)) continue;
            var relicId = parts[1];
            if (!ulong.TryParse(parts[2], out var targetNetId)) continue;

            var owner = players.FirstOrDefault(p => p.NetId == ownerNetId);
            if (owner == null) continue;
            var relic = owner.Relics.FirstOrDefault(r => r.Id.Entry == relicId);
            if (relic == null) continue;
            var target = players.FirstOrDefault(p => p.NetId == targetNetId);
            if (target == null) continue;

            parsedMoves.Add((relic, target));
        }

        // 先全部 detach，再全部 attach
        foreach (var (relic, _) in parsedMoves)
            await RelicTransfer.RemoveAndDetach(relic);

        foreach (var (relic, target) in parsedMoves)
            await RelicTransfer.AttachWithoutObtained(relic, target);
    }
}