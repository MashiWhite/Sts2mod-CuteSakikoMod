using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class FriendlyNeko : CuteRanaCard
{
    public FriendlyNeko() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 对所有敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 获取猫咪卡牌池
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();

        if (allNekoCards.Count == 0) return;

        // 3. 根据是否升级决定生成的猫咪是否升级
        var combatReadyCards = allNekoCards
            .Select(template =>
            {
                var card = CombatState.CreateCard(template, Owner);
                if (IsUpgraded)
                {
                    card.UpgradeInternal();
                    card.FinalizeUpgradeInternal();
                }
                return card;
            })
            .ToList();

        // 4. 弹出选择界面，选择一张猫咪加入手牌
        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_FRIENDLY_NEKO.selectionScreenPrompt"),
            1,
            1
        );

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            combatReadyCards,
            Owner,
            prefs
        );

        var selected = selectedCards.FirstOrDefault();
        if (selected == null) return;

        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 5 → 8
    }
}