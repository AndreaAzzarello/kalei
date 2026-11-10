# 07c — Network canary: prove at start-up that the sandbox blocks the network

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07a](07a-ipc-protocol-and-worker-host.md), [07b](07b-windows-sandbox-launcher.md), [10a](10a-main-window-file-card.md), [26](26-logging-policy.md)
- **Rules**: S5, S10
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W20 ([ticket-0](../ticket-0.md))
- **Manual check**: AC 6 (what the user sees) and AC 9 (the Hyper-V VM states) are run by the owner.
- **Spec**: [Network canary](../architecture.md#network-canary-s5)

## What to build
At start-up Kalei proves, on this PC, that a sandboxed Worker cannot reach the network, and shows the "no network" promise only when it has. The block is enforced by Windows Filtering Platform filters that depend on Windows services, and Microsoft does not document what happens when they are off, so the promise is checked, not assumed (see the Network canary section of architecture.md).

## Acceptance criteria
1. A canary mode of the built-in Worker runs in the same sandbox profile as every Worker, with no file handles, and tries three probes in parallel: a TCP connect to a listener the Core opens on 127.0.0.1 (random port), a TCP connect to 1.1.1.1:443, and a lookup of example.com. The destinations are fixed in code, not configurable. The two external probes are enabled only after the owner confirms them (they are an outbound attempt by design; in a healthy sandbox the OS drops it before it leaves the PC); until then only the local probe runs and the verdict says so in the log.
2. The verdict is `isolated` only if the Worker started, replied, and all probes were refused or timed out; any connected probe gives `leaked`; a Worker that cannot start or reply gives `unproven`; before the first result the state is `pending`. A test with a fake launcher covers each verdict, including a probe that "connects" and a Worker that crashes.
3. Timing: probe timeouts are 200 ms (local) and 1,000 ms (external), run in parallel; the verdict arrives within 1.5 s in the worst case and within 10 ms from the cache; the canary never runs on the UI thread and a Conversion never waits for it. Measured on the Hyper-V VM and on the Windows 11 PC.
4. It runs in the background at every app start (and at the first run after install), and before a Batch starts if the cached verdict is older than 10 minutes or the service state changed. The cache (verdict, time, OS build, service states) is kept in memory and in the data folder, is never reused after an OS build change or a service state change, and there is no timer.
5. The Core reads the state of the Base Filtering Engine and of the Windows Firewall service through the Platform adapter, without administrator rights, in at most 5 ms; the verdict is decided by the probes, not by the service state alone.
6. Only `isolated` shows the promise ("Converters have no network access on this PC, checked at start-up"). Any other verdict hides the promise everywhere in the app and shows once per session the notice `NETWORK_ISOLATION_UNPROVEN`, in plain language, English and Italian, saying what to check (the Windows Firewall service); Conversions are not blocked. The README and PRD wording says the promise is checked at each start-up.
7. Log event `NET_CANARY` with: verdict, the probes that connected, the states of the two services (running, stopped, unknown), duration in ms and OS build; no address, host name, path or user name of the user. The S10 log scan passes with the event present.
8. The notice and the log event code are added to the message catalogue and the ipc-spec (messages `Canary` and `CanaryResult`); the catalogue test finds no code without a message.
9. W20: on the Hyper-V VM, in each state of W19 (normal; firewall profiles off; Windows Firewall service stopped; Base Filtering Engine stopped; plus an outbound allow rule added for the AppContainer profile) the verdict is `isolated` only where every probe failed, and `leaked` wherever a probe connected: zero false `isolated`. Results are written in ticket-0 Results; a single false `isolated` keeps the ticket open.
