using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transaq.NinjaTraderAdapter.State;

namespace Transaq.NinjaTraderAdapter.Tests;

[TestClass]
public class OrderLifecycleTests
{
    [TestMethod]
    public void OrderNo_ZeroThenAssigned_PreservesSameOrderState()
    {
        var store = new OrderStateStore();

        var first = store.Upsert(77, 0, "active", 10, 0);
        var second = store.Upsert(77, 5550001, "matched", 10, 4);

        Assert.AreEqual(0, first.OrderNo);
        Assert.AreEqual(5550001, second.OrderNo);
        Assert.AreEqual(NtOrderState.PartFilled, second.NtState);
        Assert.IsTrue(store.TryGetByOrderNo(5550001, out var byOrderNo));
        Assert.AreEqual(77, byOrderNo!.TransactionId);
    }
}
