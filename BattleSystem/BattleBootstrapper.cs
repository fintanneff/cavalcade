using Godot;
using System;
using System.Collections.Generic;
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
            RpcId(1, "MarkReadyRPC", Multiplayer.GetUniqueId());
            while (!LocalContinueSignal) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
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
