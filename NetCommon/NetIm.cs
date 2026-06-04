using Godot;
using System;
using System.Net;

public partial class NetIm : Node2D
{

    [Export] Font fnt;
    [Export] Color[] colors;
    const string localhost = "127.0.0.1";
    const int port = 8888;
    const int max_players = 8;
    private MultiplayerApi MP() { return GetTree().GetMultiplayer(); }


    private float cursor_y = 0;
    private void GTxt(string txt, float yp=-1, int clr=0)
    {
        if (yp >= 0) cursor_y = yp;
        else cursor_y += 16;
        DrawString(
            fnt, new Vector2(0, cursor_y), 
            txt, modulate:colors[clr],
            fontSize: 12
            );
    }

    private int ticks_till_draw = 3;
    public override void _PhysicsProcess(double delta)
    {
        ticks_till_draw -= 1;
        if (ticks_till_draw < 0)
        {
            ticks_till_draw = 3;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        int cclr = 0;
        // Draw whatever else first
        base._Draw();
        // Check for multiplayer peer
        GTxt("Has multiplayer peer? : " + MP().HasMultiplayerPeer(), yp: 10);
        if (MP().HasMultiplayerPeer())
        {
            // Connection Status
            GTxt("Connection Status : " + MP().MultiplayerPeer.GetConnectionStatus());
            if (MP().MultiplayerPeer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected)
            {
                // Check for peer type
                cclr = (MP().MultiplayerPeer is OfflineMultiplayerPeer) ? 2 : 3;
                GTxt(MP().MultiplayerPeer.GetType().ToString(), clr: cclr);
                // Draw the peer unique ID
                GTxt("Unique ID : " + MP().GetUniqueId().ToString());
                // Are we the host/server?
                cclr = (MP().IsServer() && MP().MultiplayerPeer is ENetMultiplayerPeer) ? 1 : 0;
                GTxt("Server? : " + MP().IsServer(), clr: cclr);
                // Accepting new connections?
                cclr = (MP().MultiplayerPeer.RefuseNewConnections) ? 2 : 0;
                GTxt("Refuse New Connections? : " + MP().MultiplayerPeer.RefuseNewConnections, clr: cclr);
                // Draw list of connected peers
                cursor_y += 8;
                GTxt(" ---- peer list ---- ");
                cursor_y += 8;
                for (int i = 0; i < MP().GetPeers().Length; i++)
                {
                    int id = MP().GetPeers()[i];
                    GTxt(id.ToString());
                }
            }
        }
    }

    /* 
     * -----------------------
     *   DEBUG HOTKEYS
     * -----------------------
     */
    private bool alt_held = false;
    public override void _Input(InputEvent @event)
    {
        if (!(@event is InputEventKey)) return;
        InputEventKey key = (InputEventKey)@event;
        if (@event.IsPressed())
        {
            if (key.Keycode == Key.Alt) alt_held = true;
            else if (alt_held)
            {
                switch(key.Keycode)
                {
                    case Key.H: NetCommon.Host(port, 8); break;
                    case Key.C: NetCommon.Connect(localhost, port); break;
                    case Key.D: NetCommon.Disconnect(); break;
                    case Key.T: NetCommon.RTestMessage(Multiplayer.MultiplayerPeer.GetUniqueId().ToString()); break;
                    case Key.K: NetCommon.RLockIn(); break;
                }
            }
        }
        else if (@event.IsReleased())
        {
            if (key.Keycode == Key.Alt) alt_held = false;
        }
    }

}