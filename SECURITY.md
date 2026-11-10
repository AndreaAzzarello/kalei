# Security policy

Kalei is pre-alpha and has no released version yet, so no version is supported today. This policy applies from the first release.

## Reporting a vulnerability

Please do **not** open a public issue for a security problem. Use GitHub's private vulnerability reporting: open the **Security** tab of this repository and choose **Report a vulnerability**. (The repository owner switches this on when the repository is created; if the button is missing, open a public issue that says only "security contact needed", without details, and the owner will reply.)

Include what you did, what you expected, what happened, and the Kalei version and Windows build. Do not attach files you are not allowed to share. You will get an answer on a best-effort basis: this is a one-person, free project.

## What is in scope

Kalei converts files that may be malformed or hostile, so the scope is the requirements S1–S12 in [docs/architecture.md](docs/architecture.md#security-requirements-normative-the-only-place-they-are-written): the Worker sandbox (including the release gate that a Worker has no network access), size and bomb limits, safe output names and no overwriting of source files, safe parsers and writers, logs without paths, and browser drops.

## What Kalei promises about the network

Kalei 1.0 is designed to make no network connection and has no automatic update or update check. It only has a button that opens the project's Releases page in your browser. The promise that converters run without network access is made for an operating system only after a test proves it there; see the README.

## Checking a download

Each release will publish a `SHA256SUMS` file next to the installer and the portable ZIP. In PowerShell, from the folder that holds the download:

```powershell
Get-FileHash .\<downloaded file> -Algorithm SHA256
```

Compare the hash with the line for that file in `SHA256SUMS`. If they differ, do not run the file and report it as above. Until the code-signing question is settled (see the [release pipeline](docs/spec/release-pipeline.md)), 1.0 may ship unsigned and Windows SmartScreen will warn; the sums are then the way to check the file.
