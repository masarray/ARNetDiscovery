# AGENTS.md — ARNet Discovery Production Engineering Contract

This file is the operating contract for every coding agent, assistant, and maintainer modifying ARNet Discovery. Treat the repository as production engineering software used on real substation and industrial networks, not as a disposable scanner prototype.

## Product identity and safety boundary

ARNet Discovery is a Windows desktop application for conservative LAN discovery, protocol evidence checks, troubleshooting, and engineering inventory support.

It must remain practical for FAT/SAT and substation-LAN work while avoiding claims or behavior that exceed the evidence actually collected.

Non-negotiable rules:

- The main device table is the primary operator workspace.
- Open ports, DNS names, ARP entries, OUIs, response timing, and protocol hints are **evidence**, not final device identity proof.
- Never label a device as a relay, PLC, gateway, IEC 61850 server, Modbus device, or other definitive role solely because a common port is open.
- Preserve uncertainty explicitly: `Unknown`, `Possible`, `Observed`, `Not verified`, or equivalent evidence-oriented wording is preferred to guessing.
- Do not introduce intrusive, destructive, exploit-oriented, configuration-changing, authentication-bypassing, or denial-of-service behavior.
- Network scope must remain explicit and bounded. Never silently expand a user-selected subnet/target list to external ranges.
- Treat production/engineering networks as bandwidth- and stability-sensitive.
- Never commit customer/site IP addresses, MAC addresses, captures, topology screenshots, device exports, credentials, logs, or other project-sensitive evidence unless the user explicitly confirms it is sanitized and authorized.

## Prime directive

Do not begin with a naive, disposable, synchronous, or knowingly temporary implementation merely to make the feature appear to work.

Before editing code:

1. locate the existing implementation and its owner;
2. reproduce or characterize the current behavior;
3. identify the violated invariant or root cause;
4. identify callers, state ownership, cancellation/lifecycle, and tests;
5. make the smallest coherent production-quality change;
6. add regression protection where practical;
7. validate build, failure paths, responsiveness, and packaging impact.

Do not rewrite a working subsystem because a parallel implementation is easier.

### Three-patch circuit breaker

If the same defect or subsystem has already received roughly three corrective patches without a stable result, stop stacking workarounds. Reassess architecture, ownership, assumptions, and instrumentation before another patch.

## Architecture boundaries

Current high-level ownership is:

```text
ARNetDiscovery.Wpf
  Presentation, commands, binding, selection, dialogs, operator-facing views.

ARNetDiscovery.Core
  Discovery orchestration, scanning policy, networking, classification,
  diagnostics, target handling, snapshots and evidence semantics.

ARNetDiscovery.Cli
  Headless/operator-development entry point and repeatable validation surface.
```

Rules:

- WPF must not become the owner of scan orchestration, probe concurrency, protocol classification, network timeouts, or evidence truth.
- Core must remain usable without WPF.
- UI callbacks must not be assumed to run on the WPF dispatcher thread.
- External access belongs behind clear networking/probe boundaries.
- Keep one authoritative snapshot/state model; do not create a second hidden device list with different semantics.
- Do not introduce global mutable scan state.
- Prefer explicit ownership and deterministic disposal for sockets, timers, cancellation sources, subscriptions, streams, and background work.

## Root-cause and failure architecture

Expected runtime outcomes are not exceptional events.

Examples include:

- host unreachable;
- ICMP blocked;
- TCP timeout/refusal;
- DNS lookup unavailable;
- ARP entry absent;
- interface disappears;
- user cancellation;
- partial protocol evidence;
- malformed external text/config/data;
- access denied or platform capability unavailable.

For expected outcomes:

- prefer `Try...`, typed result/status records, nullable evidence, or another explicit non-throwing contract;
- keep exception handling at meaningful I/O/platform boundaries;
- convert infrastructure exceptions into structured diagnostics or typed failure outcomes;
- never swallow an exception silently;
- do not use exceptions as routine branch control in host/port hot loops;
- one failed target must not terminate the whole scan when isolation is possible.

Do not force a custom Result abstraction into every pure helper. Use it where failure is part of the normal contract.

