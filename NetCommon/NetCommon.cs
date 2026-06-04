using Godot;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

public partial class NetCommon : Node
{
    public static NetCommon inst;

    // Scene which players wait on before a game starts
    [Export] string lobby_scene_path;
    // Scene which players go to when the game actually starts
    [Export] string landing_scene_path;
    // Scene which players go to when they are disconnected
    [Export] string disconnect_scene_path;
    public override void _EnterTree() { 
        inst = this;
        MP().ServerDisconnected += OnDisconnect;
    }
    // Number to identify and name spawned objects as the server
    public static long server_spawn_id = 0;
    // Number to identify and name spawned objects as a client
    public static long client_spawn_id = 0;
    // Array of bool representing the 'ready' state of connected clients
    bool[] ready_states;

    private MultiplayerApi MP() { return GetTree().GetMultiplayer(); }
    public static bool LockedIn() { return inst.MP().MultiplayerPeer.RefuseNewConnections; }
    public bool Checks(bool must_be_locked_in)
    {
        if (MP().MultiplayerPeer == null) return false;
        if (MP().MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) return false;
        if (!(MP().IsServer())) return false;
        if (must_be_locked_in && !LockedIn()) return false;
        return true;
    }

    /*
     * Common functions for hosting, connecting, and disconnecting
     */
    public static void Host(int port, int max_players)
    {
        if (inst.MP().MultiplayerPeer is ENetMultiplayerPeer) return;
        var peer = new ENetMultiplayerPeer();
        Error err = peer.CreateServer(port, max_players);
        inst.MP().MultiplayerPeer = peer;
        // NOTE : GAME SPECIFIC!
        // ---------------------
        ControlSystem.ReleaseAllControl();
        // ---------------------
        inst.GetTree().ChangeSceneToPacked(GD.Load<PackedScene>(inst.lobby_scene_path));
    }
    public static void Connect(string ip, int port)
    {
        if (inst.MP().MultiplayerPeer is ENetMultiplayerPeer) return;
        var peer = new ENetMultiplayerPeer();
        Error err = peer.CreateClient(ip, port);
        inst.MP().MultiplayerPeer = peer;
        // NOTE : GAME SPECIFIC!
        // ---------------------
        ControlSystem.ReleaseAllControl();
        // ---------------------
        inst.GetTree().ChangeSceneToPacked(GD.Load<PackedScene>(inst.lobby_scene_path));
    }
    public static void Disconnect()
    {
        if (inst.MP().MultiplayerPeer != null) inst.MP().MultiplayerPeer.Close();
        inst.MP().MultiplayerPeer = new OfflineMultiplayerPeer();
        // NOTE : GAME SPECIFIC!
        // ---------------------
        ControlSystem.ReleaseAllControl();
        // ---------------------
        inst.GetTree().ChangeSceneToPacked(GD.Load<PackedScene>(inst.disconnect_scene_path));
    }
    public static void OnDisconnect()
    {
        Disconnect();
    }

    /*
     * Test message.
     */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void TestMessage(string msg)
    {
        GD.Print(Multiplayer.GetUniqueId().ToString() + " : " + msg);
    }
    public static void RTestMessage(string msg) { if (!inst.Checks(false)) return; inst.Rpc(nameof(TestMessage), msg); }

    /*
     * Allows the host to spawn packed nodes from a path onto all machines. (will place directly under the tree; needs to be locked in)
     */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Spawn(string path, string node_name, long auth_id, Vector3 pos)
    {
        Node spawn = GD.Load<PackedScene>(path).Instantiate<Node>();
        spawn.Name = node_name;
        GetTree().CurrentScene.AddChild(spawn);
        if (spawn is Node3D) {
            Node3D spawn_3d = spawn as Node3D;
            spawn_3d.GlobalPosition = pos;
        } else if (spawn is Node2D) {
            Node2D spawn_2d = spawn as Node2D;
            spawn_2d.GlobalPosition = new Vector2(pos.X, pos.Y);
        }
        spawn.SetMultiplayerAuthority((int)auth_id);
        if (spawn is INetSpawnable)
        {
            INetSpawnable ins = (INetSpawnable)spawn;
            ins.ReadyAfterSpawn();
        }
    }
    public static void RSpawn(string path, long auth_id, Vector3 pos) { 
        if (!inst.Checks(true)) return; 
        inst.Rpc(nameof(Spawn), path, server_spawn_id.ToString(), auth_id, pos);
        server_spawn_id += 1;
    }
    public static string NewClientSpawnName() 
    {
        string to_return = inst.MP().GetUniqueId() + "_" + client_spawn_id;
        client_spawn_id += 1;
        return to_return;
    }

    /*
     * Force all connected players to switch to a particular scene.
     */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LoadScene(string path)
    {
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>(path));
    }
    public static void RLoadScene(string path) { if (!inst.Checks(true)) return; inst.Rpc(nameof(LoadScene), path); }

    /*
     * Force all connected players to refuse connections and enter the landing scene
     */
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LockIn()
    {
        MP().MultiplayerPeer.RefuseNewConnections = true;
        GetTree().ChangeSceneToPacked(GD.Load<PackedScene>(landing_scene_path));
        ResetReadyArray();
    }
    public static void RLockIn() { 
        if (!inst.Checks(false)) return;
        if (LockedIn()) return;
        inst.Rpc(nameof(LockIn)); 
    }

    /*
     * An abstract function clients can use to let others know they are "ready"
     */
    [Signal] public delegate void PlayerReadyEventHandler(int _id);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void NotifyPlayerReady(int _id)
    {
        GD.Print(_id + " is ready!");
        EmitSignal(SignalName.PlayerReady, _id);
        if (ready_states != null)
        {
            int found = Array.IndexOf(MP().GetPeers(), _id);
            if (found != -1) ready_states[found] = true;
            else GD.Print("index not found for " + _id + " but ready rpc recieved!");
        }
        else GD.Print("Ready states are null!");
    }
    public static void RNotifyPlayerReady()
    {
        if (!LockedIn()) return;
        inst.Rpc(nameof(NotifyPlayerReady), inst.MP().GetUniqueId());
        GD.Print("Ready sent!");
    }
    public static async Task WaitForAllPlayersReady()
    {
        if (inst.ready_states.Length > 0)
        {
            while (inst.ready_states.Contains<bool>(false))
                await inst.ToSignal(inst.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }
    public static void ResetReadyArray()
    {
        inst.ready_states = new bool[inst.MP().GetPeers().Length];
        for (int i = 0; i < inst.ready_states.Length; i++) inst.ready_states[i] = false;
    }
}
