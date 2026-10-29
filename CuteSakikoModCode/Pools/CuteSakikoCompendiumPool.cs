using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Pools;

/// <summary>
///     图鉴专用聚合池基类：把多个子池的卡合并到一个图鉴入口里显示。
///     ⚠️ 不参与掉落/奖励——没有 [RegisterSharedCardPool]，不会进 ModelDb.AllCardPools，
///     只有 Entry 里注册的图鉴筛选器会引用它。
///     ⚠️ 直接继承 CardPoolModel 而不是 TypeListCardPoolModel，
///     因为后者的 GenerateAllCards() 是 sealed，没法按子池拼卡。
/// </summary>
public abstract class CuteSakikoCompendiumAggregatePool : CardPoolModel
{
    private static readonly Material? _poolFrameMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(0.5373f, 0.7725f, 0.9294f, 0.8f);

    /// <summary>参与本聚合池的子池类型。</summary>
    protected abstract IEnumerable<Type> SourcePools { get; }

    public override string EnergyColorName => "CuteSakikoModCardBlue";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override bool IsColorless => true;
    public override Color EnergyOutlineColor => new("#1B75B1");
    public override Color DeckEntryCardColor => new("#C4E2F6");

    protected override CardModel[] GenerateAllCards()
    {
        var cards = new List<CardModel>();
        var seen = new HashSet<ModelId>();

        foreach (var poolType in SourcePools)
        {
            var pool = ModelDb.AllCardPools.FirstOrDefault(p => p.GetType() == poolType);
            if (pool == null) continue;

            foreach (var card in pool.AllCards)
                if (seen.Add(card.Id))
                    cards.Add(card);
        }

        return cards.ToArray();
    }
}

/// <summary>彩蛋图鉴入口：只包含彩蛋卡池。</summary>
public sealed class CuteSakikoCompendiumEggPool : CuteSakikoCompendiumAggregatePool
{
    public override string Title => "CuteSakikoCompendiumEgg";
    protected override IEnumerable<Type> SourcePools => [typeof(CuteSakikoEggCardPool)];
}

/// <summary>衍生/杂项图鉴入口：状态、诅咒、Token、事件卡。</summary>
public sealed class CuteSakikoCompendiumJunkPool : CuteSakikoCompendiumAggregatePool
{
    public override string Title => "CuteSakikoCompendiumJunk";

    protected override IEnumerable<Type> SourcePools =>
    [
        typeof(CuteSakikoStatusCardPool),
        typeof(CuteSakikoCurseCardPool),
        typeof(CuteSakikoTokenCardPool),
        typeof(CuteSakikoEventCardPool)
    ];
}