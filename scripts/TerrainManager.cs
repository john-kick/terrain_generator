using System;
using System.Reflection.Metadata;
using Godot;

public partial class TerrainManager : Node3D
{
	[Export] public int TilesRadius = 2;
	[Export] public int Width, Depth;
	[Export] public float CellSize, _Scale, Amplitude;
	[Export] public bool Animate;
	[Export] public Mesh.PrimitiveType PrimitiveType = Mesh.PrimitiveType.Triangles;
	[Export] public Material Material;
	[Export] public RDShaderFile TerrainShader;

	private Vector2 Offset;
	private Tile[] _tiles;
	private TerrainRenderer _renderer;
	private bool _dirty = true;

	public override void _Ready()
	{
		Offset = Vector2.Zero;

		int numTiles = (int)Mathf.Pow(TilesRadius * 2, 2);
		_tiles = new Tile[numTiles];

		for (int i = 0; i < numTiles; i++)
		{
			_tiles[i] = new Tile()
			{
				MeshInstance = new MeshInstance3D()
				{
					MaterialOverride = Material ??= new StandardMaterial3D()
					{
						AlbedoColor = Colors.Cyan
					}
				},
				Position = GetTilePosition(i)
			};

			AddChild(_tiles[i].MeshInstance);
		}

		_renderer = new TerrainRenderer(Width, Depth, TerrainShader);
	}

	public override void _Process(double delta)
	{
		if (Animate)
		{
			Offset += new Vector2(0.1f, 0.2f);
			_dirty = true;
		}
		if (_dirty)
		{
			_dirty = false;
			GenerateTerrain();
		}
	}

	private void GenerateTerrain()
	{
		for (int i = 0; i < _tiles.Length; i++)
		{
			Vector2 offset = _tiles[i].Position / CellSize + Offset;
			_tiles[i].MeshInstance.Mesh = _renderer.GenerateMesh(CellSize, _Scale, Amplitude, offset, PrimitiveType);
		}
	}

	private Vector2 GetTilePosition(int index)
	{
		Vector2I position = Vector2I.Zero;
		int steps = 0;
		int stepSize = 1;
		int direction = 0; //  0 = down; 1 = left; 2 = up; 3 = right;

		// Starting at -1 to make the start of the algorithm work
		for (int i = 0; i < index; i++)
		{
			// Step
			position += direction switch
			{
				0 => new Vector2I(0, -1),
				1 => new Vector2I(-1, 0),
				2 => new Vector2I(0, 1),
				3 => new Vector2I(1, 0),
				_ => throw new Exception("Invalid direction"),
			};
			steps++;
			if (steps == stepSize)
			{
				steps = 0;
				direction = (direction + 1) % 4;
				if (direction % 2 == 0)
				{
					stepSize++;
				}
			}
		}

		return new Vector2(position.X * Width, position.Y * Depth) * CellSize;
	}
}
