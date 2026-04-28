---
name: review-plan
description: “Validate plan: Check the plan for redundancies, risks, and gaps, and prepare it for implementation.”
argument-hint: “Paste the plan as text or select a file/message”
agent: ask
---

# /review-plan — Validate the implementation plan

## Inputs
- Plan: ${input:plan_text_or_ref}

## Rules
- Do NOT implement code.
- Be strict: detect overengineering, missing steps, missing validation, hidden risks.

## Checklist to apply
1) Is the plan minimal and scoped?
2) Does it avoid non-required cases?
3) Does it reference correct code areas (Client/ vs Server/; files from CODEBASE_MAP)?
4) Are API/SignalR contracts explicitly covered (events, DTO sync)?
5) Are test steps sufficient (manual + automated)?
6) Any security/auth pitfalls (JWT cookie + SignalR token flow)?
7) Are acceptance criteria fully covered?

## Output
- "Problems" list (bullets)
- "Suggested fixes" (bullets)
- "Revised plan" (if changes are needed; concise)