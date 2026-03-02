using System.Collections.Generic;

namespace Transaq.NinjaTraderAdapter.State;

public sealed class InstrumentCatalog
{
    private readonly Dictionary<InstrumentKey, InstrumentInfo> _instruments = new();

    public void Upsert(InstrumentKey key, InstrumentInfo info) => _instruments[key] = info;

    public bool TryGet(InstrumentKey key, out InstrumentInfo? info) => _instruments.TryGetValue(key, out info);
}
