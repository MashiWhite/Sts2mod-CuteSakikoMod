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
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event
{
    public class TimeWatch : CuteSakikoModRelic
    {
        [SavedProperty]
        public int FlybackPlayCount { get; set; }

        private DynamicVar _blockVar;
        private bool _flybackAdded;

        public override RelicRarity Rarity => RelicRarity.Ancient;

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DynamicVar("Block", GetBlockAmount());
                yield return new DynamicVar("PlayCount", FlybackPlayCount);
            }
        }

        private void UpdateBlockDynamicVar()
        {
            if (this.DynamicVars.TryGetValue("Block", out var blockVar))
                blockVar.BaseValue = GetBlockAmount();
        }

        public void IncrementPlayCount()
        {
            FlybackPlayCount++;
            FlybackManager.InvalidatePlayerCache(Owner);
            UpdateBlockDynamicVar();
            Flash();
        }

        public int GetFlybackPlayCount() => FlybackPlayCount;

        private int GetBlockAmount()
        {
            int total = FlybackManager.Instance.TotalPlayCount;
            int block = total / 10;
            return block > 20 ? 20 : block;
        }

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player != Owner) return;
            if (Owner.Creature == null) return;

            int blockAmount = GetBlockAmount();
            if (blockAmount > 0)
            {
                await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, null);
            }

            UpdateBlockDynamicVar();
            Flash();
        }

        protected override IEnumerable<IHoverTip> AdditionalHoverTips
        {
            get
            {
                int playCount = FlybackManager.Instance.TotalPlayCount;
                int reloads = FlybackManager.GetReloadCount();
                var title = new LocString("relics", "TIMEWATCH_FLYBACK_TITLE");
                var desc = new LocString("relics", "TIMEWATCH_FLYBACK_DESC");
                desc.Add("playCount", playCount.ToString());
                desc.Add("reloadCount", reloads.ToString());
                yield return new HoverTip(title, desc);
                yield return HoverTipFactory.FromCard<Flyback>();
            }
        }

        public override async Task AfterObtained()
        {
            await base.AfterObtained();
            // 将临时计数迁移到此遗物
            FlybackManager.TransferTempCountToTimeWatch(Owner, this);

            if (!_flybackAdded && Owner != null)
            {
                var flyback = Owner.RunState.CreateCard<Flyback>(Owner);
                await CardPileCmd.Add(flyback, PileType.Deck);
                _flybackAdded = true;
                Flash();
            }

            // ★ 固定第三幕 Boss 为星爱音
            SetGloryBossEncounter();
        }

        /// <summary>
        /// 将 Glory（第三幕）的 Boss 遭遇战设置为 StarAnonEncounter。
        /// 在进阶十时，SecondBoss 会由系统自动从 AllBossEncounters 中选取，
        /// 且会跳过 BossEncounter（即星爱音），因此不会有双星爱音。
        /// </summary>
        private void SetGloryBossEncounter()
        {
            if (Owner?.RunState == null) return;

            var gloryAct = Owner.RunState.Acts
                .FirstOrDefault(act => act is Glory);

            if (gloryAct == null) return;

            var starAnonEncounter = ModelDb.GetById<EncounterModel>(
                ModelDb.GetId<StarAnonEncounter>()).ToMutable();

            gloryAct.SetBossEncounter(starAnonEncounter);
        }
    }
}