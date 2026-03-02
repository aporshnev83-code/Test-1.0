using System;
using System.Collections.Generic;
using System.Linq;

namespace Transaq.NinjaTraderAdapter.State;

public sealed class DomBook
{
    private readonly Dictionary<string, (decimal? buy, decimal? sell)> _rows = new();

    public IReadOnlyDictionary<decimal, (decimal? buy, decimal? sell)> Snapshot =>
        _rows.ToDictionary(kv => decimal.Parse(kv.Key.Split('|')[0]), kv => kv.Value);

    public void ApplyDelta(QuoteDelta delta)
    {
        var rowKey = $"{delta.Price}|{delta.Source ?? string.Empty}";

        if (delta.Buy == -1m && delta.Sell == -1m)
        {
            _rows.Remove(rowKey);
            return;
        }

        _rows.TryGetValue(rowKey, out var current);
        var nextBuy = delta.Buy == -1m ? (decimal?)null : delta.Buy ?? current.buy;
        var nextSell = delta.Sell == -1m ? (decimal?)null : delta.Sell ?? current.sell;

        if (nextBuy is null && nextSell is null)
        {
            _rows.Remove(rowKey);
            return;
        }

        _rows[rowKey] = (nextBuy, nextSell);
    }

    public void Clear() => _rows.Clear();
}
