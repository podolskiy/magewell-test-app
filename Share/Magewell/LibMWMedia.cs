﻿using System;
using System.Runtime.InteropServices;

namespace ExternalAPI.Magewell
{
    public class LibMWMedia
    {
        //////////////////////////////////////////////////////////////////////////////////////////
        // video renderer
#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateD3DRenderer(int cx, int cy, UInt32 dwFourcc, bool bReverse, IntPtr hWnd);


#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern void MWDestroyD3DRenderer(IntPtr hRenderer);


#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int MWD3DRendererPushFrame(IntPtr hRenderer, IntPtr pbyBuffer, int cbStride);


        //////////////////////////////////////////////////////////////////////////////////////////
        // audio renderer
#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWCreateDSoundRenderer(int nSamplesPerSec, int nChannels, int nSamplesPerFrame, int nBufferFrameCount);

#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern IntPtr MWDestroyDSoundRenderer(IntPtr hRenderer);

#if DEBUG
        [DllImport("LibMWMediad.dll", CallingConvention = CallingConvention.Cdecl)]
#else
        [DllImport("LibMWMedia.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
        public static extern int MWDSoundRendererPushFrame(IntPtr hRenderer, IntPtr pbFrame, int cbFram);

    }
}
