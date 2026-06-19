using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Keywords;

// 新增，用于 CardPileCmd

// 新增，引用 NotNeeded

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

// ReSharper disable once InconsistentNaming
public class AiHeart() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string ChordId => "GreyAnonChord";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CutesakiKeywords.NoNote.GetModCardKeyword(), CutesakiKeywords.Chord.GetModCardKeyword(),
        CutesakiKeywords.OtherAnon.GetModCardKeyword()
    ];

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
        if (guitar == null) return;
        
        const string chordId = "GreyAnonChord";
        // 若临时槽中还未拥有该和弦，则添加临时槽位；否则直接储存一个和弦
        var temporaryChords = guitar.GetTemporaryChords(); // 需公开此方法，见下方说明
        if (temporaryChords.Contains(chordId))
            await guitar.AddChordToStored(choiceContext, chordId);
        else
            guitar.AddTemporaryChord(chordId);

        // ----- 新增效果：添加一张费用为 0 的 NotNeeded 到手牌 -----
        var notNeeded = CombatState.CreateCard<NotNeeded>(Owner);
        notNeeded.EnergyCost.SetCustomBaseCost(0); // 正确的方法名
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