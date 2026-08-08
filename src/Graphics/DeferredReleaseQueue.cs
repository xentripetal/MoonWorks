using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MoonWorks.Graphics;

/// <summary>
/// Native GPU handles whose release could not happen inline, held until the thread that owns the
/// device drains them at a defined point in the frame.
/// </summary>
/// <remarks>
/// The only producer that exists today is the GC finalizer thread. A managed wrapper that was never
/// disposed is collected on a thread that owns nothing, and releasing its handle there runs the
/// backend's destroy path concurrently with whatever the owning thread is recording or submitting —
/// which is a heap race in the SDL Metal backend and undefined behaviour in the SDL contract on
/// every backend. The finalizer therefore hands the handle here and warns; the release happens later,
/// on the owning thread, at <see cref="GraphicsDevice.DrainDeferredReleases"/>.
/// <para>
/// The queue is deliberately not a leak fixer. A resource reaching it is a bug that
/// <see cref="GraphicsResource.Dispose()"/> was supposed to prevent, and the warning says so. What
/// the queue guarantees is that the bug costs a late release rather than a corrupted heap.
/// </para>
/// </remarks>
public sealed class DeferredReleaseQueue
{
	/// <summary>One handle waiting for its owning thread.</summary>
	private readonly struct Entry
	{
		public readonly IntPtr DeviceHandle;
		public readonly IntPtr ResourceHandle;
		public readonly Action<IntPtr, IntPtr> Release;

		public Entry(IntPtr deviceHandle, IntPtr resourceHandle, Action<IntPtr, IntPtr> release)
		{
			DeviceHandle = deviceHandle;
			ResourceHandle = resourceHandle;
			Release = release;
		}
	}

	private readonly ConcurrentQueue<Entry> pending = new();
	private long deferredTotal;
	private long releasedTotal;
	private volatile bool closed;

	/// <summary>Handles waiting to be drained.</summary>
	public int PendingCount => pending.Count;

	/// <summary>Handles that have ever been deferred — a running leak count.</summary>
	public long DeferredTotal => Interlocked.Read(ref deferredTotal);

	/// <summary>Handles that have ever been released by a drain.</summary>
	public long ReleasedTotal => Interlocked.Read(ref releasedTotal);

	/// <summary>
	/// Releases <paramref name="resourceHandle"/> now if the caller owns the device, or defers it if
	/// it does not. This is the one decision every GPU-resource wrapper's disposal funnels through.
	/// </summary>
	/// <param name="deviceHandle">The device the resource belongs to.</param>
	/// <param name="resourceHandle">The native handle. Zero is a no-op.</param>
	/// <param name="release">The backend release entry point.</param>
	/// <param name="inline">
	/// True on the deterministic disposal path — <c>Dispose()</c> called by the owner. False on the
	/// finalizer path, where the calling thread owns nothing.
	/// </param>
	/// <returns>True when the handle was released inline, false when it was deferred.</returns>
	public bool ReleaseOrDefer(
		IntPtr deviceHandle,
		IntPtr resourceHandle,
		Action<IntPtr, IntPtr> release,
		bool inline
	) {
		if (resourceHandle == IntPtr.Zero)
		{
			return true;
		}

		if (inline)
		{
			release(deviceHandle, resourceHandle);
			return true;
		}

		if (closed)
		{
			// The device is gone, which freed everything it owned. Releasing the handle now would be
			// a double free against a destroyed device, so the leak is simply dropped.
			Interlocked.Increment(ref deferredTotal);
			return false;
		}

		pending.Enqueue(new Entry(deviceHandle, resourceHandle, release));
		Interlocked.Increment(ref deferredTotal);
		return false;
	}

	/// <summary>
	/// Releases everything queued. Must be called from the thread that owns the device — the drain
	/// is the point at which a deferred release becomes an ordinary one.
	/// </summary>
	/// <returns>How many handles were released.</returns>
	public int Drain()
	{
		var count = 0;
		while (pending.TryDequeue(out var entry))
		{
			entry.Release(entry.DeviceHandle, entry.ResourceHandle);
			count += 1;
		}

		if (count > 0)
		{
			Interlocked.Add(ref releasedTotal, count);
		}

		return count;
	}

	/// <summary>
	/// Drops everything queued and refuses everything later, without releasing any of it. Called when
	/// the device itself is being destroyed: destroying the device frees its resources, so releasing
	/// them individually afterwards would be a double free — and a finalizer can still run long after
	/// the device is gone, since nothing orders the two.
	/// </summary>
	public void Close()
	{
		closed = true;
		while (pending.TryDequeue(out _)) { }
	}

	/// <summary>True once <see cref="Close"/> has run — the device is gone and drains are pointless.</summary>
	public bool IsClosed => closed;
}
