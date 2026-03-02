using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transaq.NinjaTraderAdapter.Interop;
using Transaq.NinjaTraderAdapter.State;
using Transaq.NinjaTraderAdapter.Transaq;

namespace Transaq.NinjaTraderAdapter.Tests;

[TestClass]
public class SessionCommandFormatTests
{
    [TestMethod]
    public void Session_UsesCommandIdFormat_InOutgoingXml()
    {
        var native = new FakeNative();
        var router = new XmlRouter(new InstrumentCatalog(), new MarketDataState(), new DomBook(), new OrderStateStore(), new PositionStateStore());
        using var session = new TransaqSession(native, router);

        var connectThread = new System.Threading.Thread(() => session.Connect("l", "p", "h", 1, true));
        connectThread.Start();
        System.Threading.Thread.Sleep(20);
        router.Route(XDocument.Parse("<server_status connected='true'/>"));
        connectThread.Join(TimeSpan.FromSeconds(1));
        session.SubscribeMarketData("TQBR", "SBER");
        session.SendNewOrder("TQBR", "SBER", "B", 1, price: 100m);
        session.CancelOrder(1);
        session.MoveOrder(1, 101m);
        session.RequestPositions();
        session.RequestLimits();
        session.Disconnect();

        CollectionAssert.Contains(native.CommandIds, "connect");
        CollectionAssert.Contains(native.CommandIds, "subscribe");
        CollectionAssert.Contains(native.CommandIds, "neworder");
        CollectionAssert.Contains(native.CommandIds, "cancelorder");
        CollectionAssert.Contains(native.CommandIds, "moveorder");
        CollectionAssert.Contains(native.CommandIds, "get_forts_positions");
        CollectionAssert.Contains(native.CommandIds, "get_client_limits");
        CollectionAssert.Contains(native.CommandIds, "disconnect");
    }

    private sealed class FakeNative : ITransaqNative
    {
        public List<string> CommandIds { get; } = new();

        public string Initialize(string logDir, int logLevel) => "<result success='true'/>";
        public string UnInitialize() => "<result success='true'/>";
        public string SetCallback(TransaqNative.CallbackEx callback) => "<result success='true'/>";
        public void FreeMemory(IntPtr ptr) { }

        public string SendCommand(string xmlCommand)
        {
            var doc = XDocument.Parse(xmlCommand);
            var id = doc.Root?.Attribute("id")?.Value ?? string.Empty;
            CommandIds.Add(id);
            return "<result success='true'/>";
        }
    }
}
