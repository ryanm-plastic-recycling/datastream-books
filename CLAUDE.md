# CLAUDE.md — Claude-Specific Instructions

> **Default to `AGENTS.md` for all operating instructions.**
> This file contains ONLY Claude-specific extensions and communication preferences.
> If something is in `AGENTS.md`, do not duplicate it here.

## Primary Directive

Follow `AGENTS.md`. Everything below is additive.

## Communication Style

Per user preferences:

- Direct, concise, outcome-focused. No fluff.
- Prioritize actionable solutions and next steps over explanation.
- Provide complete, ready-to-use outputs — not partials.
- Default to full-file replacements over diffs.
- Assume high technical proficiency; don't over-explain basics.
- Use structured formatting (sections, bullets) where it aids clarity, not by default.
- Minimize back-and-forth — make reasonable assumptions and proceed.

## At End of Substantive Responses (When Relevant)

Include:

- **Risks** — what could go wrong with this approach
- **Better Option** — if applicable, what would be better
- **Next Steps** — clear ordered actions

Skip for trivial tasks. Use judgment.

## Operating Principles

Added 2026-05-22 per decision §68. These principles govern how work is sequenced and how concerns are surfaced during a session. They apply to all sessions on this project; the "When to Stop and Ask" section below is a subset that handles the high-stakes case.

- **Progress is the default.** When the operator says "move forward," execute. Surface concerns once, briefly, then proceed. Do not re-raise the same concern across multiple turns. A real concern (failed verification, ambiguous decision, unexpected file in a diff, scope expansion request) is worth surfacing; a procedural concern ("should I commit?", "ready to move to item 3?") is not — just commit, just move.
- **Concrete artifacts beat design documents for accounting-team feedback.** Pam reacts better to something she can point at and complain about than to something she has to imagine. When the choice is between shipping 50% of a feature that is visible and 10% of a feature that is fully decided but invisible, prefer the visible 50%. Bias sequencing toward visible artifacts.
- **Approve-in-batches for routine work; step-by-step for high-stakes.** Routine work = doc updates, design docs, ranking passes, runbook updates, decision-log entries. Complete the full pass, then surface a single end-of-session review. High-stakes work uses step-by-step approval with explicit per-step verification; the definition is:
  - Plugin code changes (C#)
  - SQL migrations (especially DENY grants, append-only constraints)
  - Production deploys to PRI-Books
  - Schema changes touching posted-ledger tables
  - Anything touching the hash chain
  - Anything that could affect JE-2026-001005 or future audit-trail anchors
  - Anything that would reverse or substantially modify a prior decision-log row
- **Scope discipline.** When something adjacent is noticed during a session (stale doc, typo, logical follow-up), NOTE IT for the end-of-session summary; do not silently expand scope. The operator decides whether to address now or add to the backlog.
- **Operator-driven hours.** When the operator wants to keep working, support that. Do not push for stopping unless something concrete makes stopping the right call — failed verification, ambiguous decision, scope expansion.

## When to Stop and Ask

This is the high-stakes subset of Operating Principles above. For most decisions, choose, document, and proceed. Stop and ask only when:

- A decision would materially change scope, timeline, or cost
- An accounting/finance rule is ambiguous and Pam should weigh in
- A new risk emerges that wasn't previously identified
- Multiple reasonable architectures exist and the choice has long-term implications
- Production deployment is involved
- The work falls in the high-stakes list under Operating Principles

## Strategic Conversations

For high-level strategy, roadmap, or stakeholder communications, the user may switch to Claude.ai chat (Project: "Datastream Books"). Decisions made there flow back as updates to `docs/decisions/datastream-books-decisions.md`. If asked a strategic question while in this repo, answer briefly, then suggest moving to the strategy Project for extended discussion.
