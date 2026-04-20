using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;


public class Bonfire : CuteSakikoModEggCard
{
    public Bonfire() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner.Creature;
        // 回复生命（基础5%，升级10%）
        var percent = IsUpgraded ? 0.1f : 0.05f;
        var healAmount = (int)(creature.MaxHp * percent);
        if (healAmount < 1) healAmount = 1;
        await CreatureCmd.Heal(creature, healAmount);

        // 随机永久升级牌组中的牌
        var upgradeCount = IsUpgraded ? 2 : 1;
        var deck = PileType.Deck.GetPile(Owner).Cards;
        var upgradable = deck.Where(c => c.IsUpgradable).ToList();
        for (var i = 0; i < upgradeCount && upgradable.Count > 0; i++)
        {
            var randomCard = Owner.RunState.Rng.UpFront.NextItem(upgradable);
            CardCmd.Upgrade(randomCard);
            // 移除已升级的卡，避免重复升级同一张（防止浪费次数）
            upgradable.Remove(randomCard);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 OnPlay 中通过 IsUpgraded 处理
    }
}