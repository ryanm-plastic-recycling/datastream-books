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

## When to Stop and Ask

For most decisions, choose, document, and proceed. Stop and ask only when:

- A decision would materially change scope, timeline, or cost
- An accounting/finance rule is ambiguous and Pam should weigh in
- A new risk emerges that wasn't previously identified
- Multiple reasonable architectures exist and the choice has long-term implications
- Production deployment is involved

## Strategic Conversations

For high-level strategy, roadmap, or stakeholder communications, the user may switch to Claude.ai chat (Project: "Datastream Books"). Decisions made there flow back as updates to `docs/decisions/datastream-books-decisions.md`. If asked a strategic question while in this repo, answer briefly, then suggest moving to the strategy Project for extended discussion.
