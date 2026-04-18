using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

[Pool(typeof(CuteSakiCardPool))]
public class Unsheathe : CustomCardModel
{
    public Unsheathe() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(8m, ValueProp.Move); }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sword);
            yield return HoverTipFactory.FromCard<KnightSword>(IsUpgraded);
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 检查手牌是否已有骑士之剑
        var handPile = PileType.Hand.GetPile(Owner);
        bool hasInHand = handPile != null && handPile.Cards.Any(c => c is KnightSword);
        if (hasInHand) return;

        // 从抽牌堆和弃牌堆中寻找骑士之剑
        CardModel sword = null;
        var searchPiles = new[] { PileType.Draw, PileType.Discard };
        foreach (var pileType in searchPiles)
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null)
            {
                sword = pile.Cards.FirstOrDefault(c => c is KnightSword);
                if (sword != null)
                    break;
            }
        }

        if (sword != null)
        {
            // 将找到的骑士之剑移到手牌
            sword.RemoveFromCurrentPile();
            await CardPileCmd.Add(sword, PileType.Hand);
        }
        else
        {
            // 没有找到，创建一张新的骑士之剑
            var newSword = CombatState.CreateCard<KnightSword>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(newSword);
            await CardPileCmd.AddGeneratedCardToCombat(newSword, PileType.Hand, true);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：费用 1 → 0
        EnergyCost.UpgradeBy(-1);
    }
}