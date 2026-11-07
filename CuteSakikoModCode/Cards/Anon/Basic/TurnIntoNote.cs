using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;

public class TurnIntoNote() : CuteAnonCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    // 特殊演奏（本身不产生音符）+ 化为音符（供悬浮提示使用）
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CutesakiKeywords.NoNote.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Noteify.GetModCardKeyword()); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        var candidates = hand.Cards.Where(c => c != this).ToList();
        if (candidates.Count == 0) return;

        int selectCount = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        if (selectCount <= 0) return;

        // ★ 顺序敏感（2026-11-07 用户反馈）：
        //   音符的产生顺序决定能否凑出想要的和弦，因此**剩 2 张以上必须让玩家亲自点选**，
        //   否则自动全选会造成强烈挫败感。只有剩 1 张（没得选）时才自动通过。
        //
        //   原写法 CardSelectorPrefs(prompt, selectCount, selectCount) 的 min==max，
        //   会让 RequireManualConfirmation=false，而 CardSelectCmd.FromHand 里：
        //       if (!prefs.RequireManualConfirmation && list.Count <= prefs.MinSelect) → 直接全给
        //   于是候选数恰好等于所需数时连选择界面都不弹（就是"自动全选"的根因）。
        //   ⇒ 改成 min=0 / max=selectCount：min≠max → RequireManualConfirmation=true，
        //     必定弹窗且要求手动确认；返回顺序 = 玩家点击顺序
        //     （NPlayerHand._selectedCards 是 List，按点击顺序追加）。
        //     min=0 让玩家可以少选，描述相应改成「至多选择 N 张」。
        var prompt = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_TURN_INTO_NOTE.selectionScreenPrompt");
        IEnumerable<CardModel> selected;

        if (candidates.Count == 1)
        {
            // 只剩一张，没得选：直接取，不用弹窗
            selected = candidates;
        }
        else
        {
            var prefs = new CardSelectorPrefs(prompt, 0, selectCount);
            selected = await CardSelectCmd.FromHand(
                choiceContext, Owner, prefs, c => c != this, this);
        }

        var selectedList = selected.ToList();
        if (selectedList.Count == 0) return;

        // 先记录类型（消耗后 Pile 会变，但 Type 仍可读；不过提前记录更稳）
        var noteTypes = selectedList.Select(c => c.Type).ToList();

        // 消耗
        foreach (var card in selectedList)
            await CardCmd.Exhaust(choiceContext, card);

        // 依次：添加音符（触发和弦匹配）+ 触发对应效果
        foreach (var noteType in noteTypes)
        {
            await ChordNoteSystem.AddNoteAsync(Owner, noteType, choiceContext, triggerEffect: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1); // 1 → 2
    }
}