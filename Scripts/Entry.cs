using System.IO;
using System.Text.Json;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
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

        // Load localization JSON files from disk (no PCK export needed).
        // CardLoc on ReplayMasterCard provides the English fallback;
        // this overwrites entries for the current OS locale (e.g. zhs).
        LoadDiskLocalization();

        Log.Info("ReplayMaster mod initialized (enable with BaseLib; expect this line when both mods load).");
    }

    /// <summary>
    /// Reads &lt;modDir&gt;/localization/{lang}/cards.json from disk and injects
    /// every entry into the current language's LocTable._translations dictionary.
    /// This supplements (and may overwrite) the code‑based English CardLoc.
    /// </summary>
    private static void LoadDiskLocalization()
    {
        var modDir = Path.GetDirectoryName(typeof(Entry).Assembly.Location);
        if (string.IsNullOrEmpty(modDir))
            return;

        // Match OS locale to a localization subdirectory.
        var locale = OS.GetLocale(); // e.g. "zh_CN", "en_US"
        var lang = locale.StartsWith("zh") ? "zhs" : "eng";

        var locPath = Path.Combine(modDir, "localization", lang, "cards.json");
        if (!File.Exists(locPath))
        {
            // Fall back to English if the detected language isn't available.
            locPath = Path.Combine(modDir, "localization", "eng", "cards.json");
            if (!File.Exists(locPath))
            {
                Log.Warn("ReplayMaster: No disk localization found; using code-based English fallback.");
                return;
            }
        }

        try
        {
            var json = File.ReadAllText(locPath);
            var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (entries == null || entries.Count == 0)
                return;

            // Access the private _translations dictionary on LocTable
            // (same technique BaseLib.ModelLocPatch uses).
            var cardsTable = LocManager.Instance.GetTable("cards");
            var translationsField = AccessTools.Field(typeof(LocTable), "_translations");
            if (translationsField == null)
                return;
            if (translationsField.GetValue(cardsTable) is not Dictionary<string, string> translations)
                return;

            foreach (var kvp in entries)
                translations[kvp.Key] = kvp.Value;

            Log.Info($"ReplayMaster: Loaded {entries.Count} loc entries from {locPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"ReplayMaster: Failed to load localization from {locPath}: {ex.Message}");
        }
    }
}
