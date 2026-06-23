using System.IO;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ReplayMaster.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class ReplayMasterCard : CustomCardModel
{
    private const string ReplayKey = "Replay";

    public override string PortraitPath => "res://ReplayMaster/images/cards/ReplayMaster.png";

    private static Texture2D? s_cachedPortrait;
    private static bool s_portraitTried;

    /// <summary>
    /// Tries the <c>res://</c> path first (works when a PCK is exported).
    /// Falls back to loading from disk so the card art renders even without a PCK.
    /// </summary>
    public override Texture2D? CustomPortrait
    {
        get
        {
            if (s_portraitTried)
                return s_cachedPortrait;
            s_portraitTried = true;

            // 1. Try PCK (res://)
            if (ResourceLoader.Exists(PortraitPath))
                return s_cachedPortrait = ResourceLoader.Load<Texture2D>(PortraitPath);

            // 2. Try disk (no PCK needed)
            var modDir = Path.GetDirectoryName(typeof(Entry).Assembly.Location);
            if (!string.IsNullOrEmpty(modDir))
            {
                var diskPath = Path.Combine(modDir, "images", "cards", "ReplayMaster.png");
                if (File.Exists(diskPath))
                {
                    try
                    {
                        var image = Image.LoadFromFile(diskPath);
                        return s_cachedPortrait = ImageTexture.CreateFromImage(image);
                    }
                    catch
                    {
                        // Image loading failed; fall through to null
                    }
                }
            }

            return s_cachedPortrait; // null
        }
    }

    /// <summary>
    /// Card localization injected directly into the loc table at runtime.
    /// <b>No PCK export needed.</b> BaseLib.ModelLocPatch reads this property
    /// and inserts the entries into LocTable._translations.
    /// </summary>
    public override List<(string, string)>? Localization => new CardLoc(
        "Replay Master",
        "Choose a card in your hand to gain [gold]{Replay:diff()}[/gold] [gold]Replay[/gold]. Return this card to your hand."
    );

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Retain];

    public override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar(ReplayKey, 2m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    public ReplayMasterCard()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var replayBonus = DynamicVars[ReplayKey].IntValue;

        // 必须用原版已存在的 loc 表名（如 cards），mod 仅合并同名 json；独立 replay_master_ui 表不会被加载。
        var prefs = new CardSelectorPrefs(
            new LocString("cards", "REPLAYMASTER_HAND_SELECT_REPLAY"),
            1);

        // 立即 ToList 固化选择结果；在手牌堆中解析出与 UI 一致的实例并刷新 NCard，避免写回/预览不生效。
        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            filter: c => !ReferenceEquals(c, cardPlay.Card),
            source: this)).ToList();

        foreach (var picked in selected)
        {
            if (picked == null)
                continue;

            var handCards = PileType.Hand.GetPile(Owner).Cards;
            var target = handCards.FirstOrDefault(c => ReferenceEquals(c, picked)) ?? picked;

            target.BaseReplayCount += replayBonus;

            NCard.FindOnTable(target)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
            CardCmd.Preview(target);
        }
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card, this))
            return;

        if (Pile?.Type == PileType.Play)
            await CardPileCmd.Add(this, PileType.Hand);
    }

    public override void OnUpgrade()
    {
        DynamicVars[ReplayKey].UpgradeValueBy(1m);
    }
}


