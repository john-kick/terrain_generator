using System.Linq;
using Godot;

public class DebugHandler(Node3D container)
{
	private readonly Node3D container = container;

	public void RenderVertexDebugSpheres(Vector3[] vertices, float radius = 0.1f)
	{
		if (container == null) return;

		RemoveDebugSpheres();

		foreach (Vector3 vertex in vertices)
		{
			var sphere = new MeshInstance3D
			{
				Mesh = new SphereMesh
				{
					Radius = radius,
					Height = radius * 2,
					RadialSegments = 8,
					Rings = 6
				},
				MaterialOverride = new StandardMaterial3D()
				{
					AlbedoColor = Colors.Black
				},
				Position = vertex
			};
			container.AddChild(sphere);
		}
	}

	private void RemoveDebugSpheres()
	{
		if (container == null) return;

		foreach (Node3D child in container.GetChildren().Cast<Node3D>())
			child.QueueFree();
	}
}
