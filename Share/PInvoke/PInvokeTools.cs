using System;
using System.Runtime.InteropServices;

namespace Polywall.Share.PInvoke
{
    public class PInvokeTools
    {
        public static T ReadStruct<T>(Action<IntPtr> read) where T : new()
        {
            IntPtr unmanagedAddr = AllocHGlobal<T>();
            read.Invoke(unmanagedAddr);
            T channelInfo = (T)Marshal.PtrToStructure(unmanagedAddr, typeof(T));
            Marshal.FreeHGlobal(unmanagedAddr);
            unmanagedAddr = IntPtr.Zero;
            return channelInfo;
        }

        public static void WriteStruct<T>(T obj, Action<IntPtr> write) where T : new()
        {
            IntPtr unmanagedAddr = AllocHGlobal<T>();
            Marshal.StructureToPtr(obj, unmanagedAddr, false);
            write.Invoke(unmanagedAddr);
            unmanagedAddr = IntPtr.Zero;
        }
        public static IntPtr AllocHGlobal<T>()
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T))); ;
        }
    }
}