# Kalei

*Kalei — convert any file, privately.*

Kalei is a small desktop app that converts files between formats on your own PC. You drop a file (or a folder), check the proposed target format and press Convert. It is open source (MIT).

> **Status: pre-alpha, documents only.** There is no code and no download yet. This repository holds the design: what Kalei will do, how it is built, and the tickets to build it. Nothing below is shipped; "planned" means exactly that.

## What it aims to be

- **Local** (planned). Conversion happens on your PC. No upload, no account, no telemetry. Kalei 1.0 is designed to make no network connection of its own; the only manual action is a button that opens the project's Releases page in your browser.
- **Private logs** (planned). Logs stay on your PC and contain no file names or paths.
- **Your originals stay untouched** (planned). A conversion never overwrites, moves or deletes the source file; a name clash gives `name (1).ext`.
- **Safe parsing** (in development, conditional). Files are meant to be parsed in a separate, sandboxed process with no network access. This is a release gate: it is claimed for an operating system **only if** a test proves the sandbox blocks the network there (a TCP connect and a DNS lookup from inside the process must both fail). Otherwise the claim is removed for that system.
- **One gesture** (planned). Drag files over the window and a ring of target formats opens at the cursor; release on one to convert.
- **Italian-friendly data** (planned): decimal commas, `;` separators and Windows-1252 text are understood.
- **Windows first** (planned). 1.0 targets Windows 10 22H2 and Windows 11 (x64). Linux and macOS come later, each after its own feasibility spike.

Kalei is inspired by Filee (https://github.com/KnifeLemon/Filee, MIT), which pioneered converting files by dropping them on a radial menu at the cursor.

## Where to read more

- [Product requirements](docs/PRD.md) and [glossary](GLOSSARY.md)
- [1.0 specification](docs/spec/1.0-windows.md), [settings](docs/spec/settings.md), [release pipeline](docs/spec/release-pipeline.md)
- [Architecture and security requirements](docs/architecture.md), [conversion matrix](docs/conversion-matrix.md)
- [Feasibility spike](docs/ticket-0.md) and the [tickets](docs/tickets/README.md)
- [Security policy](SECURITY.md)

## Stack

Avalonia 12 on .NET 10; conversion engines run in separate processes. Details and licences are in the [TRD](docs/TRD.md).

## Licence

[MIT](LICENSE). Third-party licences will be listed in the app and in the release files.

---

## In italiano

Kalei è una piccola app per convertire i file tra formati direttamente sul tuo PC. Trascini un file (o una cartella), controlli il formato proposto e premi Converti. È open source (MIT).

> **Stato: pre-alpha, solo documenti.** Non c'è ancora codice né un download. Questo repository contiene il progetto: cosa farà Kalei, come è costruito e i ticket per realizzarlo. Quello che segue non è ancora disponibile: "previsto" significa proprio questo.

- **Locale** (previsto): nessun upload, nessun account, nessuna telemetria. Kalei 1.0 è progettato per non fare connessioni di rete; l'unica azione manuale è un pulsante che apre nel browser la pagina delle Release del progetto.
- **Log privati** (previsto): restano sul tuo PC e non contengono nomi di file né percorsi.
- **Originali intatti** (previsto): una conversione non sovrascrive, non sposta e non cancella il file di origine.
- **Lettura sicura dei file** (in sviluppo, condizionata): i file dovrebbero essere letti in un processo separato e isolato, senza rete. È un requisito di rilascio: la promessa vale per un sistema operativo **solo se** un test dimostra che l'isolamento blocca la rete; altrimenti viene tolta per quel sistema.
- **Un solo gesto** (previsto): trascinando i file sulla finestra si apre un anello di formati; rilasciando su uno si converte.
- **Dati all'italiana** (previsto): virgole decimali, separatore `;` e testo Windows-1252.
- **Prima Windows** (previsto): la 1.0 è per Windows 10 22H2 e Windows 11 (x64); Linux e macOS vengono dopo.

Kalei si ispira a Filee (https://github.com/KnifeLemon/Filee, MIT), che ha aperto la strada alla conversione dei file trascinandoli su un menu radiale al cursore.

Licenza: [MIT](LICENSE).
