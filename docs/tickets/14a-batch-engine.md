# 14a — Batch engine: folder walk, skip reasons, scheduling

- **Status**: ready-for-agent
- **Size**: M
- **Blocked by**: [07b](07b-windows-sandbox-launcher.md), [08](08-broker-and-output-writing.md), [15](15-name-clash-and-output-folder.md)
- **Rules**: S2, S4
- **Stories**: 22, 26, 27 ([user stories](../spec/1.0-windows.md#user-stories))
- **Spec**: [1.0 Windows spec](../spec/1.0-windows.md)

## What to build
The Core takes many files or folders, decides what to convert, runs the Conversions in parallel and reports each result, without any window.

## Acceptance criteria
1. Dropped folders are walked down to the folder depth limit; deeper folders are skipped with the reason "too deep" (E_DEPTH); symbolic links and junctions are not followed; hidden and system files are skipped.
2. Every skipped file has one reason: unsupported format, over a limit, system or hidden file, symbolic link or junction, too deep, already in this format, unreadable, clash declined.
3. With a chosen Output folder a dropped folder is recreated inside it under its own name with its subfolders; with "next to the source" each Output file is written beside its Source file.
4. Parallel Workers follow the formula of the Limits table; one failing file never stops the others.
5. The offered target set follows the Format rule of the Donut library (ticket [11](11-donut-library.md)); above the Batch limit the whole Batch is refused with E_COUNT.
6. Cancel all and Retry failed work at engine level; a Batch of 10,000 generated small files is scheduled with a working set under 500 MB on the main reference machine (initial threshold).
