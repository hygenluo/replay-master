using BaseLib.Abstracts;
using BaseLib.Utils;
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

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    public override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar(ReplayKey, 2m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.ReplayStatic)];

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

    public override void OnUpgrade()
    {
        DynamicVars[ReplayKey].UpgradeValueBy(1m);
    }
}


