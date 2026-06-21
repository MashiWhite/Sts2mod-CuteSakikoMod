using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class SlowerBeat() : CuteAnonCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new BlockVar(10m, ValueProp.Move);
            yield return new DynamicVar("BlockNextTurn", 5m);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        // 获得格挡
        var block = DynamicVars.Block.IntValue;
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);

        // 下回合获得格挡
        var nextTurnBlock = (int)DynamicVars["BlockNextTurn"].BaseValue;
        await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature, nextTurnBlock, Owner.Creature, this);

        // 额外获得一个技能音符（使用当前已记忆和弦列表以正确匹配）
        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var mainChords = guitar.GetCurrentChords();
            var bonusChords = guitar.GetBonusChords();
            var tempChords = guitar.GetTemporaryChords();
            
            int manualNoteCount = IsUpgraded ? 3 : 2;
            for (int i = 0; i < manualNoteCount; i++)
            {
                await MusicNoteManager.AddNoteAndAutoPlayAsync(Owner, CardType.Skill,
                    mainChords,
                    bonusChords.Concat(tempChords),
                    choiceContext); // ★ 传入上下文
            }

            // 刷新 UI
            guitar.UpdateNoteDisplay();
            guitar.UpdateStoredChordDisplay();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 10 → 13
        DynamicVars["BlockNextTurn"].UpgradeValueBy(2m); // 5 → 7
    }
}