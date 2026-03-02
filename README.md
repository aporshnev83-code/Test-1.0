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

## Build target

- **.NET Framework 4.8** (`net48`) for NinjaTrader 8 compatibility.

## Packaging output (Windows, no Visual Studio required for end-user)

Run one command on a build machine:

```powershell
./build-package.ps1 -Configuration Release -Platform "Any CPU"
```

Artifacts produced in `dist/`:

- `Transaq.NinjaTraderAdapter.NT8.AddOn.zip` — importable from NinjaTrader:
  - `Tools -> Import -> NinjaScript Add-On...`
- `Transaq.NinjaTraderAdapter.Release.zip` — portable archive for manual diagnostics/install.

## NinjaTrader package layout

`NinjaTraderPackage/` is prepared for NT8 import workflow and contains:

- `bin/Custom/Transaq.NinjaTraderAdapter.dll`
- `config/config.json` (optional runtime config copy)
- `readme.txt` (import/manual install instructions)

## TXmlConnector.dll placement

Place `TXmlConnector.dll` in any valid lookup location (recommended options):

1. `Documents\NinjaTrader 8\bin\Custom\`
2. Same folder as `Transaq.NinjaTraderAdapter.dll`
3. NinjaTrader 8 application directory

## Import/install instructions

### Recommended: NT8 Add-On import

1. Start NinjaTrader 8.
2. Open `Tools -> Import -> NinjaScript Add-On...`
3. Select `dist/Transaq.NinjaTraderAdapter.NT8.AddOn.zip`.
4. Restart NinjaTrader 8 if prompted.

### Manual portable install

1. Unpack `dist/Transaq.NinjaTraderAdapter.Release.zip`.
2. Copy `Transaq.NinjaTraderAdapter.dll` into `Documents\NinjaTrader 8\bin\Custom\`.
3. Copy `config.json` next to DLL (optional) and update credentials/host/port.

## Logging

- Outgoing/incoming XML logging is supported through injected logger in adapter host code.
- `config.json` contains debug-related switches (for example `DebugIncomingXml`).
- NinjaTrader logs are typically in:
  - `Documents\NinjaTrader 8\log\`
  - `Documents\NinjaTrader 8\trace\`

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
