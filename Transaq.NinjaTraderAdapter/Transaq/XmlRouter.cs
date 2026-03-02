using System;
using System.Globalization;
using System.Linq;
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
                OnServerStatus?.Invoke((root.Attribute("connected")?.Value ?? "false") == "true");
                break;
            case "quotations":
                foreach (var q in root.Elements("quotation"))
                {
                    var key = Key(q);
                    var snap = _marketData.Merge(key, bid: Dec(q.Element("bid")?.Value), ask: Dec(q.Element("ask")?.Value));
                    OnBidAskUpdate?.Invoke(key, snap);
                }
                break;
            case "alltrades":
            case "ticks":
                foreach (var t in root.Elements())
                {
                    var key = Key(t);
                    var price = Dec(t.Element("price")?.Value) ?? 0m;
                    var qty = Dec(t.Element("quantity")?.Value) ?? 0m;
                    var snap = _marketData.Merge(key, last: price);
                    OnLastTrade?.Invoke(key, price, qty, snap.TimestampUtc);
                }
                break;
            case "quotes":
                foreach (var q in root.Elements("quote"))
                {
                    _domBook.ApplyDelta(new QuoteDelta(
                        Dec(q.Element("price")?.Value) ?? 0m,
                        Dec(q.Element("buy")?.Value),
                        Dec(q.Element("sell")?.Value),
                        q.Element("source")?.Value));
                }
                OnDomUpdate?.Invoke(_domBook);
                break;
            case "orders":
                foreach (var o in root.Elements("order"))
                {
                    var state = _orders.Upsert(
                        Long(o.Element("transactionid")?.Value),
                        Long(o.Element("orderno")?.Value),
                        o.Element("status")?.Value ?? string.Empty,
                        Dec(o.Element("quantity")?.Value) ?? 0,
                        Dec(o.Element("filled")?.Value) ?? 0);
                    if ((Long(o.Element("withdrawtime")?.Value)) == 0 && state.Status == "cancelled")
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
                    openBalance: Dec(root.Element("open_balance")?.Value),
                    currentBalance: Dec(root.Element("current_balance")?.Value),
                    variationMargin: Dec(root.Element("varmargin")?.Value),
                    cash: Dec(root.Element("cash")?.Value),
                    usedMargin: Dec(root.Element("margin")?.Value));
                break;
            case "securities":
                foreach (var sec in root.Elements("security"))
                {
                    _instruments.Upsert(
                        Key(sec),
                        new InstrumentInfo(
                            Dec(sec.Element("lotsize")?.Value) ?? 1,
                            (int)(Dec(sec.Element("decimals")?.Value) ?? 0),
                            Dec(sec.Element("minstep")?.Value) ?? 0,
                            Dec(sec.Element("point_cost")?.Value) ?? 0));
                }
                break;
        }
    }

    private static InstrumentKey Key(XElement e) => new(e.Element("board")?.Value ?? string.Empty, e.Element("seccode")?.Value ?? string.Empty);
    private static decimal? Dec(string? value) => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    private static long Long(string? value) => long.TryParse(value, out var v) ? v : 0;
}
