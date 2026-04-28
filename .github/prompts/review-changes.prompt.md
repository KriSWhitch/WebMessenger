---

name: review-changes

description: "AI Review of all repo changes with priority triage (Fix now / Fix if cheap / Defer) + PR-ready notes."

argument-hint: "Optional: paste PR URL / branch name / Trello card URL / or say 'use current git changes'"

agent: agent

---



\# /reviewchanges — Change Review Command (Priority Triage)



\## Language (STRICT)

\- Output MUST be in ENGLISH ONLY.

\- Do not include non-English text.



\## Goal

Perform an automated code review of \*\*all current changes\*\* (working tree or PR diff) and produce:

\- a concise change summary

\- a prioritized triage list: \*\*Fix now / Fix if cheap / Defer\*\*

\- concrete improvement suggestions and risk notes

\- validation/test gaps and recommended checks

\- a PR-ready comment block



This review should surface:

\- potential regressions

\- security/auth pitfalls

\- API/SignalR contract mismatches

\- missing tests or validation gaps

\- edge cases and non-obvious issues

(Do not over-index on style nits.)



\## Mandatory project context

\- Use docs/CODEBASE_MAP.md as the source of truth for architecture and key files.

\- Respect monorepo boundaries: Client/ (Next.js) vs Server/ (.NET 8 + SignalR).

\- Pay special attention to contracts:

&nbsp; - Server contracts vs Client/src/types duplication

&nbsp; - SignalR event names and payload shapes



\## How to gather the diff (choose best available path)

1\) If you have access to a built-in "diff/changes" context in the IDE, use it.

2\) If terminal tools are available, run:

&nbsp;  - git status

&nbsp;  - git diff --stat

&nbsp;  - git diff (and --cached if needed)

3\) If you cannot access the diff automatically, ASK the user to provide:

&nbsp;  - `git diff` output (or PR diff)

&nbsp;  - list of changed files

&nbsp;  - and any relevant runtime notes



\## Review dimensions (apply all)

A) Correctness \& regressions

\- Logic errors, broken flows, lifecycle issues, race conditions, null/undefined paths

\- Client rendering/state updates, hook lifecycles, subscription cleanup

\- Server validation and error handling



B) Security \& privacy

\- Authentication/authorization boundaries

\- Token/cookie handling; avoid logging secrets

\- Input validation, file upload safety (if present)

\- CORS/auth interactions if touched



C) API + Contract Integrity

\- REST endpoint changes: routes, request/response DTOs, status codes

\- DTO sync: Server contracts vs Client types

\- Backward compatibility concerns

\- Versioning or manual mirroring hazards



D) Realtime (SignalR)

\- Event names/constants alignment

\- Subscriptions: conn.on/off symmetry

\- Reconnect behavior: re-join groups if necessary

\- Performance: over-broadcasting, unnecessary event spam

\- Authorization \& access\_token flow safety



E) Maintainability \& clarity

\- Complexity and readability

\- Overengineering detection (suggest simplification)

\- Naming consistency, dead code, duplication



F) Performance

\- N+1 queries (server), excessive rerenders (client), large payloads

\- Inefficient loops, memory leaks, unbounded lists



G) Testing \& validation

\- Missing unit/integration tests

\- Manual test scenarios that should be documented

\- Suggested minimal test additions (cheap/high ROI)



\## Output format (MUST follow)



\### 1) Executive Summary (<= 8 bullets)

\- What changed and why (high level)

\- Main risk areas



\### 2) Risk Heatmap

\- High / Medium / Low risk items with 1-line rationale each



\### 3) Findings (Prioritized Triage)



\#### Fix now (must address before merge)

For each finding:

\- \*\*Title\*\*

\- \*\*Why it matters\*\*

\- \*\*Where\*\*: file paths + approx locations

\- \*\*Suggested fix\*\* (concrete)

\- \*\*Validation\*\* (how to test)



\#### Fix if cheap (do if quick)

Same structure, shorter.



\#### Defer (log as backlog / follow-up)

Same structure, focus on rationale and next steps.



\### 4) Contract \& Realtime Checklist (Pass/Fail)

\- REST contract consistency: Pass/Fail + notes

\- DTO mirror consistency (Server vs Client types): Pass/Fail + notes

\- SignalR event name/payload consistency: Pass/Fail + notes

\- Subscription cleanup (on/off): Pass/Fail + notes

\- Auth boundaries preserved: Pass/Fail + notes



\### 5) Test \& Validation Recommendations

\- Minimal manual scenarios (numbered)

\- Minimal automated tests to consider (bullets)

\- Observability/logging suggestions (if relevant)



\### 6) PR Comment (copy-paste ready)

Provide a concise comment that can be posted on the PR:

\- Summary

\- Top Fix now items

\- Quick wins

\- Suggested tests

