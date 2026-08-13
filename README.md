# ITC-Prototype

A learning spike for the ItcVue subscription/publish design: a self-contained
ASP.NET Core app with a Vue 3 client that reproduces the SignalR group-routing
architecture with no Azure infrastructure.

- [Docs/ItcVue_Project_Overview.md](Docs/ItcVue_Project_Overview.md) — what ItcVue is
- [Docs/SignalR_WIU_Practice_App_Guide.md](Docs/SignalR_WIU_Practice_App_Guide.md) — the design this implements
- [src/WiuBroadcaster/](src/WiuBroadcaster/) — hub, subscription registry, fake WIU generator, five-second batch publisher
- [src/WiuBroadcaster/client/](src/WiuBroadcaster/client/) — Vue 3 + Vite SPA (built into `wwwroot`)
- [.claude/skills/run-wiu-broadcaster/](.claude/skills/run-wiu-broadcaster/) — how to build, run, and drive it

## Quick start

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project src\WiuBroadcaster\WiuBroadcaster.csproj --no-launch-profile
```

Then open <http://localhost:5179>. The build compiles the Vue client for you — no
separate npm step. (Plain `dotnet` works in any terminal opened after the SDK install.)

## Working on the UI

Run Vite alongside the .NET app for hot reload, and use <http://localhost:5173>:

```powershell
cd src\WiuBroadcaster\client
npm run dev
```

## Driving it programmatically

Four simulated clients, deterministic changes, assertions on which group received
what — see
[.claude/skills/run-wiu-broadcaster/SKILL.md](.claude/skills/run-wiu-broadcaster/SKILL.md).

```powershell
node .claude\skills\run-wiu-broadcaster\driver.mjs all
```
