using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transaq.NinjaTraderAdapter.State;

namespace Transaq.NinjaTraderAdapter.Tests;

[TestClass]
public class QuoteMergeTests
{
    [TestMethod]
    public void Merge_OnlyBid_DoesNotOverrideAskOrLast()
    {
        var state = new MarketDataState();
        var key = new InstrumentKey("TQBR", "SBER");

        state.Merge(key, last: 250m, bid: 249m, ask: 251m);
        var merged = state.Merge(key, bid: 248.5m);

        Assert.AreEqual(250m, merged.Last);
        Assert.AreEqual(248.5m, merged.Bid);
        Assert.AreEqual(251m, merged.Ask);
    }
}
