using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transaq.NinjaTraderAdapter.State;

namespace Transaq.NinjaTraderAdapter.Tests;

[TestClass]
public class DomBookTests
{
    [TestMethod]
    public void ApplyDelta_AddUpdateDeleteAndClear_Works()
    {
        var dom = new DomBook();

        dom.ApplyDelta(new QuoteDelta(100m, 10m, null, null));
        Assert.AreEqual(10m, dom.SnapshotByPriceAggregated[100m].buy);

        dom.ApplyDelta(new QuoteDelta(100m, 15m, null, null));
        Assert.AreEqual(15m, dom.SnapshotByPriceAggregated[100m].buy);

        dom.ApplyDelta(new QuoteDelta(100m, -1m, null, null));
        Assert.IsFalse(dom.SnapshotByPriceAggregated.ContainsKey(100m));

        dom.ApplyDelta(new QuoteDelta(101m, 1m, 2m, null));
        dom.Clear();
        Assert.AreEqual(0, dom.SnapshotByPriceAggregated.Count);
    }

    [TestMethod]
    public void SnapshotByPriceAggregated_SamePriceDifferentSources_DoesNotThrowAndAggregates()
    {
        var dom = new DomBook();

        dom.ApplyDelta(new QuoteDelta(100m, 2m, 1m, "MM1"));
        dom.ApplyDelta(new QuoteDelta(100m, 3m, 4m, "MM2"));

        Assert.AreEqual(2, dom.SnapshotByRowKey.Count);
        Assert.AreEqual(1, dom.SnapshotByPriceAggregated.Count);
        Assert.AreEqual(5m, dom.SnapshotByPriceAggregated[100m].buy);
        Assert.AreEqual(5m, dom.SnapshotByPriceAggregated[100m].sell);
    }
}
