Transaq.NinjaTraderAdapter for NinjaTrader 8

Import package (recommended):
1) Start NinjaTrader 8.
2) Tools -> Import -> NinjaScript Add-On...
3) Select dist/Transaq.NinjaTraderAdapter.NT8.AddOn.zip
4) Restart NinjaTrader 8 if prompted.

Manual/portable install:
1) Unpack dist/Transaq.NinjaTraderAdapter.Release.zip
2) Copy Transaq.NinjaTraderAdapter.dll to:
   Documents\NinjaTrader 8\bin\Custom\
3) Copy config.json next to the adapter DLL or keep your own config.

TXmlConnector.dll placement options (any one that resolves via DLL search path):
- Documents\NinjaTrader 8\bin\Custom\
- Folder next to Transaq.NinjaTraderAdapter.dll
- NinjaTrader 8 installation folder (application directory)

Logging:
- Adapter outgoing/ingoing XML can be logged by providing logger delegate in host code.
- Enable additional debug behavior via config.json (DebugIncomingXml).
- NinjaTrader logs are typically available in Documents\NinjaTrader 8\log\ and trace files in Documents\NinjaTrader 8\trace\

Notes:
- Target runtime is .NET Framework 4.8 (NinjaTrader 8 compatibility).
- No NinjaTrader assemblies are required for this packaging skeleton.
