using Godot;
using System;
using System.Collections.Generic;

public partial class GridCursor : Node3D, IControllable<Node>
{
    public static GridCursor Inst { get; set; }

    [Export] Node3D camSystem;
    [Export] GenMenu actionWindow;
    GridTile inspectingTile;
    int inspectingX;
    int inspectingY;

    public GridFlood FloodContext { get; set; }
    public List<AvailableAction> TargetList { get; set; }
    private int targetIndex;
    bool setup = false;

    public override void _Ready()
    {
        Inst = this;
        ControlSystem.GiveControl(this);
    }
    public async void SetPositionInGrid()
    {
        while (!IsInstanceValid(GridMapGen.Inst)) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        while (!GridMapGen.Inst.MapGenerated) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        Vector2I temp = GridMapGen.Inst.WorldToTilePos(GlobalPosition);
        GD.Print(temp);
        if (temp.X == -1) temp = new Vector2I(0,0);
        inspectingX = temp.X;
        inspectingY = temp.Y;
        Move(0, 0);
        setup = true;
    }

    public override void _Process(double delta)
    {
        if (camSystem == null) return;
        if (!Visible) return;
        camSystem.GlobalPosition = camSystem.GlobalPosition.Lerp(GlobalPosition, (float)(delta * 5.0));
    }

    public void ControlChange()
    {
        if (GetTree() == null) return;
        if (TargetList == null) Move(0, 0);
        else ChangeTarget(0);
    }
    public void GainControl() { ControlChange(); }
    public void LoseControl() { TargetList = null; ControlChange(); }
    public void CancelFlood()
    {
        if (FloodContext == null) return;
        GridMapGen.Inst.Floods.Remove(FloodContext);
        if (FloodContext.Pawn != null)
        {
            FloodContext.Pawn.moveFlood = null;
        }
        FloodContext = null;
        GridMapGen.Inst.Redraw();
    }
    public void CInput(InputEvent @event)
    {
        if (!setup) return;
        if (TargetList == null)
        {
            if (Input.IsActionJustPressed("ui_right")) Move(1, 0);
            if (Input.IsActionJustPressed("ui_left")) Move(-1, 0);
            if (Input.IsActionJustPressed("ui_up")) Move(0, -1);
            if (Input.IsActionJustPressed("ui_down")) Move(0, 1);
            if (Input.IsActionJustPressed("ui_accept") && inspectingTile != null)
            {
                if (FloodContext == null)
                {
                    GridPawn pawn = inspectingTile.Inhabitant;
                    if (pawn != null)
                    {
                        pawn.PawnSelected();
                    }
                }
                else
                {
                    if (FloodContext.Pawn != null)
                    {
                        List<GridTile> path = FloodContext.GetTilePathTo(inspectingX, inspectingY);
                        if (path != null)
                        {
                            MainActionMenu.Inst.PawnContext = FloodContext.Pawn;
                            MainActionMenu.Inst.FloodContext = FloodContext;
                            MainActionMenu.Inst.OriginTilePosContext = new Vector2I(inspectingX, inspectingY);
                            ControlSystem.GiveControl(actionWindow);
                        }
                    }
                }
            }
            if (Input.IsActionJustPressed("ui_cancel") && FloodContext != null)
            {
                CancelFlood();
            }
        }
        else
        {
            if (Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_left")) ChangeTarget(-1);
            if (Input.IsActionJustPressed("ui_down") || Input.IsActionJustPressed("ui_right")) ChangeTarget(1);
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                ControlSystem.ReleaseControl(this);
            }
            if (Input.IsActionPressed("ui_accept"))
            {
                ComposeAndPushAction(
                    MainActionMenu.Inst.OriginTilePosContext,
                    FloodContext.Pawn,
                    TargetList[targetIndex].TargetTile.Inhabitant,
                    FloodContext.Pawn.BaseActionPriority
                    );
            }
        }
    }


    public void Move(int _x, int _y)
    {
        if (GridMapGen.Inst == null) return;
        int new_x = _x + inspectingX;
        int new_y = _y + inspectingY;
        if (new_x < 0 || new_y < 0 || 
            new_x >= GridMapGen.Inst.MapBounds.Size.X || 
            new_y >= GridMapGen.Inst.MapBounds.Size.Y) return;
        inspectingX = new_x;
        inspectingY = new_y;
        GlobalPosition = GridMapGen.Inst.TileToWorldPos(inspectingX, inspectingY);
        inspectingTile = GridMapGen.Inst.WorldPosToTile(GlobalPosition);
    }

    public void ChangeTarget(int _dir)
    {
        if (TargetList == null) return;
        if (TargetList.Count < 1) return;
        targetIndex = Mathf.Wrap(targetIndex + _dir, 0, TargetList.Count);
        GlobalPosition = GridMapGen.Inst.TileToWorldPos(
            TargetList[targetIndex].TargetTile.TilePosition.X,
            TargetList[targetIndex].TargetTile.TilePosition.Y
            );
    }

    public void ComposeAndPushAction(Vector2I pathDestination, GridPawn actor, GridPawn target, int priority)
    {
        PawnAction pa = new PawnAction();
        List<GridTile> tilePath = FloodContext.GetTilePathTo(pathDestination.X, pathDestination.Y);
        Vector2I[] tilePosPath = new Vector2I[tilePath.Count];
        for (int i = 0; i < tilePath.Count; i++)
        {
            tilePosPath[i] = tilePath[i].TilePosition;
        }
        pa.TilePath = tilePosPath;
        pa.ActorIndex = actor.SlotIndexInReg;
        pa.Priority = priority;
        if (target != null)
        {
            pa.TargetTilePos = target.StandingTile.TilePosition;
            pa.TargetIndex = target.SlotIndexInReg;
        }
        else
        {
            pa.TargetTilePos = new Vector2I(-1, -1);
            pa.TargetIndex = -1;
        }
        //PawnActionManager.Inst.PushAction(pa);
        PawnActionManager.Inst.RpcId(1, "PushActionRPC", pa.ActorIndex, pa.TargetIndex, pa.TargetTilePos, new Godot.Collections.Array<Vector2I>(pa.TilePath), pa.Priority);
        PawnActionManager.Inst.LocalTeam.doneSelectingActions = true;
        PawnActionManager.Inst.Rpc("DeclareTeamDoneRPC", PawnActionManager.Inst.LocalTeam.TeamIndex);
        ControlSystem.ReleaseAllControl();
        CancelFlood();
        ControlSystem.GiveControl(this);
    }

    public override void _ExitTree()
    {
        Inst = null;
    }

}
