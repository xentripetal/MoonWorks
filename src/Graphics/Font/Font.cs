using System;
using System.Runtime.InteropServices;
using MoonWorks.Storage;
using WellspringCS;

namespace MoonWorks.Graphics.Font
{
	public unsafe class Font : GraphicsResource
	{
		public Texture Texture { get; }
		public float PixelsPerEm { get; }
		public float DistanceRange { get; }

		internal IntPtr Handle { get; }

		private byte* StringBytes;
		private int StringBytesLength;

		/// <summary>
		/// Loads a TTF or OTF font from a storage path for use in MSDF rendering.
		/// Note that there must be an msdf-atlas-gen JSON and image file alongside.
		/// </summary>
		/// <returns></returns>
		public unsafe static Font Load(
			GraphicsDevice graphicsDevice,
			TitleStorage storage,
			string fontPath
		) {
			if (!storage.GetFileSize(fontPath, out var fontBytesLength))
			{
				return null;
			}
			var fontBytes = NativeMemory.Alloc((nuint) fontBytesLength);
			var fontSpan = new Span<byte>(fontBytes, (int) fontBytesLength);

			var atlasPath = System.IO.Path.ChangeExtension(fontPath, ".json");
			if (!storage.GetFileSize(atlasPath, out var atlasBytesLength))
			{
				NativeMemory.Free(fontBytes);
				return null;
			}

			var atlasBytes = NativeMemory.Alloc((nuint) atlasBytesLength);
			var atlasSpan = new Span<byte>(atlasBytes, (int) atlasBytesLength);

			if (!storage.ReadFile(fontPath, fontSpan))
			{
				NativeMemory.Free(fontBytes);
				NativeMemory.Free(atlasBytes);
				return null;
			}

			if (!storage.ReadFile(atlasPath, atlasSpan))
			{
				NativeMemory.Free(fontBytes);
				NativeMemory.Free(atlasBytes);
				return null;
			}

			var handle = Wellspring.Wellspring_CreateFont(
				(IntPtr) fontBytes,
				(uint) fontBytesLength,
				(IntPtr) atlasBytes,
				(uint) atlasBytesLength,
				out float pixelsPerEm,
				out float distanceRange
			);

			var imagePath = System.IO.Path.ChangeExtension(fontPath, ".png");
			var uploader = new ResourceUploader(graphicsDevice);
			var texture = uploader.CreateTexture2DFromCompressed(storage, imagePath, TextureFormat.R8G8B8A8Unorm, TextureUsageFlags.Sampler);
			uploader.Upload();
			uploader.Dispose();

			NativeMemory.Free(fontBytes);
			NativeMemory.Free(atlasBytes);

			return new Font(graphicsDevice, handle, texture, pixelsPerEm, distanceRange);
		}

		private Font(GraphicsDevice device, IntPtr handle, Texture texture, float pixelsPerEm, float distanceRange) : base(device)
		{
			Handle = handle;
			Texture = texture;
			PixelsPerEm = pixelsPerEm;
			DistanceRange = distanceRange;
			Name = "Font";

			StringBytesLength = 32;
			StringBytes = (byte*) NativeMemory.Alloc((nuint) StringBytesLength);
		}

		public unsafe bool TextBounds(
			string text,
			int pixelSize,
			HorizontalAlignment horizontalAlignment,
			VerticalAlignment verticalAlignment,
			out Wellspring.Rectangle rectangle
		) {
			var byteCount = System.Text.Encoding.UTF8.GetByteCount(text);

			if (StringBytesLength < byteCount)
			{
				StringBytes = (byte*) NativeMemory.Realloc(StringBytes, (nuint) byteCount);
			}

			fixed (char* chars = text)
			{
				System.Text.Encoding.UTF8.GetBytes(chars, text.Length, StringBytes, byteCount);

				var result = Wellspring.Wellspring_TextBounds(
					Handle,
					pixelSize,
					(Wellspring.HorizontalAlignment) horizontalAlignment,
					(Wellspring.VerticalAlignment) verticalAlignment,
					(IntPtr) StringBytes,
					(uint) byteCount,
					out rectangle
				);

				if (result == 0)
				{
					Logger.LogWarn("Could not decode string: " + text);
					return false;
				}
			}

			return true;
		}

		/// <summary>Destroys the Wellspring font handle. Deferred off the finalizer thread.</summary>
		private static readonly Action<IntPtr, IntPtr> DestroyFont =
			static (_, handle) => Wellspring.Wellspring_DestroyFont(handle);

		/// <inheritdoc />
		/// <remarks>
		/// The handle is Wellspring's rather than SDL's, but the rule is the same one
		/// <see cref="GraphicsResource"/> states: a finalizer runs on the GC thread and may not touch
		/// a native handle, so the destroy goes through <see cref="DeferredReleaseQueue"/> and happens
		/// on the owning thread at the frame's drain point.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					Texture.Dispose();
				}

				Device.DeferredReleases.ReleaseOrDefer(IntPtr.Zero, Handle, DestroyFont, inline: disposing);
			}
			base.Dispose(disposing);
		}
	}
}
