using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class SkipClass : CuteRanaCard
{
    public override bool GainsBlock => true;
    
    private bool _hasPlayedCardThisTurn;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain,CardKeyword.Exhaust];

    public SkipClass() : base(-1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override bool HasEnergyCostX => true;

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable) return false;
            return !_hasPlayedCardThisTurn;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new BlockVar(11m, ValueProp.Move)
    };

    // 每回合开始时重置标记
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            _hasPlayedCardThisTurn = false;
        return Task.CompletedTask;
    }

    // 每次有牌打出时检查是否为自身打出
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card?.Owner == Owner)
            _hasPlayedCardThisTurn = true;
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();

        for (int i = 0; i < x; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m); 
    }
}