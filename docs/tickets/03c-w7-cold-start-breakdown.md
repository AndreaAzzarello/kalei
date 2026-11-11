# 03c — W7 cold start: where the time goes

- **Status**: needs-owner-hardware
- **Size**: S
- **Blocked by**: –
- **Rules**: none
- **Stories**: none (constraint, spike or infrastructure)
- **Proofs**: W7 ([ticket-0](../ticket-0.md))
- **Spec**: [ADR 0001](../adr/0001-avalonia-net10-aot-ui-only.md)

## What to build
W7 fails on the reference VM with ReadyToRun + trimming: median 1122.7 ms over 10 cold starts (min 1020.8, max 1614.8; all ten above the 1 s threshold), see [ticket-0 Results](../ticket-0.md#w1-w2-w3-w4-w7-w8-and-w10--reference-vm-issue-47-run-2026-11-11). Before choosing between Native AOT ([38](38-native-aot-trial.md)), a revised threshold or plain optimisation, find where the time goes on the VM. This ticket measures and proposes; the owner decides.

## Acceptance criteria
1. The reference-machine description in ticket-0 says the VM's host disk is a SATA SSD (the PC's other disk is NVMe). The W7 measure is valid: no HDD limit applies. (Written with the first results on 2026-11-11.)
2. The cold start is split into phases with time markers in the window of the spike (branch `spike/03a-build`): process creation, .NET runtime loaded, Avalonia initialised, first layout, second frame. Median and maximum of each phase over 10 cold runs on the VM, same method as issue #47 (reboot before each run, 150 s settle time).
3. Optimisations that need no AOT are tried and measured the same way, one at a time: fewer assemblies loaded at start, fonts and resources loaded lazily, no synchronous work before the first frame, window shown at once and completed afterwards.
4. Only after these figures, the choice is written up for the owner: AOT mandatory ([38](38-native-aot-trial.md)), a revised threshold, or optimisations are enough. ADR 0001 stays as it is until the owner decides.
