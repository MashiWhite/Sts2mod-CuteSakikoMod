using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class PowerOfMemory() : CustomCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memorysaki); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 未升级基础伤害6，升级后9
            var baseDamage = IsUpgraded ? 9m : 6m;
            // 未升级每张回忆加成3，升级后加成5
            var extraPerMemory = IsUpgraded ? 5m : 3m;

            return new DynamicVar[]
            {
                new CalculationBaseVar(baseDamage),
                new ExtraDamageVar(extraPerMemory),
                new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
                {
                    var owner = card.Owner;
                    if (owner == null) return 0m;

                    // 统计手牌、抽牌堆、弃牌堆中带有 Memory 关键词的卡牌数量
                    var memoryCount = 0;
                    var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
                    foreach (var pileType in piles)
                    {
                        var pile = pileType.GetPile(owner);
                        if (pile == null) continue;
                        memoryCount += pile.Cards.Count(c => c.CanonicalKeywords.Contains(CutesakiKeywords.Memory));
                    }

                    return memoryCount;
                })
            };
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
        // 升级效果在 CanonicalVars 中已通过 IsUpgraded 处理，无需额外操作
    }
}