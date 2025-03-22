static void displayText(string text) {
  var td = Singleton<DiskCardGame.TextDisplayer>.Instance;
  td.StartCoroutine(td.ShowThenClear(text, 1, speaker: DialogueEvent.Speaker.Goo));
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
  return $"YOUR Current totem (creatures of tribe X get sigil Y): {totem}\n[{_tops.Count}] YOUR totem _tops (tribes): {tops}\n[{_bottoms.Count}] YOUR totem _bottoms (abilities): {bottoms}";
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

static void Update() {
  if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.LeftBracket)) {
    switch (gfm.CurrentGameState) {
      case DiskCardGame.GameState.CardBattle:
        displayText("Currently in card battle");
        break;
      case DiskCardGame.GameState.Map:
        var nodes = getMapNode().connectedNodes;
        if (nodes.Count == 0) {
          displayText("[c:bR]No map nodes![c:]");
          return;
        }
        if (nodes.Count == 1) {
          displayText("[c:bR]Only one map node! No need to ask AI about it.[c:]");
          return;
        }
        displayText($"Available next map nodes: {string.Join(", ", System.Linq.Enumerable.Select(nodes, (n) => n.GetType().Name))}");
        return;
      case DiskCardGame.GameState.FirstPerson3D:
        displayText("[c:bR]Cannot get data from FirstPerson3D![c:]");
        break;
      case DiskCardGame.GameState.SpecialCardSequence:
        displayText($"SpecialCardSequence: {getMapNode().GetType().Name}");
        break;
    }
  }
}

static void Postfix(DiskCardGame.OilPaintingPuzzle __instance)
{
  try {
    Update();
  }
  catch (System.Exception ex) {
    UnityEngine.Debug.LogError($"Congrats, Retrocast, you fucked up!\n{ex}");
  }
}
