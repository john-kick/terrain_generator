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

	public TerrainRenderer(int width, int depth)
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
		var shaderFile = GD.Load<RDShaderFile>("res://shader/compute/wave.glsl");
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
