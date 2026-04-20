using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class Masquerade : CuteSakikoModCard
{
    public Masquerade() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }



    protected override IEnumerable<DynamicVar> CanonicalVars => System.Array.Empty<DynamicVar>();
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MasqueradePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 施加 MasqueradePower 到自身（用于记录和恢复）
        var power = await PowerCmd.Apply<MasqueradePower>(Owner.Creature, 1, Owner.Creature, this);
        // 执行移除所有生物能力的操作
        await power.RemoveAllPowers();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}