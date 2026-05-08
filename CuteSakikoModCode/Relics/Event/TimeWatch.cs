using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event
{
    public class TimeWatch : CuteSakikoModRelic
    {
        [SavedProperty]
        public int FlybackPlayCount { get; set; }  // 改为属性！

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
            // DynamicVarSet 是一个 IReadOnlyDictionary<string, DynamicVar>
            if (this.DynamicVars.TryGetValue("Block", out var blockVar))
                blockVar.BaseValue = GetBlockAmount();
        }

        public void IncrementPlayCount()
        {
            FlybackPlayCount++;
            FlybackManager.InvalidatePlayerCache(Owner);
            UpdateBlockDynamicVar();   // 更新动态变量
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

            UpdateBlockDynamicVar();   // 回合开始也更新，确保同步
            Flash();
        }



// 悬浮提示中的计数也从 FlybackManager 读取（可选）
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
                // 将飞返加入牌组
                var flyback = Owner.RunState.CreateCard<Flyback>(Owner);
                await CardPileCmd.Add(flyback, PileType.Deck);
                _flybackAdded = true;
                Flash();
            }
        }

       
    }
}