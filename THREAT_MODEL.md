# Threat Model

## Scope and assumptions

Protected assets include source snippets, embedded credentials, model prompts and
responses, internal architecture, and release integrity. The model assumes a managed
Windows endpoint, standard-user execution, a separately approved Ollama service, and
approved model weights. A compromised administrator or kernel is outside the app's
ability to defend against.

## Threats and treatment

| Threat | Application treatment | Residual risk / inherited control |
|---|---|---|
| External exfiltration | Network destination is compiled and validated as `127.0.0.1:11434`; no other network client exists. | Firewall and application-control policy must detect future changes and constrain Ollama itself. |
| Local Ollama impersonation | Exact loopback endpoint and port are checked. | HTTP has no server identity; host service control and process allowlisting are mandatory. |
| Accidental secret inclusion | Expanded secret patterns, forced redaction, masked previews. | Detection is incomplete; data minimization and user training remain necessary. |
| Prompt injection in pasted code | Prompt explicitly treats snippet contents as data; output is structured and inert. | Models can still follow malicious instructions or produce deceptive output. |
| Resource exhaustion | Input, model-list, generation-response, error-body, and request-time limits. | Model inference can consume significant CPU, RAM, and GPU; enforce host quotas and approved models. |
| Screen/clipboard disclosure | Restricted build requires display-affinity protection, disables app copy, and suppresses raw output. | Clipboard is still an input path; OS features and privileged capture require endpoint controls. |
| Memory/disk remnants | App has no save/log/database feature and releases UI references on clear. | Managed strings, pagefile, hibernation, dumps, Ollama, EDR, and backups can retain data. |
| Malicious model/output | JSON parsing, bounded response, plain-text display, no tool execution. | Incorrect or adversarial analysis remains possible; human review is mandatory. |
| Supply-chain compromise | Source manifests, artifact hashes, SPDX inventory, deterministic compilation, no runtime NuGet packages. | .NET, Windows, Ollama, model weights, build host, installer, and signing chain require separate review. |
| Unauthorized use | Restricted mode cannot be disabled in the restricted binary. | Identity, need-to-know, session control, and physical security are inherited from the environment. |
| Repudiation / missing audit history | Git commit and build/test evidence are generated without logging snippet content. | Operational audit logging must be provided by approved deployment tooling without capturing sensitive code. |

## Abuse cases explicitly tested

- Overlapping credential patterns cannot leave token fragments during redaction.
- Secret previews do not reveal detected values.
- Prompt-injection guidance is included for every analysis mode.
- The endpoint remains fixed to IPv4 loopback.
- Structured responses are parsed and confidence values bounded.

## Reassessment triggers

Repeat security review after changes to endpoint behavior, dependencies, secret patterns,
prompt construction, model API shape, persistence, clipboard/capture behavior, installer,
target framework, or deployment environment.
