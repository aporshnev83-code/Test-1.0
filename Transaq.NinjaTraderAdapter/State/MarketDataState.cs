using System;
using System.Collections.Generic;

namespace Transaq.NinjaTraderAdapter.State;

public sealed class MarketDataState
{
    private readonly Dictionary<InstrumentKey, MarketDataSnapshot> _map = new();

    public MarketDataSnapshot Merge(InstrumentKey key, decimal? last = null, decimal? bid = null, decimal? ask = null, DateTimeOffset? timestampUtc = null)
    {
        _map.TryGetValue(key, out var current);
        var next = new MarketDataSnapshot(
            last ?? current?.Last,
            bid ?? current?.Bid,
            ask ?? current?.Ask,
            timestampUtc ?? DateTimeOffset.UtcNow);
        _map[key] = next;
        return next;
    }

    public bool TryGet(InstrumentKey key, out MarketDataSnapshot? snapshot) => _map.TryGetValue(key, out snapshot);
}
