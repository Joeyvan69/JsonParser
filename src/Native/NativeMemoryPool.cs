namespace Native
{
    using System;
    using System.Runtime.InteropServices;

    public class NativeMemoryPool
    {
        public IntPtr Memory { get; private set; }
        public int Size { get; private set; }
        public int Used { get; private set; }
        public NativeMemoryPool(int size)
        {
            Size = size;
            Memory = Marshal.AllocHGlobal(Size);
        }

        ~NativeMemoryPool()
        {
            if(Memory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Memory);
                Memory = IntPtr.Zero;
            }
        }
    }
}