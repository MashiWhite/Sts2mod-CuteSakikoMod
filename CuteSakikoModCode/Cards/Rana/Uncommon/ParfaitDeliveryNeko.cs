using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

/// <summary>
///     送芭菲的猫：你每食用 1 杯芭菲，就将 N 张猫咪加入手牌（N = 本 Power 层数）。
///     升级：召唤的猫咪改为**猫咪+**（已升级形态），费用不变。
///     2026-11-02 用户改版：原「每打出一张猫咪获得 1 层莱芜、升级降费 2c→1c」
///        → 现「吃芭菲加猫、升级改为召唤猫咪+」。
///     ★ 计数口径：**每杯加层数张**。吃 3 杯、2 层 → 3 × 2 = 6 张。
///       Power 是 PowerStackType.Counter，**可以叠层**（多打几张本卡层数累加）。
///     ⚠️ 描述里的 {ParfaitDeliveryNekoPower:diff()} 必须与本卡声明的 DynamicVar 同名，
///        否则会刷「Localization formatting error: no source extension could handle the selector」。
/// </summary>
public class ParfaitDeliveryNeko : CuteRanaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Parfait.GetModCardKeyword()];

        public ParfaitDeliveryNeko() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ParfaitDeliveryNekoPower", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ParfaitDeliveryNekoPower>(
            choiceContext, Owner.Creature, DynamicVars["ParfaitDeliveryNekoPower"].IntValue, Owner.Creature, this);

        // 把「本卡是否已升级」写到 Power 上：升级过的牌召唤猫咪+。
        // 重复打出时以最后一次为准（牌组里通常只有一张，影响可忽略）。
        var power = Owner.Creature.GetPower<ParfaitDeliveryNekoPower>();
        if (power != null)
            power.SummonUpgraded = IsUpgraded;
    }

    protected override void OnUpgrade()
    {
        // 升级不改费用、不改层数；只让召唤物变成猫咪+（通过 OnPlay 写入 Power.SummonUpgraded）。
    }
}
