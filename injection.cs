// Disclaimer: the code is terrible!
// Firstly, because of how I injected code into the game, I was not able to use `using` statements.
// That's why I had to fully qualify all type names, making everything overly verbose.
// Secondly, I did most of the code editing in-game or in Code-OSS without any C# extensions. Basic syntax highlighting is all I had.
// And thirdly, I have no frickin' clue how to code in C# XD
// But hey, at least it compiles/works, right?

static System.Collections.IEnumerator coDisplayText(string text) {
  yield return Singleton<DiskCardGame.TextDisplayer>.Instance.ShowThenClear(text, 1, speaker: DialogueEvent.Speaker.Goo);
}
static void coExecute(System.Collections.IEnumerator co) {
  Singleton<DiskCardGame.TextDisplayer>.Instance.StartCoroutine(co);
}
static void displayText(string text) {
  coExecute(coDisplayText(text));
}
static DiskCardGame.GameFlowManager gfm = Singleton<DiskCardGame.GameFlowManager>.Instance;
static Func<DiskCardGame.NodeData> getMapNode = () => DiskCardGame.RunState.Run.map.nodeData.Find(n => n.id == DiskCardGame.RunState.Run.currentNodeId);
static string sConsumables() {
  return $"Consumable items ({DiskCardGame.RunState.Run.consumables.Count}/{DiskCardGame.RunState.Run.MaxConsumables}):\n{DiskCardGame.RunState.Run.consumables.Count == 0 ? "- You have none": string.Join("\n", System.Linq.Enumerable.Select(System.Linq.Enumerable.Select(DiskCardGame.RunState.Run.consumables, x => DiskCardGame.ItemsUtil.GetConsumableByName(x)), x => $"- {x.rulebookName}[{DiskCardGame.RuleBookPage.ParseCardDefinition(x.rulebookDescription)}]"))}";
}
static string sAbilityInfo(DiskCardGame.Ability a, string c = null) {
  return $"{DiskCardGame.AbilitiesUtil.GetInfo(a).rulebookName}[{DiskCardGame.RuleBookPage.ParseCardDefinition(DiskCardGame.AbilitiesUtil.GetInfo(a).rulebookDescription).Replace("[creature]", (c ?? "a card bearing this sigil"))}]";
}
static string sTotems() {
  var totems = DiskCardGame.RunState.Run.totems;
  var totem = totems.Count == 0 ? "you don't have one" : $"{totems[0].tribe} tribe creatures get {sAbilityInfo(totems[0].ability)}";
  var _tops = DiskCardGame.RunState.Run.totemTops;
  var tops = _tops.Count == 0 ? "you don't have any" : string.Join(", ", _tops);
  var _bottoms = DiskCardGame.RunState.Run.totemBottoms;
  var bottoms = _bottoms.Count == 0 ? "you don't have any" : string.Join(", ", System.Linq.Enumerable.Select(_bottoms, a => sAbilityInfo(a)));
  return $"YOUR Current totem (creatures of tribe X get sigil Y): {totem}\n[{_tops.Count}] YOUR totem tops (tribes): {tops}\n[{_bottoms.Count}] YOUR totem bottoms (sigils): {bottoms}";
}
static string sCardInfo(DiskCardGame.CardInfo c) {
	if (c.name == "!STATIC!GLITCH") return "Static glitch card (turns into random card when drawn)";
  string cost = c.BloodCost == 0 ? (c.BonesCost == 0 ? "free" : $"{c.BonesCost} bone cost") : $"{c.BloodCost} blood cost";
  string power = c.SpecialStatIcon == DiskCardGame.SpecialStatIcon.None ? $"{c.Attack} power" : "power - " + (string.IsNullOrEmpty(DiskCardGame.StatIconInfo.GetIconInfo(c.SpecialStatIcon).gbcDescription) ? DiskCardGame.StatIconInfo.GetIconInfo(c.SpecialStatIcon).rulebookDescription : DiskCardGame.StatIconInfo.GetIconInfo(c.SpecialStatIcon).gbcDescription).Replace("[creature]", c.DisplayedNameEnglish);
  string tribes = (c.tribes.Count == 0 ? "not part of a" : string.Join(", ", c.tribes)) + " tribe" + (c.tribes.Count > 1 ? "s" : "");
  string naturalSigils = c.DefaultAbilities.Count == 0 ? "none" : string.Join(", ", System.Linq.Enumerable.Select(c.DefaultAbilities, a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  string infusedSigils = c.ModAbilities.Count == 0 ? "none" : string.Join(", ", System.Linq.Enumerable.Select(c.ModAbilities, a => sAbilityInfo(a, c.DisplayedNameEnglish)));
  return $"{c.DisplayedNameEnglish} ({cost}; {power}; {c.Health} health; {tribes}; natural sigils - {naturalSigils}; infused sigils - {infusedSigils})";
}
static string sDeck() {
  return $"Your deck:\n{string.Join("\n", System.Linq.Enumerable.Select(DiskCardGame.RunState.DeckList, c => $"- {sCardInfo(c)}"))}";
}

static System.Collections.IEnumerator sendSystemMessage(string displayText, string text, string successText="Request sent successfully!")
{
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

static System.Collections.IEnumerator getResponse(string meta)
{
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

static Func<DiskCardGame.SpecialNodeHandler> snh = () => Singleton<DiskCardGame.SpecialNodeHandler>.Instance;
static string sTotemPiece(DiskCardGame.SelectableItemSlot s) {
  var data = s.Item.Data;
  if (data is DiskCardGame.TotemTopData t) {
    return $"Totem top for tribe {t.prerequisites.tribe}";
  }
  if (data is DiskCardGame.TotemBottomData b) {
    return $"Totem bottom for sigil {sAbilityInfo(b.effectParams.ability)}";
  }
  return $"Unknown totem piece. Blame Retrocast. Ask for additional information.";
}
static string sWoodcarverEvent() {
  var seq = snh().buildTotemSequencer;
  if (seq.GetFirstEmptyInventorySlot() == null || (DiskCardGame.RunState.Run.totemTops.Count+DiskCardGame.RunState.Run.totemBottoms.Count) >= 7) {
    var amalgam = seq.gameObject.GetComponentInChildren<DiskCardGame.SelectableCard>().Info;
    return $"A look of regret fell over the old woodcarver. You were overburdened with totem pieces and you could carry no more.\nShe gestured toward a disturbing creature lurking nearby.\n{sCardInfo(amalgam)} was added to your deck.\nCanine. Hooved. Reptile. Bird. Insect. The Amalgam is all.";
  }
  var pieces = string.Join("\n", System.Linq.Enumerable.Select(seq.slots, (s) => $"- {sTotemPiece(s)}"));
  return $"You are currently at Woodcarver's event, where you pick one of 3 totem pieces. Tops indicate a tribe. Bottoms indicate the sigil that will be added to all cards of that tribe on top. You need at least one top and bottom to build a totem. If you already have a totem, but one of offered pieces will make it even better, pick it and say you want to change the totem. If your current is better than what you can build with current options, just pick the lesser of evils and say you want to keep the current totem.\nTotem pieces you can pick:\n{pieces}";
}

static string sEvent() {
  var n = getMapNode();
  if (n is DiskCardGame.BuildTotemNodeData) {
    return sWoodcarverEvent();
  }
  return null;
}

static string sNodeData(DiskCardGame.NodeData n) {
  if (n is DiskCardGame.BossBattleNodeData b) {
    return $"Boss battle node - {b.bossType}BattleSequencer";
  }
  if (n is DiskCardGame.BoulderChoiceNodeData) {
    return "Boulder choice node[Pick one of 3 boulders to hit. One of them contains gold. KEEP GAMBLING]";
  }
  if (n is DiskCardGame.BuildTotemNodeData) {
    return "Woodcarver node[Pick one of 3 totem parts. Once you have at least one top and bottom, build a totem, adding a sigil to all creatures of specific tribe.]";
  }
  if (n is DiskCardGame.BuyPeltsNodeData) {
    return $"Trapper node[Exchange teeth(overkill damage) for Rabbit/Wolf/Golden pelts. You currently have {DiskCardGame.RunState.Run.currency} teeth.]";
  }
  if (n is DiskCardGame.TotemBattleNodeData) {
    return "Totem battle node[Card battle, but all Leshy's creatures of specific tribe will have an additional sigil.]";
  }
  if (n is DiskCardGame.CardBattleNodeData) {
    return "Normal card battle node";
  }
  if (n is DiskCardGame.CardChoicesNodeData c) {
    switch (c.choicesType) {
      case DiskCardGame.CardChoicesType.Random:
        return "Card choice node[Pick a card from 3 randomly chosen ones to add to your deck. You may optionally reroll it once with a clover to get another 3 cards.]";
      case DiskCardGame.CardChoicesType.Cost:
        return "Cost-based card choice node[Pick a cost from 3 randomly chosen ones and add random card of that cost to your deck. You may optionally reroll costs once with a clover, but you may not reroll the actual card.]";
      case DiskCardGame.CardChoicesType.Tribe:
        return "Tribe-based card choice node[Pick a tribe from 3 randomly chosen ones and add random card of that tribe to your deck. You may optionally reroll tribes once with a clover, but you may not reroll the actual card.]";
      case DiskCardGame.CardChoicesType.Deathcard:
        return "Deathcard choice node[Pick a deathcard from 3 randomly chosen ones to add to your deck.]";
    }
  }
  if (n is DiskCardGame.CardMergeNodeData) {
    return "Mysterious stones node[Sacrifice one card, removing it from your deck, and transferring its natural sigils to another card in your deck.]";
  }
  if (n is DiskCardGame.CardRemoveNodeData) {
    return "Bone Lord node[Sacrifice one card, removing it from your deck. Bone Lord will be pleased, if sacrifice is good enough.]";
  }
  if (n is DiskCardGame.CardStatBoostNodeData) {
    return "Campfire node[Increase card's power or health. First time is entirely safe, second time has some risk.]";
  }
  if (n is DiskCardGame.CopyCardNodeData) {
    return "Goobert node[Goobert will draw a copy of chosen card and add it to your deck.]";
  }
  if (n is DiskCardGame.DeckTrialNodeData) {
    return "Deck trial node[Pick one of 3 powerful cards, but only if you pass a trial of choice.]";
  }
  if (n is DiskCardGame.DuplicateMergeNodeData) {
    return "Mycologists node[Mycologists will fuse two of the same cards into one, combining stats of both while keeping price the same. If you have no duplicates, they will offer you one instead.]";
  }
  if (n is DiskCardGame.GainConsumablesNodeData) {
    return "Pack node[Refill your consumable items. If all your item slots are full, receive a Pack Rat card instead.]";
  }
  if (n is DiskCardGame.TradePeltsNodeData) {
    return "Trader node[Exchange pelts you have for cards. If you have no pelts, receive 5 teeth instead.]";
  }
  UnityEngine.Debug.LogError($"Unknown node type encountered: {n}");
  return "Unknown node[Something went wrong. Blame Retrocast.]";
}

static string sMap(DiskCardGame.NodeData startNode) {
  return sNode(startNode, 0);
}
static string sNode(DiskCardGame.NodeData node, int depth) {
  if (depth >= 4) return "";
  var indent = new string(' ', depth * 2);
  var summary = depth == 0 ? "→ You're here\n" : indent + "→ " + sNodeData(node) + "\n";
  foreach (var nextNode in node.connectedNodes) {
    summary += sNode(nextNode, depth + 1);
  }
  return summary;
}

static string getMetadata() {
  return $"{sDeck()}\n{sConsumables()}\n{sTotems()}";
}

static void Update() {
  if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.RightBracket)) {
    coExecute(getResponse(getMetadata()));
  }
  if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.LeftBracket)) {
    switch (gfm.CurrentGameState) {
      case DiskCardGame.GameState.CardBattle:
        displayText("Currently in card battle");
        break;
      case DiskCardGame.GameState.Map:
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
        var nodeDefs = string.Join("\n", System.Linq.Enumerable.Select(nodes, (n) => $"- {sNodeData(n)}"));
        coExecute(sendSystemMessage("map summary", $"You are currently on the game map and you have a choice to make. Map structure for reference:\n{sMap(node)}\n\nYou must choose one of the following nodes:\n{nodeDefs}\nYou must ONLY PICK THE NODE, do NOT specify what exactly to do on it, you will be explicitly asked in next message(s)."));
        return;
      case DiskCardGame.GameState.FirstPerson3D:
        displayText("[c:bR]Cannot get data from FirstPerson3D![c:]");
        break;
      case DiskCardGame.GameState.SpecialCardSequence:
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

// The thingy that actually calls the Update as patch for ManagedUpdate of OilPaintingPuzzle and wraps it in try/catch.
// Seems like a good candidate, since it is always present in cabin in single instance.
// In case it causes any issues, it can be moved to any other single/global object's Update/ManagedUpdate.
static void Postfix(DiskCardGame.OilPaintingPuzzle __instance)
{
  try {
    Update();
  }
  catch (System.Exception ex) {
    UnityEngine.Debug.LogError($"Congrats, Retrocast, you fucked up!\n{ex}");
  }
}
