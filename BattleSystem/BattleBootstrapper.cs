using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public partial class BattleBootstrapper : Node
{
    private List<int> ClientReadyList;
    private bool LocalContinueSignal;

    public override void _Ready()
    {
        CallDeferred("Setup");
        if (Multiplayer.IsServer())
        {
            ClientReadyList = new List<int>();
        }
    }

    public async Task WaitForPeersReady()
    {
        if (!Multiplayer.IsServer())
        {
            GD.Print("Server starting new ready check...");
            ClientReadyList = new List<int>();
            RpcId(1, "MarkReadyRPC", Multiplayer.GetUniqueId());
            while (!LocalContinueSignal) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            GD.Print("Server ready check complete!");
            return;
        }
        ClientReadyList.Clear();
        bool done = false;
        while (!done)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            done = ClientReadyList.Count >= Multiplayer.GetPeers().Length;
        }
        foreach (int id in ClientReadyList) { RpcId(id, "ClientContinueRPC"); }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void MarkReadyRPC(int _id)
    {
        if (!Multiplayer.IsServer()) return;
        if (ClientReadyList.Contains(_id)) return;
        ClientReadyList.Add(_id);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClientContinueRPC()
    {
        LocalContinueSignal = true;
    }

    public async void Setup()
    {
        GridMapGen.Inst.GenerateTiles();
        // TODO LATER : Probably spawn pawns based on party lists and player defined stats
        Godot.Collections.Array<Node> gub_nodes = GetTree().GetNodesInGroup("GenericUnitBuilder");
        foreach (Node n in gub_nodes)
        {
            GenericUnitBuilder gub = n as GenericUnitBuilder;
            gub.BuildUnit();
        }
        await WaitForPeersReady();
        foreach (GridPawn gp in PawnActionManager.Inst.PawnRegistry)
        {
            if (gp == null) continue;
            gp.SetPositionInGrid();
            gp.SetColorsAndGraphics();
        }
        GridCursor.Inst.SetPositionInGrid();
        await WaitForPeersReady();
        PawnActionManager.Inst.ServerSetupAllTeams();
        GD.Print("Battle Bootstrap setup done!");
    }
}
