using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class SuccessRecital() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new BlockVar(5m,ValueProp.Move); // 基础倍数
            yield return new SuccessRecitalBlockVar(); // 实时总格挡
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var perBlock = (int)DynamicVars.Block.BaseValue;

        // 演奏前获取当前储存的和弦数量（实际将被演奏的数量）
        var chordCount = MusicNoteManager.GetStoredChords(Owner).Count;

        // 演奏所有储存的和弦
        await guitar.TriggerAllStoredChords(choiceContext);

        // 获得格挡 = 实际演奏数量 × 倍数
        var totalBlock = perBlock * chordCount;
        if (totalBlock > 0)
            await CreatureCmd.GainBlock(Owner.Creature, totalBlock, ValueProp.Move, null);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m); // 5 → 6
    }

    /// <summary>动态变量：实时计算总格挡（基于当前储存的和弦数）</summary>
    private class SuccessRecitalBlockVar : DynamicVar
    {
        public SuccessRecitalBlockVar() : base("TotalBlock", 0m)
        {
        }

        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            if (card.Owner == null) return;

            var perBlock = (int)card.DynamicVars.Block.BaseValue;
            var chordCount = MusicNoteManager.GetStoredChords(card.Owner).Count; // 使用储存和弦数
            BaseValue = perBlock * chordCount;
        }
    }
}