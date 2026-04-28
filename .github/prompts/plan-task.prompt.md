---
name: plan-task
description: "Plan mode: Create an implementation plan from an agent-ready spec (read-only) and save it to docs/plans/."
argument-hint: "Spec path (docs/specs/...) OR Trello card URL/cardId/exact title"
agent: plan
tools:
  - "trello/*"
---

# /plan-task — Implementation Plan (Read-only)

## Language constraint (IMPORTANT)
- The generated plan file content MUST be written in ENGLISH ONLY.
- Do not include any non-English text in the plan file.

## Inputs
- Spec reference: ${input:spec_ref}
  - Prefer: a path to docs/specs/TRELLO-...md
  - Allowed: Trello card URL / cardId / exact title (only if the spec file does not exist)

## Mandatory context
- Architecture map: docs/CODEBASE_MAP.md

## Rules (strict)
- Planning only. Do NOT implement code.
- Keep changes minimal and reviewable.
- Respect monorepo boundaries:
  - Client/ (Next.js)
  - Server/ (.NET 8 Web API + SignalR)
- No new external services unless explicitly allowed in the spec.
- If the spec is missing or ambiguous: list what is missing and ask clarifying questions. Do NOT continue with assumptions.

## Workflow
1) Resolve the input:
   - If ${input:spec_ref} is a file path, read that spec file.
   - Otherwise, use Trello MCP tools to locate and open the card and extract:
     title, description, labels, due date, checklist items, and recent comments.
   - If a spec file exists for that card, prefer the spec file content.

2) Use docs/CODEBASE_MAP.md to anchor the plan:
   - Reference concrete file paths and modules from the map.
   - Avoid inventing folders or files.

3) Produce the plan and save it to the repo:
   - Ensure folder docs/plans/ exists; create it if missing.
   - Save plan as:
     docs/plans/PLAN-<id>-<slug>.md
     Where:
       <id> = Trello short ID if available; otherwise use a short unique token derived from the card ID.
       <slug> = kebab-case title (lowercase, hyphens).
   - The saved file MUST contain only English text.

## Plan structure (what to write into the plan file)
The plan MUST include the following sections:

1) Title
- "Implementation Plan — <Feature Name>"

2) Summary
- One paragraph describing what will change and why.

3) Assumptions & Risks
- Bullet list of assumptions.
- Bullet list of risks and mitigations.

4) Work Breakdown Structure (WBS)
- Group steps by:
  - Client
  - Server
  - Contracts
  - Infra/Config
- Each step MUST reference likely file paths from docs/CODEBASE_MAP.md.

5) API / SignalR Contract Changes (if any)
- REST endpoints and request/response shapes
- SignalR events, payload shapes, and event names
- DTO changes and how to mirror them (Client/src/types vs Server contracts)

6) Testing & Validation
- Manual test scenarios (end-to-end)
- Automated tests to add or adjust (unit/integration), if applicable
- Logging/observability checks if relevant

7) Rollback Strategy
- How to revert safely if something goes wrong.

8) Definition of Done
- A checklist that matches the Acceptance Criteria from the spec.

## Chat output format (after saving)
Return the following:

A) Saved plan path:
- docs/plans/PLAN-<id>-<slug>.md

B) The full plan Markdown (same content as saved file)