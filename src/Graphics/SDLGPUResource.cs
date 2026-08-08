using System;
using System.Threading;

namespace MoonWorks.Graphics;

public abstract class SDLGPUResource : GraphicsResource
{
	public IntPtr Handle { get => handle; internal set => handle = value; }
	private IntPtr handle;

	protected abstract Action<IntPtr, IntPtr> ReleaseFunction { get; }

	// Resident-memory accounting: set by the creating subtype, subtracted on dispose.
	private GpuResourceKind statKind;
	private long statBytes;

	protected SDLGPUResource(GraphicsDevice device) : base(device)
	{
	}

	/// <summary>
	/// Registers this resource's resident size with the device statistics. Called by the
	/// creating subtype once its size is known; the amount is automatically subtracted on dispose.
	/// </summary>
	internal void TrackMemory(GpuResourceKind kind, long bytes)
	{
		statKind = kind;
		statBytes = bytes;
		Device.Statistics.TrackResource(kind, bytes);
	}

	public static implicit operator IntPtr(SDLGPUResource resource)
	{
		return resource.Handle;
	}

	/// <inheritdoc />
	/// <remarks>
	/// The release is inline only on the deterministic path. On the finalizer path the handle goes to
	/// <see cref="GraphicsDevice.DeferredReleases"/> instead, because the GC thread owns neither the
	/// device nor any ordering against the thread that is recording commands — see
	/// <see cref="DeferredReleaseQueue"/>. The handle is exchanged out atomically either way, so a
	/// racing <c>Dispose()</c> and finalizer cannot both hand the same handle onward.
	/// </remarks>
	protected override void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (statBytes != 0)
			{
				Device.Statistics.UntrackResource(statKind, statBytes);
				statBytes = 0;
			}

			var toDispose = Interlocked.Exchange(ref handle, IntPtr.Zero);
			Device.DeferredReleases.ReleaseOrDefer(
				Device.Handle,
				toDispose,
				ReleaseFunction,
				inline: disposing
			);
		}
		base.Dispose(disposing);
	}
}
