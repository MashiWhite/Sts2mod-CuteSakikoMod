using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic
{
    [Pool(typeof(CuteAnonRelicPool))]
    public class FlashAnonGuitar : AnonGuitar
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        protected override int MaxLearnedChordsPerCategory => 2;
        protected override int EffectMultiplier => 2;

        public override async Task AfterObtained()
        {
            await base.AfterObtained();

            // 如果额外槽位为空，随机生成一个
            if (!HasBonusChord())
            {
                var rng = Owner.RunState.Rng.UpFront;
                var allPools = new List<string>();
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
                if (allPools.Count > 0)
                {
                    SetBonusChord(rng.NextItem(allPools));
                }
            }
        }
    }
}