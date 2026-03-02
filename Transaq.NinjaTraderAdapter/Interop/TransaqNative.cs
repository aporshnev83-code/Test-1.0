using System;
using System.Runtime.InteropServices;

namespace Transaq.NinjaTraderAdapter.Interop;

public interface ITransaqNative
{
    string Initialize(string logDir, int logLevel);
    string UnInitialize();
    string SetCallback(TransaqNative.CallbackEx callback);
    string SendCommand(string xmlCommand);
    void FreeMemory(IntPtr ptr);
}

public sealed class TransaqNative : ITransaqNative
{
    public delegate bool CallbackEx(IntPtr data);

    private readonly object _dllLock = new();

    [DllImport("TXmlConnector.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern IntPtr NativeInitialize([MarshalAs(UnmanagedType.LPStr)] string pPath, int logLevel);

    [DllImport("TXmlConnector.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr NativeUnInitialize();

    [DllImport("TXmlConnector.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr NativeSetCallbackEx(CallbackEx pCallback);

    [DllImport("TXmlConnector.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private static extern IntPtr NativeSendCommand([MarshalAs(UnmanagedType.LPStr)] string pData);

    [DllImport("TXmlConnector.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern bool NativeFreeMemory(IntPtr pData);

    public string Initialize(string logDir, int logLevel) => CallString(() => NativeInitialize(logDir, logLevel));
    public string UnInitialize() => CallString(NativeUnInitialize);
    public string SetCallback(CallbackEx callback) => CallString(() => NativeSetCallbackEx(callback));
    public string SendCommand(string xmlCommand) => CallString(() => NativeSendCommand(xmlCommand));

    public void FreeMemory(IntPtr ptr)
    {
        lock (_dllLock)
        {
            NativeFreeMemory(ptr);
        }
    }

    private string CallString(Func<IntPtr> action)
    {
        lock (_dllLock)
        {
            var ptr = action();
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            var value = Utf8StringReader.ReadUtf8Z(ptr);
            NativeFreeMemory(ptr);
            return value;
        }
    }
}
