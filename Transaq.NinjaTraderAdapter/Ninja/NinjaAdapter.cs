using System;
using Transaq.NinjaTraderAdapter.State;
using Transaq.NinjaTraderAdapter.Transaq;

namespace Transaq.NinjaTraderAdapter.Ninja;

public sealed class NinjaAdapter
{
    private readonly NinjaDispatcher _dispatcher;

    public NinjaAdapter(XmlRouter router, NinjaDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        router.OnBidAskUpdate += (key, snap) => _dispatcher.Invoke(() => OnBidAskUpdate?.Invoke(key, snap));
        router.OnLastTrade += (key, price, qty, ts) => _dispatcher.Invoke(() => OnLastTrade?.Invoke(key, price, qty, ts));
        router.OnDomUpdate += dom => _dispatcher.Invoke(() => OnDomUpdate?.Invoke(dom));
        router.OnOrderUpdate += order => _dispatcher.Invoke(() => OnOrderUpdate?.Invoke(order));
    }

    public event Action<InstrumentKey, MarketDataSnapshot>? OnBidAskUpdate;
    public event Action<InstrumentKey, decimal, decimal, DateTimeOffset>? OnLastTrade;
    public event Action<DomBook>? OnDomUpdate;
    public event Action<OrderState>? OnOrderUpdate;
}
