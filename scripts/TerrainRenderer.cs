using System;
using Godot;

public class TerrainRenderer
{
	private readonly int Width, Depth;
	private readonly RenderingDevice _rd;

	private readonly Rid
		_shader,
		_pipeline,
		_heightBuffer,
		_normalBuffer,
		_uniformSet;

	private readonly int
		_vertexCount,
		_heightBufferSize,
		_normalBufferSize;

	public TerrainRenderer(int width, int depth, RDShaderFile shaderFile)
	{
		Width = width;
		Depth = depth;

		_rd = RenderingServer.CreateLocalRenderingDevice();

		_vertexCount = (Width + 1) * (Depth + 1);
		_heightBufferSize = _vertexCount * sizeof(float);
		_normalBufferSize = _vertexCount * sizeof(float) * 3;

		// Create buffers once
		_heightBuffer = _rd.StorageBufferCreate((uint)_heightBufferSize);
		_normalBuffer = _rd.StorageBufferCreate((uint)_normalBufferSize);

		// Load shader once
		_shader = _rd.ShaderCreateFromSpirV(shaderFile.GetSpirV());
		_pipeline = _rd.ComputePipelineCreate(_shader);

		// Create uniform set once
		var heightUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		heightUniform.AddId(_heightBuffer);

		var normalUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1
		};
		normalUniform.AddId(_normalBuffer);

		_uniformSet = _rd.UniformSetCreate([heightUniform, normalUniform], _shader, 0);
	}

	public ArrayMesh GenerateMesh(
		float cellSize,
		float scale,
		float amplitude,
		Vector2 offset,
		Mesh.PrimitiveType renderType
	) {
		ComputeResult result = Compute(scale, amplitude, offset.X, offset.Y);
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
				vertices[v] = new Vector3(x * cellSize, result.Heights[v], z * cellSize);
				normals[v] = result.Normals[v];
				uvs[v] = new Vector2((float)x / Width, (float)z / Depth);
				v++;
			}
		}

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

				// Triangle 1
				indices[i++] = topLeft;
				indices[i++] = bottomRight;
				indices[i++] = bottomLeft;

				// Triangle 2
				indices[i++] = topLeft;
				indices[i++] = topRight;
				indices[i++] = bottomRight;
			}
		}

		// Collect arrays into the godot mesh array
		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		mesh.AddSurfaceFromArrays(renderType, arrays);
		return mesh;
	}

	/// <summary>
	/// Compute terrain heights and normals with dynamic parameters.
	/// </summary>
	public ComputeResult Compute(float scale, float amplitude, float offsetX = 0, float offsetZ = 0)
	{
		// Prepare 32-byte push constants (shader expects 32 bytes)
		var pushConstants = new byte[32];
		Buffer.BlockCopy(BitConverter.GetBytes(Width), 0, pushConstants, 0, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(Depth), 0, pushConstants, 4, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(scale), 0, pushConstants, 8, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(amplitude), 0, pushConstants, 12, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(offsetX), 0, pushConstants, 16, 4);
		Buffer.BlockCopy(BitConverter.GetBytes(offsetZ), 0, pushConstants, 20, 4);
		// last 8 bytes are padding, left as zero

		// Dispatch compute shader
		var computeList = _rd.ComputeListBegin();
		_rd.ComputeListBindComputePipeline(computeList, _pipeline);
		_rd.ComputeListBindUniformSet(computeList, _uniformSet, 0);
		_rd.ComputeListSetPushConstant(computeList, pushConstants, (uint)pushConstants.Length);
		_rd.ComputeListDispatch(computeList,
			(uint)Mathf.Ceil((Width + 1) / 8.0f),
			(uint)Mathf.Ceil((Depth + 1) / 8.0f),
			1);
		_rd.ComputeListEnd();
		_rd.Submit();
		_rd.Sync();

		// Read back height data
		byte[] heightData = _rd.BufferGetData(_heightBuffer);
		float[] heights = new float[_vertexCount];
		Buffer.BlockCopy(heightData, 0, heights, 0, _heightBufferSize);

		// Read back normal data
		byte[] normalData = _rd.BufferGetData(_normalBuffer);
		float[] normalsFlat = new float[_vertexCount * 3];
		Buffer.BlockCopy(normalData, 0, normalsFlat, 0, _normalBufferSize);

		Vector3[] normalVectors = new Vector3[_vertexCount];
		for (int v = 0; v < _vertexCount; v++)
		{
			int baseIndex = v * 3;
			normalVectors[v] = new Vector3(
				normalsFlat[baseIndex],
				normalsFlat[baseIndex + 1],
				normalsFlat[baseIndex + 2]
			);
		}

		return new ComputeResult
		{
			Heights = heights,
			Normals = normalVectors
		};
	}

	public void Free()
	{
		_rd.FreeRid(_shader);
		_rd.FreeRid(_pipeline);
		_rd.FreeRid(_heightBuffer);
		_rd.FreeRid(_normalBuffer);
		_rd.FreeRid(_uniformSet);
	}
}
