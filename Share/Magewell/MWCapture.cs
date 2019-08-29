using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Polywall.Share.PInvoke;

namespace ExternalAPI.Magewell
{
    public class MWCapture
    {
        //video device 
        protected IntPtr VideoChannel = IntPtr.Zero;
        protected IntPtr D3DRenderer = IntPtr.Zero;
        protected IntPtr AudioRender = IntPtr.Zero;


        protected int Board = 0;
        protected int ChannelIndex = 0;

        protected IntPtr Video;
        protected IntPtr Audio;
        LibMWCapture.VIDEO_CAPTURE_STDCALLBACK video_callback;
        LibMWCapture.AUDIO_CAPTURE_STDCALLBACK audio_callback;

        protected static int llCount;
        protected static long CurrentTime;
        protected static long RefTime;
        protected static double Fps;
        private bool _muteAudio;

        public bool HasVideoChannel => VideoChannel != IntPtr.Zero;
        public bool HasD3DRenderer => D3DRenderer != IntPtr.Zero;
        public bool HasVideo => Video != IntPtr.Zero;
        public bool HasAudioRender => AudioRender != IntPtr.Zero;
        public bool HasAudio => Audio != IntPtr.Zero;

        public static void video_callback_sub(IntPtr pbFrame, int cbFrame, ulong u64TimeStamp, IntPtr pParam)
        {
            try
            {
                LibMWMedia.MWD3DRendererPushFrame(pParam, pbFrame, 720 * 4);
                llCount += 1;
                if (llCount >= 10)
                {
                    CurrentTime = Libkernel32.GetTickCount();
                    unchecked
                    {
                        Fps = (llCount * 1000) / (CurrentTime - RefTime);
                    }
                    RefTime = CurrentTime;
                    llCount = 0;
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void audio_callback_sub(IntPtr pbFrame, int cbFrame, ulong u64TimeStamp, IntPtr pParam)
        {
            try
            {
                LibMWMedia.MWDSoundRendererPushFrame(pParam, pbFrame, cbFrame);
            }
            catch (Exception ex)
            {

            }
        }

        public MWCapture()
        {

        }

        public static bool Init()
        {
            return LibMWCapture.MWCaptureInitInstance() > 0;
        }

        public static void Exit()
        {
            LibMWCapture.MWCaptureExitInstance();
        }

        public static Boolean RefreshDevices()
        {
            LibMWCapture.MW_RESULT mr;
            mr = LibMWCapture.MWRefreshDevice();
            if (mr != LibMWCapture.MW_RESULT.MW_SUCCEEDED)
                return false;

            return true;
        }

        public static int GetChannelCount()
        {
            return LibMWCapture.MWGetChannelCount();
        }

        public static void GetChannelInfobyIndex(int nChannelIndex, ref LibMWCapture.MWCAP_CHANNEL_INFO channelInfo)
        {
            channelInfo = PInvokeTools.ReadStruct<LibMWCapture.MWCAP_CHANNEL_INFO>(ptr =>
                LibMWCapture.MWGetChannelInfoByIndex(nChannelIndex, ptr));
        }

        public LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS GetVideoSignalStatus()
        {
            return PInvokeTools.ReadStruct<LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS>(ptr =>
                LibMWCapture.MWGetVideoSignalStatus(VideoChannel, ptr));
        }

        public static LibMWCapture.MWCAP_CHANNEL_INFO? GetChannelInfoByIndex(int channelIndex)
        {
            RefreshDevices();
            var videoChannelCount = GetChannelCount();

            if (videoChannelCount == 0)
            {
                return null;
            }

            for (int i = 0; i < videoChannelCount; i++)
            {
                LibMWCapture.MWCAP_CHANNEL_INFO channelInfo = new LibMWCapture.MWCAP_CHANNEL_INFO();
                GetChannelInfobyIndex(i, ref channelInfo);
                if (channelInfo.byChannelIndex == channelIndex)
                {
                    return channelInfo;
                }
            }
            return null;
        }

        public bool OpenVideoChannel(LibMWCapture.MWCAP_CHANNEL_INFO channelInfo, UInt32 dwFourcc, int cx, int cy, UInt32 nFrameDuration, IntPtr hWnd)
        {
            // open video device
            ushort[] wpath = new ushort[512];
            IntPtr pwpath = GCHandle.Alloc(wpath, GCHandleType.Pinned).AddrOfPinnedObject();
            LibMWCapture.MWGetDevicePath(channelInfo.byChannelIndex, pwpath);

            Board = channelInfo.byBoardIndex;
            ChannelIndex = channelInfo.byChannelIndex;

            VideoChannel = LibMWCapture.MWOpenChannelByPath(pwpath);
            if (VideoChannel == IntPtr.Zero)
                return false;

            video_callback = new LibMWCapture.VIDEO_CAPTURE_STDCALLBACK(video_callback_sub);
            // create video renderer

            bool reverse = dwFourcc == MWCap_FOURCC.MWCAP_FOURCC_BGR24 || dwFourcc == MWCap_FOURCC.MWCAP_FOURCC_BGRA;
            D3DRenderer = LibMWMedia.MWCreateD3DRenderer(cx, cy, dwFourcc, reverse, hWnd);

            if (D3DRenderer == IntPtr.Zero)
            {
                return false;
            }
            llCount = 0;
            CurrentTime = RefTime = Libkernel32.GetTickCount();
            uint fourcc = (uint)dwFourcc;
            int frameduration = (int)nFrameDuration;
            Video = LibMWCapture.MWCreateVideoCaptureWithStdCallBack(VideoChannel, cx, cy, fourcc, frameduration, video_callback, D3DRenderer);

            if (Video == IntPtr.Zero)
            {
                return false;
            }

            if (!InitAudioCapture())
            {
                // может быть без аудио если этот источник уже открыт
                //return false;
            }
            return true;
        }

        private bool InitAudioCapture()
        {
            audio_callback = new LibMWCapture.AUDIO_CAPTURE_STDCALLBACK(audio_callback_sub);
            AudioRender = LibMWMedia.MWCreateDSoundRenderer(48000, 2, 480, 10);
            if (AudioRender == IntPtr.Zero)
            {
                return false;
            }
            Audio = LibMWCapture.MWCreateAudioCaptureWithStdCallBack(VideoChannel, LibMWCapture.MWCAP_AUDIO_CAPTURE_NODE.MWCAP_AUDIO_CAPTURE_NODE_DEFAULT, 48000, 16, 2, audio_callback, AudioRender);
            if (Audio == IntPtr.Zero)
            {
                return false;
            }
            return true;
        }

        public void Destory()
        {
            if (Video != IntPtr.Zero)
            {
                LibMWCapture.MWDestoryVideoCapture(Video);
                Video = IntPtr.Zero;
            }

            if (Audio != IntPtr.Zero)
            {
                LibMWCapture.MWDestoryAudioCapture(Audio);
                Audio = IntPtr.Zero;
            }

            if (VideoChannel != IntPtr.Zero)
            {
                LibMWCapture.MWCloseChannel(VideoChannel);
                VideoChannel = IntPtr.Zero;
            }

            if (D3DRenderer != IntPtr.Zero)
            {
                LibMWMedia.MWDestroyD3DRenderer(D3DRenderer);
                D3DRenderer = IntPtr.Zero;
            }

            if (AudioRender != IntPtr.Zero)
            {
                LibMWMedia.MWDestroyDSoundRenderer(AudioRender);
                AudioRender = IntPtr.Zero;
            }
        }

        public double GetFps()
        {
            return Fps;
        }


        public LibMWCapture.MWCAP_AUDIO_VOLUME GetMWAudioVolume()
        {
            return PInvokeTools.ReadStruct<LibMWCapture.MWCAP_AUDIO_VOLUME>(ptr =>
                LibMWCapture.MWGetAudioVolume(VideoChannel, LibMWCapture.MWCAP_AUDIO_NODE.MWCAP_AUDIO_EMBEDDED_CAPTURE, ptr));
        }
        public void SetAudioVolume(int percent)
        {
            GetMWAudioVolume()
                 .SetAudioVolume(VideoChannel, percent);
        }

        public void SetMute(bool mute)
        {
            GetMWAudioVolume()
                  .SetMute(VideoChannel, mute);
        }

        public bool GetMute()
        {
            return GetMWAudioVolume()
                .GetMute();
        }

        public int GetAudioVolume()
        {
            return GetMWAudioVolume()
                .GetAudioVolume();
        }



        public static LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS? GetSignalStatus(int channelIndex, out LibMWCapture.MWCAP_AUDIO_VOLUME? audioStatus)
        {
            audioStatus = null;
            ushort[] wpath = new ushort[512];
            IntPtr pwpath = GCHandle.Alloc(wpath, GCHandleType.Pinned).AddrOfPinnedObject();
            LibMWCapture.MWGetDevicePath(channelIndex, pwpath);

            var videoChannel = LibMWCapture.MWOpenChannelByPath(pwpath);
            if (videoChannel == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                audioStatus = PInvokeTools.ReadStruct<LibMWCapture.MWCAP_AUDIO_VOLUME>(ptr =>
                    LibMWCapture.MWGetAudioVolume(videoChannel, LibMWCapture.MWCAP_AUDIO_NODE.MWCAP_AUDIO_EMBEDDED_CAPTURE, ptr));

                return PInvokeTools.ReadStruct<LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS>(
                    ptr =>
                        LibMWCapture.MWGetVideoSignalStatus(videoChannel, ptr));
            }
            finally
            {
                LibMWCapture.MWCloseChannel(videoChannel);
            }
        }
    }

    public static class MWCaptureExt
    {


        private static readonly short[] AudioVolumes = {
            -9000, -6151, -5219, -4645, -4230, -3904, -3635, -3408, -3209, -3034,
            -2877, -2735, -2604, -2484, -2373, -2270, -2173, -2082, -1996, -1914,
            -1837, -1763, -1693, -1626, -1562, -1500, -1441, -1384, -1329, -1276,
            -1225, -1176, -1128, -1081, -1036,  -992,  -950,  -908,  -868,  -828,
            -790,  -753,  -716,  -681,  -646,  -612,  -579,  -546,  -514,  -483,
            -452,  -422,  -393,  -364,  -336,  -308,  -281,  -254,  -227,  -202,
            -176,  -151,  -126,  -102,   -78,   -55,   -32,    -9,    13,    36,
            59,    82,   106,   131,   155,   180,   206,   232,   258,   285,
            313,   341,   369,   398,   427,   457,   488,   520,   552,   584,
            618,   652,   687,   722,   759,   797,   835,   875,   915,   957,
            1000,
        };

        public static LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS? HasSignal(this LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS signalStatus, out bool hasSignal)
        {
            switch (signalStatus.state)
            {
                case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_UNSUPPORTED:
                case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_NONE:
                    hasSignal = false;
                    break;
                case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKING:
                case LibMWCapture.MWCAP_VIDEO_SIGNAL_STATE.MWCAP_VIDEO_SIGNAL_LOCKED:
                    hasSignal = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return signalStatus;
        }

        public static LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS? GetChannelSize(this LibMWCapture.MWCAP_VIDEO_SIGNAL_STATUS signalStatus, out Size channelSize)
        {
            channelSize = new Size(signalStatus.cx, signalStatus.cy);
            return signalStatus;
        }

        public static int GetAudioVolume(this LibMWCapture.MWCAP_AUDIO_VOLUME audioStatus)
        {
            var volume = audioStatus.asVolume.LastOrDefault();
            var volumeIndex = Array.IndexOf(AudioVolumes, volume);
            volumeIndex = Math.Min(Math.Max(volumeIndex, 0), AudioVolumes.Length - 1);
            return (int)(volumeIndex * 100.0 / AudioVolumes.Length);
        }

        public static void SetAudioVolume(this LibMWCapture.MWCAP_AUDIO_VOLUME audioStatus, IntPtr videoChannel, int percent)
        {
            var volumeIndex = (int)(AudioVolumes.Length / 100.0 * percent);
            volumeIndex = Math.Min(Math.Max(volumeIndex, 0), AudioVolumes.Length - 1);
            var volume = AudioVolumes[volumeIndex];
            for (int i = 0; i < audioStatus.asVolume.Length; i++)
            {
                audioStatus.asVolume[i] = volume;
            }
            PInvokeTools.WriteStruct(audioStatus, ptr =>
                LibMWCapture.MWSetAudioVolume(videoChannel, LibMWCapture.MWCAP_AUDIO_NODE.MWCAP_AUDIO_EMBEDDED_CAPTURE, ptr));
        }

        public static bool GetMute(this LibMWCapture.MWCAP_AUDIO_VOLUME audioStatus)
        {
            var mute = audioStatus.abMute.LastOrDefault();
            return mute;
        }

        public static void SetMute(this LibMWCapture.MWCAP_AUDIO_VOLUME audioStatus, IntPtr videoChannel, bool mute)
        {
            for (int i = 0; i < audioStatus.abMute.Length; i++)
            {
                audioStatus.abMute[i] = mute;
            }
            PInvokeTools.WriteStruct(audioStatus, ptr =>
                LibMWCapture.MWSetAudioVolume(videoChannel, LibMWCapture.MWCAP_AUDIO_NODE.MWCAP_AUDIO_EMBEDDED_CAPTURE, ptr));
        }
    }
}
