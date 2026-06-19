# Privacy and Data Handling

## Data flow

The app processes pasted source code, language and mode selections, local model names,
secret-scan findings, and Ollama responses. It sends prompts only to
`http://127.0.0.1:11434`. The application contains no external HTTP endpoint,
telemetry client, analytics SDK, user account, database, or file-save feature.

## Restricted build behavior

- Redaction and screen-capture exclusion are mandatory.
- Raw model output is discarded after structured parsing.
- Copy buttons are disabled so the app does not place analysis on the clipboard.
- Secret warnings use masked previews rather than detected values.
- Clearing the form removes UI references, but managed-memory zeroization is not guaranteed.

## Data that may exist outside the app

The claim "the app does not persist snippets" applies only to application code. Reviewers
must evaluate:

- Windows pagefile, hibernation file, crash dumps, diagnostic collection, clipboard history, screen capture, and endpoint monitoring.
- Ollama request handling, logs, model caches, temporary files, and crash behavior.
- Backup, EDR, antivirus, accessibility, and remote-administration tools.
- User actions before data enters or after it leaves the UI.

For classified processing, configure these inherited mechanisms according to the
approved system security plan. Full-disk encryption alone does not control data while
the endpoint is running.

## Data minimization

Paste the smallest necessary snippet. Do not rely on regex redaction as permission to
include credentials, operational data, classified markings, or unrelated records.
Classification and need-to-know rules still apply to the user, workstation, Ollama
runtime, model, and output.
