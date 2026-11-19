using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;

/// <summary>种植卡的全局阶段编号；其他效果可用它和 <see cref="PlantingCardBase.Stage" /> 比较。</summary>
public static class PlantStages
{
    public const int Seedling = 1;
    public const int Sprout = 2;
    public const int Bloom = 3;
    public const int Fruit = 4;
    public const int Mature = 5;
}

/// <summary>
///     种植卡基类：放在种植牌堆里，随玩家回合成长；卡面关键词行、伤害、卡图都随阶段变化。
///     其他效果可以查询 <see cref="Stage" />、<see cref="IsFullyGrown" />，
///     或用 <see cref="CardPiles.PlantingCardPile.GetPlantsAtStage" /> 按阶段筛选。
/// </summary>
public abstract class PlantingCardBase(int cost, CardType type, CardRarity rarity, TargetType target)
    : CuteMutsumiCard(cost, type, rarity, target), IMutsumiArchetypeCard
{
    /// <summary>种植系列统一归黄瓜睦流派（小睦专属），Mortis 奖励不出现。</summary>
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    private static readonly string[] DefaultStageNames = ["幼苗", "抽蔓", "开花", "结果", "成熟"];

    private int _stage = PlantStages.Seedling;

    /// <summary>当前阶段（从 1 开始，见 <see cref="PlantStages" />）。</summary>
    public int Stage => _stage;

    /// <summary>这张卡的最大阶段数；黄瓜为 2，基类默认预留 5 阶段框架。</summary>
    public virtual int MaxStage => PlantStages.Mature;

    public bool IsFullyGrown => _stage >= MaxStage;

    /// <summary>种植时的回合数，由 <see cref="Singletons.PlantingPileManager" /> 维护。</summary>
    internal int PlantedTurn { get; set; } = -1;

    /// <summary>阶段显示名；子类可覆盖成自己的阶段名表。</summary>
    protected virtual IReadOnlyList<string> StageNames => DefaultStageNames;

    public string StageName => StageNames[Math.Clamp(_stage - 1, 0, StageNames.Count - 1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CutesakiKeywords.Planting.GetModCardKeyword(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(DamageForStage(_stage), ValueProp.Move);
            yield return new StringVar("StageName", StageName);
        }
    }

    /// <summary>该阶段的基础伤害；升级加成由子类在 DamageForStage 里自己算。</summary>
    protected virtual decimal DamageForStage(int stage) => 0m;

    /// <summary>该阶段的攻击段数（命中次数）；默认 1，黄瓜按阶段/升级增加。</summary>
    protected virtual int HitsForStage(int stage) => 1;

    /// <summary>推进一个阶段（满阶段后无效），并同步伤害、阶段名、卡图与提示。</summary>
    public void AdvanceStage()
    {
        if (IsFullyGrown) return;
        _stage = Math.Min(_stage + 1, MaxStage);
        SyncStageVars();
        RefreshVisuals();
    }

    protected void SyncStageVars()
    {
        DynamicVars.Damage.BaseValue = DamageForStage(_stage);
        if (DynamicVars.TryGetValue("Repeat", out var repeatVar) && repeatVar is RepeatVar repeats)
            repeats.BaseValue = HitsForStage(_stage);
        if (DynamicVars.TryGetValue("StageName", out var stageVar) && stageVar is StringVar stringVar)
            stringVar.StringValue = StageName;
        SyncExtraVars();
    }

    /// <summary>子类可在这里同步阶段相关的额外变量（例如茄子每阶段的重放次数）。</summary>
    protected virtual void SyncExtraVars()
    {
    }

    /// <summary>阶段变化后刷新卡面（描述里的 {StageName}/{Damage}、关键词行、卡图）。</summary>
    protected void RefreshVisuals()
    {
        // 0.107 没有 InvokeKeywordsChanged；UpdateVisuals 本身会重建描述
#if STS2_AT_LEAST_0_113_0
        InvokeKeywordsChanged();
#endif
        NCard.FindOnTable(this)?.UpdateVisuals(Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
    }

    /// <summary>
    ///     阶段卡图：优先读 images/cards/&lt;类名&gt;_&lt;阶段&gt;.png，
    ///     没有就回退到 RitsuLib 内嵌占位图（返回 null）。
    /// </summary>
    public override string? CustomPortraitPath
    {
        get
        {
            var path = $"res://CuteSakikoMod/images/cards/{GetType().Name}_{_stage}.png";
            return ResourceLoader.Exists(path) ? path : CardModel.MissingPortraitPath;
        }
    }

    /// <summary>悬浮提示里附上「下一阶段」的卡面预览；已满阶段时不显示。</summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (IsCanonical)
            {
                yield return PlantingKeywordTip.Create(null);
                yield break;
            }

            yield return PlantingKeywordTip.Create(Owner);

            var next = _stage + 1;
            if (next > MaxStage) yield break;

            IHoverTip? tip = null;
            try
            {
                var preview = CreatePreviewCard();
                if (preview != null)
                {
                    preview.ApplyPreviewStage(next);
                    tip = HoverTipFactory.FromCard(preview);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"[PlantingCard] 下一阶段预览失败：{ex.Message}");
            }

            if (tip != null) yield return tip;
        }
    }

    protected override void OnUpgrade()
    {
        SyncStageVars();
    }

    /// <summary>子类提供一张用于「下一阶段预览」的临时卡；返回 null 表示不显示预览。</summary>
    protected virtual PlantingCardBase? CreatePreviewCard() => null;

    /// <summary>把预览卡切到指定阶段（不触发卡面刷新，避免动到真实卡）。</summary>
    internal void ApplyPreviewStage(int stage)
    {
        _stage = Math.Clamp(stage, 1, MaxStage);
        SyncStageVars();
#if STS2_AT_LEAST_0_113_0
        InvokeKeywordsChanged();
#endif
    }
}
