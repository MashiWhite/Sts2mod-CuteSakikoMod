using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;
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

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            var lastNote = MusicNoteManager.GetLastNote(Owner);
            return lastNote == CardType.Attack;
        }
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        // 订阅音符变化事件，实时更新费用
        MusicNoteManager.PlayerNotesChanged += OnPlayerNotesChanged;
    }

    private void OnPlayerNotesChanged(Player changedPlayer)
    {
        if (Owner == null || changedPlayer != Owner) return;
        UpdateCostBasedOnLastNote();
    }

    private void UpdateCostBasedOnLastNote()
    {
        if (Owner?.Creature?.CombatState == null) return;
        var lastNote = MusicNoteManager.GetLastNote(Owner);
        if (lastNote == CardType.Attack)
            EnergyCost.SetThisTurn(0);
        else
            EnergyCost.SetThisTurn(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        if (cardPlay.Target != null)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    // 不再需要手动刷新费用，事件会自动处理
    // 但保留 base 调用以维持钩子链
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 7 → 11
    }
}