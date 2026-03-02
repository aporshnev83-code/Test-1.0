using System;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transaq.NinjaTraderAdapter.State;
using Transaq.NinjaTraderAdapter.Transaq;

namespace Transaq.NinjaTraderAdapter.Tests;

[TestClass]
public class XmlRouterParsingTests
{
    [TestMethod]
    public void Route_ParsesAttributesAndElements_ForMarketDataAndOrders()
    {
        var router = new XmlRouter(new InstrumentCatalog(), new MarketDataState(), new DomBook(), new OrderStateStore(), new PositionStateStore());

        OrderState? lastOrder = null;
        router.OnOrderUpdate += o => lastOrder = o;

        router.Route(XDocument.Parse("<quotations><quotation board='TQBR' seccode='SBER' bid='100.1'><ask>100.2</ask><last>100.15</last></quotation></quotations>"));
        router.Route(XDocument.Parse("<orders><order transactionid='1' orderno='0' status='active' quantity='10' filled='0'/><order><transactionid>1</transactionid><orderno>99</orderno><status>matched</status><quantity>10</quantity><filled>10</filled></order></orders>"));

        Assert.IsNotNull(lastOrder);
        Assert.AreEqual(99L, lastOrder!.OrderNo);
        Assert.AreEqual(NtOrderState.Filled, lastOrder.NtState);
    }

    [TestMethod]
    public void Route_ParsesDomQuoteWithAttributes()
    {
        var dom = new DomBook();
        var router = new XmlRouter(new InstrumentCatalog(), new MarketDataState(), dom, new OrderStateStore(), new PositionStateStore());

        router.Route(XDocument.Parse("<quotes><quote price='123.45' buy='7' source='S1'/></quotes>"));

        Assert.AreEqual(7m, dom.SnapshotByPriceAggregated[123.45m].buy);
    }
}
