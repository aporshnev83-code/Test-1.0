using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Xml.Linq;
using Transaq.NinjaTraderAdapter.Interop;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqSession : IDisposable
{
    private readonly ITransaqNative _native;
    private readonly TransaqCommandClient _client;
    private readonly BlockingCollection<string> _queue = new();
    private readonly ManualResetEventSlim _connectedEvent = new();
    private readonly XmlRouter _router;
    private readonly TransaqMessagePump _pump;
    private readonly TransaqNative.CallbackEx _callback;

    public TransaqSession(ITransaqNative native, XmlRouter router, Action<string>? log = null)
    {
        _native = native;
        _router = router;
        var logger = log ?? (_ => { });
        _client = new TransaqCommandClient(_native, logger);
        _pump = new TransaqMessagePump(_queue, _router, logger);
        _callback = new TransaqCallbackBridge(_native, _queue, logger).Build();
        _router.OnServerStatus += connected =>
        {
            if (connected)
            {
                IsConnected = true;
                _connectedEvent.Set();
            }
        };
    }

    public bool IsConnected { get; private set; }

    public void Connect(string login, string password, string host, int port, bool autopos)
    {
        _native.Initialize("logs", 1);
        _native.SetCallback(_callback);
        _pump.Start();

        _client.Send(new XElement("command",
            new XAttribute("id", "connect"),
            new XElement("login", login),
            new XElement("password", password),
            new XElement("host", host),
            new XElement("port", port),
            new XElement("autopos", autopos ? "true" : "false")));

        if (!SpinWait.SpinUntil(() => IsConnected || _connectedEvent.IsSet, 5000))
        {
            throw new TimeoutException("No server_status connected=true received.");
        }
    }

    public void Disconnect()
    {
        if (!IsConnected) return;
        _client.Send(new XElement("command", new XAttribute("id", "disconnect")));
        _pump.Stop();
        _native.UnInitialize();
        IsConnected = false;
    }

    public XDocument SubscribeMarketData(string board, string seccode) =>
        _client.Send(new XElement("command",
            new XAttribute("id", "subscribe"),
            new XElement("quotations", new XElement("security", new XElement("board", board), new XElement("seccode", seccode))),
            new XElement("alltrades", new XElement("security", new XElement("board", board), new XElement("seccode", seccode))),
            new XElement("quotes", new XElement("security", new XElement("board", board), new XElement("seccode", seccode)))));

    public XDocument SendNewOrder(string board, string seccode, string buySell, decimal quantity, decimal? price = null, bool byMarket = false, string unfilled = "PutInQueue")
    {
        var command = new XElement("command",
            new XAttribute("id", "neworder"),
            new XElement("security", new XElement("board", board), new XElement("seccode", seccode)),
            new XElement("buysell", buySell),
            new XElement("quantity", quantity),
            new XElement("unfilled", unfilled));

        if (byMarket)
        {
            command.Add(new XElement("bymarket", "true"));
        }
        else if (price.HasValue)
        {
            command.Add(new XElement("price", price.Value));
        }

        var res = _client.Send(command);
        ValidateResult(res);
        return res;
    }

    public XDocument CancelOrder(long transactionId) =>
        _client.Send(new XElement("command", new XAttribute("id", "cancelorder"), new XElement("transactionid", transactionId)));

    public XDocument MoveOrder(long transactionId, decimal newPrice) =>
        _client.Send(new XElement("command", new XAttribute("id", "moveorder"), new XElement("transactionid", transactionId), new XElement("price", newPrice)));

    public XDocument RequestPositions() => _client.Send(new XElement("command", new XAttribute("id", "get_forts_positions")));
    public XDocument RequestLimits() => _client.Send(new XElement("command", new XAttribute("id", "get_client_limits")));

    private static void ValidateResult(XDocument document)
    {
        var result = document.Root;
        if (result?.Name.LocalName != "result") return;
        if ((result.Attribute("success")?.Value ?? "false") == "false")
        {
            throw new InvalidOperationException(result.Value);
        }
    }

    public void Dispose() => Disconnect();
}
