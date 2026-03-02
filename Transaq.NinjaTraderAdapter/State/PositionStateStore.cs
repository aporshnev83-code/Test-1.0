namespace Transaq.NinjaTraderAdapter.State;

public sealed class PositionStateStore
{
    private readonly PositionState _state = new();

    public PositionState Merge(
        decimal? openBalance = null,
        decimal? currentBalance = null,
        decimal? variationMargin = null,
        decimal? cash = null,
        decimal? usedMargin = null)
    {
        _state.OpenBalance = openBalance ?? _state.OpenBalance;
        _state.CurrentBalance = currentBalance ?? _state.CurrentBalance;
        _state.VariationMargin = variationMargin ?? _state.VariationMargin;
        _state.Cash = cash ?? _state.Cash;
        _state.UsedMargin = usedMargin ?? _state.UsedMargin;
        return _state;
    }

    public PositionState Snapshot() => new()
    {
        OpenBalance = _state.OpenBalance,
        CurrentBalance = _state.CurrentBalance,
        VariationMargin = _state.VariationMargin,
        Cash = _state.Cash,
        UsedMargin = _state.UsedMargin
    };
}