## Network scanning discipline

`LanDiscoveryEngine`, probe implementations, and future discovery protocols must remain bounded and cancellation-aware.

Required rules:

- Cap host count before scheduling work.
- Cap concurrent host and port probes.
- No unbounded `Task.Run`, task-per-packet, task-per-log-entry, or unbounded queues.
- Timeouts must be explicit and finite.
- Cancellation must propagate to waits and I/O where supported.
- A cancellation request must stop new work promptly and converge to a usable final/partial state.
- Never solve races with arbitrary `Task.Delay` sleeps.
- Do not retry indefinitely. Retries must be bounded and justified by protocol/network behavior.
- Avoid synchronized bursts that unnecessarily hammer switches, relays, RTUs, gateways, or embedded devices.
- Treat very large CIDRs or imported target lists as high-cost operations requiring explicit bounds/policy.
- Preserve partial results if later enrichment fails.
- ARP/DNS/vendor lookup enrichment must not invalidate earlier reachability evidence.

Concurrency values are policy inputs, not permission to saturate the machine or network. Any increase must be justified with measured scan throughput and resource/network impact.

## Evidence and classification invariants

Classification must remain explainable from collected evidence.

- Keep raw evidence available for why a tag/classification was assigned.
- Prefer deterministic rules to opaque guesses.
- A port number alone is a hint, not proof of protocol behavior.
- A hostname or OUI is supporting metadata, not identity authority.
- Do not invent device manufacturer/model information when absent.
- When evidence conflicts, surface uncertainty rather than choosing the most attractive label.
- Keep `DeviceClassifier` conservative.
- Changes to classification rules require representative positive and negative regression cases.

## Snapshot and state publication

A device snapshot is an operator-facing coherent state, not an append-only stream of every probe event.

Rules:

- Publish immutable/coherent snapshots where practical.
- Preserve stable identity keys for the same target.
- Do not mix fields from unrelated scan sessions or targets.
- Newer enrichment may extend a snapshot but must not silently erase stronger earlier evidence.
- If a staged result is published (`ping found` → `protocol probing` → `enriched`), stage meaning must remain explicit.
- Start of a new scan must not leak stale state into the new session unless intentionally represented as historical evidence.
- Completion/cancellation/fault state must be deterministic.

## UI responsiveness and backpressure

The 16.7 ms value is the entire frame budget for a 60 Hz UI, not a per-function allowance.

WPF rules:

- No synchronous network, filesystem, DNS, ARP, package, or heavy parsing work on the dispatcher thread.
- Worker callbacks such as `onDeviceDiscovered` must never directly assume dispatcher affinity.
- High-frequency discovery updates must be batched, coalesced, sampled, or otherwise backpressured before expensive UI mutation.
- Do not create one DataGrid row rebuild/render per low-level probe completion when a bounded timed flush can preserve meaning.
- Keep large tables virtualized/recycling-enabled.
- Avoid recreating the entire collection or visual tree for one device update.
- Expensive filtering/sorting/export work belongs off the UI thread when material.
- Cancellation, close, and navigation must remain responsive while scanning.

Do not hard-code an arbitrary universal throttle interval. Choose batching/coalescing based on measured update rate and operator-visible semantics.

## Diagnostics contract

Diagnostics exist to make failures understandable without becoming a performance problem.

- Diagnostic publication from hot/high-rate paths must be cheap.
- Use bounded buffering for asynchronous diagnostic delivery when volume can spike.
- Aggregate, deduplicate, or rate-limit repeated timeout/error storms.
- Formatting, file persistence, and expensive rendering must not occur inside probe hot loops.
- Diagnostic sink failure must never crash scan orchestration or block network progress.
- Include enough context to act: stage/component, target where appropriate, failure category, concise message, and exception detail at the boundary where captured.
- Normal negative evidence such as closed ports or expected timeout states should not flood severe-error channels.

## Memory and resource lifecycle

- Dispose sockets, `Ping`, semaphores, streams, timers, cancellation registrations, and other disposable resources deterministically.
- Avoid hot-loop allocation when measurement shows it matters, but do not add pooling speculatively.
- No unbounded retained histories of probe events or diagnostics.
- Large exports/logs must use streaming/chunked processing when appropriate.
- A stopped/cancelled scan must not leave background tasks, event handlers, or handles accumulating across repeated runs.

