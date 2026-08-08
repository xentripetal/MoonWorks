using System.Collections.Concurrent;
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
/// rendering code. Call <see cref="BeginFrame"/> once per frame, from the recording thread, to
/// publish the frame that just ended and reset for the next.
///
/// Draw work is additionally attributed to the active debug group
/// (<see cref="CommandBuffer.PushDebugGroup"/> / <see cref="CommandBuffer.PopDebugGroup"/>), so a
/// per-group breakdown (e.g. "Terrain", "UI") comes for free and lines up with the groups shown in
/// GPU frame captures (RenderDoc, Xcode).
/// </summary>
/// <remarks>
/// <b>Two surfaces, one for each side of a thread boundary.</b> The live counters are internal and
/// accumulate with interlocked adds, because a pipelined application records on more than one thread.
/// Everyone else — an overlay, a stats CLI, anything that wants to display a number — reads
/// <see cref="LastFrame"/>, an immutable snapshot published at <see cref="BeginFrame"/>. Publishing
/// rather than locking is what gives a reader a value that is internally consistent: it is one
/// finished frame, never a half-updated one.
/// <para>
/// The group map is a <see cref="ConcurrentDictionary{TKey, TValue}"/> even so. Entries are created
/// by name when a debug group is first pushed, which is authored by whoever is recording — and the
/// present step and the render frame need not be the same thread once rendering is pipelined. A
/// plain dictionary structurally mutated by one thread while another enumerates it is the failure
/// that costs a torn read at best; group creation happens a handful of times per frame, so the
/// concurrent map costs nothing measurable.
/// </para>
/// <para>
/// Resident-memory counters are separate again: they are updated on resource create and dispose from
/// any thread, so they use interlocked operations and are safe to read at any time.
/// </para>
/// </remarks>
public sealed class GraphicsStatistics
{
	/// <summary>A bucket of live per-frame counters (frame total, or one debug group).</summary>
	/// <remarks>
	/// Accumulated with interlocked adds, because <em>recording</em> is not one thread once rendering is
	/// pipelined: the render frame is recorded on one thread and the present step — the swapchain blit
	/// and whatever overlay rides on top of it — on another. Both go through the same counters. Plain
	/// <c>++</c> across two threads loses increments silently, and a lost increment in a statistics
	/// overlay reads as "the renderer did less work", which is exactly the wrong thing for a number
	/// people tune against. The cost is one interlocked add per recorded call, which is nothing beside
	/// the call it is counting.
	/// <para>
	/// What interlocking does <em>not</em> buy is frame attribution. <see cref="BeginFrame"/> closes a
	/// frame from wherever it is called, and work another thread records either side of that instant
	/// lands in whichever frame was open — so a pipelined present step's counts may be attributed one
	/// frame off. That is inherent to counting two threads' work under one frame boundary, and it is
	/// why the read surface is a published snapshot rather than a live total.
	/// </para>
	/// </remarks>
	internal sealed class Counters
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

		internal FrameCounters ToValue() =>
			new(
				Interlocked.Read(ref DrawCalls),
				Interlocked.Read(ref RenderPasses),
				Interlocked.Read(ref ComputePasses),
				Interlocked.Read(ref CopyPasses),
				Interlocked.Read(ref Triangles),
				Interlocked.Read(ref TextureBinds),
				Interlocked.Read(ref UploadBytes)
			);

