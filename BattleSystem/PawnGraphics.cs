using Godot;
using System;

public partial class PawnGraphics : Node3D
{
    [Export] public Node3D VisibleComponent;
    [Export] public Sprite3D MainSprite;
    [Export] public AnimationPlayer Anim;
    [Export] public SubViewport subViewport;

    public override void _Ready()
    {
        /*
        ViewportTexture newVpt = new ViewportTexture();
        newVpt.ViewportPath = subViewport.GetPath();
        MainSprite.Texture = newVpt;
        */
        // ViewportTexture newVpt = MainSprite.Texture as ViewportTexture;
        // newVpt.ViewportPath = subViewport.GetPath();
    }

    public void BattleSetup()
    {
        Anim.Stop();
        Anim.Play("battle_idle");
    }
}
