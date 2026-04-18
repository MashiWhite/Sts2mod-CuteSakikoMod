using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

[Pool(typeof(CuteSakiCardPool))]
public class Intermission : CustomCardModel
{
    public Intermission() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(1, ValueProp.Move);
            yield return new RepeatVar(IsUpgraded ? 6 : 3);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 造成多次 1 点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 抽 1 张牌
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 CanonicalVars 中通过 IsUpgraded 处理
    }
}