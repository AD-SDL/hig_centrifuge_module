# AGENTS.md/CLAUDE.md

This file provides guidance to AI Agents. Note that CLAUDE.md and AGENTS.md are symlinked together.

## Overview

MADSci node module for the **BioNex HiG4 centrifuge**. Exposes a MADSci REST node that translates MADSci actions into calls on the vendor's .NET integration library (`BioNex.HiGIntegration`), loaded into Python via `pythonnet` (`clr`).

**Windows-only.** The node depends on the .NET Framework 4.8.1 vendor DLLs installed under `C:\Program Files (x86)\BioNex\HiG\`, so it runs only on the Windows PC physically connected to the centrifuge.

## Installation and Running

Prerequisites (see `README.md`):
1. Install the BioNex HiG device driver and complete the vendor software setup.
2. In PowerShell: `Install-Package Grapevine -Version 5.0.0-rc.10`.
3. Build the C# interface project so the vendor DLLs are copied into `bin/Debug/` (open `hig_centrifuge_module.sln` / `src/hig_centrifuge_interface/HiGCentrifugeInterface.csproj` in Visual Studio, or build with MSBuild). `pythonnet` loads `HiGIntegration.dll` from that output folder at runtime.

Python requirements: `madsci` (node framework) and `pythonnet` (the `clr` bridge).

```bash
# Run the node (Windows)
python src/hig_centrifuge_rest_node.py
```

The node runs in **simulation by default** (`simulate=True`); set it false to drive real hardware.

## Code Architecture

Two layers plus the vendor library:

### 1. `src/hig_centrifuge_rest_node.py` — MADSci REST Node
`HiGCentrifugeNode(RestNode)` implements the MADSci node interface:
- `startup_handler`: instantiates the vendor `HiG()`, sets `Blocking = True`, and calls `Initialize(device_name, device_id, simulate)`.
- `shutdown_handler`: `Close()` on the interface.
- `state_handler`: maps vendor flags `Idle` / `InErrorState` / `Busy` → `node_state["hig4_status_code"]` of `READY` / `ERRORED` / `BUSY` / `UNKNOWN`.
- Actions (decorated with `@action`), all thin pass-throughs to the vendor `HiG` object:
  - `spin(gs, accel_percent, decel_percent, time_seconds)`
  - `open_shield(bucket_index)`
  - `home()`
  - `close_shield()`
  - `abort_spin()`
- Config via `HiGCentrifugeNodeConfig(RestNodeConfig)` — adds `device_id` (0), `device_name` ("HiG4 Centrifuge"), `simulate` (True).

### 2. `src/hig_centrifuge_interface/` — C# build shim
A .NET Framework 4.8.1 class library whose real purpose is to produce a `bin/Debug/` folder with the BioNex vendor DLLs copied in (via the references in `HiGCentrifugeInterface.csproj`), so `pythonnet` can load `HiGIntegration.dll`. The C# source itself (`HiGCentrifugeInterface.cs`) is only a `Callback_Wrapper` stub and is **not** used by the Python node.

### Vendor library — `BioNex.HiGIntegration`
All real centrifuge control (`HiG`, `HiGInterface`) lives in the closed-source vendor DLL, not in this repo. This module is a thin wrapper over it.

## Configuration

Uses MADSci's Pydantic Settings system with walk-up file discovery (searching from CWD up to the `.madsci/` sentinel directory): CLI args → environment variables → `node.settings.yaml` → `settings.yaml`. The node's stable ID is persisted in `.madsci/registry.json` (registered as `hi_g_centrifuge_node`).

Example `node.settings.yaml`:

```yaml
node_name: hig_centrifuge
node_url: http://0.0.0.0:2005
device_id: 0
device_name: HiG4 Centrifuge
simulate: true
```

| Parameter | Default | Description |
|---|---|---|
| `device_id` | 0 | Vendor device index passed to `Initialize` |
| `device_name` | "HiG4 Centrifuge" | Vendor device name passed to `Initialize` |
| `simulate` | True | Run against the vendor simulator instead of hardware |
| `node_url` | (MADSci default) | REST API base URL; examples use port 2005 |

## Migration status

The repo is mid-migration from the deprecated **WEI** framework to **MADSci** (branch `update/madsci-v0.8`, `module_version = "0.8"`). The node itself is MADSci-based, but:
- `examples/` and `test/` YAMLs are still WEI-format (`interface: wei_rest_node`, `flowdef:`) and `test/run_test.py` / `test/run_wei_server.sh` still use `wei`. These are to be **converted** to MADSci (do not install WEI).
- The example/test workflows reference a `home_shield` action that does **not** exist on the node — reconcile against the node's real action list above.
