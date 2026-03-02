using System;
using System.Collections.Concurrent;
using System.Text;
using Transaq.NinjaTraderAdapter.Interop;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqCallbackBridge
{
    private readonly ITransaqNative _native;
    private readonly ConcurrentQueue<string> _queue;

    public TransaqCallbackBridge(ITransaqNative native, ConcurrentQueue<string> queue)
    {
        _native = native;
        _queue = queue;
    }

    public TransaqNative.CallbackEx Build() => Callback;

    private bool Callback(IntPtr data)
    {
        try
        {
            if (data == IntPtr.Zero)
            {
                return true;
            }

            var message = ReadUtf8Z(data);
            _native.FreeMemory(data);
            _queue.Enqueue(message);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadUtf8Z(IntPtr ptr)
    {
        var bytes = new System.Collections.Generic.List<byte>();
        var offset = 0;
        byte value;
        while ((value = System.Runtime.InteropServices.Marshal.ReadByte(ptr, offset++)) != 0)
        {
            bytes.Add(value);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }
}
