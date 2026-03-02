namespace Transaq.NinjaTraderAdapter.Transaq;

public sealed class TransaqConfig
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3900;
    public bool AutoPos { get; set; } = true;
    public bool DebugIncomingXml { get; set; }
    public bool AutoReconnect { get; set; }
}
