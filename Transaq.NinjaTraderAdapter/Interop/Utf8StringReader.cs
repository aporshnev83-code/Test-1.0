using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Transaq.NinjaTraderAdapter.Interop;

internal static class Utf8StringReader
{
    public static string ReadUtf8Z(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        var length = 0;
        while (Marshal.ReadByte(ptr, length) != 0)
        {
            length++;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        var bytes = new byte[length];
        Marshal.Copy(ptr, bytes, 0, length);
        return Encoding.UTF8.GetString(bytes);
    }
}
