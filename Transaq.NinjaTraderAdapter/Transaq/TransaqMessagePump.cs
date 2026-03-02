using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Xml.Linq;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqMessagePump : IDisposable
{
    private readonly BlockingCollection<string> _queue;
    private readonly XmlRouter _router;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public TransaqMessagePump(BlockingCollection<string> queue, XmlRouter router, Action<string>? log = null)
    {
        _queue = queue;
        _router = router;
        _log = log ?? (_ => { });
    }

    public void Start()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "TransaqMessagePump" };
        _thread.Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _queue.CompleteAdding();
        _thread?.Join(TimeSpan.FromSeconds(2));
    }

    private void Run()
    {
        try
        {
            foreach (var xml in _queue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    _log($"RX << {xml}");
                    _router.Route(XDocument.Parse(xml));
                }
                catch (Exception ex)
                {
                    _log($"Pump error: {ex}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }
}
