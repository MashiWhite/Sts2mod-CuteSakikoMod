using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class LittleMoments() : CuteAnonCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
        }
    }
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 根据是否升级，选择不同的描述 key
            string descKey = IsUpgraded
                ? "CUTE_SAKIKO_MOD_CARD_LITTLE_MOMENTS.mergeHint.description_upgraded"
                : "CUTE_SAKIKO_MOD_CARD_LITTLE_MOMENTS.mergeHint.description";

            yield return new HoverTip(
                new LocString("cards", "CUTE_SAKIKO_MOD_CARD_LITTLE_MOMENTS.mergeHint.title"),
                new LocString("cards", descKey)
            );

            // 一辈子卡牌预览
            yield return HoverTipFactory.FromCard<Lifetime>(IsUpgraded);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CardsVar(1);
            yield return new DynamicVar("Copies", 1);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        if (CombatState != null && Owner != null)
        {
            int copyCount = DynamicVars["Copies"].IntValue;
            for (int i = 0; i < copyCount; i++)
            {
                var copy = CombatState.CreateCard<LittleMoments>(Owner);
                if (IsUpgraded)
                {
                    copy.UpgradeInternal();
                    copy.FinalizeUpgradeInternal();
                }
                await CardPileCmd.Add(copy, PileType.Discard, CardPilePosition.Random);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}