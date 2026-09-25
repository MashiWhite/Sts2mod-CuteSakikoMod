using System.Collections.Generic;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class BagOfMatchaCandy : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 描述里提到了“附魔：MyGo了”，必须给出附魔提示
            foreach (var tip in HoverTipFactory.FromEnchantment<MyGoEnchantment>())
                yield return tip;
        }
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null) return;

        var enchantment = ModelDb.Enchantment<MyGoEnchantment>();
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 2, 2)
        {
            RequireManualConfirmation = true
        };

        var selected = await CardSelectCmd.FromDeckForEnchantment(
            Owner, enchantment, 1, null, prefs);

        foreach (var card in selected)
        {
            CardCmd.Enchant(enchantment.ToMutable(), card, 1);
        }
    }
}