## Data, config, and parsing safety

Treat all external/configured input as untrusted:

- validate IP/CIDR/target-list bounds;
- validate integer ranges and concurrency/timeouts;
- validate file/schema/version/encoding before use;
- use `TryParse`/validated conversion for user or imported values;
- reject malformed entries without corrupting the entire session;
- guard indexes, lengths, nullability, overflow, and invalid enum/config states.

Use C# nullable-reference analysis and language-native patterns; do not mechanically copy JavaScript-style null operators as a universal rule.

## Performance policy

Performance is a feature, but optimization claims require evidence.

For performance-sensitive changes, identify the relevant baseline and measure when practical:

- time to first discovered device;
- total scan duration for representative target counts;
- active task/concurrency bounds;
- cancellation convergence time;
- UI update latency;
- dispatcher responsiveness;
- allocation/GC pressure;
- process CPU and memory;
- diagnostic queue depth/drop/aggregation behavior.

Prefer better algorithms, bounded work, and less rendering over speculative object pools or extra worker layers.

## Release and repository integrity

- Keep documentation user-oriented.
- Keep GitHub Pages deployed through GitHub Actions.
- Keep portable/release packages clean and runnable for users without Visual Studio.
- `VERSION`, package metadata, release notes, checksums, README/download references, and workflow inputs must not silently drift.
- Do not commit `bin`, `obj`, transient artifacts, private captures/logs, secrets, or user-specific machine output.
- New dependencies require a reason and review of license, maintenance, binary size, runtime cost, and security impact.

## Regression discipline

Every meaningful fix should protect the failure mode at the highest stable seam available.

Examples:

- parser/config bug → exact malformed/edge input regression case;
- classification bug → positive + confusing-negative cases;
- cancellation bug → repeated start/cancel lifecycle test;
- concurrency bug → bounded scheduling/state-coherence test;
- UI bug → operator interaction/state test where practical;
- packaging bug → package verification path.

Do not change shared behavior without identifying consumers and compatibility impact.

## Quality gate and definition of done

Existing CI is authoritative for normal repository validation. At minimum, changes must preserve:

```text
restore
Release build
tests when test projects exist
documentation/landing preflight
portable package preview
portable package verification
```

For runtime/network changes, CI alone is not proof of field interoperability. Report separately what was validated with synthetic/local tests versus an authorized real network/device.

A task is not complete merely because code compiles. Applicable completion evidence includes:

- root cause identified;
- build succeeds;
- targeted regression tests pass;
- cancellation/failure path checked;
- UI remains responsive;
- state/evidence semantics preserved;
- performance impact measured when relevant;
- package/release validation passes when affected;
- remaining field-validation limits stated explicitly.

## Required agent completion report

For substantial changes, report:

1. **Changed** — files/subsystems and user-visible effect.
2. **Root cause** — demonstrated cause, not the symptom.
3. **Architecture** — ownership/boundaries affected.
4. **Regression protection** — tests/invariants added or preserved.
5. **Performance impact** — measured result or why not applicable.
6. **Validation** — exact commands/checks actually run.
7. **Remaining limitations** — especially real-network/device validation not performed.

Never claim a check passed if it was not run.

## Priority order

When trade-offs are unavoidable, use this order:

1. correctness and evidence integrity;
2. network safety and failure containment;
3. regression compatibility;
4. responsiveness and bounded concurrency;
5. performance and memory;
6. maintainability;
7. implementation convenience;
8. visual polish.

UI direction remains calm, readable, premium Windows desktop design with restrained typography, predictable keyboard behavior, and collapsible inspector/diagnostics surfaces. Never trade discovery correctness or network stability for decorative effects.

## Final rule

Think like the engineer who must trust and maintain this tool during years of FAT/SAT and troubleshooting work.

Understand first. Preserve evidence. Bound the work. Respect the network. Keep the UI responsive. Contain failures. Measure hot paths. Prevent regressions. Ship only what has evidence.
