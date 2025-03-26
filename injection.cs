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
static GameFlowManager gfm = Singleton<GameFlowManager>.Instance;
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
static string getMetadata() {
  return $"{sDeck()}\n{sConsumables()}\n{sTotems()}";
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

static string sOpponent() {
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
  return $"Currently in battle with [{name}] w/ {totem}";
}

static string sPiles() {
  var cdp = CardDrawPiles3D.Instance;
  return $"Cards remaining in main pile (deck) - {cdp.pile.NumCards}\nCards remaining in side pile (squirrels) - {cdp.sidePile.NumCards}";
}

static string sBattle() {
  var draw = "\nYou have already drawn a card. Now you have to decide what cards to play";
  if (Singleton<TurnManager>.Instance.PlayerPhase == TurnManager.PlayerTurnPhase.Draw) {
    draw = $"\nYou must choose whether you want to draw from main pile (remaining cards in your deck) or side pile (guaranteed Squirrel). If you choose main pile, do NOT specify your play yet, since in most cases you do not know what card you'll draw.";
  }
  return $"{sOpponent()}\nTurn #{Singleton<TurnManager>.Instance.TurnNumber}\n{sScales()}\n{sBoardLeshySide()}\n{sBoardPlayerSide()}\n{sHand()}\nYou have {Singleton<ResourcesManager>.Instance.PlayerBones} bones (you get one each time your creature perishes for any reason)\n{sPiles()}\n{draw}\nWhen specifying your plays, make sure to explicitly clarify all the card placements and what cards to sacrifice.";
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
  var seq = snh().buildTotemSequencer;
  if (seq.GetFirstEmptyInventorySlot() == null || (RunState.Run.totemTops.Count+RunState.Run.totemBottoms.Count) >= 7) {
    var amalgam = seq.gameObject.GetComponentInChildren<SelectableCard>().Info;
    return $"A look of regret fell over the old woodcarver. You were overburdened with totem pieces and you could carry no more.\nShe gestured toward a disturbing creature lurking nearby.\n{sCardInfo(amalgam)} was added to your deck.\nCanine. Hooved. Reptile. Bird. Insect. The Amalgam is all.";
  }
  var pieces = string.Join("\n", seq.slots.Select(s => $"- {sTotemPiece(s)}"));
  return $"You are currently at Woodcarver's event, where you pick one of 3 totem pieces. Tops indicate a tribe. Bottoms indicate the sigil that will be added to all cards of that tribe on top. You need at least one top and bottom to build a totem. If you already have a totem, but one of offered pieces will make it even better, pick it and say you want to change the totem. If your current is better than what you can build with current options, just pick the lesser of evils and say you want to keep the current totem.\nTotem pieces you can pick:\n{pieces}";
}

static string sTrapperEvent() {
  var seq = snh().buyPeltsSequencer;
  var p = seq.PeltPrices;
  return $"You are currently at Trapper's event, where you can exchange teeth(overkill damage) for Rabbit/Wolf/Golden Pelts. Pelts are added as cards to your deck, and can be exchanged for proper cards at Trader's event (Rabbit - random cards, Wolf - random cards with infused sigils, Golden - rares). They are free cards that can't be sacrificed and have 1, 2 and 3 Health respectively, and can be used as damage blockers (you do not lose them if they die in battle). You receive one Rabbit Pelt free of charge. You currently have {RunState.Run.currency} teeth. Prices are:\n- {p[0]} teeth per Rabbit Pelt\n- {p[1]} teeth per Wolf Pelt\n- {p[2]} teeth for Golden Pelt\n{(RunState.Run.trapperKnifeBought || RunState.Run.consumables.Count > RunState.Run.MaxConsumables) ? "" : "- 7 teeth for Trapper's Knife, a one-time consumable item that acts similarly to Scissors, destroying selected Leshy's card, but also gives you a free Wolf Pelt in hand"}\n\nYou are not required to buy anything, and you can pass, if you feel like you don't want to clutter your deck with pelts.";
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
  switch (c.choicesType) {
    case CardChoicesType.Random:
      var cards = string.Join("\n", seq.gameObject.GetComponentsInChildren<SelectableCard>().Select(s => $"- {sCardInfo(s.Info)}"));
      return $"You are currently at card choice event. Pick one of the following cards:\n{cards}{seq.choicesRerolled ? "" : "\nYou may also reroll the cards with clover, but you can do so only once per event."}";
    default:
      return null; // TODO: Add other card choice types.
  }
}

static string sEvent() {
  var n = getMapNode();
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
    switch (gfm.CurrentGameState) {
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
