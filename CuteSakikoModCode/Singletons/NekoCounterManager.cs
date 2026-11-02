using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;
using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

/// <summary>
///     战斗内「打出猫咪」计数器（HookedSingletonModel，战斗作用域）。
///     同时维护两个计数，语义不同、不能混用：
///       · 本回合（GetCountThisTurn）→ 狂喵病爆发 MeowRampage（本回合每张猫 +N）
///       · 本场战斗（GetCount）→ 猫抓完就撒盐 SaltAfterScratch（本场每张猫 +N）
///     ⚠️ 计数按玩家 NetId 分桶，联机下各自独立。
///     ⚠️ 回合计数用「懒重置」：读的时候比对 RoundNumber，变了就清零。
///        这套写法照搬 ChordNoteSystem.GetNotesGainedThisTurn —— 不能只在 AfterCardPlayed 里加，
///        因为那张牌可能整回合都没被打出，读的时候必须自己发现回合已经变了。
/// </summary>
[RegisterSingleton]
public sealed class NekoCounterManager : HookedSingletonModel
{
    public static NekoCounterManager Instance => ModelDb.Singleton<NekoCounterManager>();

    /// <summary>本场战斗累计打出猫咪数。</summary>
    private readonly Dictionary<ulong, int> _nekoPlayed = new();

    /// <summary>本回合打出猫咪数。</summary>
    private readonly Dictionary<ulong, int> _nekoThisTurn = new();

    /// <summary>各玩家上次结算到的回合号，用于懒重置本回合计数。</summary>
    private readonly Dictionary<ulong, int> _lastRound = new();

    public NekoCounterManager() : base(HookType.Combat)
    {
    }

    /// <summary>读取某玩家本场战斗打出过的猫咪数（不重置，供成长型卡）。</summary>
    public int GetCount(Player player)
        => _nekoPlayed.TryGetValue(player.NetId, out var n) ? n : 0;

    /// <summary>读取某玩家本回合打出过的猫咪数；回合号变了自动清零。</summary>
    public int GetCountThisTurn(Player player)
    {
        var combat = player.Creature?.CombatState;
        if (combat == null) return 0;

        if (!_lastRound.TryGetValue(player.NetId, out var last) || last != combat.RoundNumber)
        {
            _lastRound[player.NetId] = combat.RoundNumber;
            _nekoThisTurn[player.NetId] = 0;
        }

        return _nekoThisTurn.TryGetValue(player.NetId, out var n) ? n : 0;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var player = card.Owner;
        if (player == null) return;
        if (!card.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword())) return;

        // 先走一次本回合读取，借它的懒重置把跨回合的旧计数清掉
        _ = GetCountThisTurn(player);

        _nekoPlayed[player.NetId] = GetCount(player) + 1;
        _nekoThisTurn[player.NetId] = (_nekoThisTurn.TryGetValue(player.NetId, out var t) ? t : 0) + 1;
        await Task.CompletedTask;
    }
}
