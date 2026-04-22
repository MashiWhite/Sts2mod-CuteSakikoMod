using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient
{
    public class PlayImmediately : CuteAnonCard
    {
        public PlayImmediately() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
        {
        }

        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Innate;
                yield return CutesakiKeywords.NoNote; // 不产生音符
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars => System.Array.Empty<DynamicVar>();

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<PlayImmediatelyPower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            // 获得“即刻演奏”能力
            await PowerCmd.Apply<PlayImmediatelyPower>(Owner.Creature, 1, Owner.Creature, this);

            // 获取吉他遗物并演奏所有储存的和弦（保留音符序列）
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
            {
                await guitar.TriggerAllStoredChordsKeepNotes(choiceContext);
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1); // 升级后1费
        }
    }
}