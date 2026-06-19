# Security Policy

## Authorization statement

Code Summarizer is a candidate application for assessment. It is not certified,
accredited, approved, or authorized for classified information by this repository.
Only the deploying organization's Authorizing Official or equivalent authority can
approve operational use. A restricted build is a hardened assessment artifact, not
an ATO, RMF package, classification determination, or security guarantee.

## Enforced application controls

The restricted build (`GovernmentMode=true`) enforces:

- Ollama endpoint fixed to `http://127.0.0.1:11434`; startup rejects any compiled endpoint outside IPv4 loopback and port 11434.
- Sensitive Mode cannot be disabled.
- Secret redaction cannot be disabled.
- Raw model output is not retained for display.
- App-provided clipboard export is disabled.
- Windows `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` is required; analysis fails closed if it cannot be applied.
- Input is limited to 100,000 characters.
- Ollama model-list responses are limited to 1 MiB and generation responses to 5 MiB.
- Error bodies displayed by the app are truncated.
- Model output is parsed as data and rendered as plain text, never executed.
- Release compilation is deterministic, warnings are errors, and recommended .NET analyzers run during builds.
- No application telemetry, updater, account system, cloud API, plugin loader, or persistence layer exists.

## Important limitations

- Loopback HTTP has no server authentication. A malicious local process could impersonate Ollama. Service control, application allowlisting, least privilege, and host integrity are mandatory inherited controls.
- Secret scanning is regex-based and can miss secrets or produce false positives.
- Display-affinity protection is defense in depth, not DRM; administrators, cameras, unsupported capture paths, or compromised hosts can bypass it.
- Strings are managed memory. The app cannot guarantee immediate zeroization, and Windows paging, hibernation, crash dumps, or memory acquisition may expose content.
- Ollama and model behavior, storage, logs, provenance, vulnerabilities, and licensing are outside this application's control boundary.
- The model can be wrong or manipulated by prompt content. Results require human review.
- `Validate` is LLM-guided and is not compiler-backed.
- The app has no identity, authorization, audit-log, key-management, or centralized policy service. Those controls must be inherited from the endpoint and deployment environment.

## Reporting vulnerabilities

Do not publish sensitive vulnerability details in a public issue. Report privately to
`sekacorn@gmail.com` with the affected commit, reproduction steps, impact, and any
suggested remediation. Response targets are best effort: acknowledgement within seven
days and an initial triage update within fourteen days.

## Release acceptance

An operational candidate must have all of the following:

1. Clean tracked source tree and reviewed commit.
2. Passing security regression tests and static analysis.
3. Restricted profile build.
4. Organization-approved Authenticode signature with trusted timestamp.
5. Verified SHA-256 artifact hash and retained audit evidence.
6. Separate approval and hashes for Ollama and every model artifact.
7. Malware, SAST, SCA, and endpoint-policy scans required by the organization.
8. Completed RMF/ATO or equivalent authorization decision.
