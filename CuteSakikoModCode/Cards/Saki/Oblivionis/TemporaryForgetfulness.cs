using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Oblivionis;

[RegisterArchaicToothTranscendence(typeof(OnlyOblivion))]
[RegisterCharacterStarterCard(typeof(CuteOb), Order = 3)]
public class TemporaryForgetfulness : CuteObCard
{
    public TemporaryForgetfulness() : base(1, CardType.Skill, CardRarity.Basic, TargetType.None)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null || hand.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_TEMPORARY_FORGETFULNESS.selectionScreenPrompt"),
            1,
            1
        );

        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            _ => true,
            this
        );

        var cardToForget = selected.FirstOrDefault();
        if (cardToForget == null) return;

        // 打出选中的牌
        await CardCmd.AutoPlay(choiceContext, cardToForget, null);

        // 遗忘（由 MemoryCmd.Forget 处理移入遗忘堆等）
        await MemoryCmd.Forget(choiceContext, new[] { cardToForget }, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}