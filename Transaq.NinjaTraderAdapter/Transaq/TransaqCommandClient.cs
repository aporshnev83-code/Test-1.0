using System;
using System.Xml.Linq;
using Transaq.NinjaTraderAdapter.Interop;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqCommandClient
{
    private readonly ITransaqNative _native;
    private readonly Action<string> _log;

    public TransaqCommandClient(ITransaqNative native, Action<string>? log = null)
    {
        _native = native;
        _log = log ?? (_ => { });
    }

    public XDocument Send(XElement command)
    {
        var xml = command.ToString(SaveOptions.DisableFormatting);
        _log($"TX >> {Sanitize(xml)}");
        var response = _native.SendCommand(xml);
        _log($"TX << {response}");
        return XDocument.Parse(string.IsNullOrWhiteSpace(response) ? "<result success=\"false\"/>" : response);
    }

    private static string Sanitize(string xml) => xml.Replace("<password>", "<password>***");
}
