using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public struct AvailableAction
{
    public GridTile TargetTile { get; set; }
    public bool IsBasicAttack { get; set; }
    public CharAction SpecialAction { get; set; }
    public bool CurrentlyAchievable { get; set; }
}
public class GridFloodNode
{
    public GridFloodNode(GridTile _tile, GridFloodNode _prev) { Tile = _tile; PrevNode = _prev; targetsFromNode = new List<AvailableAction>(); }
    public GridTile Tile { get; set; }
    public GridFloodNode PrevNode { get; set; }
    public int RemainingRange { get; set; }
    public bool ValidEndpoint { get; set; }
    public List<AvailableAction> targetsFromNode { get; set; }
    public int StrategicFavoribility { get; set; }
}
public class GridFlood
{
    public GridPawn Pawn { get; set; }
    public GridFloodNode[,] Nodes { get; set; }
    public bool Functional { get; set; }
    public bool Hidden { get; set; }
    private GridTile[,] tiles;
    private List<List<Vector2I>> operationPhases;
    public GridFlood() { 
        tiles = GridMapGen.Inst.Tiles; 
        Nodes = new GridFloodNode[tiles.GetLength(0),tiles.GetLength(1)];
        operationPhases = new List<List<Vector2I>>();
    }

    public GridFloodNode AddNode(int _x, int _y, int range, GridFloodNode prev, List<Vector2I> opPhase)
    {
        if (_x < 0 || _y < 0 || _y >= Nodes.GetLength(0) || _x >= Nodes.GetLength(1)) return null;
        bool replace = false;
        if (Nodes[_y, _x] != null)
        {
            if (Nodes[_y, _x].RemainingRange > range) return null;
            else replace = true;
        }
        if (tiles[_y, _x] == null) { return null; }
        if (tiles[_y, _x].Inhabitant != null)
        {
            if (tiles[_y, _x].Inhabitant.TeamIndex != Pawn.TeamIndex) return null;
        }
        if (replace)
        {
            Nodes[_y, _x].PrevNode = prev;
        }
        else
        {
            if (prev == null) Nodes[_y, _x] = new GridFloodNode(tiles[_y, _x], null);
            else Nodes[_y, _x] = new GridFloodNode(tiles[_y, _x], prev);
            if (opPhase != null) opPhase.Add(Nodes[_y, _x].Tile.TilePosition);
        }
        Nodes[_y, _x].RemainingRange = range;
        Nodes[_y, _x].ValidEndpoint = (Nodes[_y, _x].Tile.Inhabitant == null || Nodes[_y, _x].Tile.Inhabitant == Pawn);
        return Nodes[_y, _x];
    }
    public void FloodWalkable(int _x, int _y, int range, GridFloodNode prev, List<Vector2I> opPhase)
    {
        if (range <= 0) return;
        if (AddNode(_x, _y, range, prev, opPhase) == null) return;
        FloodWalkable(_x, _y - 1, range - 1, Nodes[_y, _x], opPhase);
        FloodWalkable(_x, _y + 1, range - 1, Nodes[_y, _x], opPhase);
        FloodWalkable(_x - 1, _y, range - 1, Nodes[_y, _x], opPhase);
        FloodWalkable(_x + 1, _y, range - 1, Nodes[_y, _x], opPhase);
    }
    public void FloodAction(int _x, int _y, List<Vector2I> opPhase) {
        if (Pawn == null) return;
        List<GridTile> temp = Pawn.GetTargetableTilesFrom(
            GridMapGen.Inst.GetTileAt(_x,_y), 
            TeamSelectionType.ANYONE, false
            );
        foreach ( GridTile tile in temp )
        {
            AddNode(tile.TilePosition.X, tile.TilePosition.Y, 99, null, opPhase);
        }
    }
    public void FloodReachable() 
    {
        operationPhases.Add(new List<Vector2I>());
        if (Pawn == null) return;
        FloodWalkable(Pawn.TilePos.X, Pawn.TilePos.Y, Pawn.MovementTiles, null, operationPhases[0]);
        foreach (Vector2I v in operationPhases[0])
        {
            FloodAction(v.X, v.Y, null);
        }
    }
    public void FillOpPhaseWithActionData(List<Vector2I> opPhase, CharAction specialAction)
    {
        if (specialAction != null) return;
        foreach (Vector2I v in opPhase) {
            List<GridTile> temp = Pawn.GetTargetableTilesFrom(
            GridMapGen.Inst.GetTileAt(v.X, v.Y),
            TeamSelectionType.OTHER_TEAM, true
            );
            if (temp == null) continue;
            if (temp.Count < 1) continue;
            foreach (GridTile tile in temp)
            {
                AvailableAction aa = new AvailableAction();
                aa.CurrentlyAchievable = true;
                aa.IsBasicAttack = true;
                aa.SpecialAction = null;
                aa.TargetTile = tile;
                Nodes[v.Y, v.X].targetsFromNode.Add(aa);
            }
        }
    }
    public List<GridTile> GetTilePathTo(int _x, int _y)
    {
        List<GridTile> temp = new List<GridTile>();
        if (_x < 0 || _y < 0 || _y >= Nodes.GetLength(0) || _x >= Nodes.GetLength(1)) return null;
        if (Nodes[_y, _x] == null) return null;
        if (!Nodes[_y, _x].ValidEndpoint) return null;
        GridFloodNode next = Nodes[_y, _x];
        while (next != null)
        {
            temp.Add(next.Tile);
            next = next.PrevNode;
        }
        return temp;
    }
    public Vector3[] GetWorldPathTo(int _x, int _y) { return null; }
}

