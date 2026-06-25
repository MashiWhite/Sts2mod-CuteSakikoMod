using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class VocalHarmonyPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        // 必须是自己打出的，且含有关键词 RanaLive
        if (card.Owner?.Creature != Owner) return;
        if (!card.Keywords.Contains(CutesakiKeywords.RanaLive.GetModCardKeyword())) return;

        int damagePerEnemy = Amount;
        if (damagePerEnemy <= 0) return;

        var enemies = Owner.CombatState.HittableEnemies;
        if (enemies.Count == 0) return;

        Flash();

        // 对所有敌人造成伤害
        await CreatureCmd.Damage(
            choiceContext,
            enemies,
            damagePerEnemy,
            ValueProp.Unpowered,
            Owner,
            null
        );
    }
}