		internal void Reset()
		{
			Interlocked.Exchange(ref DrawCalls, 0);
			Interlocked.Exchange(ref RenderPasses, 0);
			Interlocked.Exchange(ref ComputePasses, 0);
			Interlocked.Exchange(ref CopyPasses, 0);
			Interlocked.Exchange(ref Triangles, 0);
			Interlocked.Exchange(ref TextureBinds, 0);
			Interlocked.Exchange(ref UploadBytes, 0);
		}
	}

	/// <summary>One finished frame's counters, by value. Safe to read from any thread.</summary>
	public readonly record struct FrameCounters(
		long DrawCalls,
		long RenderPasses,
		long ComputePasses,
		long CopyPasses,
		long Triangles,
		long TextureBinds,
		long UploadBytes
	);

	/// <summary>
	/// The counters of the most recently finished frame — totals plus the per-debug-group breakdown.
	/// Immutable, published as a whole, and therefore readable from any thread.
	/// </summary>
	public sealed class FrameSnapshot
	{
		internal FrameSnapshot(FrameCounters total, IReadOnlyDictionary<string, FrameCounters> groups)
		{
			Total = total;
			Groups = groups;
		}

		/// <summary>Frame totals across all debug groups.</summary>
		public FrameCounters Total { get; }

		/// <summary>Per-debug-group totals, keyed by group name.</summary>
		public IReadOnlyDictionary<string, FrameCounters> Groups { get; }

		/// <summary>The value published before any frame has finished.</summary>
		public static readonly FrameSnapshot Empty = new(default, new Dictionary<string, FrameCounters>());
	}

	/// <summary>Live frame totals. Accumulated by every recording thread; read via <see cref="LastFrame"/>.</summary>
	internal Counters Total { get; } = new();

	private readonly ConcurrentDictionary<string, Counters> groups = new();

	private FrameSnapshot lastFrame = FrameSnapshot.Empty;

	/// <summary>
	/// The last frame <see cref="BeginFrame"/> closed. This is the read surface: an overlay or a
	/// debug command reads it from wherever it runs, and gets one whole frame rather than counters
	/// another thread is halfway through writing.
	/// </summary>
	public FrameSnapshot LastFrame => Volatile.Read(ref lastFrame);

	/// <summary>Returns the counters bucket for a debug-group name, creating it on first use.</summary>
	internal Counters GroupCounters(string name) => groups.GetOrAdd(name, static _ => new Counters());

	// ---- recording (any recording thread) ----

	internal void RecordDrawCall(long triangles, Counters group)
	{
		Interlocked.Increment(ref Total.DrawCalls);
		Interlocked.Add(ref Total.Triangles, triangles);
		if (group != null)
		{
			Interlocked.Increment(ref group.DrawCalls);
			Interlocked.Add(ref group.Triangles, triangles);
		}
	}

	internal void RecordRenderPass(Counters group)
	{
		Interlocked.Increment(ref Total.RenderPasses);
		if (group != null) Interlocked.Increment(ref group.RenderPasses);
	}

	internal void RecordComputePass(Counters group)
	{
		Interlocked.Increment(ref Total.ComputePasses);
		if (group != null) Interlocked.Increment(ref group.ComputePasses);
	}

	internal void RecordCopyPass(Counters group)
	{
		Interlocked.Increment(ref Total.CopyPasses);
		if (group != null) Interlocked.Increment(ref group.CopyPasses);
	}

	internal void RecordTextureBinds(long count, Counters group)
	{
		Interlocked.Add(ref Total.TextureBinds, count);
		if (group != null) Interlocked.Add(ref group.TextureBinds, count);
	}

	internal void RecordUpload(long bytes, Counters group)
	{
		Interlocked.Add(ref Total.UploadBytes, bytes);
		if (group != null) Interlocked.Add(ref group.UploadBytes, bytes);
	}

	/// <summary>
	/// Publishes the frame that just ended into <see cref="LastFrame"/> and resets every live
	/// counter. Call once per frame, at a point in the frame that is the same every frame — publishing
	/// is what makes the counters readable elsewhere.
	/// </summary>
	/// <remarks>
	/// Group entries persist across frames (reset, not removed) so a UI can show stable rows; a group
	/// that issued no work shows zeros.
	/// <para>
	/// Where to call it when more than one thread records: at a point where the others are idle, so a
	/// frame boundary means the same thing to all of them. A pipelined renderer has exactly one such
	/// point per frame — the hand-off — and calling it anywhere else attributes the other thread's work
	/// to whichever frame happened to be open.
	/// </para>
	/// </remarks>
	public void BeginFrame()
	{
		var snapshotGroups = new Dictionary<string, FrameCounters>(groups.Count);
		foreach (var (name, counters) in groups)
		{
			snapshotGroups[name] = counters.ToValue();
			counters.Reset();
		}

		Volatile.Write(ref lastFrame, new FrameSnapshot(Total.ToValue(), snapshotGroups));
		Total.Reset();
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
