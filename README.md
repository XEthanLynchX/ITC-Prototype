# ITC-Prototype
A Vue 3 client that reproduce the SignalR group-routing architecture with no Azure
infrastructure — no Event Hubs, Redis, SQL, or auth.

**The idea:** clients declare which WIUs (Wayside Interface Units) they care about. The
server turns each distinct WIU set into a SignalR group. A background generator produces
fake changes into a coalescing buffer, and every ten (configurable) seconds a publisher drains that
buffer and sends each changed WIU only to the groups subscribed to it.

```text
Browser clients
      │  Subscribe(["WIU-101", "WIU-102"])
      ▼
WiuHub ──────────► SubscriptionRegistry
                     ▲  group ↔ WIU lookup
WiuGenerator         │
      │ every 500ms  │
      ▼              │
PendingChanges ──► BatchPublisher ──► IHubContext.Group(name).ReceiveWiuBatch(...)
   (coalesces)        every 10s
```

## Quick start

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project src\WiuBroadcaster\WiuBroadcaster.csproj --no-launch-profile
```

Open <http://localhost:5179>. The build compiles the Vue client automatically — no
separate npm step. `Ctrl+C` to stop. (Plain `dotnet` works in any terminal opened after
the .NET 9 SDK was installed.)

Requires the .NET 9 SDK and Node 22+.

## Server

`src/WiuBroadcaster/` — one process, everything in memory.

| File | Role |
|---|---|
| `Program.cs` | DI wiring, hub mapping, `/debug` and `/test` endpoints |
| `WiuOptions.cs` | Config bound from the `Wiu` section of `appsettings.json` |
| `Models/WiuUpdate.cs` | The payload: WIU id, aspect, timestamp, sequence number |
| `Hubs/WiuHub.cs` | `Subscribe` / `Unsubscribe`; moves connections between groups |
| `Hubs/IWiuClient.cs` | Strongly-typed client methods — these names are the wire contract |
| `Services/SubscriptionRegistry.cs` | Group ↔ WIU ↔ connection maps, including the reverse index |
| `Services/WiuGenerator.cs` | Background service; mutates one random WIU every 500ms |
| `Services/PendingChanges.cs` | Coalescing buffer keyed by WIU id, plus last-known state |
| `Services/BatchPublisher.cs` | Background service; the periodic fan-out |
| `Services/FlushLog.cs` | Ring buffer of the last 25 flushes, for `/debug` |

**The registry** is four `ConcurrentDictionary`s: `group→WIUs`, `WIU→groups` (the reverse
index the publisher depends on), `group→connections`, and `connection→group`. Group names
are readable rather than hashed — `subscription:WIU-101|WIU-103`, built by sorting and
deduplicating the requested ids — because debuggability matters more than name length
here. When the last connection leaves a group, its index entries are torn down.

**The coalescing buffer** is keyed by WIU id, so ten changes to WIU-101 inside one flush
window ship as one update carrying the latest state. It also keeps a `_lastKnown` map
permanently, so a client that just subscribed gets an immediate snapshot instead of
waiting out the next flush window.

**The publisher** is the piece the project exists to demonstrate: it drains the buffer,
asks the registry which groups contain each changed WIU, builds one filtered batch per
group, and sends it via `IHubContext` — publishing to clients from *outside* the hub,
from a hosted background service.

## Client

`src/WiuBroadcaster/client/` — Vue 3 + Vite, built into `wwwroot/`.

`src/useWiuConnection.js` holds the SignalR connection and all reactive state; the
components below it are presentational. `App.vue` composes them and handles the
"leave group when tab is hidden" toggle via the Page Visibility API.

| Component | Shows |
|---|---|
| `ConnectionStatus.vue` | Connection state, current group, batches received |
| `SubscriptionPanel.vue` | WIU checkboxes, apply/clear |
| `WiuStateTable.vue` | Live state of the subscribed WIUs |
| `RegistryPanel.vue` | The whole server registry, polled from `/debug` |
| `EventLog.vue` | Per-tab event history |
| `PanelBox.vue` | Shared titled-box wrapper |

`wwwroot/` is generated output: gitignored, wiped on every client build. Edit
`client/src/` instead.

### UI development

For hot reload, run Vite alongside the .NET app and use <http://localhost:5173>, which
proxies `/hubs`, `/debug`, and `/test` to port 5179:

```powershell
cd src\WiuBroadcaster\client
npm run dev
```

## Configuration

The `Wiu` section of `appsettings.json`. Every value can be overridden with a
double-underscore environment variable, e.g. `WIU__GENERATORENABLED=false`.

| Key | Default | Meaning |
|---|---|---|
| `WiuIds` | `WIU-101`…`WIU-105` | WIUs the generator knows about and the UI offers |
| `Aspects` | Clear, Approach, Restricting, Stop | Signal indications to pick from |
| `GeneratorEnabled` | `true` | Set false to suppress random changes |
| `GeneratorIntervalMs` | `500` | How often one random WIU is mutated |
| `FlushSeconds` | `10` | Publisher interval |
| `GeneratorSeed` | `1234` | RNG seed, for reproducible runs |

## Endpoints

| Route | Purpose |
|---|---|
| `GET /debug` | Registry, reverse index, pending count, recent flushes |
| `GET /debug/config` | Effective `WiuOptions` |
| `POST /test/change` | Inject exact changes: `{"changes":[{"wiuId":"WIU-101","aspect":"Clear"}]}` |
| `POST /test/flush` | Force a publish cycle now; returns the flush record |

The `/test` routes exist so a driver can be deterministic: turn the generator off, inject
precisely the changes a scenario needs, and flush on demand instead of sleeping through a
whole flush window.



## Docs

- [Docs/ItcVue_Project_Overview.md](Docs/ItcVue_Project_Overview.md) — what ItcVue is
- [Docs/SignalR_WIU_Practice_App_Guide.md](Docs/SignalR_WIU_Practice_App_Guide.md) — the
  staged design this implements, and the milestone that proves you understand it
