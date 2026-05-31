using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class UninvitedGuest : CuteRanaCard
{
    [SavedProperty] private bool _hasIncreasedCost;  // 是否已经增加过费用

    public UninvitedGuest() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(10m, ValueProp.Move)
    };
    
    protected override bool ShouldGlowGoldInternal =>
        // 尚未打出时发光，提示玩家首次打出有额外收益
        !_hasIncreasedCost;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 第一次打出后，将本场战斗后续费用改为1
        if (!_hasIncreasedCost)
        {
            // 当前这张卡费用已经是0（因为基类传入0）
            // 打出后将本场战斗的费用永久设置为1
            EnergyCost.SetThisCombat(1);
            _hasIncreasedCost = true;
        }

        // 造成伤害
        if (cardPlay.Target != null)
        {
            int damage = (int)DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害 +3（10 -> 13）
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}