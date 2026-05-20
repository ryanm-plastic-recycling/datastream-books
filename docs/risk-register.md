# Datastream Books — Risk Register

> Living document. Risks added as they are identified; updated as
> mitigations land; closed when the risk no longer applies. Cross-reference
> with the Risks table in [`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md) —
> that table is the historical record; this file is the live working register.
>
> Severity scale: **Low / Medium / High / Critical**. A risk's severity is
> the product of likelihood × impact, judged at the time of entry.
> Re-judged when the situation changes.

## Open Risks

### Phase 7 (UI Track) risks

| ID | Risk | Severity | Owner | Mitigation | Status |
|---|---|---|---|---|---|
| R-7-01 | Strict sequential creates 7+ month UI invisibility — Pam loses confidence or interest during the backend-only period | Medium | IT | Weekly written status to Pam through Phase 6B-11 (no UI to show, but progress is reported); Pam's CR-based ownership model means she does not need design review during construction; surface to Executive Sponsor at month 1 if Pam expresses anxiety. Decision §57, §58. | Open — monitor monthly |
| R-7-02 | Heavy CR volume in first weeks of Phase 7B — Pam files many CRs as she encounters real screens for the first time, overwhelming IT triage capacity | Medium | IT + Pam | Phase 7E (CR burn-down) is built into the plan as 2-3 weeks of no-new-scope CR work; triage weekly during 7A-7D, daily during 7E; severity-classify CRs (Blocker / Major / Minor / Cosmetic) and batch implementation. Decision §57. | Open — monitor at Phase 7B kickoff |
| R-7-03 | Universal report drill-down adds architectural complexity — live queries against `ledger.GeneralLedgerEntries` may be slow under multi-entity consolidated loads | Medium | IT | Phase 7C kickoff has explicit decision point: live queries vs. cached aggregates. Bench under realistic multi-entity load before committing. Closed-period reports already snapshot via `ReportSnapshots` per immutability §G — drill-down through snapshot to ledger preserves provenance. Decision §55. | Open — defer to Phase 7C kickoff |
| R-7-04 | Hybrid JE entry is the most complex screen and a long pole — Excel-like grid + form mode on same page may exceed time estimate | Medium | IT | Schedule JE entry as the **first** deliverable in Phase 7B so it has the longest runway; if Phase 7B slips, slip the Phase 7B end-date — do not strip the hybrid mode (decision §50 is firm). | Open — monitor at Phase 7B |
| R-7-05 | Cutover date slips because front-end starts too late — backend phase slips compound into Phase 7 slip | High | IT | Backend phases must finish on schedule for Phase 7 to start on schedule. Surface backend slippage to Executive Sponsor **as soon as it is forecast**, not after it lands. Decision §58 made the trade-off accepting this risk explicitly. | Open — monitor monthly |

### Pre-existing risks (carried over from decision log)

| ID | Risk | Severity | Owner | Mitigation | Status |
|---|---|---|---|---|---|
| R-A-01 | Auditor rejects custom system | Medium | IT + Pam | Document immutability architecture; proactive disclosure at next audit. | Open |
| R-A-02 | Build timeline slips | Medium | IT | Hard MVP scope, no scope creep, fixed cutover date. | Open |
| R-A-03 | Dataverse capacity costs higher than expected | Medium | IT | Monitor capacity during build. | Open |
| R-A-04 | Key-person dependency (IT) | Medium | IT + Executive Sponsor | Documentation standards (this repo); AI-assisted handoff. | Open |
| R-A-05 | Macola data quality during migration | High | IT + Pam | Extract early, profile data. | Open |
| R-A-06 | Period close logic bugs | High | IT | Heavy test coverage, parallel run. | Open |
| R-A-07 | AI-generated code introduces subtle bugs | Medium | IT | Mandatory human review of financial logic. | Open |
| R-A-08 | Hash-chain verification missed corruption | Low | IT | Nightly verification + alerting. | Open |
| R-A-09 | SoD bypassed by privileged user | Medium | IT | Enforce in plugin code; quarterly role audit. | Open |
| R-A-10 | Cutover failure | High | IT + Pam | Parallel run mandatory; penny-perfect reconciliation. | Open |
| R-A-11 | Document AI accuracy insufficient for headcount story | Medium | IT | Phase 2 only; pilot before committing. | Open |
| R-A-12 | Leahy ACH service unavailable post-Macola | High | IT | NACHA file generation built in v1. | Open |
| R-A-13 | Change management not actually used | Medium | Pam | Built into workflow such that changes can't happen without it. | Open |
| R-A-14 | Pam refuses ownership role | High | Executive Sponsor | President introduces role, reinforces in escalations. Surface at month 1 if Pam resists. Not a project issue if persistent — becomes HR. | Open |
| R-A-15 | Leadership rescues Pam from ownership during escalations | High | Executive Sponsor | President + COO rollout meeting sets escalation handling protocol upfront. | Open |

## Closed Risks

| ID | Risk | Closed Date | Resolution |
|---|---|---|---|
| R-A-16 | Managed env constraints surprise developer | 2026-05-19 | PRI-Books-Dev sandbox established as proper source. |

## See Also

- [`decisions/datastream-books-decisions.md`](decisions/datastream-books-decisions.md) — full decision log + historical risk table
- [`decisions/phase-7-ui-design.md`](decisions/phase-7-ui-design.md) — Phase 7 risks expanded in context
- [`roadmap.md`](roadmap.md) — phase sequencing
- [`architecture/immutability-design.md`](architecture/immutability-design.md) — controls that mitigate R-A-06, R-A-08, R-A-09
