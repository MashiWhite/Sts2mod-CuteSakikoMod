using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Commands;          // 新增，用于 CardPileCmd
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status; // 新增，引用 NotNeeded

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

// ReSharper disable once InconsistentNaming
public class AiHeart() : CuteAnonCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string ChordId => "GreyAnonChord";

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [CutesakiKeywords.NoNote, CutesakiKeywords.Chord, CutesakiKeywords.OtherAnon];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue("GreyAnonChord", out var def))
            {
                var condition = def.GetConditionText();
                var effectDesc = ChordDisplayHelper.GetFormattedDescription(def, 1);
                var fullDesc = $"{condition}\n{effectDesc}";
                var title = new LocString("card_keywords", def.TitleKey);
                yield return new HoverTip(title, fullDesc);
                yield return HoverTipFactory.FromCard<NotNeeded>(IsUpgraded);
            }
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null)
        {
            var currentMinor = guitar.GetCurrentChords().GetValueOrDefault(ChordCategory.Minor);
            if (currentMinor == "GreyAnonChord")
                await guitar.AddChordToStored(choiceContext, "GreyAnonChord");
            else
                guitar.TempReplaceChord(ChordCategory.Minor, "GreyAnonChord");
        }

        // ----- 新增效果：添加一张费用为 0 的 NotNeeded 到手牌 -----
        var notNeeded =  CombatState.CreateCard<NotNeeded>(Owner);
        notNeeded.EnergyCost.SetCustomBaseCost(0);   // 正确的方法名
        if (IsUpgraded)
        {
            notNeeded.UpgradeInternal();
            notNeeded.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(notNeeded, PileType.Hand, Owner);
        // -------------------------------------------------------
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}