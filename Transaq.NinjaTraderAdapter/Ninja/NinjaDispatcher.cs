using System;

namespace Transaq.NinjaTraderAdapter.Ninja;

public sealed class NinjaDispatcher
{
    private readonly Action<Action> _dispatch;

    public NinjaDispatcher(Action<Action>? dispatch = null)
    {
        _dispatch = dispatch ?? (a => a());
    }

    public void Invoke(Action action) => _dispatch(action);
}
