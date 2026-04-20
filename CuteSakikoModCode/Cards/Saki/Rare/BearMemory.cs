using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class BearMemory() : CuteSakikoModCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{

    // 动态变量：能力层数（基础1层）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BearMemoryPower>(1m)
    ];

    // 悬停提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BearMemoryPower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var powerAmount = 1; // 升级只改变费用，能力层数不变
        await PowerCmd.Apply<BearMemoryPower>(Owner.Creature, powerAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用从2减为1
        EnergyCost.UpgradeBy(-1);
    }
}