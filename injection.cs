var td = Singleton<DiskCardGame.TextDisplayer>.Instance;
Action<string> displayText = (text) => td.StartCoroutine(td.ShowThenClear(text, 1, speaker: DialogueEvent.Speaker.Goo));
var gfm = Singleton<DiskCardGame.GameFlowManager>.Instance;
Func<DiskCardGame.NodeData> mapNode = () => DiskCardGame.RunState.Run.map.nodeData.Find((n) => n.id == DiskCardGame.RunState.Run.currentNodeId);
Func<string> consumables = () => $"Consumable items ({DiskCardGame.RunState.Run.consumables.Count}/{DiskCardGame.RunState.Run.MaxConsumables}):\n{DiskCardGame.RunState.Run.consumables.Count == 0 ? "- You have none": string.Join("\n", DiskCardGame.RunState.Run.consumables.Select(x => DiskCardGame.ItemsUtil.GetConsumableByName(x)).Select(x => $"- {x.rulebookName}[{DiskCardGame.RuleBookPage.ParseCardDefinition(x.rulebookDescription)}]"))}";
Func<DiskCardGame.Ability, string, string> abilityInfo = (a,c) => $"{DiskCardGame.AbilitiesUtil.GetInfo(a).rulebookName}[{DiskCardGame.RuleBookPage.ParseCardDefinition(DiskCardGame.AbilitiesUtil.GetInfo(a).rulebookDescription).Replace("[creature]", (c ?? "a card bearing this sigil"))}]";
Func<string> totems = () => $"YOUR Current totem (creatures of tribe X get sigil Y): {DiskCardGame.RunState.Run.totems.Count == 0 ? "you don't have one" : $"{DiskCardGame.RunState.Run.totems[0].tribe} tribe creatures get {abilityInfo(DiskCardGame.RunState.Run.totems[0].ability, null)}"}\n[{DiskCardGame.RunState.Run.totemTops.Count}] YOUR totem tops (tribes): {DiskCardGame.RunState.Run.totemTops.Count == 0 ? "you don't have any" : string.Join(", ", DiskCardGame.RunState.Run.totemBottoms)}\n[{DiskCardGame.RunState.Run.totemBottoms.Count}] YOUR totem bottoms (abilities): {DiskCardGame.RunState.Run.totemBottoms.Count == 0 ? "you don't have any" : string.Join(", ", DiskCardGame.RunState.Run.totemBottoms.Select(a => abilityInfo(a, null)))}";
switch (gfm.CurrentGameState) {
  case DiskCardGame.GameState.CardBattle:
    displayText("Currently in card battle");
    break;
  case DiskCardGame.GameState.Map:
    displayText($"Available next map nodes: {string.Join(", ", mapNode().connectedNodes.Select((n) => n.GetType().Name))}");
    break;
  case DiskCardGame.GameState.FirstPerson3D:
    displayText("[c:bR]Cannot get data from FirstPerson3D![c:]");
    break;
  case DiskCardGame.GameState.SpecialCardSequence:
    displayText($"SpecialCardSequence: {mapNode().GetType().Name}");
    break;
}
