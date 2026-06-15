using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;


public class PawnAction
{
    public int ActorIndex { get; set; }
    public int TargetIndex { get; set; }
    public Vector2I TargetTilePos { get; set; }
    public Vector2I[] TilePath { get; set; }
    public int Priority { get; set; }
}

public class Team
{
    public int TeamIndex { get; set; }
    public int NetworkPeerId { get; set; }
    public bool doneSelectingActions { get; set; }
}

public partial class PawnActionManager : Node
{
    public static PawnActionManager Inst { get; set; }

    public GridPawn[] PawnRegistry { get; set; }
    private int maxRegisteredTeam { get; set; }
    public List<Team> TeamRegistry { get; set; }
    public int PawnRegistryCounter { get; set; } = 0;
    public Team LocalTeam { get; set; }
    public int AccomodateNewPawn(GridPawn _pawn)
    {
        for (int i = 0; i < 128; i++)
        {
            if (PawnRegistry[PawnRegistryCounter] == null) break;
            PawnRegistryCounter++;
            if (PawnRegistryCounter > 127) return -1;
        }
        PawnRegistry[PawnRegistryCounter] = _pawn;
        if (maxRegisteredTeam < _pawn.TeamIndex)
        {
            maxRegisteredTeam = _pawn.TeamIndex;
            for (int i = 0; i <= maxRegisteredTeam; i++)
            {
                if (i >= TeamRegistry.Count)
                {
                    GD.Print("Registering team " + i);
                    TeamRegistry.Insert(i, new Team());
                    TeamRegistry[i].TeamIndex = i;
                }
            }
        }
        return PawnRegistryCounter;
    }
    public List<GridPawn> GetTeamPawns(int _index)
    {
        if (_index < 0 || _index > 127) return null;
        List<GridPawn> temp = new List<GridPawn>();
        for (int i = 0; i < 128; i++)
        {
            if (PawnRegistry[i] == null) continue;
            if (PawnRegistry[i].TeamIndex == _index) temp.Add(PawnRegistry[i]);
        }
        return temp;
    }
    public void ServerSetupAllTeams()
    {
        if (!Multiplayer.IsServer()) return;
        int cteam = 0;
        Rpc("SetupTeamRPC", cteam, 1);
        for (int i = 0; i < Multiplayer.GetPeers().Length; i++)
        {
            cteam += 1;
            if (cteam > maxRegisteredTeam) return;
            int netId = Multiplayer.GetPeers()[i];
            Rpc("SetupTeamRPC", cteam, netId);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetupTeamRPC(int _index, int _peerId)
    {
        TeamRegistry[_index].NetworkPeerId = _peerId;
        if (_peerId == Multiplayer.GetUniqueId()) LocalTeam = TeamRegistry[_index];
    }

    [Export] public Node3D camSystem { get; set; }
    private GridPawn networkedCamFocusPawn;
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetNetworkedCamFocusPawnRPC(int _pawnIndex)
    {
        GD.Print("Focused pawn index : " + _pawnIndex);
        networkedCamFocusPawn = PawnRegistry[_pawnIndex];
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetCamModeRPC(bool _followActor)
    {
        GridCursor.Inst.Visible = !_followActor;
    }
    private List<PawnAction> actionList { get; set; }
    private int actionExecutionIndex { get; set; }
    private bool currentlyExecuting;

    public override void _EnterTree()
    {
        GD.Print("Enter Tree");
        Inst = this;
        PawnRegistry = new GridPawn[128];
        TeamRegistry = new List<Team>();
        actionList = new List<PawnAction>();
    }

    public void SortListByPriority()
    {
        var sortedActions = actionList.OrderBy(a => a.Priority).ToList();
        actionList = sortedActions;
    }

    public void ExecuteActions()
    {
        if (currentlyExecuting) return;
        if (actionList.Count < 1) return;
        Rpc("SetCamModeRPC", true);
        ControlSystem.ReleaseAllControl();
        currentlyExecuting = true;
        SortListByPriority();
        PerformActionSequence();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PushActionRPC(int _actorIndex, int _targetIndex, Vector2I _targetTilePos, Godot.Collections.Array<Vector2I> _path, int _priority)
    {
        GD.Print("Action pushed!");
        PawnAction pa = new PawnAction();
        pa.ActorIndex = _actorIndex;
        pa.TargetIndex = _targetIndex;
        pa.TargetTilePos = _targetTilePos;
        pa.TilePath = _path.ToArray<Vector2I>();
        pa.Priority = _priority;
        PushAction(pa);
    }
    public void PushAction(PawnAction newAction)
    {
        for (int i = 0; i < actionList.Count; i++)
        {
            PawnAction pa = actionList[i];
            if (newAction.ActorIndex == pa.ActorIndex)
            {
                actionList[i] = newAction;
                return;
            }
        }
        actionList.Add(newAction);
    }
    public void ExecuteIfReady()
    {
        if (!Multiplayer.IsServer()) return;
        bool allReady = true;
        foreach (Team team in TeamRegistry) if (!team.doneSelectingActions) allReady = false;
        if (allReady) ExecuteActions();
    }

    public override void _Process(double delta)
    {
        if (GridCursor.Inst.Visible) return;
        if (networkedCamFocusPawn == null) return;
        camSystem.GlobalPosition = camSystem.GlobalPosition.Lerp(networkedCamFocusPawn.GlobalPosition, (float)(delta * 2.5));
    }

    private async void PerformActionSequence()
    {
        for (int i = 0; i < actionList.Count; i++)
        {
            actionExecutionIndex = i;
            GridPawn actor = PawnRegistry[actionList[actionExecutionIndex].ActorIndex];
            Rpc("SetNetworkedCamFocusPawnRPC", actionList[actionExecutionIndex].ActorIndex);
            actor.PathInput(actionList[i].TilePath);
            while (!actor.ActionCompleted()) { await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
            if (actor.WasHalted) await HaltAttackProcess();
            await ToSignal(GetTree().CreateTimer(0.5), Timer.SignalName.Timeout);
            await AttackProcess();
            actor.Rpc("ExhaustRPC");
        }
        await ToSignal(GetTree().CreateTimer(0.5), Timer.SignalName.Timeout);
        actionList.Clear();
        currentlyExecuting = false;
        RefreshExhaustedTeams();
        Rpc("SetCamModeRPC", false);
        Rpc("TurnResetLocalEnviromentRPC");
        ControlSystem.GiveControl(GridCursor.Inst);
    }

    private async Task AttackProcess()
    {
        PawnAction pa = actionList[actionExecutionIndex];
        GridPawn actor = PawnRegistry[pa.ActorIndex];
        GridTile targetTile = GridMapGen.Inst.GetTileAt(pa.TargetTilePos.X, pa.TargetTilePos.Y);
        if (pa.TargetIndex > -1)
        {
            GridPawn target = PawnRegistry[pa.TargetIndex];
            bool moveConnects = false;
            if (target.StandingTile == targetTile && !actor.WasHalted) moveConnects = true;
            else {
                foreach (GridTile t in actor.GetTargetableTilesFrom(actor.StandingTile, TeamSelectionType.OTHER_TEAM, true)) {
                    if (t.Inhabitant == target) { moveConnects = true; GD.Print("Attack connects despite movement!");  break; }
                }
            }
            if (moveConnects) {
                await SetupAndInitiateBattleSequence(actor.SlotIndexInReg, target.SlotIndexInReg);
            } else {
                GD.Print("The attacked was incomplete due to target reposition out of range!");
            }
        }
    }
    private async Task HaltAttackProcess()
    {
        PawnAction pa = actionList[actionExecutionIndex];
        GridPawn actor = PawnRegistry[pa.ActorIndex];
        if (!actor.WasHalted) return;
        if (actor.Halter == null) return;
        if (actor.Halter.TeamIndex == actor.TeamIndex) return;
        bool doHaltAttack = false;
        foreach (GridTile t in actor.Halter.GetTargetableTilesFrom(actor.Halter.StandingTile, TeamSelectionType.OTHER_TEAM, true))
        {
            if (t.Inhabitant == actor) { doHaltAttack = true; break; }
        }
        if (doHaltAttack)
        {
            await SetupAndInitiateBattleSequence(actor.Halter.SlotIndexInReg, actor.SlotIndexInReg);
        }
    }
    private async Task SetupAndInitiateBattleSequence(int _attackerId, int _defenderId)
    {
        BattleSceneManager.Inst.leftPawnId = _defenderId;
        BattleSceneManager.Inst.rightPawnId = _attackerId;
        await BattleSceneManager.Inst.MainBattleSequence();
    }
    private void RefreshExhaustedTeams()
    {
        for (int i = 0; i <= maxRegisteredTeam; i++)
        {
            RefreshTeam(i);
        }
    }
    private void RefreshTeam(int _index)
    {
        List<GridPawn> teamPawns = GetTeamPawns(_index);
        if (teamPawns.Count < 1) return;
        foreach (GridPawn gp in teamPawns) { if (!gp.Exhausted) return; }
        foreach (GridPawn gp in teamPawns) { gp.Rpc("RefreshRPC"); }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void TurnResetLocalEnviromentRPC()
    {
        foreach (Team t in TeamRegistry)
        {
            t.doneSelectingActions = false;
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void DeclareTeamDoneRPC(int _index)
    {
        TeamRegistry[_index].doneSelectingActions = true;
        ExecuteIfReady();
    }

    public override void _ExitTree()
    {
        Inst = null;
    }


    /*
     * RPC function for spawning new grid pawns.
     */
    public static int MPSpawnCounter { get; private set; }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RequestSpawnCountedRPC(int _x, int _y, int team_index, string charsheet_json)
    {
        if (!Inst.Multiplayer.IsServer()) return;
        MPSpawnCounter += 1;
        Inst.Rpc("SpawnCountedRPC", _x, _y, team_index, charsheet_json, MPSpawnCounter);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnCountedRPC(int _x, int _y, int team_index, string charsheet_json, int count_index)
    { 
        SpawnCounted(GridMapGen.Inst.Tiles[_y, _x], team_index, charsheet_json, count_index); 
    }
    public void SpawnCounted(GridTile _tile, int team_index, string charsheet_json, int count_index)
    {
        CharSheet charSheet = CharSheet.FromJSONString(charsheet_json);
        PackedScene spawnScene = StatRegistry.Inst.Classes[charSheet.ClassKey].DefaultPawnScene;
        GridPawn newGP = spawnScene.Instantiate<GridPawn>();
        newGP.CharSheet = charSheet;
        newGP.TeamIndex = team_index;
        Inst.GetParent().AddChild(newGP);
        newGP.GlobalPosition = _tile.GlobalPosition;
        newGP.Name = "MP_spawn_pawn__" + count_index.ToString();
    }

}
