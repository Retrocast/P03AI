// Disclaimer: the code is terrible!
// I did most of the code editing in-game or in Code-OSS without any C# extensions. Basic syntax highlighting is all I had.
// And lastly, I have no frickin' clue how to code in C# XD
// But hey, at least it compiles/works, right?

// Uncomment imports (I have them in the injector, they are here for reference)
/*
using System;
using System.Collections;
using System.Linq;
using DiskCardGame;
using UnityEngine;
*/

#region General utilities
static IEnumerator coDisplayText(string text) {
  yield return Singleton<TextDisplayer>.Instance.ShowThenClear(text, 1, speaker: DialogueEvent.Speaker.Goo);
}
static void coExecute(IEnumerator co) {
  Singleton<TextDisplayer>.Instance.StartCoroutine(co);
}
static void displayText(string text) {
  coExecute(coDisplayText(text));
}
static Func<GameFlowManager> gfm = () => Singleton<GameFlowManager>.Instance;
static Func<NodeData> getMapNode = () => RunState.Run.map.nodeData.Find(n => n.id == RunState.Run.currentNodeId);
#endregion
#region API calls
static IEnumerator sendSystemMessage(string displayText, string text, string successText="Request sent successfully!") {
  string url = "http://localhost:1337/sendSystemMessage";
  var www = UnityEngine.Networking.UnityWebRequest.Post(url, $"{displayText}|{text}");
  yield return www.SendWebRequest();
  string _text;
  if (www.isNetworkError || www.isHttpError) {
    UnityEngine.Debug.LogError(www.error);
    _text = "[c:bR]Request error, check the console![c:]";
  } else {
    _text = successText;
  }
  yield return coDisplayText(_text);
}

