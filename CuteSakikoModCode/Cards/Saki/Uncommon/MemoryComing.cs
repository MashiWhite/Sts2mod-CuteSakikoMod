using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class MemoryComing() : CustomCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) // 费用改为1
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 动态变量：能力层数（基础1层，升级后为2层，用于描述）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MemoryComingPower>(1m)
    ];

    // 悬停提示，显示能力详情
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MemoryComingPower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 根据升级状态决定施加层数：未升级1层，升级2层
        var amount = IsUpgraded ? 2 : 1;
        await PowerCmd.Apply<MemoryComingPower>(Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：能力层数从1提升到2，费用不变
        DynamicVars["MemoryComingPower"].UpgradeValueBy(1m);
    }
}