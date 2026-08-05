
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class NekoTrancePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 单层，不可叠加
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CardKeyword.Retain);
        }
    }

    /// <summary>
    /// 任意卡牌进入战斗时，如果是猫咪且属于拥有者，则赋予保留。
    /// </summary>
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (!card.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()) || card.Owner != Owner.Player)
            return;

        CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 能力被应用时，为场上所有已有的猫咪添加保留。
    /// </summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        foreach (var card in Owner.Player.PlayerCombatState.AllCards
                     .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword())))
        {
            CardCmd.ApplyKeyword(card, CardKeyword.Retain);
        }
        await Task.CompletedTask;
    }
}