using Godot;
using System;

public partial class ScenarioInfo : Node
{
    public static ScenarioInfo Inst { get; private set; }
    [Export] public Color[] teamColors;

    public override void _Ready()
    {
        Inst = this;
    }
}
