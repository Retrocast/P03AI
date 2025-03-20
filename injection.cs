var td = Singleton<DiskCardGame.TextDisplayer>.Instance;
Action<string> displayText = (text) => td.StartCoroutine(td.ShowThenClear(text, 1, speaker: DialogueEvent.Speaker.Goo));
var gfm = Singleton<DiskCardGame.GameFlowManager>.Instance;
Func<DiskCardGame.NodeData> mapNode = () => DiskCardGame.RunState.Run.map.nodeData.Find((n) => n.id == DiskCardGame.RunState.Run.currentNodeId);
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
