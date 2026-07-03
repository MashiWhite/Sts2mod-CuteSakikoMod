using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class SmoothPlay : CuteAnonCard
{
    private bool _eventSubscribed;

    public SmoothPlay() : base(4, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(20m, ValueProp.Move); }
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        SubscribeAndRefresh();
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (card == this)
            SubscribeAndRefresh();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        // 任何卡牌打出后音符都可能改变，兜底刷新一次
        if (Owner != null)
            UpdateCost();
    }

    private void SubscribeAndRefresh()
    {
        if (!_eventSubscribed)
        {
            MusicNoteManager.PlayerNotesChanged += OnPlayerNotesChanged;
            _eventSubscribed = true;
        }
        UpdateCost();
    }

    private void OnPlayerNotesChanged(Player changedPlayer)
    {
        if (Owner == null || changedPlayer != Owner) return;
        UpdateCost();
    }

    private void UpdateCost()
    {
        if (Owner?.Creature?.CombatState == null) return;
        var attackCount = MusicNoteManager.GetCurrentNotes(Owner)
            .Count(n => n == CardType.Attack);
        EnergyCost.SetThisTurn(Math.Max(0, 4 - attackCount));
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        // 清除所有音符
        MusicNoteManager.ClearNotes(Owner);

        // 造成伤害
        var damage = DynamicVars.Damage.IntValue;
        await DamageCmd.Attack(damage)
            .FromCard(this,cardPlay)
            .TargetingRandomOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 刷新 UI
        Owner.Relics.OfType<AnonGuitar>().FirstOrDefault()?.UpdateNoteDisplay();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m); // 20 → 25
    }
}