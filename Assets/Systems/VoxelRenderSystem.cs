using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.Experimental;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Unity.Mathematics.math;

[ExecuteAlways]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public class VoxelRenderSystem : JobComponentSystem
{
	private EntityQuery _query;
	private const int batchSize = VoxSRP.BatchSize;

	private NativeArray<Vector4> _colorCache;
	public NativeQueue<Batch> _batchQueue;
	private NativeArray<float> _defaultScales;
	public NativeArray<VoxelColor> _lastColors;
	public NativeArray<LocalToWorld> _lastMatrices;
	private int chunksPerBatch;
	private int _lastAllocation;
	public JobHandle LastJob;

	protected override void OnCreate()
	{
		base.OnCreate();
		var settings = (VoxSRPAsset)GraphicsSettings.currentRenderPipeline;

		if (settings != null)
		{
			chunksPerBatch = settings.chunksPerBatch;
		}
		else
		{
			chunksPerBatch = 5;
		}

		_query = GetEntityQuery(new EntityQueryDesc
		{
			All = new[] { ComponentType.ReadOnly<VoxelColor>(), ComponentType.ReadOnly<LocalToWorld>(),  },
		});
		_colorCache = new NativeArray<Vector4>(batchSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		_batchQueue = new NativeQueue<Batch>(Allocator.Persistent);
		_defaultScales = new NativeArray<float>(batchSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

		for (int i = 0; i < batchSize; i++)
			_defaultScales[i] = 1f;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		_colorCache.Dispose();
		_batchQueue.Dispose();
		_defaultScales.Dispose();
		if (_lastColors.IsCreated)
			_lastColors.Dispose();
		if (_lastMatrices.IsCreated)
			_lastMatrices.Dispose();
	}

	public struct Batch
	{
		public int GlobalIndex;
		public int Length;
	}

	[BurstCompile]
	private struct BatchingJob : IJobParallelForBatch
	{
		public NativeArray<VoxelColor> Colors;
		public NativeArray<LocalToWorld> Matrices;
		public NativeQueue<Batch>.ParallelWriter Batches;
		[ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
		[ReadOnly] public ArchetypeChunkComponentType<VoxelColor> ColorType;
		[ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> MatrixType;
		[ReadOnly] public NativeArray<int> IndexMappings;

		public void Execute(int startIndex, int chunkCount)
		{
			int globalIndex = IndexMappings[startIndex];
			int i = 0;

			for (var j = 0; j < chunkCount; j++)
			{
				var chunk = Chunks[j + startIndex];
				for (int k = 0; k < chunk.Count;)
				{
					var entriesLeft = batchSize - i;

					var count = min(entriesLeft, chunk.Count - k);

					var start = k;

					Colors.GetSubArray(globalIndex, count)
						.CopyFrom(chunk.GetNativeArray(ColorType).GetSubArray(start, count));
					Matrices.GetSubArray(globalIndex, count)
						.CopyFrom(chunk.GetNativeArray(MatrixType).GetSubArray(start, count));
					
					k += count;
					i += count;

					if (i == batchSize || j + 1 == chunkCount)
					{
						Batches.Enqueue(new Batch
						{
							GlobalIndex = globalIndex,
							Length = count
						});

						i = 0;
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct IndexingJob : IJob
	{
		public NativeArray<int> IndexMappings;
		[ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
		public int ChunksPerBatch;

		public void Execute()
		{
			int spaceRequirement = 0;
			for (var c = 0; c < Chunks.Length; c += ChunksPerBatch)
			{
				for (int j = 0; j < min(ChunksPerBatch, Chunks.Length - c); j++)
				{
					var chunk = Chunks[c + j];
					IndexMappings[c + j] = spaceRequirement;
					spaceRequirement += chunk.Count;
				}
			}
		}
	}

	protected override JobHandle OnUpdate(JobHandle inputDependencies)
	{
		_lastAllocation++;
		LastJob.Complete();
		
		if (_batchQueue.Count > 0)
			return inputDependencies;
		
		Profiler.BeginSample("Dependencies");
		var voxelColorType = GetArchetypeChunkComponentType<VoxelColor>(true);
		var matrixType = GetArchetypeChunkComponentType<LocalToWorld>(true);
		var chunks = _query.CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var chunksHandle);
		var chunkCount = _query.CalculateChunkCount();
		var spaceRequirement = _query.CalculateEntityCount();
		var indexMappings = new NativeArray<int>(chunkCount, Allocator.TempJob);
		var indexing = new IndexingJob
		{
			IndexMappings = indexMappings,
			Chunks = chunks,
			ChunksPerBatch = chunksPerBatch
		}.Schedule(JobHandle.CombineDependencies(inputDependencies, chunksHandle));
		Profiler.EndSample();

		JobHandle deps = indexing;
		Profiler.BeginSample("Prepare Space");
		if (_lastColors.Length == spaceRequirement && _lastAllocation < 4)
		{
			var colorClear = new MemsetNativeArray<VoxelColor>
			{
				Source = _lastColors,
				Value = default
			}.Schedule(_lastColors.Length, 256, inputDependencies);

			var translationClear = new MemsetNativeArray<LocalToWorld>
			{
				Source = _lastMatrices,
				Value = default
			}.Schedule(_lastMatrices.Length, 256, inputDependencies);

			deps = JobHandle.CombineDependencies(colorClear, translationClear, deps);
		}
		else
		{
			Profiler.BeginSample("Cleanup");
			if (_lastColors.IsCreated)
				_lastColors.Dispose();
			if (_lastMatrices.IsCreated)
				_lastMatrices.Dispose();
			Profiler.EndSample();
			
			Profiler.BeginSample("Allocate");
			_lastColors = new NativeArray<VoxelColor>(spaceRequirement, Allocator.TempJob);
			_lastMatrices = new NativeArray<LocalToWorld>(spaceRequirement, Allocator.TempJob);
			Profiler.EndSample();
			_lastAllocation = 0;
		}
		Profiler.EndSample();

		var batchingJob = new BatchingJob
		{
			IndexMappings = indexMappings,
			Colors = _lastColors,
			Matrices = _lastMatrices,
			Batches = _batchQueue.AsParallelWriter(),
			Chunks = chunks,
			ColorType = voxelColorType,
			MatrixType = matrixType,
		}.ScheduleBatch(chunks.Length, chunksPerBatch, deps); 
		chunks.Dispose(batchingJob);
		indexMappings.Dispose(batchingJob);
		LastJob = batchingJob;
		return batchingJob;
	}
}