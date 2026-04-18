using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Events
{
    public class PracticeGuitarOption : RestSiteOption
    {
        private readonly Player _player;
        private readonly AnonGuitar _relic;

        public override string OptionId => "PracticeGuitar";

        public override LocString Description => new("rest_site_ui", "PRACTICE_GUITAR_DESC");

        public PracticeGuitarOption(Player player, AnonGuitar relic)
            : base(player)
        {
            _player = player;
            _relic = relic;
        }

        public override async Task<bool> OnSelect()
        {
            var rng = _player.RunState.Rng.UpFront;

            foreach (ChordCategory cat in Enum.GetValues<ChordCategory>())
            {
                var pool = ChordManager.GetLearnableChordIds(cat);
                if (pool.Count == 0) continue;
                _relic.ReplaceChord(cat, rng.NextItem(pool));
            }

            // 只要遗物有额外槽位字段（非空即表示支持），就随机替换
            // 普通吉他 _bonusChord 为 null，不会进入此逻辑
            if (_relic.HasBonusChord() || _relic.GetBonusChord() != null) // 实际上 HasBonusChord 更准确
            {
                var allPools = new List<string>();
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
                if (allPools.Count > 0)
                {
                    _relic.SetBonusChord(rng.NextItem(allPools));
                }
            }

            return true;
        }
    }
}