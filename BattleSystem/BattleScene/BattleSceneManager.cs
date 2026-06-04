using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public partial class BattleSceneManager : Node3D, IControllable<Node>
{
    public static BattleSceneManager Inst { get; set; }

	[Export] public BattleHPUI leftCharUI;
	[Export] public BattleHPUI rightCharUI;
    [Export] public Node3D LeftCharOrigin { get; set; }
    [Export] public Node3D RightCharOrigin { get; set; }
    public int leftPawnId { get; set; }
    public int rightPawnId { get; set; }
    private GridPawn leftPawnRef;
    private GridPawn rightPawnRef;
    private PawnGraphics leftGraphics;
    private Node3D leftVisible;
    private PawnGraphics rightGraphics;
    private Node3D rightVisible;
    private Node leftSavedParent;
    private Node rightSavedParent;

	[Export] public Node3D camSystem;
	[Export] AnimationPlayer testAnim;
	private Camera3D directCamRef;
	public Vector3 CamEulerAnglesTarget { get; set; }
	public bool BattleActive { get; set; }
	private float YRotTarget;
	private float YRot;
    private float CamDistanceTarget;

    [Export] Label letterTemplate;
	[Export] string message;
	int message_print_index;
	int message_start_offset;
	List<Label> letterLabels;
	[Export] float timeBetweenLetters;
	[Export] int pixelsBetweenLetters;
	float cTimeBetweenLetters;
	int messageCounter;

    public enum FieldPos { LEFT = 0, RIGHT = 1 }

    public void PrintLetter(string _l, Vector2 _pos) { Rpc("PrintLetterRPC", _l, _pos); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PrintLetterRPC(string _l, Vector2 _pos)
	{
		Label newLetter = letterTemplate.Duplicate() as Label;
		letterLabels.Add(newLetter);
		cTimeBetweenLetters = timeBetweenLetters;
		AddChild(newLetter);
        newLetter.GlobalPosition = _pos;
        newLetter.Text = _l;
		newLetter.Visible = true;
		newLetter.Scale = new Vector2(1, 2.0f);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnCombatantGraphicsRPC(int _leftIndex, int _rightIndex)
    {
        leftPawnId = _leftIndex;
        rightPawnId = _rightIndex;
        leftPawnRef = PawnActionManager.Inst.PawnRegistry[leftPawnId];
        rightPawnRef = PawnActionManager.Inst.PawnRegistry[rightPawnId];
        leftSavedParent = leftPawnRef.GetParent();
        rightSavedParent = rightPawnRef.GetParent();
        foreach (Node n in LeftCharOrigin.GetChildren()) n.QueueFree();
        foreach (Node n in RightCharOrigin.GetChildren()) n.QueueFree();
        leftGraphics = leftPawnRef.GetNode<PawnGraphics>("Graphics");
        leftVisible = leftGraphics.GetNode<Node3D>("Visible");
        rightGraphics = rightPawnRef.GetNode<PawnGraphics>("Graphics");
        rightVisible = rightGraphics.GetNode<Node3D>("Visible");
        leftPawnRef.InBattleMode = true;
        leftPawnRef.Reparent(LeftCharOrigin, true);
        leftPawnRef.GlobalPosition = LeftCharOrigin.GlobalPosition;
        rightPawnRef.InBattleMode = true;
        rightPawnRef.Reparent(RightCharOrigin, true);
        rightPawnRef.GlobalPosition = RightCharOrigin.GlobalPosition;
        SetupGraphicsObjectForBattle(leftGraphics, leftVisible);
        SetupGraphicsObjectForBattle(rightGraphics, rightVisible);
        leftGraphics.Scale = new Vector3(-1, 1, 1);
        leftGraphics.MainSprite.FlipH = true;
    }
    public void SetupGraphicsObjectForBattle(PawnGraphics node, Node3D visible)
    {
        //node.MainSprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear;
        visible.GetNode<Sprite3D>("TeamCircle").Visible = false;
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ReturnCombatantGraphicsRPC()
    {
        leftPawnRef.Reparent(leftSavedParent);
        leftPawnRef.InBattleMode = false;
        rightPawnRef.Reparent(rightSavedParent);
        rightPawnRef.InBattleMode = false;
        leftGraphics.Scale = Vector3.One;
        leftGraphics.MainSprite.FlipH = false;
        leftGraphics.Anim.Play("idle");
        rightGraphics.Anim.Play("idle");
        SetupGraphicsObjectForMap(leftGraphics, leftVisible);
        SetupGraphicsObjectForMap(rightGraphics, rightVisible);
    }
    public void SetupGraphicsObjectForMap(PawnGraphics node, Node3D visible)
    {
        //node.MainSprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        visible.GetNode<Sprite3D>("TeamCircle").Visible = true;
    }

    public override void _Ready()
	{
        Inst = this;
		letterLabels= new List<Label>();
		directCamRef = camSystem.GetNode("Camera3D") as Camera3D;
    }

	public override void _Process(double delta)
	{
		if (camSystem == null) return;
		directCamRef.Position = directCamRef.Position.Lerp(new Vector3(0,0,CamDistanceTarget), (float)delta*5.0f);
		YRot = (float)Mathf.Lerp(YRot, YRotTarget, delta*3.0f);
		Vector3 rot = camSystem.RotationDegrees;
        rot.Y = YRot;
		rot.X = -10.0f;
		camSystem.RotationDegrees = rot;
		foreach (Label label in letterLabels)
		{
			label.Scale = label.Scale.Lerp(Vector2.One, (float)delta*10.0f);
			label.Modulate = label.Modulate.Lerp(new Color(1,1,1,1),(float)delta*10.0f);

        }
    }
	public void CInput(InputEvent inputEvent) { }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void TestEnterRPC()
	{
        if (testAnim != null) testAnim.Play("zoom");
        else SpinIn();
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void TestExitRPC()
	{
        if (testAnim != null) testAnim.Play("zoomout");
        else SpinOut();
    }

	public async Task MainBattleSequence()
	{
        Rpc("TestEnterRPC");
        Rpc("SpawnCombatantGraphicsRPC", leftPawnId, rightPawnId);
        PrintMessage("Starting the battle!");
        Rpc("PlayPawnAnimRPC", (int)FieldPos.LEFT, "battle_idle");
        Rpc("PlayPawnAnimRPC", (int)FieldPos.RIGHT, "battle_idle");
        await ToSignal(GetTree().CreateTimer(0.8f), Timer.SignalName.Timeout);
        PrintMessage("Testing full heal!");
        Rpc("PlayPawnAnimRPC", (int)FieldPos.RIGHT, "battle_strike");
        await ToSignal(rightGraphics.Anim, AnimationPlayer.SignalName.AnimationFinished);
        Rpc("DamageEffectRPC", (int)FieldPos.LEFT);
        Rpc("SetYRotTargetRPC", -60);
        leftCharUI.TestFullHeal();
        rightCharUI.TestFullHeal();
        await AllMetersDoneTicking();
        Rpc("PlayPawnAnimRPC", (int)FieldPos.RIGHT, "battle_return");
        await ToSignal(rightGraphics.Anim, AnimationPlayer.SignalName.AnimationFinished);
        Rpc("PlayPawnAnimRPC", (int)FieldPos.RIGHT, "battle_idle");
        PrintMessage("Testing full damage!");
        Rpc("PlayPawnAnimRPC", (int)FieldPos.LEFT, "battle_strike");
        await ToSignal(leftGraphics.Anim, AnimationPlayer.SignalName.AnimationFinished);
        Rpc("DamageEffectRPC", (int)FieldPos.RIGHT);
        Rpc("SetYRotTargetRPC", 60);
        leftCharUI.TestDeath();
        rightCharUI.TestDeath();
        await AllMetersDoneTicking();
        Rpc("PlayPawnAnimRPC", (int)FieldPos.LEFT, "battle_return");
        await ToSignal(leftGraphics.Anim, AnimationPlayer.SignalName.AnimationFinished);
        Rpc("PlayPawnAnimRPC", (int)FieldPos.LEFT, "battle_idle");
        Rpc("TestExitRPC");
        Rpc("ReturnCombatantGraphicsRPC");
        await ToSignal(GetTree().CreateTimer(0.5f), Timer.SignalName.Timeout);
    }
    public async Task AllMetersDoneTicking()
    {
        while (leftCharUI.currentRollingHp != leftCharUI.currentHp) { await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
        while (rightCharUI.currentRollingHp != rightCharUI.currentHp) { await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    }

    public void SpinIn() { Rpc("SpinInRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpinInRPC()
    {
        ClearLetters();
        YRot = 359.0f;
        YRotTarget = 40.0f;
        directCamRef.Position = new Vector3(0, 0, 75);
        CamDistanceTarget = 5.0f;
        if (ControlSystem.GetControlling() != this) ControlSystem.GiveControl(this);
        BattleActive = true;
    }
    public void SpinOut() { Rpc("SpinOutRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpinOutRPC()
	{
        ClearLetters();
        YRotTarget = 359.0f;
        CamDistanceTarget = 75.0f;
        ControlSystem.ReleaseControl(this);
        BattleActive = false;
    }

	public void ClearLetters() { Rpc("ClearLettersRPC"); }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ClearLettersRPC()
    {
        foreach (Label letter in letterLabels)
        {
            letter.QueueFree();
        }
        letterLabels.Clear();
        message_print_index = 0;
    }
    public async void PrintMessage(string message)
	{
		messageCounter++;
		int thisCounter = messageCounter;
		ClearLetters();
		//message_start_offset = 200 - (message.Length*(pixelsBetweenLetters/2));
		message_start_offset = 8;
        await ToSignal(GetTree().CreateTimer(0.1f), Timer.SignalName.Timeout);
        for (int i = 0; i < message.Length; i++)
		{
			if (thisCounter != messageCounter) return;
			char c = message[i];
            PrintLetter(c.ToString(), new Vector2(message_start_offset + (message_print_index * pixelsBetweenLetters), 0));
			if (c != " ".ToCharArray()[0])
				await ToSignal(GetTree().CreateTimer(timeBetweenLetters), Timer.SignalName.Timeout);
            message_print_index += 1;
        }
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PlayPawnAnimRPC(int pos, string animname)
    {
        PawnGraphics pg = pos == (int)FieldPos.LEFT ? leftGraphics : rightGraphics;
        pg.Anim.Play(animname);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetYRotTargetRPC(int _yrot)
    {
        YRotTarget = _yrot;
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public async void DamageEffectRPC(int pos)
    {
        PawnGraphics pg = pos == (int)FieldPos.LEFT ? leftGraphics : rightGraphics;
        BattleHPUI bhpui = pos == (int)FieldPos.LEFT ? leftCharUI : rightCharUI;
        Sprite3D s3d = pg.MainSprite;
        bhpui.PortraitAnim.Play("damage");
        s3d.Modulate = new Color(1, 0, 0, 1);
        for (int i = 5; i >= 0; i--)
        {
            int sign = (i % 2 == 0) ? 1 : -1;
            pg.Position = new Vector3(0, 0, (sign * i) * 0.1f);
            await ToSignal(GetTree().CreateTimer(0.05f), Timer.SignalName.Timeout);
        }
        s3d.Modulate = new Color(1, 1, 1, 1);
    }
}
