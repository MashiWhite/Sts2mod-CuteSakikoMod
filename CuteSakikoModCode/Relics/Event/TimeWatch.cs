using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Token;
using CuteSakikoMod.CuteSakikoModCode.Encounters.Boss;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class TimeWatch : CuteSakikoModRelic
{
    private bool _flybackAdded;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Block", GetBlockAmount());
            yield return new DynamicVar("PlayCount", GetFlybackPlayCount());
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            var playCount = FlybackManager.Instance.TotalPlayCount;
            var reloads = FlybackManager.GetReloadCount();
            var title = new LocString("relics", "TIMEWATCH_FLYBACK_TITLE");
            var desc = new LocString("relics", "TIMEWATCH_FLYBACK_DESC");
            desc.Add("playCount", playCount.ToString());
            desc.Add("reloadCount", reloads.ToString());
            yield return new HoverTip(title, desc);
            yield return HoverTipFactory.FromCard<Flyback>();
        }
    }

    public void IncrementPlayCount()
    {
        FlybackManager.Instance.IncrementPlayCountForPlayer(Owner);
        UpdateBlockDynamicVar();
        Flash();
    }

    public int GetFlybackPlayCount()
    {
        // Canonical 实例没有 Owner，直接返回 0
        if (!IsMutable)
            return 0;

        if (Owner == null || FlybackManager.PlayerDataSlot == null)
            return 0;
        if (Owner.RunState is not RunState runState)
            return 0;
        return FlybackManager.PlayerDataSlot.Get(runState, Owner.NetId).PlayCount;
    }

    private void UpdateBlockDynamicVar()
    {
        if (DynamicVars.TryGetValue("Block", out var blockVar))
            blockVar.BaseValue = GetBlockAmount();
    }

    private int GetBlockAmount()
    {
        var total = FlybackManager.Instance.TotalPlayCount;
        var block = total / 10;
        return block > 20 ? 20 : block;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        if (Owner.Creature == null) return;

        var blockAmount = GetBlockAmount();
        if (blockAmount > 0)
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, null);

        UpdateBlockDynamicVar();
        Flash();
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (!_flybackAdded && Owner != null)
        {
            var flyback = Owner.RunState.CreateCard<Flyback>(Owner);
            await CardPileCmd.Add(flyback, PileType.Deck);
            _flybackAdded = true;
            Flash();
        }

        SetGloryBossEncounter();
    }

    private void SetGloryBossEncounter()
    {
        if (Owner?.RunState == null) return;
        var gloryAct = Owner.RunState.Acts.FirstOrDefault(act => act is Glory);
        if (gloryAct == null) return;

        var starAnonEncounter = ModelDb.GetById<EncounterModel>(
            ModelDb.GetId<StarAnonEncounter>());

        if (gloryAct.BossEncounter.Id == starAnonEncounter.Id) return;

        gloryAct.SetBossEncounter(starAnonEncounter);

        if (gloryAct.HasSecondBoss && gloryAct.SecondBossEncounter?.Id == starAnonEncounter.Id)
        {
            var otherBoss = Owner.RunState.Rng.UpFront.NextItem(
                gloryAct.AllBossEncounters.Where(e => e.Id != starAnonEncounter.Id));
            gloryAct.SetSecondBossEncounter(otherBoss);
        }
    }
}