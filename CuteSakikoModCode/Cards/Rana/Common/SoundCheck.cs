using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class SoundCheck() : CuteRanaCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };
    
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CutesakiKeywords.RanaLive.GetModCardKeyword()
    ];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<RanaLivePower>();
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 执行攻击
        var attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 检查是否有未格挡的伤害
        bool hasUnblockedDamage = attackCommand.Results
            .SelectMany(hitResults => hitResults)
            .Any(result => result.UnblockedDamage > 0);

        if (hasUnblockedDamage)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2); // 5 → 7
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}