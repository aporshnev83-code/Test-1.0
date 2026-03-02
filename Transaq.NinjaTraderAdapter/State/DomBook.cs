using System;
using System.Collections.Generic;
using System.Globalization;

namespace Transaq.NinjaTraderAdapter.State;

public sealed class DomBook
{
    private readonly Dictionary<string, (decimal? buy, decimal? sell)> _rows = new();

    public IReadOnlyDictionary<string, (decimal? buy, decimal? sell)> SnapshotByRowKey =>
        new Dictionary<string, (decimal? buy, decimal? sell)>(_rows);

    // Aggregation rule: sum all buy/sell amounts for the same price across different sources.
    public IReadOnlyDictionary<decimal, (decimal? buy, decimal? sell)> SnapshotByPriceAggregated
    {
        get
        {
            var aggregated = new Dictionary<decimal, (decimal? buy, decimal? sell)>();
            foreach (var row in _rows)
            {
                var price = ParsePrice(row.Key);
                var current = aggregated.TryGetValue(price, out var existing) ? existing : (null, null);

                aggregated[price] = (
                    SumNullable(current.buy, row.Value.buy),
                    SumNullable(current.sell, row.Value.sell));
            }

            return aggregated;
        }
    }

    public void ApplyDelta(QuoteDelta delta)
    {
        var rowKey = $"{delta.Price.ToString(CultureInfo.InvariantCulture)}|{delta.Source ?? string.Empty}";

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

    private static decimal ParsePrice(string rowKey)
    {
        var separator = rowKey.IndexOf('|');
        var pricePart = separator >= 0 ? rowKey.Substring(0, separator) : rowKey;
        return decimal.TryParse(pricePart, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0m;
    }

    private static decimal? SumNullable(decimal? left, decimal? right)
    {
        if (left is null && right is null) return null;
        return (left ?? 0m) + (right ?? 0m);
    }
}
