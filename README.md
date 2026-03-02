# Transaq XML Connector adapter for NinjaTrader 8

Production-oriented adapter skeleton for integrating `TXmlConnector.dll` with NinjaTrader 8 on `.NET Framework 4.8`.

## Features

- Market data: Bid/Ask/Last (`quotations`, `alltrades`/`ticks`).
- Level2/DOM (`quotes`) with delta merge and delete rules.
- Trading operations:
  - Limit order
  - Market order (`bymarket`)
  - Cancel order
  - Move order (FORTS optional)
- Stores for orders, positions, limits, and instruments.
- Strict callback -> queue -> single-thread message pump pipeline.
- Testable core logic (unit tests without actual DLL).

## Project structure

- `Transaq.NinjaTraderAdapter/Interop/TransaqNative.cs` — P/Invoke + global DLL lock.
- `Transaq.NinjaTraderAdapter/Transaq/*` — session, command client, callback bridge, message pump, XML routing.
- `Transaq.NinjaTraderAdapter/State/*` — state stores and merge logic.
- `Transaq.NinjaTraderAdapter/Ninja/*` — adapter and dispatch into Ninja thread.
- `Transaq.NinjaTraderAdapter/Tests/*` — unit tests linked into test project.

## Setup

1. Copy `TXmlConnector.dll` near NinjaTrader 8 custom assembly folder (or into probe path used by the adapter host).
2. Open `Transaq.NinjaTraderAdapter.sln` in Visual Studio 2022+.
3. Restore NuGet packages.
4. Build solution in `Release|Any CPU`.
5. Configure `Transaq.NinjaTraderAdapter/config.json`:

```json
{
  "Login": "your-login",
  "Password": "your-password",
  "Host": "127.0.0.1",
  "Port": 3900,
  "AutoPos": true,
  "DebugIncomingXml": false,
  "AutoReconnect": false
}
```

## NinjaTrader 8 connection flow

1. Create stores and `XmlRouter`.
2. Create `TransaqSession` with `TransaqNative`.
3. Create `NinjaAdapter` and subscribe to events (`OnBidAskUpdate`, `OnLastTrade`, `OnDomUpdate`, `OnOrderUpdate`).
4. Call `Connect(login, password, host, port, autopos)`.
5. Call `SubscribeMarketData(board, seccode)` for instruments.
6. Send orders via `SendNewOrder`, cancel via `CancelOrder`, move via `MoveOrder`.
7. On shutdown call `Disconnect()`/`Dispose()`.

## Thread safety guarantees

- All DLL calls are guarded by one global lock inside `TransaqNative`.
- Callback only does: pointer copy -> `FreeMemory` -> enqueue string.
- No lock acquisition and no heavy parsing in callback.
- Single mutation thread: `TransaqMessagePump` routes XML and updates all state stores.

## Notes for real trading hardening

- Add persistent audit log sink.
- Add robust reconnection finite-state machine.
- Add explicit FORTS risk/limits validation before market orders.
- Add integration tests against a staging Transaq gateway.
