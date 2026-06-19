# Restricted Environment Deployment Guide

## Stop condition

Do not load classified or controlled data merely because the restricted build runs.
Deployment requires written authorization for the exact app commit, binary hash, Windows
baseline, Ollama version/hash, model version/hash, network, users, and classification
level. If any item changes, follow the organization's change-control and reassessment
process.

## Build and release

1. Clone the reviewed commit into an approved, isolated build environment.
2. Verify the source manifest and review all tracked changes.
3. Run `audit.ps1`; retain its complete output.
4. Build with the restricted profile (the default for `publish.ps1`).
5. Sign `CodeSummarizer.exe` and any installer with the organization-approved certificate.
6. Verify Authenticode status and SHA-256 hashes on the destination side.
7. Scan the exact signed artifacts with required malware, SAST, SCA, and organizational tools.
8. Promote artifacts through the approved software-distribution and change-control process.

Example assessment build:

```powershell
.\audit.ps1
```

Example signed build using a certificate already protected in the Windows certificate store:

```powershell
.\audit.ps1 -CertificateThumbprint "ORGANIZATION_CERTIFICATE_THUMBPRINT"
```

The signing key must not be stored in this repository or passed as a password on the
command line.

## Endpoint baseline

- Use an organization-approved Windows image and security baseline.
- Run as a standard user; deny local administrator rights unless separately justified.
- Enforce WDAC/AppLocker or equivalent allowlisting for the app, Ollama, model-management tools, and approved dependencies.
- Restrict inbound and outbound traffic. The app itself needs only loopback TCP 11434.
- Ensure Ollama binds only to loopback and cannot proxy or call external services.
- Disable or control clipboard history, cloud clipboard, remote assistance, screen capture, crash dumps, hibernation, pagefile handling, indexing, backups, and diagnostic upload according to the SSP.
- Apply required BitLocker, EDR, antivirus, audit, session-lock, removable-media, physical-security, and account controls.
- Prevent unapproved users or processes from replacing binaries or binding to TCP 11434.

## Ollama and model approval

Record and approve separately:

- Download source, version, publisher signature, SHA-256, license, vulnerabilities, and configuration for Ollama.
- Exact model name, manifest, digest for every layer/blob, source, license, training/data restrictions if known, quantization, size, and security evaluation.
- Storage paths, ACLs, logs, caches, temporary files, memory/GPU behavior, and update mechanism.
- Evidence that runtime and models were transferred through the approved cross-domain or media process.

Disable automatic or user-driven model downloads in the operational enclave. Install only
artifacts already approved and hashed.

## Acceptance tests

- Confirm the UI says `RESTRICTED BUILD`.
- Confirm Sensitive Mode and secret masking cannot be turned off.
- Confirm Copy buttons are disabled and raw model output is absent.
- Confirm screen capture of the app window is excluded on the approved Windows build.
- Confirm analysis fails if capture protection cannot be enabled.
- Confirm only `127.0.0.1:11434` is contacted using host/network instrumentation.
- Stop Ollama and verify the app fails closed with no fallback endpoint.
- Attempt oversized input and oversized mock responses.
- Exercise every secret pattern with synthetic—not operational—credentials.
- Verify no app-created snippet/result files or telemetry are produced.
- Verify the signed executable and installer hashes match the authorization record.

## Operations and incident response

- Maintain an approved-version register and software owner.
- Monitor security advisories for .NET, Windows, Ollama, and model/runtime tooling.
- Define vulnerability triage, patch timelines, rollback, revocation, and incident reporting.
- Preserve audit evidence without collecting source snippets or model output into logs.
- Remove the app, runtime, models, caches, and media using approved sanitization procedures at end of life.
