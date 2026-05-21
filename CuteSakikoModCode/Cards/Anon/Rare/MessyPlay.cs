using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class MessyPlay() : CuteAnonCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModKeywordCardKeyword()];

    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new PowerVar<MessyPlayPower>(1m); } // 未升级每次额外1个音符
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var existing = Owner.Creature.GetPower<MessyPlayPower>();
        var amount = DynamicVars["MessyPlayPower"].IntValue;

        if (existing != null)
        {
            // 已存在能力：增加层数（额外音符数），并更新阈值（如果升级版则缩短间隔）
            existing.UpdateThreshold(IsUpgraded ? 2 : 3);
            await PowerCmd.ModifyAmount(choiceContext, existing, amount, Owner.Creature, this);
        }
        else
        {
            // 首次施加
            await PowerCmd.Apply<MessyPlayPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果由 UpdateThreshold 内部处理
    }
}