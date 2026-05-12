using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class DifficultChoice() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(5m),   // 基础 5
        new PowerVar<StrengthPower>(3m),   // 基础 3
        new("Gold", 35m)                   // 基础 35
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pressureToken = ModelDb.Card<PressureOption>().ToMutable();
        var goldToken = ModelDb.Card<GoldOption>().ToMutable();

        // 根据主卡是否升级来升级 token
        if (IsUpgraded)
        {
            pressureToken.UpgradeInternal();
            pressureToken.FinalizeUpgradeInternal();
            goldToken.UpgradeInternal();
            goldToken.FinalizeUpgradeInternal();
        }

        pressureToken.Owner = Owner;
        goldToken.Owner = Owner;

        var options = new List<CardModel> { pressureToken, goldToken };
        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);

        if (selected == null) return;

        if (selected is PressureOption)
        {
            await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature,
                selected.DynamicVars["PressurePower"].IntValue, Owner.Creature, this);
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature,
                selected.DynamicVars["StrengthPower"].IntValue, Owner.Creature, this);
        }
        else if (selected is GoldOption)
        {
            await PlayerCmd.GainGold(selected.DynamicVars["Gold"].IntValue, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级时增加数值
        if (DynamicVars.TryGetValue("PressurePower", out var pv))
            pv.UpgradeValueBy(3);   // 5→8
        if (DynamicVars.TryGetValue("StrengthPower", out var sv))
            sv.UpgradeValueBy(2);   // 3→5
        if (DynamicVars.TryGetValue("Gold", out var gv))
            gv.UpgradeValueBy(10);  // 35→45
    }
}