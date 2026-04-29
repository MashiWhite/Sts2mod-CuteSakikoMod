using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class ByMyReign() : CuteSakikoModCard(3, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    // 设置为多人卡限定
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sword);
        }
    }


    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null) return false;
            foreach (var player in CombatState.Players)
            {
                var hand = PileType.Hand.GetPile(player);
                if (hand != null && hand.Cards.Any(c => c is KnightSword))
                    return true;
            }

            return false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var player in CombatState.Players)
        {
            var hand = PileType.Hand.GetPile(player);
            if (hand == null) continue;

            var swords = hand.Cards.Where(c => c is KnightSword).ToList();

            if (swords.Count > 0)
            {
                foreach (var sword in swords)
                {
                    var target = GetRandomEnemy();
                    if (target == null) continue;


                    if (IsUpgraded) sword.BaseReplayCount += 1;

                    await CardCmd.AutoPlay(choiceContext, sword, target);


                    if (IsUpgraded) sword.BaseReplayCount -= 1;
                }
            }
            else
            {
                // 手牌中没有剑时，添加一张
                var newSword = CombatState.CreateCard<KnightSword>(player);
                await CardPileCmd.AddGeneratedCardToCombat(newSword, PileType.Hand, Owner);
            }
        }
    }

    private Creature? GetRandomEnemy()
    {
        var enemies = CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return null;
        return enemies[Owner.RunState.Rng.CombatCardSelection.NextInt(enemies.Count)];
    }

    protected override void OnUpgrade()
    {
        // 升级后效果在 OnPlay 中已处理
    }
}