public partial class GridMapGen : RayCast3D
{
    public static GridMapGen Inst { get; set; }
    public bool MapGenerated { get; private set; }

	[Export] public Rect2I MapBounds { get; set; }
	[Export] PackedScene gridTilePacked;
	public GridTile[,] Tiles { get; set; }

    public List<GridFlood> Floods { get; set; }

    // On ready, use the map bounds to generate the map tiles
    public override void _Ready()
	{
        Inst = this;
        Floods = new List<GridFlood>();
		Tiles = new GridTile[MapBounds.Size.Y , MapBounds.Size.X];
	}
	public void GenerateTiles()
	{
        for (int y = 0; y < Tiles.GetLength(0); y++)
        {
            for (int x = 0; x < Tiles.GetLength(1); x++)
            {
                GlobalPosition = new Vector3(x + MapBounds.Position.X, 2, y + MapBounds.Position.Y);
                ForceRaycastUpdate();
                if (IsColliding())
                {
                    Tiles[y, x] = gridTilePacked.Instantiate() as GridTile;
                    GetParent().AddChild(Tiles[y, x]);
                    Tiles[y, x].GlobalPosition = GetCollisionPoint();
                    Tiles[y, x].TilePosition = new Vector2I(x, y);
                }
                else Tiles[y, x] = null;
            }
        }
        ResetTileVisuals();
        MapGenerated = true;
    }

	public override void _Process(double delta)
	{
	}

    public GridTile GetTileAt(int _x, int _y)
    {
        if (_x < 0 || _y < 0 || _x >= MapBounds.Size.X || _y >= MapBounds.Size.Y) return null;
        return Tiles[_y, _x];
    }
    public GridTile WorldPosToTile(Vector3 _pos)
    {
        int x = Mathf.RoundToInt(_pos.X) - MapBounds.Position.X;
        int y = Mathf.RoundToInt(_pos.Z) - MapBounds.Position.Y;
        if (x < 0 || y < 0 || x >= MapBounds.Size.X || y >= MapBounds.Size.Y) return null;
        return Tiles[y, x];
    }
    public Vector3 TileToWorldPos(int _x, int _y)
    {
        _x = Mathf.Clamp(_x, 0, MapBounds.Size.X - 1);
        _y = Mathf.Clamp(_y, 0, MapBounds.Size.Y - 1);
        if (Tiles[_y, _x] == null) return new Vector3(_x + MapBounds.Position.X, 0, _y + MapBounds.Position.Y);
        else return Tiles[_y, _x].GlobalPosition;
    }
    public Vector2I WorldToTilePos(Vector3 _pos)
    {
        int x = Mathf.RoundToInt(_pos.X) - MapBounds.Position.X;
        int y = Mathf.RoundToInt(_pos.Z) - MapBounds.Position.Y;
        if (x < 0 || y < 0 || x >= MapBounds.Size.X || y >= MapBounds.Size.Y) return new Vector2I(-1,-1);
        return new Vector2I(x, y);
    }

    public void RenderAllFloods()
    {
        foreach (GridFlood flood in Floods)
        {
            foreach (GridFloodNode node in flood.Nodes)
            {
                if (node == null) continue;
                node.Tile.Visible = node.ValidEndpoint;
            }
        }
    }
    public void ClearAllFloods()
    {
        Floods.Clear();
        ResetTileVisuals();
    }
    public void ResetTileVisuals()
    {
        foreach (GridTile tile in Tiles)
        {
            if (tile == null) continue;
            tile.Visible = false;
        }
    }
    public void Redraw()
    {
        ResetTileVisuals();
        RenderAllFloods();
    }
    public override void _ExitTree()
    {
        Inst = null;
    }

}
