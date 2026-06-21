using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class SmoothPlay : CuteAnonCard
{
    public SmoothPlay() : base(4, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(20m, ValueProp.Move); }
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        // 订阅音符变化事件，动态更新费用
        MusicNoteManager.PlayerNotesChanged += OnPlayerNotesChanged;
    }

    private void OnPlayerNotesChanged(Player changedPlayer)
    {
        // 只处理自己的音符变化
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
            .FromCard(this)
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