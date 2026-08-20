using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class HastyPlay : CuteAnonCard
{
    public HastyPlay() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Playguitar.GetModCardKeyword()];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(7m, ValueProp.Move);
            yield return new CardsVar(1);
        }
    }

    // 金色高亮：上一个音符是攻击时发光
    protected override bool ShouldGlowGoldInternal =>
        MusicNoteManager.GetLastNote(Owner) == CardType.Attack;

    public override void AfterCreated()
    {
        base.AfterCreated();
        SubscribeAndRefresh();
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (card == this) SubscribeAndRefresh();
    }

    private void SubscribeAndRefresh()
    {
        // 避免重复订阅
        MusicNoteManager.PlayerNotesChanged -= OnNotesChanged;
        MusicNoteManager.PlayerNotesChanged += OnNotesChanged;
        RefreshCost();
    }

    private void OnNotesChanged(Player changedPlayer)
    {
        if (Owner != null && changedPlayer == Owner)
            RefreshCost();
    }

    private void RefreshCost()
    {
        if (Owner?.Creature?.CombatState == null) return;
        var lastNote = MusicNoteManager.GetLastNote(Owner);
        // 核心：用 SetThisTurn 动态设置费用
        EnergyCost.SetThisTurn(lastNote == CardType.Attack ? 0 : 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        if (cardPlay.Target != null)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this,cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 7 → 11
    }
}