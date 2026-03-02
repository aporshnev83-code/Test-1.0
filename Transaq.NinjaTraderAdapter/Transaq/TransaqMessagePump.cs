using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Xml.Linq;

namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqMessagePump : IDisposable
{
    private readonly ConcurrentQueue<string> _queue;
    private readonly XmlRouter _router;
    private readonly Action<string> _log;
    private readonly CancellationTokenSource _cts = new();
    private Thread? _thread;

    public TransaqMessagePump(ConcurrentQueue<string> queue, XmlRouter router, Action<string>? log = null)
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
        _thread?.Join(TimeSpan.FromSeconds(2));
    }

    private void Run()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (!_queue.TryDequeue(out var xml))
            {
                Thread.Sleep(5);
                continue;
            }

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

    public void Dispose()
    {
        Stop();
        _cts.Dispose();
    }
}
