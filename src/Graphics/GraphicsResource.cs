using System;
using System.Runtime.InteropServices;

namespace MoonWorks.Graphics;

public abstract class GraphicsResource : IDisposable
{
	public GraphicsDevice Device { get; }

	private GCHandle SelfReference;

	public bool IsDisposed { get; private set; }
	public string Name { get; protected set; }

	protected GraphicsResource(GraphicsDevice device)
	{
		Device = device;

		SelfReference = GCHandle.Alloc(this, GCHandleType.Weak);
		Device.AddResourceReference(SelfReference);
	}

	/// <summary>
	/// Releases this resource.
	/// </summary>
	/// <param name="disposing">
	/// True on the deterministic path — someone called <see cref="Dispose()"/> on a thread that owns
	/// the device. False on the finalizer path, where the calling thread owns nothing and no native
	/// handle may be touched. Subtypes holding a native handle must respect the distinction; the
	/// sanctioned way is <see cref="DeferredReleaseQueue.ReleaseOrDefer"/>, which
	/// <see cref="SDLGPUResource"/> already funnels through.
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!IsDisposed)
		{
			if (disposing)
			{
				Device.RemoveResourceReference(SelfReference);
				SelfReference.Free();
			}

			IsDisposed = true;
		}
	}

	/// <summary>
	/// Leak detector, not a cleanup path.
	/// </summary>
	/// <remarks>
	/// The finalizer runs on the GC's own thread, concurrently with whatever the thread that owns the
	/// device is doing. Releasing a GPU handle from here is therefore a race with command recording
	/// and submission — latent even in a single-threaded renderer, because the GC thread is a second
	/// thread whether or not the application has one. So the finalizer's whole job is to say a
	/// resource was leaked and hand its handle to the deferred-release queue, which the owning thread
	/// drains at a defined point in the frame. Disposing properly suppresses the finalizer entirely
	/// and none of this happens.
	/// </remarks>
	~GraphicsResource()
	{
		// If you see this log message, you leaked a graphics resource without disposing it!
		// The handle is released later, on the thread that owns the device — but you really should
		// fix this, because until then the resource stays resident.
		Logger.LogWarn($"A resource named {Name} of type {GetType().Name} was not Disposed.");

		Dispose(false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
