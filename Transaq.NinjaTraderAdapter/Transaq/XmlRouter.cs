using System;
using System.Globalization;
using System.Xml.Linq;
using Transaq.NinjaTraderAdapter.State;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class XmlRouter
{
    private readonly InstrumentCatalog _instruments;
    private readonly MarketDataState _marketData;
    private readonly DomBook _domBook;
    private readonly OrderStateStore _orders;
    private readonly PositionStateStore _positions;

    public XmlRouter(InstrumentCatalog instruments, MarketDataState marketData, DomBook domBook, OrderStateStore orders, PositionStateStore positions)
    {
        _instruments = instruments;
        _marketData = marketData;
        _domBook = domBook;
        _orders = orders;
        _positions = positions;
    }

    public event Action<InstrumentKey, MarketDataSnapshot>? OnBidAskUpdate;
    public event Action<InstrumentKey, decimal, decimal, DateTimeOffset>? OnLastTrade;
    public event Action<DomBook>? OnDomUpdate;
    public event Action<OrderState>? OnOrderUpdate;
    public event Action<bool>? OnServerStatus;

    public void Route(XDocument document)
    {
        var root = document.Root;
        if (root is null) return;

        switch (root.Name.LocalName)
        {
            case "server_status":
                OnServerStatus?.Invoke((V(root, "connected") ?? "false") == "true");
                break;
            case "quotations":
                foreach (var q in root.Elements("quotation"))
                {
                    var key = Key(q);
                    var snap = _marketData.Merge(key,
                        last: DecV(q, "last"),
                        bid: DecV(q, "bid"),
                        ask: DecV(q, "ask"));
                    OnBidAskUpdate?.Invoke(key, snap);
                }
                break;
            case "alltrades":
            case "ticks":
                foreach (var t in root.Elements())
                {
                    var key = Key(t);
                    var price = DecV(t, "price") ?? 0m;
                    var qty = DecV(t, "quantity") ?? 0m;
                    var timestamp = DateTimeOffset.TryParse(V(t, "time"), out var parsed) ? parsed : DateTimeOffset.UtcNow;
                    _marketData.Merge(key, last: price, timestampUtc: timestamp);
                    OnLastTrade?.Invoke(key, price, qty, timestamp);
                }
                break;
            case "quotes":
                foreach (var q in root.Elements("quote"))
                {
                    _domBook.ApplyDelta(new QuoteDelta(
                        DecV(q, "price") ?? 0m,
                        DecV(q, "buy"),
                        DecV(q, "sell"),
                        V(q, "source")));
                }
                OnDomUpdate?.Invoke(_domBook);
                break;
            case "orders":
                foreach (var o in root.Elements("order"))
                {
                    var state = _orders.Upsert(
                        LongV(o, "transactionid"),
                        LongV(o, "orderno"),
                        V(o, "status") ?? string.Empty,
                        DecV(o, "quantity") ?? 0,
                        DecV(o, "filled") ?? 0);
                    if (LongV(o, "withdrawtime") == 0 && state.Status == "cancelled")
                    {
                        state.NtState = NtOrderState.Cancelling;
                    }

                    OnOrderUpdate?.Invoke(state);
                }
                break;
            case "forts_position":
            case "forts_money":
            case "clientlimits":
                _positions.Merge(
                    openBalance: DecV(root, "open_balance"),
                    currentBalance: DecV(root, "current_balance"),
                    variationMargin: DecV(root, "varmargin"),
                    cash: DecV(root, "cash"),
                    usedMargin: DecV(root, "margin"));
                break;
            case "securities":
                foreach (var sec in root.Elements("security"))
                {
                    _instruments.Upsert(
                        Key(sec),
                        new InstrumentInfo(
                            DecV(sec, "lotsize") ?? 1,
                            (int)(DecV(sec, "decimals") ?? 0),
                            DecV(sec, "minstep") ?? 0,
                            DecV(sec, "point_cost") ?? 0));
                }
                break;
        }
    }

    private static string? V(XElement e, string name) => e.Attribute(name)?.Value ?? e.Element(name)?.Value;
    private static InstrumentKey Key(XElement e) => new(V(e, "board") ?? string.Empty, V(e, "seccode") ?? string.Empty);
    private static decimal? Dec(string? value) => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    private static decimal? DecV(XElement e, string name) => Dec(V(e, name));
    private static long Long(string? value) => long.TryParse(value, out var v) ? v : 0;
    private static long LongV(XElement e, string name) => Long(V(e, name));
}
