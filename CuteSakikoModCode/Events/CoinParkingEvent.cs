
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events
{
    [RegisterSharedEvent]
    public sealed class CoinParkingEvent : ModEventTemplate
    {
        private IHoverTip[]? _relicHoverTips;  // 缓存遗物提示
        
        public override bool IsShared => true;

        public override EventAssetProfile AssetProfile => new(
            InitialPortraitPath: "res://CuteSakikoMod/images/events/coin_parking.png"
        );

        protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();

        public override bool IsAllowed(IRunState runState) => true;

        protected override IReadOnlyList<EventOption> GenerateInitialOptions()
        {
            _relicHoverTips ??= HoverTipFactory.FromRelic<MillionTimesCat>().ToArray();
            
            var options = new List<EventOption>
            {
                new(this, Stay, InitialOptionKey("STAY")),
            };

            // 进入Space选项：根据是否有乐奈决定锁定状态
            if (HasCuteRanaPlayer())
            {
                options.Add(new(this, EnterSpace, InitialOptionKey("ENTER_SPACE"),_relicHoverTips));
            }
            else
            {
                // 锁定选项，不可选择
                options.Add(new EventOption(
                    this,
                    null, // 不执行任何操作
                    InitialOptionKey("ENTER_SPACE_LOCKED") // 使用锁定版本文本键
                ));
            }

            options.Add(new(this, Leave, InitialOptionKey("LEAVE")));

            return options;
        }

        private bool HasCuteRanaPlayer()
        {
            if (Owner?.RunState == null) return false;
            return Owner.RunState.Players.Any(p => p.Character is CuteRana);
        }

        // 选项1：停留 - 恢复10点生命，升级1张牌
        private async Task Stay()
        {
            await CreatureCmd.Heal(Owner!.Creature, 10m);

            var selected = await CardSelectCmd.FromDeckForUpgrade(
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
            );
            var card = selected.FirstOrDefault();
            if (card != null)
            {
                CardCmd.Upgrade(card);
            }

            SetEventFinished(PageDescription("STAY_DESC"));
        }

        // 选项2：进入Space - 获得遗物《死了一百万次的猫》（仅当有乐奈时）
        private async Task EnterSpace()
        {
            var relic = ModelDb.Relic<MillionTimesCat>().ToMutable();
            relic.Owner = Owner;
            await RelicCmd.Obtain(relic, Owner!);
            SetEventFinished(PageDescription("ENTER_SPACE_DESC"));
        }

        // 选项3：离开 - 失去5点生命，移除1张牌
        private async Task Leave()
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner!.Creature,
                5m,
                ValueProp.Unblockable | ValueProp.Unpowered,
                (Creature?)null,
                (CardModel?)null
            );

            var selected = await CardSelectCmd.FromDeckForRemoval(
                Owner,
                new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1)
            );
            var card = selected.FirstOrDefault();
            if (card != null)
            {
                await CardPileCmd.RemoveFromDeck(card);
            }

            SetEventFinished(PageDescription("LEAVE_DESC"));
        }

        private LocString PageDescription(string pageKey) => L10NLookup($"{Id.Entry}.pages.{pageKey}.description");
    }
}