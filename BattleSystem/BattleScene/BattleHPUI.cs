using Godot;
using System;

public partial class BattleHPUI : Node2D
{
    [Export] internal int maxHp;
    [Export] internal int currentHp;
    internal int currentRollingHp;
    [Export] Label hpNumber;
    [Export] Sprite2D[] hpFronts;
    [Export] Sprite2D[] hpBacks;
    [Export] float hpRollFrequency;
    [Export] public AnimationPlayer PortraitAnim { get; set; }
    float hpRollTimer;
    Vector2 numStartScale;

    public override void _Ready()
    {
        numStartScale = hpNumber.Scale;
        hpRollTimer = hpRollFrequency;
        InitialBarSetup();
        InitializeGraphicalValues();
        UpdateHpBackGraphics();
        UpdateCurrentHpGraphics();
    }

    public void InitialBarSetup()
    {
        Vector2 barAnchor = hpFronts[0].GlobalPosition;
        int barNum = Mathf.Clamp(maxHp / 32, 0, hpFronts.Length);
        for (int i = 0; i < hpFronts.Length; i++)
        {
            int yoffset = ((barNum) * 5) - (i * 12);
            hpFronts[i].GlobalPosition = new Vector2(barAnchor.X, GlobalPosition.Y+yoffset );
            hpBacks[i].GlobalPosition = hpFronts[i].GlobalPosition;
            //hpFronts[i].Visible = i < barNum;
            //hpBacks[i].Visible = hpFronts[i].Visible;
        }
    }

    public void HpChangeEffects(bool increase)
    {
        if (!increase)
        {
            hpNumber.Scale = numStartScale + new Vector2(0, 0.5f);
            hpNumber.Modulate = new Color(1, 0, 0);
        }
        else
        {
            hpNumber.Modulate = new Color(0, 1, 0);
        }
    }
    public void UpdateCurrentHpGraphics()
    {
        for (int i = 0; i < hpFronts.Length && i < hpBacks.Length; i++)
        {
            int tempHp = Mathf.Clamp(currentRollingHp - i * 32, 0, 32);
            hpFronts[i].RegionRect = new Rect2(0, 0, tempHp*3, 12);
        }
        hpNumber.Text = currentRollingHp.ToString();
    }
    public void UpdateHpBackGraphics()
    {
        for (int i = 0; i < hpFronts.Length && i < hpBacks.Length; i++)
        {
            int tempHp = Mathf.Clamp(maxHp - i * 32, 0, 32);
            hpBacks[i].RegionRect = new Rect2(0, 0, tempHp * 3, 12);
        }
    }

    public override void _Process(double delta)
    {
        if (currentHp != currentRollingHp)
        {
            hpRollTimer -= (float)delta;
            if (hpRollTimer <= 0.0)
            {
                hpRollTimer = hpRollFrequency;
                HpChangeEffects(currentRollingHp < currentHp);
                currentRollingHp = (int)Mathf.MoveToward(currentRollingHp, currentHp, 1);
                UpdateCurrentHpGraphics();
            }
        }
        hpNumber.Scale = hpNumber.Scale.Lerp(numStartScale, (float)delta * 5.0f);
        hpNumber.Modulate = hpNumber.Modulate.Lerp(new Color(1, 1, 1, 1), (float)delta);
    }

    public void InitializeGraphicalValues() { Rpc("InitializeGraphicalValuesRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void InitializeGraphicalValuesRPC()
    {
        currentRollingHp = currentHp;
        hpNumber.Scale = numStartScale;
        hpNumber.Modulate = new Color(1, 1, 1, 1);
    }
    public void ResetRoll() { Rpc("ResetRollRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ResetRollRPC()
    {
        currentRollingHp = 0;
        UpdateCurrentHpGraphics();
    }
    public void TestFullHeal() { Rpc("TestFullHealRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void TestFullHealRPC()
    {
        currentHp = maxHp;
    }
    public void TestDeath() { Rpc("TestDeathRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void TestDeathRPC()
    {
        currentHp = 0;
    }
}
