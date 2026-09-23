using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class SharpenBlade() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Sword.GetModCardKeyword()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>(IsUpgraded);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pressure = Owner.Creature.GetPower<PressurePower>();
        int pressureAmount = pressure?.Amount ?? 0;
        if (pressureAmount <= 0) return;

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        // 所有骑士之剑增加压力层数等量点伤害
        SwordManager.IncreaseAllSwordDamage(pressureAmount, combat);

        // 升级后：同时升级所有骑士之剑
        if (IsUpgraded)
        {
            foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
            {
                var pile = pileType.GetPile(Owner);
                if (pile == null) continue;
                foreach (var card in pile.Cards.ToList())
                {
                    if (card is KnightSword ks && ks.IsUpgradable)
                    {
                        ks.UpgradeInternal();
                        ks.FinalizeUpgradeInternal();
                    }
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}