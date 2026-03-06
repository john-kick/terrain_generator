using Godot;

public partial class Terrain : MeshInstance3D
{
	[Export] public int Width = 20;
	[Export] public int Depth = 20;
	[Export] public float NoiseScale = 1.1f;
	[Export] public float Amplitude = 1;
	[Export] public Vector2 Offset = Vector2.Zero;
	[Export] public float CellSize = 1f;
	[Export] public bool Animate = false;
	[Export] public bool Debug = false;
	[Export] public Material Material;
	[Export] public Mesh.PrimitiveType RenderType = Mesh.PrimitiveType.Triangles;
	private bool printed = false;

	private Node3D _debugContainer;
	private DebugHandler _debugHandler;
	private TerrainRenderer _terrainRenderer;

	public override void _Ready()
	{
		Material ??= new StandardMaterial3D()
		{
			AlbedoColor = Colors.Cyan
		};
		MaterialOverride = Material;

		_debugContainer = GetNode<Node3D>("DebugContainer");
		_debugContainer ??= new Node3D();
		
		_debugHandler = new DebugHandler(_debugContainer);
		_terrainRenderer = new TerrainRenderer(Width, Depth);
		// Mesh = GenerateMesh();
	}

	public override void _Process(double delta)
	{
		if (Animate)
		{
			Offset += new Vector2(0.1f, 0.2f);
		}
		Mesh = GenerateMesh();
	}

	private ArrayMesh GenerateMesh(bool clockwise = true)
	{
		ComputeResult result = _terrainRenderer.Compute(NoiseScale, Amplitude, Offset.X, Offset.Y);
		ArrayMesh mesh = new();
		int numVertices = (Width + 1) * (Depth + 1);

		// Define arrays
		Vector3[] vertices = new Vector3[numVertices];
		Vector3[] normals = new Vector3[numVertices];
		Vector2[] uvs = new Vector2[numVertices];
		int[] indices = new int[Width * Depth * 6];

		// Fill vertices, normals and uvs
		int v = 0;
		for (int z = 0; z < Depth + 1; z++)
		{
			for (int x = 0; x < Width + 1; x++)
			{
				vertices[v] = new Vector3(x * CellSize, result.Heights[v], z * CellSize);
				normals[v] = result.Normals[v];
				uvs[v] = new Vector2((float)x / Width, (float)z / Depth);
				if (!printed && result.Heights[v] == x + Width + 1)
				{
					GD.Print($"({x}|{z}) = {result.Heights[v]}");
				}
				v++;
			}
		}
		printed = true;

		// Fill indices (Triangles)
		// Godot uses clockwise triangles, shaders use anti-clockwise
		//
		//  1.------- 2
		//  |  .      |         Clockwise:          1 -> 2 -> 4, 1 -> 4 -> 3
		//  |     .   |
		//  |        .|         Anti-clockwise:     1 -> 4 -> 2, 1 -> 3 -> 4
		//  3---------4
		//

		int i = 0;
		int stride = Width + 1;
		for (int z = 0; z < Depth; z++)
		{
			for (int x = 0; x < Width; x++)
			{
				int topLeft = z * stride + x;
				int topRight = topLeft + 1;
				int bottomLeft = topLeft + stride;
				int bottomRight = bottomLeft + 1;

				if (clockwise)
				{

					// Triangle 1
					indices[i++] = topLeft;
					indices[i++] = bottomRight;
					indices[i++] = bottomLeft;

					// Triangle 2
					indices[i++] = topLeft;
					indices[i++] = topRight;
					indices[i++] = bottomRight;
				}
				else
				{

					// Triangle 1
					indices[i++] = topLeft;
					indices[i++] = bottomLeft;
					indices[i++] = bottomRight;

					// Triangle 2
					indices[i++] = topLeft;
					indices[i++] = bottomRight;
					indices[i++] = topRight;
				}
			}
		}

		// Collect arrays into the godot mesh array
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		mesh.AddSurfaceFromArrays(RenderType, arrays);

		if (Debug)
		{
			_debugHandler.RenderVertexDebugSpheres(vertices);
		}

		return mesh;
	}

	private ImmediateMesh GenerateImmediateMesh(bool clockwise = true)
	{
		ComputeResult result = _terrainRenderer.Compute(NoiseScale, Amplitude, Offset.X, Offset.Y);
		ImmediateMesh mesh = new ();

		// if (Debug)
		// {
		// 	_degubHandler.RenderVertexDebugSpheres(vertices);
		// }

		return mesh;
	}
}
