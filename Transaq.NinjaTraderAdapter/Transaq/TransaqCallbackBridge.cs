using System;
using System.Collections.Concurrent;
using Transaq.NinjaTraderAdapter.Interop;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqCallbackBridge
{
    private readonly ITransaqNative _native;
    private readonly BlockingCollection<string> _queue;
    private readonly Action<string> _log;

    public TransaqCallbackBridge(ITransaqNative native, BlockingCollection<string> queue, Action<string>? log = null)
    {
        _native = native;
        _queue = queue;
        _log = log ?? (_ => { });
    }

    public TransaqNative.CallbackEx Build() => Callback;

    private bool Callback(IntPtr data)
    {
        try
        {
            if (data != IntPtr.Zero)
            {
                var message = Utf8StringReader.ReadUtf8Z(data);
                _native.FreeMemory(data);
                _queue.Add(message);
            }
        }
        catch (Exception ex)
        {
            _log($"Callback error: {ex}");
        }

        return true;
    }
}
