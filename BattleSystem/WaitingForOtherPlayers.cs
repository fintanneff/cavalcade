using Godot;
using System;

public partial class WaitingForOtherPlayers : Control
{
	public override void _Process(double delta)
	{
		if (!IsInstanceValid(PawnActionManager.Inst)) { Visible = false; return; }
		if (PawnActionManager.Inst.LocalTeam == null) { Visible = false; return; }
		if (!PawnActionManager.Inst.LocalTeam.doneSelectingActions) { Visible = false; return; }
		if (!GridCursor.Inst.Visible) { Visible = false; return; }
        if (PawnActionManager.Inst.LocalTeam.doneSelectingActions && GridCursor.Inst.Visible) { Visible = true; return; }
    }
}
