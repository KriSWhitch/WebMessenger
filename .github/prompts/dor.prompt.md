---
name: dor
description: “DoR: Convert a Trello card into an agent-ready spec (and optionally normalize the card description).”
argument-hint: “Enter the Trello card URL / cardId / exact title”
agent: agent
tools:
  - "trello/*"
---

# /dor — Definition of Ready + Agent-ready Spec

## Inputs
- Trello card reference: ${input:trello_card_ref}
  - Accept: full Trello card URL, cardId, or exact card title.

## Context you MUST use
- Repo architecture map: docs/CODEBASE_MAP.md

## Goals
1) Retrieve the Trello card and related context using Trello MCP tools:
   - title, description, labels, due date
   - checklist items
   - last ~10 comments / activity items (if available)
   - card URL and cardId

2) Perform a Definition of Ready (DoR) check:
   - Identify missing/ambiguous info required to implement safely
   - Ask questions grouped by topic (UX, API/SignalR, Data, Security, Testing)
   - Keep questions concise and actionable

3) Produce an "Agent-ready Spec" in Markdown suitable for Copilot Plan mode later.
   - The spec MUST include:
     - Objective
     - Scope / Non-scope
     - Constraints (no new external services unless approved; keep changes small; respect auth model)
     - Relevant code areas & files (MUST reference real paths from docs/CODEBASE_MAP.md)
     - API + SignalR expectations (endpoints/events)
     - Acceptance Criteria (testable checklist)
     - Validation checklist (manual + automated)
     - Links (Trello URL)

4) Save the spec into the repo:
   - Create folder docs/specs/ if missing
   - File name format: docs/specs/TRELLO-<shortId>-<slug>.md
   - Put the final spec content there.

5) OPTIONAL: Normalize the Trello card description
   - If the card description is not already in the DoR template format, propose a normalized description in Markdown.
   - Preserve original card description under a section: "## Original notes (archived)".
   - Ask for confirmation BEFORE calling any Trello tool that modifies the card (e.g., update description/checklists).

## Output format in chat
Return in this exact order:

### A) Card summary
- Card: <title> (<URL>)
- Labels, due, checklist count, comment count
- Key context highlights (3-7 bullets)

### B) DoR gaps / questions
(If none, say: "No blockers. Spec ready.")

### C) Agent-ready Spec (Markdown)
(Full content that will be saved into docs/specs/...)

### D) Proposed Trello description update (Markdown) [optional]
(Only if needed; do not apply until confirmed.)