using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;

/// <summary>
///     茄子：0c 衍生种植技能，5 阶段；打出时把「变身茄子」选中的牌重放 N 次。
///     N = 阶段 - 1 +（茄子+ ? 1 : 0）；幼苗普通版为 0 次，成熟普通版为 4 次。
/// </summary>
public class Eggplant() : PlantingCardBase(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    private static readonly string[] EggplantStageNames = ["茄苗", "抽枝", "开花", "坐果", "成熟"];

    /// <summary>被选中的牌的运行时快照；由 TurnIntoEggplant 写入。</summary>
    public CardModel? SourceCard { get; private set; }

    public override int MaxStage => 5;

    protected override IReadOnlyList<string> StageNames => EggplantStageNames;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
            yield return CutesakiKeywords.Planting.GetModCardKeyword();
        }
    }

    /// <summary>茄子本身没有固定目标；目标类型跟随被选中的牌。</summary>
    public override TargetType TargetType => SourceCard?.TargetType switch
    {
        TargetType.AnyEnemy => TargetType.AnyEnemy,
        TargetType.AnyAlly => TargetType.AnyAlly,
        TargetType.AnyPlayer => TargetType.AnyPlayer,
        _ => TargetType.Self
    };

    public void SetSourceCard(CardModel source) => SourceCard = source;

    protected override decimal DamageForStage(int stage) => 0m;

    private static int PlaysForStage(int stage, bool upgraded)
    {
        return Math.Max(0, stage - 1 + (upgraded ? 1 : 0));
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            foreach (var v in base.CanonicalVars) yield return v;
            yield return new DynamicVar("Plays", 0);
        }
    }

    protected override void SyncExtraVars()
    {
        DynamicVars["Plays"].BaseValue = PlaysForStage(Stage, IsUpgraded);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (SourceCard == null) return;

        var plays = DynamicVars["Plays"].IntValue;
        for (var i = 0; i < plays; i++)
        {
            var clone = SourceCard.CreateClone();
            await CardCmd.AutoPlay(choiceContext, clone, cardPlay.Target);
        }
    }

    /// <summary>下一阶段预览也带上选中的牌，方便看阶段与对应重放次数。</summary>
    protected override PlantingCardBase? CreatePreviewCard()
    {
        var preview = Owner?.RunState.CreateCard<Eggplant>(Owner);
        if (preview != null && SourceCard != null)
            preview.SetSourceCard(SourceCard);
        return preview;
    }

    /// <summary>悬浮提示：下一阶段茄子 + 选中牌的卡面 + 选中牌的附魔/关键词/附加提示。</summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in base.AdditionalHoverTips) yield return tip;

            if (IsCanonical || SourceCard == null) yield break;

            yield return HoverTipFactory.FromCard(SourceCard);
            foreach (var tip in SourceCard.HoverTips) yield return tip;
        }
    }
}
