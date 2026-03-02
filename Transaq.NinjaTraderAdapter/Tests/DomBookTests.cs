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
        Assert.AreEqual(10m, dom.Snapshot[100m].buy);

        dom.ApplyDelta(new QuoteDelta(100m, 15m, null, null));
        Assert.AreEqual(15m, dom.Snapshot[100m].buy);

        dom.ApplyDelta(new QuoteDelta(100m, -1m, null, null));
        Assert.IsFalse(dom.Snapshot.ContainsKey(100m));

        dom.ApplyDelta(new QuoteDelta(101m, 1m, 2m, null));
        dom.Clear();
        Assert.AreEqual(0, dom.Snapshot.Count);
    }
}
