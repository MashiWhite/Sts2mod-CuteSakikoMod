using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;

/// <summary>
///     大肚皮软软：食用 1 杯抹茶芭菲，获得 30 点格挡。
///     升级：30 → 36。
/// </summary>
public class SoftBigBelly : CuteRanaCard, CuteRanaCard.IEatParfaitCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Parfait.GetModCardKeyword()];

        public SoftBigBelly() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(30m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword()); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 走 RemoveCharges 才真正扣杯（SimulateParfaitEaten 只记账不扣杯，2026-11-07 修）
        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait != null)
            await MatchaParfait.RemoveCharges(parfait, GetParfaitConsumeCount(), choiceContext);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(6m); // 30 → 36
    }

    /// <summary>食用 1 杯抹茶芭菲。杯数不足时由基类 IsPlayable 拦下，牌不可打出。</summary>
    public int GetParfaitConsumeCount()
    {
        return 1;
    }
}
