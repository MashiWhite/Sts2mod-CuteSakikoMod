using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class UninvitedGuest : CuteRanaCard
{
    [SavedProperty]
    private bool HasIncreasedCost { get; set; }  // 改为属性

    public UninvitedGuest() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(10m, ValueProp.Move)
    };
    
    // 如果基类有 ShouldGlowGold，用这个；否则删除
    protected override bool ShouldGlowGoldInternal => !HasIncreasedCost;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!HasIncreasedCost)
        {
            // 确保 SetThisCombat 方法存在，否则改用 AddThisCombat(1) 或直接赋值
            EnergyCost.SetThisCombat(1);
            HasIncreasedCost = true;
        }

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
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}