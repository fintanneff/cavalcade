using Godot;
using System;

public partial class MainActionMenu : GenMenu
{
    public static MainActionMenu Inst { get; private set; }
    public GridPawn PawnContext { get; set; }
    public Vector2I OriginTilePosContext { get; set; }
    public GridFlood FloodContext { get; set; }
    [Export] GenMenu waitConfirm;

    public override void _Ready()
    {
        base._Ready();
        Inst = this;
    }

    public override void GainControl()
    {
        MenuItems[0].Visible = FloodContext.Nodes[OriginTilePosContext.Y, OriginTilePosContext.X].targetsFromNode.Count > 0;
        RepositionItems();
        base.GainControl();
    }
    protected override bool SwitchItemName(string itemName)
    {
        if (FloodContext == null) { return false; }
        GridFloodNode node = FloodContext.Nodes[OriginTilePosContext.Y, OriginTilePosContext.X];
        if (node == null) { return false; }
        switch (itemName)
        {
            case "Attack":
                if (node.targetsFromNode.Count > 0)
                {
                    GridCursor.Inst.TargetList = node.targetsFromNode;
                    GridCursor.Inst.ChangeTarget(0);
                    ControlSystem.GiveControl(GridCursor.Inst);
                }
                return true;
            case "Wait":
                if (waitConfirm != null) ControlSystem.GiveControl(waitConfirm);
                return true;
            case "Yes":
                GridCursor.Inst.ComposeAndPushAction(
                    OriginTilePosContext, 
                    PawnContext, 
                    null, 
                    PawnContext.BaseActionPriority
                    );
                return true;
            case "No":
                ControlSystem.ReleaseControl(waitConfirm);
                return true;
        }
        return false;
    }
}
