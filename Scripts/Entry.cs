using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ReplayMaster.Cards;

namespace ReplayMaster;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public static void Init()
    {
        _ = typeof(ReplayMasterCard);
        var harmony = new Harmony("ReplayMaster");
        harmony.PatchAll(typeof(Entry).Assembly);
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Info("ReplayMaster mod initialized (enable with BaseLib; expect this line when both mods load).");
    }
}
