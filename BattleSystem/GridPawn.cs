using Godot;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

public enum TeamSelectionType
{
    OTHER_TEAM,
    OWN_TEAM,
    ANYONE
}

public partial class GridPawn : Node3D
{
    public CharSheet CharSheet { get; set; }
    public GridTile StandingTile { get; set; }
    public GridFlood moveFlood { get; set; }
    public Vector2I TilePos { get { return StandingTile.TilePosition; } }
    
    // Path follow logic
    private float nextMovementTimer = 0.0f;
    private List<GridTile> tilePath;
    // Graphics
    [Export] public Sprite3D mainCharsprite;
    private Color mainCharspriteStartColor;
    [Export] public Sprite3D unitCircle;
    private Color unitCircleStartColor;
    [Export] public Sprite2D bodySpriteComponent;
    [Export] public Sprite2D teamColorSpriteComponent;
    private EditorUpdater editorUpdater;
    // Stats
    [Export] public int TeamIndex { get; set; } = 0;
    [Export] public int MovementTiles { get; set; } = 3;
    [Export] public int StandardRangeMax { get; set; } = 1;
    [Export] public int StandardRangeMin { get; set; } = 0;
    [Export] public Vector2[] SpecialPattern { get; set; }
    [Export] public int BaseActionPriority { get; set; }
    // State
    [Export] public bool Exhausted { get; set; }
    [Export] public int SlotIndexInReg;
    [Export] public bool InBattleMode { get; set; }
    public bool WasHalted { get; set; }
    public GridPawn Halter { get; set; }

