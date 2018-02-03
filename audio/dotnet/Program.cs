using System;
using System.Text;
using System.Runtime.InteropServices;

namespace dotnet
{
	// http://www.alsa-project.org/alsa-doc/alsa-lib
	internal static class LinuxAudioInterop
	{
		public static unsafe void* Open(string name, bool capture)
		{
			void* handle;
			if (Open(&handle, name, capture ? Capture : Playback, 0) != 0)
			{
				throw new ArgumentException("Open failed.");
			}

			return handle;
		}

		public static unsafe void* HardwareParamsMalloc()
		{
			void *param;
			if (HardwareParamsMalloc(&param) != 0)
			{
				throw new ArgumentException("Hardware params malloc failed.");
			}

			return param;
		}

		public static unsafe void HardwareParamsInit(void *handle, void *param)
		{
			if (HardwareParamsAny(handle, param) != 0)
			{
				throw new ArgumentException("Hardware params any failed.");
			}
		}

		public static unsafe void HardwareParamsSetAccess(void *handle, void *param, bool interleaved)
		{
			if (HardwareParamsSetAccess(handle, param, interleaved ? Interleaved : NonInterleaved) != 0)
			{
				throw new ArgumentException("Hardware params set access failed.");
			}
		}

		public enum Format : int
		{
			Signed16LittleEndian = 2
			// TODO
		}

		public static unsafe void HardwareParamsSetFormat(void *handle, void *param, Format format)
		{
			if (HardwareParamsSetFormat(handle, param, (int)format) != 0)
			{
				throw new ArgumentException("Hardware params set format failed.");
			}
		}

		public static unsafe void HardwareParamsSetRate(void *handle, void *param, int rate)
		{
			int *pRate = &rate;
			int dir = 0;
			int *pDir = &dir;
			if (HardwareParamsSetRate(handle, param, pRate, pDir) != 0)
			{
				throw new ArgumentException("Hardware params set rate failed.");
			}
		}


		public static unsafe void HardwareParamsSetChannels(void *handle, void *param, int channels)
		{
			if (HardwareParamsSetChannels(handle, param, (uint)channels) != 0)
			{
				throw new ArgumentException("Hardware params set channels failed.");
			}
		}

		public static unsafe void HardwareSetParams(void *handle, void *param)
		{
			if (HardwareParams(handle, param) != 0)
			{
				throw new ArgumentException("Hardware set params failed.");
			}
		}

		public static unsafe void HardwareFreeParams(void *param)
		{
			if (HardwareParamsFree(param) != 0)
			{
				throw new ArgumentException("Hardware free params failed.");
			}
		}

		public static unsafe void Prepare(void *handle)
		{
			if (PrepareHandle(handle) != 0)
			{
				throw new ArgumentException("Prepare handle failed.");
			}
		}

		public static unsafe void Read(void *handle, short[] buffer, int size)
		{
			// TODO: support other formats?
			fixed (void* pBuffer = buffer)
			{
				if (Read(handle, pBuffer, (ulong)size) != size)
				{
					throw new ArgumentException("Read failed.");
				}
			}
		}

		public static unsafe void Close(void *handle)
		{
			if (CloseHandle(handle) != 0)
			{
				throw new ArgumentException("Close failed.");
			}
		}

		private const int Playback = 0; // SND_PCM_STREAM_PLAYBACK
		private const int Capture = 1; // SND_PCM_STREAM_CAPTURE

		[DllImport("asound", EntryPoint="snd_pcm_open")]
		private static unsafe extern int Open(void **handle, [MarshalAs(UnmanagedType.LPStr)]string name, int capture, int mode);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_malloc")]
		private static unsafe extern int HardwareParamsMalloc(void **param);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_any")]
		private static unsafe extern int HardwareParamsAny(void *handle, void *param);

		private const int Interleaved = 3; // SND_PCM_ACCESS_RW_INTERLEAVED
		private const int NonInterleaved = 4; // SND_PCM_ACCESS_RW_NONINTERLEAVED

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_set_access")]
		private static unsafe extern int HardwareParamsSetAccess(void *handle, void *param, int access);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_set_format")]
		private static unsafe extern int HardwareParamsSetFormat(void *handle, void *param, int format);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_set_rate_near")]
		private static unsafe extern int HardwareParamsSetRate(void *handle, void *param, int *rate, int *dir);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_set_channels")]
		private static unsafe extern int HardwareParamsSetChannels(void *handle, void *param, uint channels);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params")]
		private static unsafe extern int HardwareParams(void *handle, void *param);

		[DllImport("asound", EntryPoint="snd_pcm_hw_params_free")]
		private static unsafe extern int HardwareParamsFree(void *param);

		[DllImport("asound", EntryPoint="snd_pcm_prepare")]
		private static unsafe extern int PrepareHandle(void *handle);

		[DllImport("asound", EntryPoint="snd_pcm_readi")]
		private static unsafe extern long Read(void *handle, void* buffer, ulong blockSize);

		[DllImport("asound", EntryPoint="snd_pcm_close")]
		private static unsafe extern int CloseHandle(void *handle);
	}
	
    class Program
    {
        static unsafe void Main(string[] args)
        {
			try
			{
				Console.WriteLine("ALSA Test App");
				var device = "plughw:0,0";
				var handle = LinuxAudioInterop.Open(device, true);
				Console.WriteLine("Opened");
				var param = LinuxAudioInterop.HardwareParamsMalloc();
				Console.WriteLine("Params");
				LinuxAudioInterop.HardwareParamsInit(handle, param);
				Console.WriteLine("Params initialized");
				LinuxAudioInterop.HardwareParamsSetAccess(handle, param, true);
				Console.WriteLine("Params set access");
				LinuxAudioInterop.HardwareParamsSetFormat(handle, param, LinuxAudioInterop.Format.Signed16LittleEndian);
				Console.WriteLine("Params set format");
				LinuxAudioInterop.HardwareParamsSetRate(handle, param, 44100);
				Console.WriteLine("Params set rate");
				LinuxAudioInterop.HardwareParamsSetChannels(handle, param, 1);
				Console.WriteLine("Params set channels");
				LinuxAudioInterop.HardwareSetParams(handle, param);
				Console.WriteLine("Set params");
				LinuxAudioInterop.HardwareFreeParams(param);
				Console.WriteLine("Free params");
				LinuxAudioInterop.Prepare(handle);
				Console.WriteLine("Prepare handle");

				const int blockSize = 128;
				var buf = new short[blockSize * 2];

				for (var i = 0; i < 1000; i++)
				{
					LinuxAudioInterop.Read(handle, buf, blockSize);
					Console.WriteLine($"Read buffer: {buf[0]}");
				}

				LinuxAudioInterop.Close(handle);
				Console.WriteLine("Close handle");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
        }
    }
}
