using System;

namespace Transaq.NinjaTraderAdapter.State;

public readonly struct InstrumentKey : IEquatable<InstrumentKey>
{
    public InstrumentKey(string board, string seccode)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));
        Seccode = seccode ?? throw new ArgumentNullException(nameof(seccode));
    }

    public string Board { get; }
    public string Seccode { get; }
    public override string ToString() => $"{Board}:{Seccode}";
    public bool Equals(InstrumentKey other) => Board == other.Board && Seccode == other.Seccode;
    public override bool Equals(object? obj) => obj is InstrumentKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Board, Seccode);
}

public record InstrumentInfo(decimal LotSize, int Decimals, decimal TickSize, decimal PointCost);
public record MarketDataSnapshot(decimal? Last, decimal? Bid, decimal? Ask, DateTimeOffset TimestampUtc);
public record QuoteDelta(decimal Price, decimal? Buy, decimal? Sell, string? Source);

public enum NtOrderState
{
    Unknown,
    Working,
    PartFilled,
    Filled,
    Cancelled,
    Rejected,
    Cancelling
}

public sealed class OrderState
{
    public long TransactionId { get; set; }
    public long OrderNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public NtOrderState NtState { get; set; }
    public decimal Quantity { get; set; }
    public decimal Filled { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PositionState
{
    public decimal? OpenBalance { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? VariationMargin { get; set; }
    public decimal? Cash { get; set; }
    public decimal? UsedMargin { get; set; }
}
