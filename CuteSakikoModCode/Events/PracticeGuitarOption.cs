using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

public class PracticeGuitarOption : ModRestSiteOptionTemplate
{
    private readonly AnonGuitar _relic;

    private static readonly LocString _titleLoc = new(
        "rest_site_ui", "CUTE_SAKIKO_MOD_OPTION_PRACTICE_GUITAR_NAME");
    private static readonly LocString _descLoc = new(
        "rest_site_ui", "CUTE_SAKIKO_MOD_PRACTICE_GUITAR_DESC");
    private static readonly LocString _promptLoc = new(
        "rest_site_ui", "CUTE_SAKIKO_MOD_PRACTICE_GUITAR_SELECT_PROMPT");

    public PracticeGuitarOption(Player player, AnonGuitar relic) : base(player)
    {
        _relic = relic;
    }

    public override string OptionId => "PracticeGuitar";

    public override LocString Description => _descLoc;

    public override RestSiteOptionAssetProfile AssetProfile => new(
        IconPath: "res://CuteSakikoMod/images/ui/rest_site/practice_guitar.png"
    );

    public override LocString? CustomTitle => _titleLoc;

    public override async Task<bool> OnSelect()
    {
        try
        {
            if (_relic.PracticeUsedThisVisit) return false;
            if (!IsEnabled) return false;

            // ---- 收集当前拥有的和弦 ----
            var allCurrent = new List<string>();
            foreach (ChordCategory cat in Enum.GetValues<ChordCategory>())
            {
                if (cat == ChordCategory.Bonus || cat == ChordCategory.Anon) continue;
                if (_relic.GetCurrentChords().TryGetValue(cat, out var chordId) && !string.IsNullOrEmpty(chordId))
                    allCurrent.Add(chordId);
            }
            allCurrent.AddRange(_relic.GetBonusChords());

            if (allCurrent.Count == 0)
                return false;

            var changeIds = await ChordSelectCmd.SelectChordsForPractice(
                Owner,
                allCurrent,
                _promptLoc
            );

            _relic.PracticeUsedThisVisit = true;

            // ---- 检查是否所有其他选项均已不可用（用于自动完成） ----
            bool allOtherOptionsDisabled = true;
            var localOptions = RunManager.Instance.RestSiteSynchronizer?.GetLocalOptions();
            if (localOptions != null)
            {
                foreach (var opt in localOptions)
                {
                    if (opt is PracticeGuitarOption) continue;
                    if (opt.IsEnabled)
                    {
                        allOtherOptionsDisabled = false;
                        break;
                    }
                }
            }
            if (allOtherOptionsDisabled)
                _relic.NormalOptionUsed = true;

            // 练习吉他使用后，游戏会自动移除该选项按钮（无需手动操作）

            if (changeIds.Count == 0)
                return true;

            // ---- 和弦替换逻辑（所有客户端统一执行） ----
            var rng = Owner.RunState.Rng.UpFront;
            var changeSet = new HashSet<string>(changeIds);

            var ownedChordIds = new HashSet<string>();
            foreach (var kv in _relic.GetCurrentChords())
                if (!string.IsNullOrEmpty(kv.Value))
                    ownedChordIds.Add(kv.Value);
            foreach (var id in _relic.GetBonusChords())
                ownedChordIds.Add(id);

            foreach (ChordCategory cat in Enum.GetValues<ChordCategory>())
            {
                if (cat == ChordCategory.Bonus || cat == ChordCategory.Anon) continue;
                if (!_relic.GetCurrentChords().TryGetValue(cat, out var currentId) || string.IsNullOrEmpty(currentId))
                    continue;

                if (changeSet.Contains(currentId))
                {
                    var pool = ChordManager.GetLearnableChordIds(cat);
                    var available = pool.Where(id => !ownedChordIds.Contains(id)).ToList();
                    if (available.Count > 0)
                    {
                        var newChord = rng.NextItem(available);
                        ownedChordIds.Remove(currentId);
                        ownedChordIds.Add(newChord);
                        _relic.ReplaceChord(cat, newChord);
                    }
                }
            }

            var originalBonus = _relic.GetBonusChords().ToList();
            if (originalBonus.Count > 0)
            {
                var toRemove = originalBonus.Where(id => changeSet.Contains(id)).ToList();
                foreach (var id in toRemove)
                    _relic.RemoveBonusChord(id);

                if (toRemove.Count > 0)
                {
                    var allPools = new List<string>();
                    allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
                    allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
                    allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

                    var availableBonus = allPools.Where(id => !ownedChordIds.Contains(id)).ToList();
                    if (availableBonus.Count > 0)
                    {
                        rng.Shuffle(availableBonus);
                        int count = Math.Min(toRemove.Count, availableBonus.Count);
                        for (int i = 0; i < count; i++)
                            _relic.AddBonusChord(availableBonus[i]);
                    }
                }
            }

            return true;
        }
        catch (Exception e)
        {
            RitsuLibFramework.Logger.Error($"PracticeGuitarOption.OnSelect error: {e}");
            return false;
        }
    }
}