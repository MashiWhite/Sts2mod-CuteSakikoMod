using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class PowerOfMemory() : CuteSakikoModCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 基础伤害（未升级6）
            yield return new CalculationBaseVar(6m);
            // 每张回忆额外伤害（未升级3）
            yield return new ExtraDamageVar(3m);
            // 动态计算最终伤害
            yield return new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
            {
                var owner = card.Owner;
                if (owner == null) return 0m;

                var memoryCount = 0;
                var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
                foreach (var pileType in piles)
                {
                    var pile = pileType.GetPile(owner);
                    if (pile == null) continue;
                    memoryCount += pile.Cards.Count(c => c.HasModKeyword(CutesakiKeywords.Memory));
                }

                return memoryCount;
            });
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 基础伤害 6 → 9
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
        // 每张回忆加成 3 → 5
        DynamicVars.ExtraDamage.UpgradeValueBy(2m);
    }
}