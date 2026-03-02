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
5. Configure `Transaq.NinjaTraderAdapter/config.json`.

## Transaq command format

All outgoing commands use Transaq connector format with command id in attribute:

- `<command id="connect">...</command>`
- `<command id="subscribe">...</command>`
- `<command id="neworder">...</command>`
- `<command id="cancelorder">...</command>`
- `<command id="disconnect">...</command>`
- `<command id="moveorder">...</command>`
- `<command id="get_forts_positions"/>`
- `<command id="get_client_limits"/>`

## Parser behavior (attributes + elements)

`XmlRouter` reads important fields using attribute-or-element fallback for real Transaq payload variability:

- quotations: `board`, `seccode`, `bid`, `ask`, `last`
- alltrades/ticks: `board`, `seccode`, `price`, `quantity`, `time`
- quotes: `price`, `buy`, `sell`, `source`
- orders: `transactionid`, `orderno`, `status`, `quantity`, `filled`, `withdrawtime`
- securities: `board`, `seccode`, `lotsize`, `decimals`, `minstep`, `point_cost`

## DOM snapshot behavior

`DomBook` keeps native rows by `price|source` key and provides two safe snapshots:

- `SnapshotByRowKey`: raw rows by `price|source`
- `SnapshotByPriceAggregated`: aggregation by `price` across all sources

Aggregation rule for `SnapshotByPriceAggregated`: sum `buy` and `sell` sizes for all rows with same price.

## Thread safety guarantees

- All DLL calls are guarded by one global lock inside `TransaqNative`.
- Callback only does: pointer copy (UTF-8 zero-terminated) -> `FreeMemory` -> enqueue string.
- Callback always returns `true`; errors are logged.
- Single mutation thread: `TransaqMessagePump` routes XML and updates all state stores.
- Message pump uses blocking queue semantics (no busy polling/sleep loops).

## NinjaTrader 8 connection flow

1. Create stores and `XmlRouter`.
2. Create `TransaqSession` with `TransaqNative`.
3. Create `NinjaAdapter` and subscribe to events (`OnBidAskUpdate`, `OnLastTrade`, `OnDomUpdate`, `OnOrderUpdate`).
4. Call `Connect(login, password, host, port, autopos)`.
5. Call `SubscribeMarketData(board, seccode)` for instruments.
6. Send orders via `SendNewOrder`, cancel via `CancelOrder`, move via `MoveOrder`.
7. On shutdown call `Disconnect()`/`Dispose()`.