    public override void _Ready()
    {
        tilePath = new List<GridTile>();
        SlotIndexInReg = PawnActionManager.Inst.AccomodateNewPawn(this);
        if (SlotIndexInReg < 0 || SlotIndexInReg > 127) QueueFree();
    }
    public void SetColorsAndGraphics()
    {
        if (ScenarioInfo.Inst != null)
        {
            Color myTeamColor = ScenarioInfo.Inst.teamColors[TeamIndex];
            myTeamColor.A = unitCircle.Modulate.A;
            unitCircle.Modulate = myTeamColor;
        }
        if (CharSheet == null) return;
        //SpeciesSheet speciesSheet = StatRegistry.Inst.Species[CharSheet.SpeciesKey];
        //bodySpriteComponent.Texture = speciesSheet.SpriteSheets[0];
    }
    public void SetPositionInGrid()
    {
        Vector2I temp = GridMapGen.Inst.WorldToTilePos(GlobalPosition);
        if (temp.X == -1) temp = new Vector2I(0, 0);
        OccupyTile(temp.X, temp.Y);
    }
    public void FreeStandingTile()
    {
        if (StandingTile != null)
        {
            if (StandingTile.Inhabitant == this) StandingTile.Inhabitant = null;
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void OccupyTileRPC(int _x, int _y) { OccupyTile(_x, _y); }
    public void OccupyTile(int _x, int _y) { OccupyTile(GridMapGen.Inst.Tiles[_y, _x]); }
    public void OccupyTile(GridTile _tile)
    {
        FreeStandingTile();
        StandingTile = _tile;
        StandingTile.Inhabitant = this;
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PassThroughTileRPC(int _x, int _y)
    {
        FreeStandingTile();
        StandingTile = GridMapGen.Inst.Tiles[_y, _x];
    }

    public override void _Process(double delta)
    {
        if (InBattleMode) { return; }
        if (StandingTile == null) return;
        if (GlobalPosition != StandingTile.GlobalPosition)
        {
            GlobalPosition = GlobalPosition.Lerp(StandingTile.GlobalPosition, (float)(delta * 8.0));
        }
        if (tilePath.Count > 0)
        {
            nextMovementTimer -= (float)delta;
            if (nextMovementTimer < 0.0f)
            {
                bool halted = false;
                nextMovementTimer = 0.2f;
                Vector2I newTilePos = new Vector2I(tilePath[tilePath.Count - 1].TilePosition.X, tilePath[tilePath.Count - 1].TilePosition.Y);
                if (tilePath[tilePath.Count - 1].Inhabitant != null)
                {
                    if (tilePath[tilePath.Count - 1].Inhabitant.TeamIndex != TeamIndex) {
                        GD.Print("Halted due to next Inhabitant being an enemy.");
                        halted = true;
                    }
                    else
                    {
                        bool foundFreeTile = false;
                        foreach (GridTile t in tilePath) {
                            if (t.Inhabitant == null) { foundFreeTile = true; break; }
                        }
                        if (!foundFreeTile) {
                            GD.Print("Halted due to there being no more free tiles in the path.");
                            halted = true;
                        }
                    }
                }
                if (!halted)
                {
                    Rpc("PassThroughTileRPC", newTilePos.X, newTilePos.Y);
                    tilePath.RemoveAt(tilePath.Count - 1);
                    if (tilePath.Count < 1) {
                        Rpc("OccupyTileRPC", newTilePos.X, newTilePos.Y);
                    }
                }
                else
                {
                    Halter = tilePath[tilePath.Count - 1].Inhabitant;
                    tilePath.Clear();
                    Rpc("OccupyTileRPC", StandingTile.TilePosition.X, StandingTile.TilePosition.Y);
                    GD.Print("Halting Pawn regslot : " + Halter.SlotIndexInReg);
                    WasHalted = true;
                }
            }
        }
        if (!Exhausted) { unitCircle.Rotate(Vector3.Up, (float)delta * 2.0f); }
        else { unitCircle.Rotate(Vector3.Up, -(float)delta * 1.0f); }
    }

    public void PawnSelected()
    {
        if (InBattleMode) return;
        if (PawnActionManager.Inst.TeamRegistry[TeamIndex].NetworkPeerId != Multiplayer.GetUniqueId()) return;
        if (PawnActionManager.Inst.LocalTeam == null) return;
        if (PawnActionManager.Inst.LocalTeam.doneSelectingActions) { return; }
        if (Exhausted) return;
        if (tilePath.Count > 0) return;
        if (moveFlood == null)
        {
            GD.Print("Selected ID in reg : " + SlotIndexInReg);
            moveFlood = new GridFlood();
            moveFlood.Pawn = this;
            List<Vector2I> walkableTiles = new List<Vector2I>();
            moveFlood.FloodWalkable(TilePos.X, TilePos.Y, MovementTiles, null, walkableTiles);
            moveFlood.FillOpPhaseWithActionData(walkableTiles, null);
            GridMapGen.Inst.Floods.Add(moveFlood);
            GridMapGen.Inst.RenderAllFloods();
            GridCursor.Inst.FloodContext = moveFlood;
        }
        else
        {
            GridMapGen.Inst.Floods.Remove(moveFlood);
            moveFlood = null;
            GridMapGen.Inst.Redraw();
        }
    }

    public bool PathCompleted()
    {
        return tilePath.Count < 1;
    }
    public bool ActionCompleted()
    {
        return PathCompleted();
    }

    public List<GridTile> GetTargetableTilesFrom(GridTile _tile, TeamSelectionType _selectType, bool mustHavePawn)
    {
        if (_tile == null) return null;
        List<GridTile> temp = new List<GridTile>();
        void TryAddTile(int _x, int _y)
        {
            Vector2I checkTilePos = _tile.TilePosition + new Vector2I(_x - StandardRangeMax, _y - StandardRangeMax);
            GridTile checkTile = GridMapGen.Inst.GetTileAt(checkTilePos.X, checkTilePos.Y);
            if (checkTile == null) return;
            if (TargetValid(checkTile.Inhabitant, _selectType) || !mustHavePawn) temp.Add(checkTile);
        }
        if (SpecialPattern != null)
        {
            foreach (Vector2 offset in SpecialPattern)
            {
                TryAddTile(Mathf.RoundToInt(offset.X), Mathf.RoundToInt(offset.Y));
            }
        }
        if (StandardRangeMax > 0)
        {
            int checkBoxSize = (StandardRangeMax * 2) + 1;
            for (int i = 0; i < checkBoxSize; i++)
            {
                int chamfer = Mathf.Abs(i - StandardRangeMax);
                int innerChamfer = Mathf.Clamp((StandardRangeMin)-chamfer, 0, StandardRangeMax);
                for (int j = chamfer; j < checkBoxSize-chamfer; j++)
                {
                    if (i == StandardRangeMax && j == StandardRangeMax) continue;
                    if (Mathf.Abs(StandardRangeMax - j) < innerChamfer && innerChamfer > 0) continue;
                    TryAddTile(j, i);
                }
            }
        }
        return temp;
    }
    private bool TargetValid(GridPawn _target, TeamSelectionType _selectType)
    {
        if (_target == null) return false;
        if (_selectType != TeamSelectionType.ANYONE)
        {
            if (_selectType == TeamSelectionType.OTHER_TEAM &&
                TeamIndex == _target.TeamIndex) return false;
            if (_selectType == TeamSelectionType.OWN_TEAM &&
                TeamIndex != _target.TeamIndex) return false;
        }
        return true;
    }

    public void PathInput(List<GridTile> gridTiles)
    { 
        tilePath = gridTiles;
        moveFlood = null;
        nextMovementTimer = 0.0f;
        FreeStandingTile();
    }
    public void PathInput(Vector2I[] gridPositions)
    {
        List<GridTile> tilePath = new List<GridTile>();
        for (int i = 0; i < gridPositions.Length; i++)
        {
            Vector2I pos = gridPositions[i];
            tilePath.Add(GridMapGen.Inst.GetTileAt(pos.X, pos.Y));
        }
        PathInput(tilePath);
    }
    public void AttackInput(int _pawnIndex)
    {
        Rpc("AttackRPC", _pawnIndex);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void AttackRPC(int _pawnIndex)
    {
        GridPawn pawn = PawnActionManager.Inst.PawnRegistry[_pawnIndex];
        if (pawn == null) return;
    }

    private Color ColorMulti(Color _color, float _r, float _g, float _b, float _a)
    {
        return new Color(_color.R * _r,_color.G * _g,_color.B * _b,_color.A * _a);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void ExhaustRPC() { Exhaust(); }
    public void Exhaust()
    {
        Exhausted = true;
        if (mainCharsprite != null)
        {
            mainCharspriteStartColor = mainCharsprite.Modulate;
            mainCharsprite.Modulate = ColorMulti(mainCharsprite.Modulate, 0.7f, 0.7f, 0.7f, 1.0f);
        }
        if (unitCircle != null)
        {
            unitCircleStartColor = unitCircle.Modulate;
            unitCircle.Modulate = ColorMulti(unitCircle.Modulate, 0.5f, 0.5f, 0.5f, 0.4f);
        }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RefreshRPC() { Refresh(); }
    public void Refresh()
    {
        Exhausted = false;
        WasHalted = false;
        Halter = null;
        if (mainCharsprite != null) mainCharsprite.Modulate = mainCharspriteStartColor;
        if (unitCircle != null) unitCircle.Modulate = unitCircleStartColor;
    }

}
