
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;


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

            // 替换三个主槽位（排除 Bonus 分类）
            foreach (ChordCategory cat in Enum.GetValues<ChordCategory>())
            {
                if (cat == ChordCategory.Bonus) continue; // Bonus 单独处理
                var pool = ChordManager.GetLearnableChordIds(cat);
                if (pool.Count == 0) continue;
                _relic.ReplaceChord(cat, rng.NextItem(pool));
            }

            // 处理所有 Bonus 槽位：逐个替换，保持数量不变
            var bonusChords = _relic.GetBonusChords();
            if (bonusChords.Count > 0)
            {
                var allPools = new List<string>();
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
        
                if (allPools.Count > 0)
                {
                    // 记录当前 Bonus 数量
                    int bonusCount = bonusChords.Count;
            
                    // 清空所有 Bonus（方法一：逐个移除）
                    var oldIds = bonusChords.ToList();
                    foreach (var oldId in oldIds)
                    {
                        _relic.RemoveBonusChord(oldId);
                    }
            
                    // 重新添加相同数量的随机 Bonus
                    for (int i = 0; i < bonusCount; i++)
                    {
                        _relic.AddBonusChord(rng.NextItem(allPools));
                    }
                }
            }

            return true;
        }
    }
}