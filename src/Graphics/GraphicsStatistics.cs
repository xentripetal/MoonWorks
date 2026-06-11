using System.Collections.Generic;
using System.Threading;

namespace MoonWorks.Graphics;

/// <summary>
/// Coarse kind of a GPU resource, for resident-memory accounting in <see cref="GraphicsStatistics"/>.
/// </summary>
public enum GpuResourceKind
{
	Texture,
	Buffer,
	TransferBuffer
}

/// <summary>
/// Automatic per-frame render statistics and resident GPU-memory accounting, maintained by the
/// <see cref="GraphicsDevice"/>. Draw calls, render/compute/copy passes, triangles, texture binds,
/// and upload bytes are accumulated as commands are recorded — no instrumentation is needed in
/// rendering code. Call <see cref="BeginFrame"/> once per frame to read the previous frame's totals
/// and reset for the next.
///
/// Draw work is additionally attributed to the active debug group
/// (<see cref="CommandBuffer.PushDebugGroup"/> / <see cref="CommandBuffer.PopDebugGroup"/>), so a
/// per-group breakdown (e.g. "Terrain", "UI") comes for free and lines up with the groups shown in
/// GPU frame captures (RenderDoc, Xcode).
///
/// Per-frame counters are written from the render thread and so are unsynchronized; memory counters
/// are updated on resource create/dispose from any thread and use interlocked operations.
/// </summary>
public sealed class GraphicsStatistics
{
	/// <summary>A bucket of per-frame counters (frame total, or one debug group).</summary>
	public sealed class Counters
	{
		/// <summary>Draw calls issued (DrawIndexedPrimitives / DrawPrimitives / indirect).</summary>
		public long DrawCalls;

		/// <summary>Render passes begun.</summary>
		public long RenderPasses;

		/// <summary>Compute passes begun.</summary>
		public long ComputePasses;

		/// <summary>Copy passes begun.</summary>
		public long CopyPasses;

		/// <summary>Triangles submitted (derived from index/vertex counts).</summary>
		public long Triangles;

		/// <summary>Texture/sampler bindings issued.</summary>
		public long TextureBinds;

		/// <summary>Bytes streamed to the GPU via copy passes.</summary>
		public long UploadBytes;

		internal void Reset()
		{
			DrawCalls = 0;
			RenderPasses = 0;
			ComputePasses = 0;
			CopyPasses = 0;
			Triangles = 0;
			TextureBinds = 0;
			UploadBytes = 0;
		}
	}

	/// <summary>Frame totals across all debug groups.</summary>
	public Counters Total { get; } = new();

	private readonly Dictionary<string, Counters> groups = new();

	/// <summary>
	/// Per-debug-group frame counters, keyed by group name. Entries persist across frames (reset,
	/// not removed) so a UI can show stable rows; a group that issued no work shows zeros.
	/// </summary>
	public IReadOnlyDictionary<string, Counters> Groups => groups;

	/// <summary>Returns the counters bucket for a debug-group name, creating it on first use.</summary>
	internal Counters GroupCounters(string name)
	{
		if (!groups.TryGetValue(name, out var c))
		{
			c = new Counters();
			groups[name] = c;
		}
		return c;
	}

	// ---- recording (render thread) ----

	internal void RecordDrawCall(long triangles, Counters group)
	{
		Total.DrawCalls++;
		Total.Triangles += triangles;
		if (group != null)
		{
			group.DrawCalls++;
			group.Triangles += triangles;
		}
	}

	internal void RecordRenderPass(Counters group)
	{
		Total.RenderPasses++;
		if (group != null) group.RenderPasses++;
	}

	internal void RecordComputePass(Counters group)
	{
		Total.ComputePasses++;
		if (group != null) group.ComputePasses++;
	}

	internal void RecordCopyPass(Counters group)
	{
		Total.CopyPasses++;
		if (group != null) group.CopyPasses++;
	}

	internal void RecordTextureBinds(long count, Counters group)
	{
		Total.TextureBinds += count;
		if (group != null) group.TextureBinds += count;
	}

	internal void RecordUpload(long bytes, Counters group)
	{
		Total.UploadBytes += bytes;
		if (group != null) group.UploadBytes += bytes;
	}

	/// <summary>Resets every per-frame counter (total and groups). Call at the start of each frame.</summary>
	public void BeginFrame()
	{
		Total.Reset();
		foreach (var c in groups.Values)
		{
			c.Reset();
		}
	}

	// ---- resident GPU memory (any thread) ----

	private long textureBytes;
	private long bufferBytes;
	private long transferBytes;
	private int textureCount;
	private int bufferCount;
	private int transferCount;

	/// <summary>Estimated resident bytes of sampled/render-target textures.</summary>
	public long TextureBytes => Interlocked.Read(ref textureBytes);

	/// <summary>Estimated resident bytes of vertex/index/storage buffers.</summary>
	public long BufferBytes => Interlocked.Read(ref bufferBytes);

	/// <summary>Estimated resident bytes of transfer (upload/download) buffers.</summary>
	public long TransferBytes => Interlocked.Read(ref transferBytes);

	/// <summary>Total estimated resident GPU bytes across all tracked resource kinds.</summary>
	public long TotalResourceBytes => TextureBytes + BufferBytes + TransferBytes;

	/// <summary>Number of live textures.</summary>
	public int TextureCount => Volatile.Read(ref textureCount);

	/// <summary>Number of live buffers.</summary>
	public int BufferCount => Volatile.Read(ref bufferCount);

	/// <summary>Number of live transfer buffers.</summary>
	public int TransferCount => Volatile.Read(ref transferCount);

	internal void TrackResource(GpuResourceKind kind, long bytes)
	{
		switch (kind)
		{
			case GpuResourceKind.Texture:
				Interlocked.Add(ref textureBytes, bytes);
				Interlocked.Increment(ref textureCount);
				break;
			case GpuResourceKind.Buffer:
				Interlocked.Add(ref bufferBytes, bytes);
				Interlocked.Increment(ref bufferCount);
				break;
			case GpuResourceKind.TransferBuffer:
				Interlocked.Add(ref transferBytes, bytes);
				Interlocked.Increment(ref transferCount);
				break;
		}
	}

	internal void UntrackResource(GpuResourceKind kind, long bytes)
	{
		switch (kind)
		{
			case GpuResourceKind.Texture:
				Interlocked.Add(ref textureBytes, -bytes);
				Interlocked.Decrement(ref textureCount);
				break;
			case GpuResourceKind.Buffer:
				Interlocked.Add(ref bufferBytes, -bytes);
				Interlocked.Decrement(ref bufferCount);
				break;
			case GpuResourceKind.TransferBuffer:
				Interlocked.Add(ref transferBytes, -bytes);
				Interlocked.Decrement(ref transferCount);
				break;
		}
	}
}
