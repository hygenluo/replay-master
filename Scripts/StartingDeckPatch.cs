using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReplayMaster.Cards;

namespace ReplayMaster.Patches;

[HarmonyPatch(typeof(Player), nameof(Player.PopulateStartingDeck))]
public static class StartingDeckPatch
{
    static void Postfix(Player __instance)
    {
        var template = ModelDb.Card<ReplayMasterCard>();
        if (__instance.Deck.Cards.Any(c => c.Id == template.Id))
            return;

        var card = template.ToMutable();
        card.FloorAddedToDeck = 1;
        __instance.Deck.AddInternal(card, -1, silent: true);
    }
}
