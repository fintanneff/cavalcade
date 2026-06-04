using Godot;
using System;

public partial class MenuCursor : Node2D
{
	public static MenuCursor Inst { get; private set; }
	public Vector2 HoverPos { get; set; }

	public override void _Ready()
	{
		Inst = this;
	}

	public override void _Process(double delta)
	{
		GlobalPosition = GlobalPosition.Lerp(HoverPos, (float)delta*20.0f);
		Visible = (ControlSystem.GetControlling() is GenMenu);
	}
}