static IEnumerator getResponse(string meta) {
  string url = "http://localhost:1337/getResponse";
  var www = UnityEngine.Networking.UnityWebRequest.Post(url, meta);
  yield return www.SendWebRequest();
  string _text;
  if (www.isNetworkError || www.isHttpError) {
    UnityEngine.Debug.LogError(www.error);
    _text = "[c:bR]Request error, check the console![c:]";
  } else {
    _text = "AI generation started";
  }
  yield return coDisplayText(_text);
}
#endregion
#region Basic summarizers
static string sConsumable(string name) {
  var c = ItemsUtil.GetConsumableByName(name);
  return $"{c.rulebookName}[{RuleBookPage.ParseCardDefinition(c.rulebookDescription)}]";
}
static string sConsumables() {
  return $"Consumable items ({RunState.Run.consumables.Count}/{RunState.Run.MaxConsumables}):\n{RunState.Run.consumables.Count == 0 ? "- You have none": string.Join("\n", RunState.Run.consumables.Select(x => $"- {sConsumable(x)}"))}";
}
static string sAbilityInfo(Ability a, string c = null) {
  return $"{AbilitiesUtil.GetInfo(a).rulebookName}[{RuleBookPage.ParseCardDefinition(AbilitiesUtil.GetInfo(a).rulebookDescription).Replace("[creature]", (c ?? "a card bearing this sigil"))}]";
}
static string sTotems() {
  var totems = RunState.Run.totems;
  var totem = totems.Count == 0 ? "you don't have one" : $"{totems[0].tribe} tribe creatures get {sAbilityInfo(totems[0].ability)}";
  var _tops = RunState.Run.totemTops;
  var tops = _tops.Count == 0 ? "you don't have any" : string.Join(", ", _tops);
  var _bottoms = RunState.Run.totemBottoms;
  var bottoms = _bottoms.Count == 0 ? "you don't have any" : string.Join(", ", _bottoms.Select(a => sAbilityInfo(a)));
  return $"YOUR Current totem (creatures of tribe X get sigil Y): {totem}\n[{_tops.Count}] YOUR totem tops (tribes): {tops}\n[{_bottoms.Count}] YOUR totem bottoms (sigils): {bottoms}";
}
static string sCardInfo(CardInfo c, bool withDescription=false) {
	if (c.name == "!STATIC!GLITCH") return "Static glitch card (turns into random card when drawn)";
  string cost = c.BloodCost == 0 ? (c.BonesCost == 0 ? "free" : $"{c.BonesCost} bone cost") : $"{c.BloodCost} blood cost";
  string power = c.SpecialStatIcon == SpecialStatIcon.None ? $"{c.Attack} power" : "power - " + (string.IsNullOrEmpty(StatIconInfo.GetIconInfo(c.SpecialStatIcon).gbcDescription) ? StatIconInfo.GetIconInfo(c.SpecialStatIcon).rulebookDescription : StatIconInfo.GetIconInfo(c.SpecialStatIcon).gbcDescription).Replace("[creature]", c.DisplayedNameEnglish);
  string tribes = (c.tribes.Count == 0 ? "not part of a" : string.Join(", ", c.tribes)) + " tribe" + (c.tribes.Count > 1 ? "s" : "");
  string naturalSigils = c.DefaultAbilities.Count == 0 ? "none" : string.Join(", ", c.DefaultAbilities.Select(a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  string infusedSigils = c.ModAbilities.Count == 0 ? "none" : string.Join(", ", c.ModAbilities.Select(a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  string description = (withDescription && !string.IsNullOrEmpty(c.description)) ? " // " + c.description : "";
  return $"{c.DisplayedNameEnglish} ({cost}; {power}; {c.Health} health; {tribes}; natural sigils - {naturalSigils}; infused sigils - {infusedSigils}){description}";
}
static string sDeck() {
  return $"Your deck:\n{string.Join("\n", RunState.DeckList.Select(c => $"- {sCardInfo(c)}"))}";
}
static string sPlayableCard(PlayableCard p, bool withCost=false) {
  var c = p.Info;
  string cost = (c.BloodCost == 0 ? (c.BonesCost == 0 ? "free" : $"{c.BonesCost} bone cost") : $"{c.BloodCost} blood cost") + "; ";
  string naturalSigils = c.DefaultAbilities.Count == 0 ? "none" : string.Join(", ", c.DefaultAbilities.Select(a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  string infusedSigils = c.ModAbilities.Count == 0 ? "" : "; infused sigils - " + string.Join(", ", c.ModAbilities.Select(a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  var _tempSigils = new List<Ability>();
  foreach (var mod in p.TemporaryMods) {
    foreach (var ability in mod.abilities) {
      _tempSigils.Add(ability);
    }
  }
  string tempSigils = _tempSigils.Count == 0 ? "" : "; temporary sigils(from totems/buffs/etc) - " + string.Join(", ", _tempSigils.Select(a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  return $"{c.DisplayedNameEnglish} ({withCost ? cost : ""}{p.Attack} power{c.SpecialStatIcon == SpecialStatIcon.None ? "" : "[" + (string.IsNullOrEmpty(StatIconInfo.GetIconInfo(c.SpecialStatIcon).gbcDescription) ? StatIconInfo.GetIconInfo(c.SpecialStatIcon).rulebookDescription : StatIconInfo.GetIconInfo(c.SpecialStatIcon).gbcDescription).Replace("[creature]", c.DisplayedNameEnglish) + "]"}; {p.Health} health; natural sigils - {naturalSigils}{infusedSigils}{tempSigils})";
}
static readonly Dictionary<string, string> MAP_REGIONS = new Dictionary<string, string>{
  { "Forest", "The Woodlands" },
  { "Wetlands", "The Wetlands" },
  { "Alpine", "The Snow Line" },
  // The most appropriate name (unfortunately it has no dialogue stating real one)
  { "Midnight", "Leshy's Cabin" },
  { "Midnight_Ascension", "Leshy's Cabin" },
  // HEAVE. HO.
  { "Pirateville", "The Pirateville" }
};
static string sMapRegion() {
  var boss = (RunState.Run.map.EndNode as BossBattleNodeData).bossType.ToString().Replace("Boss", "");
  if (boss == "PirateSkull") {
    boss = "Royal Dominguez";
  }
  return $"You're on the map region {RunState.Run.regionTier + 1}/4 - {MAP_REGIONS[RunState.CurrentMapRegion.name] ?? RunState.CurrentMapRegion.name}. Boss of this region is {boss}. {RunState.Run.regionTier == 3 ? "Ready for the final boss fight?" : "Keep in mind that region 4 is final boss fight. You might make decisions differently, knowing that."}";
}
static string getMetadata() {
  return $"{sDeck()}\n{sConsumables()}\n{sTotems()}\n{sMapRegion()}";
}
#endregion
#region Battle summarizers
static string sHand() {
  return $"Your hand:\n{string.Join("\n", Singleton<PlayerHand>.Instance.CardsInHand.Select(p => $"- {sPlayableCard(p, true)}"))}";
}

static string sBoardPlayerSide() {
  var bm = Singleton<BoardManager>.Instance;
  if (bm.PlayerSlotsCopy.Find(s => s.Card != null) == null) {
    return "Your side of the board is completely empty";
  }
  return $"Your side of the board:\n{string.Join("\n", bm.PlayerSlotsCopy.Select(s => s.Card == null ? "- Empty" : $"- {sPlayableCard(s.Card)}"))}";
}

static string sCardInSlot(PlayableCard p) {
  return p == null ? "- Empty" : $"- {sPlayableCard(p)}";
}

static string sBoardLeshySide() {
  var bm = Singleton<BoardManager>.Instance;
  var moonSlot = bm.OpponentSlotsCopy.Find(x => x.Card != null && x.Card.Info.HasTrait(Trait.Giant));
  if (moonSlot != null) {
    return $"Leshy's side of the board contains a single giant card that takes all 4 lanes - {sPlayableCard(moonSlot.Card)}";
  }
  var q = Singleton<TurnManager>.Instance.Opponent.Queue;
  var queue = "Leshy's queue is completely empty";
  if (q.Count > 0) {
    queue = $"Leshy's queue:\n{sCardInSlot(q.Find(x => x.QueuedSlot.Index == 0))}\n{sCardInSlot(q.Find(x => x.QueuedSlot.Index == 1))}\n{sCardInSlot(q.Find(x => x.QueuedSlot.Index == 2))}\n{sCardInSlot(q.Find(x => x.QueuedSlot.Index == 3))}";
  }
  var board = "Leshy's side of the board is completely empty";
  if (bm.OpponentSlotsCopy.Find(s => s.Card != null) != null) {
    board = $"Leshy's side of the board:\n{string.Join("\n", Singleton<BoardManager>.Instance.OpponentSlotsCopy.Select(s => sCardInSlot(s.Card)))}";
  }
  return $"{queue}\n{board}";
}

static string sScales() {
  var b = Singleton<LifeManager>.Instance.Balance;
  var i = $"Scales balance - {b}";
  if (b == 0) return i;
  if (b >= 5 || b <= -5) return $"{i} ({b > 0 ? "you" : "Leshy"} won)";
  if (b > 0) {
    return $"{i} ({5 - b} damage until you win)";
  } else {
    return $"{i} ({5 + b} damage until Leshy wins)";
  }
}

static string sHookGrab() {
  var grab = Singleton<TurnManager>.Instance.Opponent.GetComponent<FishHookGrab>();
  if (grab == null) {
    return "";
  }
  if (grab.hookTargetSlot == null || grab.hookTargetSlot.Card == null) {
    return "Hook is currently targeting nothing.";
  }
  return $"Hook is currently targeting {sPlayableCard(grab.hookTargetSlot.Card)}.";
}

static (bool, string, string) sOpponent() {
  var somethingSpecial = false;
  var meta = "";
  var _o = Singleton<TurnManager>.Instance.Opponent;
  var name = _o.GetType().Name;
  if (name == "Part1Opponent") {
    name = "NormalOpponent";
  }
  var totem = "no totem";
  if (_o is Part1Opponent o) {
    if (o.totem != null) {
      totem = $"totem that inscribes {sAbilityInfo(o.totem.TotemItemData.bottom.effectParams.ability)} sigil onto all Leshy's cards of tribe {o.totem.TotemItemData.top.prerequisites.tribe}";
    }
  }
  var notes = "";
  if (_o is AnglerBossOpponent a) {
    var baitBuckets = $"\nAttacking Bait Buckets will spawn {sCardInfo(CardLoader.GetCardByName("Shark"))}, so it can be smart to only play Airborne creatures or attack empty lanes, as well as sacrificing current cards that will trigger Great Whites and won't have enough health to take a hit from them. Keep in mind that because of Waterborne you will not be able to kill Great Whites once they are spawned, and can only attack Leshy directly while they are submerged.";
    if (a.NumLives == 2) {
      notes = $"\nEvery two turns Angler will start targeting a card with his hook, and target every newly placed card. At the end of next turn (AFTER his creatures attack), he will pull the targeted card to his side. If there's a card opposing it, it will be pushed back into queue. If there's a queued card already, it will be removed from the board. It can be beneficial to play a weak card like a Squirrel so it gets stolen instead of important cards. {sHookGrab()}{baitBuckets}";
    } else {
      notes = baitBuckets;
    }
  }
  if (_o is LeshyBossOpponent l) {
    var leshyPhase = "";
    switch (l.NumLives) {
      case 1:
        leshyPhase = "This is the final phase. DESTROY THE MOON.";
        break;
      case 2:
        leshyPhase = "This is the second phase. Leshy will only play powerful Deathcards, and obstruct your cards with terrain at the beginning.";
        break;
      case 3:
        leshyPhase = "This is the first phase. Leshy will only play powerful rare cards. He will alternate between masks of his subordinates, doing their boss fight gimmicks.";
        break;
    }
    var currentMask = "";
    if (l.NumLives > 1 && l.maskState == LeshyBossOpponent.MaskState.MaskEquipped) {
      var bossMask = l.maskBossTypes[l.currentMaskIndex];
      var mask = $" Leshy is currently wearing {bossMask.ToString().Replace("Boss", "")}'s mask.";
      switch (bossMask) {
        case Opponent.Type.ProspectorBoss:
          currentMask = $"{mask} At the end of this turn (BEFORE his creatures attack), he will hit all your cards with pickaxe, turning them into non-sacrificable Gold Nuggets, effectively locking up your board until they are killed by his creatures. Cards turned into Gold Nuggets do NOT return to your hand, so you might want to keep some of cards in your hand to avoid losing in next phases. Ending the phase by doing enough scales damage delays this to the next turn.";
          break;
        case Opponent.Type.AnglerBoss:
          currentMask = $"{mask} Leshy will initially target a random card with his hook, and then target any played card. And the end of this turn (BEFORE his creatures attack), he will pull the targeted card to his side. If there's a card opposing it, it will be pushed back into queue. If there's a queued card already, it will be removed from the board. It can be beneficial to play a weak card like a Squirrel opposing a threatening creature, so it gets pushed back instead of attacking you. {sHookGrab()}";
          break;
        case Opponent.Type.TrapperTraderBoss:
          currentMask = $"{mask} At the end of this turn (BEFORE his creatures attack), he will play two new cards and let you to take any card from his side into your hand, but the rest will stay to fight against you.";
          break;
      }
    }
    notes = $"\n{leshyPhase}{currentMask}";
  }
  if (_o is ProspectorBossOpponent p && p.NumLives == 2) {
    notes = "\nAt the end of phase 1, Prospector will strike all your cards with his pickaxe, turning them into non-sacrificable Gold Nuggets, effectively locking up your board until they are killed by his creatures. Cards turned into Gold Nuggets do NOT return to your hand, so you might want to keep some of cards in your hand to avoid losing in phase 2.\nKilling Pack Mule will give you free Squirrel and 3 random cards, so you may want to do that to have enough cards after Gold Nugget board wipe.";
  }
  if (_o is TrapperTraderBossOpponent t) {
    var trade = t.GetComponent<TradeCardsForPelts>();
    var pelts = Singleton<PlayerHand>.Instance.CardsInHand.Where((PlayableCard x) => x.Info.HasTrait(Trait.Pelt)).ToList();
    // You can't really tell whether trading is over or not without hooking deep into the code, so I'm just checking for other signs.
    // Condition should be rare enough to never give false positives, from what it seems only TradeCardsForPelts and MagnificusGameFlowManager disable the bell like this.
    if (t.NumLives == 1 && trade != null && pelts.Count > 0 && Singleton<PlayerHand>.Instance.PlayingLocked && !(Singleton<BoardManager>.Instance as BoardManager3D).Bell.enabled) {
      var numPelts = Mathf.Min(pelts.Count, 8);
      somethingSpecial = true;
      meta = $"\nTime for the Trader gimmick. You may take {numPelts} card{numPelts > 1 ? "s" : ""} from opponent side of the board or queue, trading them for pelts in your hand. Keep in mind that rest will stay to fight against you, so sometimes it might be smart to take away a dangerous card even if you do not plan to play it, or take cards that oppose your powerful cards so you can win quicker.";
    }
    if (t.NumLives == 2) {
      notes = "\nStrange Frogs turn into Leaping Traps when killed, be careful! At the end of phase 1, Trader will play 8 powerful cards, allowing you to trade pelts you have for them. You get pelts from cards perishing by Leaping Traps, but be careful to not kill all your good cards to get pelts, since remaining cards you couldn't afford will stay to fight against you.";
    }
  }
  var phase = "";
  if (_o is Part1BossOpponent b) {
    phase = $"[Phase {b.StartingLives - b.NumLives + 1}/{b.StartingLives}]";
  }
  return (somethingSpecial, $"Currently in battle with {name}{phase} w/ {totem}{notes}", meta);
}

static string sPiles() {
  var cdp = CardDrawPiles3D.Instance;
  return $"Cards remaining in main pile (deck) - {cdp.pile.NumCards}\nCards remaining in side pile (squirrels) - {cdp.sidePile.NumCards}";
}

static string sBattle() {
  var (somethingSpecial, opponent, meta) = sOpponent();
  var draw = "You have already drawn a card. Now you have to decide what cards to play";
  if (Singleton<TurnManager>.Instance.PlayerPhase == TurnManager.PlayerTurnPhase.Draw) {
    draw = $"You must choose whether you want to draw from main pile (remaining cards in your deck) or side pile (guaranteed Squirrel). If you choose main pile, do NOT specify your play yet, since in most cases you do not know what card you'll draw.";
  }
  var surrender = "";
  if (Singleton<TurnManager>.Instance.Opponent.OfferingSurrender) {
    surrender = "\nLeshy had ran out of cards and offers you surrender. If you accept it, you just win the battle and continue going. It has no consequences. IN 99.9% OF CASES YOU SHOULD ACCEPT IT, SINCE YOU JUST WIN WITH 100% CHANCE INSTEAD OF RISKING OR WASTING TIME.";
  }
  var basic = $"{opponent}\nTurn #{Singleton<TurnManager>.Instance.TurnNumber}\n{sScales()}\n{sBoardLeshySide()}\n{sBoardPlayerSide()}\n{sHand()}\nYou have {Singleton<ResourcesManager>.Instance.PlayerBones} bones (you get one each time your creature perishes for any reason)";
  if (!somethingSpecial) {
    return $"{basic}\n{sPiles()}\n{draw}\nWhen specifying your plays, make sure to explicitly clarify all the card placements and what cards to sacrifice.{surrender}";
  } else {
    return $"{basic}\n{meta}";
  }
}
#endregion
#region Event summarizers
static Func<SpecialNodeHandler> snh = () => Singleton<SpecialNodeHandler>.Instance;
static string sTotemPiece(SelectableItemSlot s) {
  var data = s.Item.Data;
  if (data is TotemTopData t) {
    return $"Totem top for tribe {t.prerequisites.tribe}";
  }
  if (data is TotemBottomData b) {
    return $"Totem bottom for sigil {sAbilityInfo(b.effectParams.ability)}";
  }
  return $"Unknown totem piece. Blame Retrocast. Ask for additional information.";
}
static string sWoodcarverEvent() {
  var intro = "You are currently at Woodcarver's event, where you pick one of 3 totem pieces. Tops indicate a tribe. Bottoms indicate the sigil that will be added to all cards of that tribe on top. You need at least one top and bottom to build a totem. If you already have a totem, but one of offered pieces will make it even better, pick it and say you want to change the totem. If your current is better than what you can build with current options, just pick the lesser of evils and say you want to keep the current totem.";
  var seq = snh().buildTotemSequencer;
  if (seq.GetFirstEmptyInventorySlot() == null || (RunState.Run.totemTops.Count+RunState.Run.totemBottoms.Count) >= 7) {
    CardInfo amalgam = null;
    foreach (var s in GameObject.FindObjectsOfType<SelectableCard>()) {
      var c = s.Info;
      // Not entirely sure about cardSelected check. Seems good enough, but might break shit in future.
      // Definitely the first candidate for removal in case of issues, I assume there aren't many Amalgam SelectableCards lying around in this event anyways.
      if (c != null && c.name == "Amalgam" && s.cardSelected != null && s.cardSelected.Method != null && s.cardSelected.Method.Name.Contains("TotemSlotsFull")) {
        amalgam = c;
      }
    }
    if (amalgam == null) return null;
    return $"{intro}\n\nA look of regret fell over the old woodcarver. You were overburdened with totem pieces and you could carry no more.\nShe gestured toward a disturbing creature lurking nearby.\n{sCardInfo(amalgam)} was added to your deck.\nCanine. Hooved. Reptile. Bird. Insect. The Amalgam is all.\n";
  }
  var pieces = string.Join("\n", seq.slots.Select(s => $"- {sTotemPiece(s)}"));
  return $"{intro}\nTotem pieces you can pick:\n{pieces}";
}

static string sTrapperEvent() {
  var seq = snh().buyPeltsSequencer;
  var p = seq.PeltPrices;
  return $"You are currently at Trapper's event, where you can exchange teeth(overkill damage) for Rabbit/Wolf/Golden Pelts. Pelts are added as cards to your deck, and can be exchanged for proper cards at Trader's event (Rabbit - random cards, Wolf - random cards with infused sigils, Golden - rares). They are free cards that can't be sacrificed and have 1, 2 and 3 Health respectively, and can be used as damage blockers (you do not lose them if they die in battle). You receive one Rabbit Pelt free of charge. You currently have {RunState.Run.currency} teeth. Prices are:\n- {p[0]} teeth per Rabbit Pelt\n- {p[1]} teeth per Wolf Pelt\n- {p[2]} teeth for Golden Pelt\n{(RunState.Run.trapperKnifeBought || RunState.Run.consumables.Count >= RunState.Run.MaxConsumables) ? "" : "- 7 teeth for Trapper's Knife, a one-time consumable item that acts similarly to Scissors, destroying selected Leshy's card, but also gives you a free Wolf Pelt in hand"}\n\nYou are not required to buy anything, and you can pass, if you feel like you don't want to clutter your deck with pelts.";
}

static string sPackEvent() {
  var seq = snh().gainConsumablesSequencer;
  if (RunState.Run.consumables.Count == RunState.Run.MaxConsumables) {
    return $"Your pack was full... But a small critter approached.\n{sCardInfo(seq.fullConsumablesReward)} was added to your deck.";
  }
  var items = string.Join("\n", seq.slots.Select(s => $"- {sConsumable(s.Item.Data.name)}"));
  var more = RunState.Run.MaxConsumables - RunState.Run.consumables.Count - 1;
  return $"You are currently at Pack event. Chose ONE of the following items:\n{items}{more > 0 ? $"\nYou will be asked {more} more time(s) with different items in next message(s)." : ""}";
}

static string sTrader() {
  var seq = snh().tradePeltsSequencer;
  return $"You are currently at Trader's event, where you can exchange your pelts for cards. Choose {seq.peltCards.Count} cards from the following:\n{string.Join("\n", seq.tradeCards.Select(s => $"- {sCardInfo(s.Info)}"))}";
}

static string sMysteriousStones() {
  var seq = snh().cardMerger;
  var hosts = string.Join("\n", seq.GetValidCardsForHost().Select(c => $"- {sCardInfo(c)}"));
  var sacrifices = string.Join("\n", seq.GetValidCardsForSacrifice().Select(c => $"- {sCardInfo(c)}"));
  return $"You are currently at Mysterious Stones event, where you can sacrifice a card to transfer its sigils to another card. Pick the sacrifice and the host (card that will receive the sigils). It can't be the same card!\nSacrifice list:\n{sacrifices}\nHost list:\n{hosts}";
}

static string sCardChoice(CardChoicesNodeData c) {
  var seq = snh().cardChoiceSequencer;
  var firstCard = (new List<SelectableCard>(seq.gameObject.GetComponentsInChildren<SelectableCard>())).Find(s => s.Info != null);
  switch (c.choicesType) {
    case CardChoicesType.Random:
    case CardChoicesType.Deathcard:
      var cards = string.Join("\n", seq.gameObject.GetComponentsInChildren<SelectableCard>().Select(s => $"- {sCardInfo(s.Info, true)}"));
      return $"You are currently at {c.choicesType == CardChoicesType.Deathcard ? "death" : ""}card choice event. Pick one of the following cards:\n{cards}{seq.choicesRerolled ? "" : "\nYou may also reroll the cards with clover, but you can do so only once per event."}";
    case CardChoicesType.Tribe:
      if (firstCard != null) {
        return $"You got {sCardInfo(firstCard.Info, true)}";
      }
      var tribes = string.Join("\n", seq.gameObject.GetComponentsInChildren<SelectableCard>().Select(s => $"- {s.ChoiceInfo.tribe}"));
      return $"You are currently at tribe-based card choice event. Pick one of the following tribes:\n{tribes}{seq.choicesRerolled ? "" : "\nYou may also reroll the tribes (not cards) with clover, but you can do so only once per event."}";
    case CardChoicesType.Cost:
      if (firstCard != null) {
        return $"You got {sCardInfo(firstCard.Info, true)}";
      }
      var costs = string.Join("\n", seq.gameObject.GetComponentsInChildren<SelectableCard>().Select(s => $"- {s.ChoiceInfo.resourceType == ResourceType.Bone ? "Any bone cost" : $"{s.ChoiceInfo.resourceAmount} blood cost"}"));
      return $"You are currently at cost-based card choice event. Pick one of the following costs:\n{costs}{seq.choicesRerolled ? "" : "\nYou may also reroll the costs (not cards) with clover, but you can do so only once per event."}";
    default:
      return null;
  }
}

static string sCampfire() {
  var seq = snh().cardStatBoostSequencer;
  var isAttack = seq.selectionSlot.specificRenderers[0].material.mainTexture == seq.attackModSlotTexture;
  var cards = string.Join("\n", seq.GetValidCards(isAttack).Select(c => $"- {sCardInfo(c)}"));
  return $"You are currently at Campfire event. Pick one of the following cards:\n{cards}\nCampfire will increase its {isAttack ? "Power by 1" : "Health by 2"}.\n{RunState.Run.survivorsDead ? "Since survivors are dead, you can upgrade second time without any risk!" : "You may also upgrade for second time, but it has 50% risk to lose your card."}";
}

static string sBossRares() {
  var cards = string.Join("\n", snh().rareCardChoiceSequencer.gameObject.GetComponentsInChildren<SelectableCard>().Select(s => $"- {sCardInfo(s.Info, true)}"));
  return $"As a reward for beating a boss, pick one of the following cards:\n{cards}\nAfter choosing the card, you will advance to the next map region.";
}

static string sDeckTrial() {
  var seq = snh().deckTrialSequencer;
  var cards = seq.transform.Find("TrialCardsAnchor").GetComponentsInChildren<SelectableCard>().Where(s => s.transform.parent.gameObject.name != "DrawnCardsAnchor");
  if ((new List<SelectableCard>(cards)).Find(s => s.Info != null && s.Info.metaCategories.Contains(CardMetaCategory.ChoiceNode)) != null) {
    return $"Pick your reward:\n{string.Join("\n", cards.Select(s => $"- {sCardInfo(s.Info)}"))}";
  }
  var trials = string.Join("\n", seq.trialChoices.Select(t => $"- Trial of {t.GetTypeStringLocalized()}[{t.description}]"));
  return $"You are currently at Deck Trial event. Pick one of the trials, and if you pass it, you'll get to add a powerful card to your deck:\n{trials}";
}

static string sMycologists() {
  var seq = snh().duplicateMerger;
  if (seq.GetValidDuplicateCards().Count == 0) {
    var cards = string.Join("\n", seq.gameObject.GetComponentsInChildren<SelectableCard>().Select(s => $"- {sCardInfo(s.Info)}"));
    return $"You are currently at the Mycologists' event. They would usually merge duplicate cards into more powerful single card, but you have none. So, they offered you to take one of their cards instead. Choose one:\n{cards}";
  }
  var pairs = string.Join("\n", (seq.selectionSlot.cardSelector as SelectableCardPairArray).pairs.Select(p => $"Combine {sCardInfo(p.LeftCard.Info)} + {sCardInfo(p.RightCard.Info)}"));
  return $"You are currently at the Mycologists' event. They will merge duplicate cards from your deck into more powerful single card while keeping price the same. Pick a pair:\n{pairs}";
}

static string sBoneLord() {
  return $"You came across some familiar stones. But there was something different...\nYou intuited that the fate of the creature selected for this... would not be pleasant.\nChoose, which card to remove from {sDeck()}";
}

static string sProspectorGamble() {
  var card = new List<SelectableCard>(snh().boulderChoiceSequencer.gameObject.GetComponentsInChildren<DiskCardGame.SelectableCard>()).Find(x => x.Info.name != "Boulder");
  if (card == null) {
    return $"You are currently at Prospector's event.\n\"Iffen ye can pick a boulder that has gold in it... Ye can keep it!\"\nThere are three boulders - left, center, and right.\n\"Show me where to strike!\"";
  }
  if (card.Info.name == "PeltGolden") {
    return $"\"\"\"Heeeeeeee-haaw! 'tis gold!\nI can barely abide giving it to ye...\nBut a promise is a promise where I'm from.\"\"\"\n{sCardInfo(card.Info)} was added to your deck.";
  }
  return $"\"\"\"Dag nab it... no gold.\nBut that is a funny lookin' varmint.\nKeep it.\"\"\"\n{sCardInfo(card.Info)} was added to your deck.";
}

static string sGoobert() {
  var seq = snh().copyCardSequencer;
  var lastCard = RunState.DeckList.Last();
  var lastCardCopied = false;
  foreach (var mod in lastCard.Mods) {
    if (mod.singletonId == "paint_decal") {
      lastCardCopied = true;
    }
  }
  if (lastCardCopied) {
    return $"Goobert's copy was added to your deck - {sCardInfo(lastCard)}";
  }
  var cards = string.Join("\n", seq.GetValidCards().Select(c => $"- {sCardInfo(c)}"));
  return $"You are currently on Goobert's event, where he may draw a copy of one of your cards. Keep in mind that copies may not always be perfect, but hey, Goobert tries his best. Pick one of the following cards:\n{cards}";
}

static string sEvent() {
  var n = getMapNode();
  if (n is BossBattleNodeData) {
    return sBossRares();
  }
  if (n is BoulderChoiceNodeData) {
    return sProspectorGamble();
  }
  if (n is BuildTotemNodeData) {
    return sWoodcarverEvent();
  }
  if (n is BuyPeltsNodeData) {
    return sTrapperEvent();
  }
  if (n is CardChoicesNodeData c) {
    return sCardChoice(c);
  }
  if (n is CardMergeNodeData) {
    return sMysteriousStones();
  }
  if (n is CardRemoveNodeData) {
    return sBoneLord();
  }
  if (n is CardStatBoostNodeData) {
    return sCampfire();
  }
  if (n is CopyCardNodeData) {
    return sGoobert();
  }
  if (n is DeckTrialNodeData) {
    return sDeckTrial();
  }
  if (n is DuplicateMergeNodeData) {
    return sMycologists();
  }
  if (n is GainConsumablesNodeData) {
    return sPackEvent();
  }
  if (n is TradePeltsNodeData) {
    return sTrader();
  }
  return null;
}
#endregion
#region Map summarizers
static string sNodeData(NodeData n) {
  if (n is BossBattleNodeData b) {
    return $"Boss battle node - {b.bossType}BattleSequencer";
  }
  if (n is BoulderChoiceNodeData) {
    return "Boulder choice node[Pick one of 3 boulders to hit. One of them contains gold. KEEP GAMBLING]";
  }
  if (n is BuildTotemNodeData) {
    return "Woodcarver node[Pick one of 3 totem parts. Once you have at least one top and bottom, build a totem, adding a sigil to all creatures of specific tribe.]";
  }
  if (n is BuyPeltsNodeData) {
    return $"Trapper node[Exchange teeth(overkill damage) for Rabbit/Wolf/Golden pelts. You currently have {RunState.Run.currency} teeth.]";
  }
  if (n is TotemBattleNodeData) {
    return "Totem battle node[Card battle, but all Leshy's creatures of specific tribe will have an additional sigil.]";
  }
  if (n is CardBattleNodeData) {
    return "Normal card battle node";
  }
  if (n is CardChoicesNodeData c) {
    switch (c.choicesType) {
      case CardChoicesType.Random:
        return "Card choice node[Pick a card from 3 randomly chosen ones to add to your deck. You may optionally reroll it once with a clover to get another 3 cards.]";
      case CardChoicesType.Cost:
        return "Cost-based card choice node[Pick a cost from 3 randomly chosen ones and add random card of that cost to your deck. You may optionally reroll costs once with a clover, but you may not reroll the actual card.]";
      case CardChoicesType.Tribe:
        return "Tribe-based card choice node[Pick a tribe from 3 randomly chosen ones and add random card of that tribe to your deck. You may optionally reroll tribes once with a clover, but you may not reroll the actual card.]";
      case CardChoicesType.Deathcard:
        return "Deathcard choice node[Pick a deathcard from 3 randomly chosen ones to add to your deck.]";
    }
  }
  if (n is CardMergeNodeData) {
    return "Mysterious stones node[Sacrifice one card, removing it from your deck, and transferring its natural sigils to another card in your deck.]";
  }
  if (n is CardRemoveNodeData) {
    return "Bone Lord node[Sacrifice one card, removing it from your deck. Bone Lord will be pleased, if sacrifice is good enough.]";
  }
  if (n is CardStatBoostNodeData) {
    return "Campfire node[Increase card's power or health. First time is entirely safe, second time has some risk.]";
  }
  if (n is CopyCardNodeData) {
    return "Goobert node[Goobert will draw a copy of chosen card and add it to your deck.]";
  }
  if (n is DeckTrialNodeData) {
    return "Deck trial node[Pick one of 3 powerful cards, but only if you pass a trial of choice.]";
  }
  if (n is DuplicateMergeNodeData) {
    return "Mycologists node[Mycologists will fuse two of the same cards into one, combining stats of both while keeping price the same. If you have no duplicates, they will offer you one instead.]";
  }
  if (n is GainConsumablesNodeData) {
    return "Pack node[Refill your consumable items. If all your item slots are full, receive a Pack Rat card instead.]";
  }
  if (n is TradePeltsNodeData) {
    return "Trader node[Exchange pelts you have for cards. If you have no pelts, receive 5 teeth instead.]";
  }
  UnityEngine.Debug.LogError($"Unknown node type encountered: {n}");
  return "Unknown node[Something went wrong. Blame Retrocast.]";
}

static string sMap(NodeData startNode) {
  return sNode(startNode, 0);
}
static string sNode(NodeData node, int depth) {
  if (depth >= 4) return "";
  var indent = new string(' ', depth * 2);
  var summary = depth == 0 ? "→ You're here\n" : indent + "→ " + sNodeData(node) + "\n";
  foreach (var nextNode in node.connectedNodes) {
    summary += sNode(nextNode, depth + 1);
  }
  return summary;
}
#endregion
#region Key handler
static void Update() {
  if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.RightBracket)) {
    coExecute(getResponse(getMetadata()));
  }
  if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.LeftBracket)) {
    switch (gfm().CurrentGameState) {
      case GameState.CardBattle:
        coExecute(sendSystemMessage("battle summary", sBattle()));
        break;
      case GameState.Map:
        var node = getMapNode();
        var nodes = node.connectedNodes;
        if (nodes.Count == 0) {
          displayText("[c:bR]No map nodes![c:]");
          return;
        }
        if (nodes.Count == 1) {
          displayText("[c:bR]Only one map node! No need to ask AI about it.[c:]");
          return;
        }
        var nodeDefs = string.Join("\n", nodes.Select(n => $"- {sNodeData(n)}"));
        coExecute(sendSystemMessage("map summary", $"You are currently on the game map and you have a choice to make. Map structure for reference:\n{sMap(node)}\n\nYou must choose one of the following nodes:\n{nodeDefs}\nYou must ONLY PICK THE NODE, do NOT specify what exactly to do on it, you will be explicitly asked in next message(s)."));
        return;
      case GameState.FirstPerson3D:
        displayText("[c:bR]Cannot get data from FirstPerson3D![c:]");
        break;
      case GameState.SpecialCardSequence:
        string s = sEvent();
        if (s == null) {
          displayText("[c:bR]Summary is not yet implemented for this.[c:]");
          return;
        }
        coExecute(sendSystemMessage($"{getMapNode().GetType().Name} event summary", s));
        break;
    }
  }
}
#endregion

// The thingy that actually calls the Update as patch for ManagedUpdate of OilPaintingPuzzle and wraps it in try/catch.
// Seems like a good candidate, since it is always present in cabin in single instance.
// In case it causes any issues, it can be moved to any other single/global object's Update/ManagedUpdate.
static void Postfix(OilPaintingPuzzle __instance) {
  try {
    Update();
  } catch (System.Exception ex) {
    UnityEngine.Debug.LogError($"Congrats, Retrocast, you fucked up!\n{ex}");
  }
}
