using System;
using System.Runtime.InteropServices;

namespace MoonWorks
{
	public static class Logger
	{
		public static Action<string> LogInfo = LogInfoDefault;
		public static Action<string> LogWarn = LogWarnDefault;
		public static Action<string> LogError = LogErrorDefault;

		private static void LogInfoDefault(string str)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write("INFO: ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(str);
		}

		private static void LogWarnDefault(string str)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("WARN: ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(str);
		}

		private static void LogErrorDefault(string str)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("ERROR: ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(str);
		}

		internal static unsafe void InitSDLLogging()
		{
			SDL3.SDL.SDL_SetLogPriority((int) SDL3.SDL.SDL_LogCategory.SDL_LOG_CATEGORY_GPU, SDL3.SDL.SDL_LogPriority.SDL_LOG_PRIORITY_INFO);
			// A function pointer rather than a marshalled delegate: the browser runtime can only call
			// back into managed code through [UnmanagedCallersOnly] methods.
			SDL_SetLogOutputFunction(&SDLLog, IntPtr.Zero);
		}

		[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void SDL_SetLogOutputFunction(
			delegate* unmanaged[Cdecl]<IntPtr, int, SDL3.SDL.SDL_LogPriority, byte*, void> callback,
			IntPtr userdata
		);

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		internal static unsafe void SDLLog(IntPtr userdata, int category, SDL3.SDL.SDL_LogPriority priority, byte* message)
		{
			if (priority == SDL3.SDL.SDL_LogPriority.SDL_LOG_PRIORITY_INFO)
			{
				LogInfo(Marshal.PtrToStringUTF8((nint) message));
			}
			else if (priority == SDL3.SDL.SDL_LogPriority.SDL_LOG_PRIORITY_WARN)
			{
				LogWarn(Marshal.PtrToStringUTF8((nint) message));
			}
			else if (priority == SDL3.SDL.SDL_LogPriority.SDL_LOG_PRIORITY_ERROR)
			{
				LogError(Marshal.PtrToStringUTF8((nint) message));
			}
		}
	}
}
