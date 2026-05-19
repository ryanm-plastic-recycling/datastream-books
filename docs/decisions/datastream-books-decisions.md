# Datastream Books — Decision Log & Project Notes

> Living document. Update continuously as decisions are made and assumptions evolve.
> Last updated: May 19, 2026

---

## Project Identity

- **Internal name:** Datastream Books
- **Purpose:** Replace Macola accounting system with an internally-built finance application
- **Platform:** Microsoft Dataverse (model-driven app) + Azure SQL (hybrid)
- **Parallel system:** Datastream ERP (operations, already in production on Dataverse)
- **Strategic context:** Advances The Lighthouse IT modernization strategy
- **Authentication:** Microsoft Entra ID
- **Estimated user count:** 15 (5 accountants + 10 occasional contributors)
- **Tenant account:** ryanm@plastic-recycling.net
- **GitHub repo:** `datastream-books`
- **Finance System Owner:** Pam (controller-level accountant)
- **Executive Sponsor:** President

---

## Tenant Environment Inventory

| Environment | Type | URL | Purpose | Status |
|---|---|---|---|---|
| Plastic Recycling (default) | Default | orgd0b31c1b.crm.dynamics.com | Tenant default | Active |
| PRI-Datastream | Production | datastream.crm.dynamics.com | Datastream ERP | Active |
| PRI-Dev | Sandbox | pridev.crm.dynamics.com | ERP dev/test | Active |
| PRI-Sales | Production | prisales.crm.dynamics.com | Sales/CRM | Active |
| **PRI-Books** | **Production (Managed)** | **books.crm.dynamics.com** | **Datastream Books PROD** | **Deployed** |
| **PRI-Books-Dev** | **Sandbox (Unmanaged)** | **booksdev.crm.dynamics.com** | **Datastream Books DEV** | **Deployed & Authenticated** |

### Auth Profiles Established

```
pri-books          - PRI-Books production
pri-books-dev      - PRI-Books-Dev sandbox (primary dev target)
pri-datastream     - PRI-Datastream (ERP prod, for integration testing)
pri-dev            - PRI-Dev (ERP sandbox)
```

All four auth profiles confirmed active via `pac auth list`.

---

## Why We Are Building This

- Macola is being deprecated by the vendor in calendar year 2026
- Vendor ERP/finance replacements quoted at $300K–$1.7M, outside budget
- Business Central viable at ~$100/user/month + AP automation add-ons
- Internal Dataverse build leverages existing platform investment
- Datastream ERP build has proven the platform and team capability
- **Primary value driver:** AI-driven document discrepancy detection enables headcount reallocation
- **Secondary strategic benefit:** Cutover advances The Lighthouse (local DC + file server decom, full Entra migration)

---

## Decisions Made

| # | Date | Decision | Rationale |
|---|---|---|---|
| 1 | 2026-05-19 | Project name: Datastream Books | Matches accounting vernacular |
| 2 | 2026-05-19 | Skip local prototype | Team already knows Dataverse |
| 3 | 2026-05-19 | Separate Dataverse environment | Audit isolation, security boundary |
| 4 | 2026-05-19 | Hybrid data: Dataverse + Azure SQL | Dataverse for master/UX; Azure SQL for immutable ledger |
| 5 | 2026-05-19 | Microsoft Entra ID authentication | Consistency with Datastream ERP |
| 6 | 2026-05-19 | Payroll out of scope (Paylocity stays) | Permanent decision |
| 7 | 2026-05-19 | BC POC waived per executive direction | Build approach committed |
| 8 | 2026-05-19 | CI/CD via GitHub + Power Platform Build Tools | Native ALM |
| 9 | 2026-05-19 | Strategy in Claude.ai Project; execution in Claude Code | Repo is source of truth |
| 10 | 2026-05-19 | Per-app licensing (managed manually by IT) | Cost flexibility |
| 11 | 2026-05-19 | SharePoint for document storage | Native, audit-friendly |
| 12 | 2026-05-19 | Document AI long-term: Claude API | Phase 2 |
| 13 | 2026-05-19 | Track1099 for 1099 generation and W-9 collection | Compliant, low-friction |
| 14 | 2026-05-19 | Microsoft Graph API for customer/vendor email | Native, audit trail |
| 15 | 2026-05-19 | Native reporting v1; Power BI Phase 2 | Reduce dependency |
| 16 | 2026-05-19 | NACHA file generation in v1 for ACH | Removes Leahy dependency |
| 17 | 2026-05-19 | Credit limit management deferred to Phase 2 | Not used today |
| 18 | 2026-05-19 | Server-side plugins preferred over Power Automate | Atomicity, audit defensibility |
| 19 | 2026-05-19 | Change Management built into the app | Audit-friendly history |
| 20 | 2026-05-19 | Environment names: PRI-Books / PRI-Books-Dev | Matches PRI-* pattern |
| 21 | 2026-05-19 | PRI-Books-Dev sandbox as unmanaged dev source | Required by managed prod env |
| 22 | 2026-05-19 | Vendors/customers added as needed | Natural data hygiene |
| 23 | 2026-05-19 | Pre-populate standard COA; finance modifies | "React to draft" pattern |
| 24 | 2026-05-19 | Foreign currency NOT in v1; schema future-proofed | USD only |
| 25 | 2026-05-19 | Multi-entity REQUIRED in v1 | Multiple legal entities |
| 26 | 2026-05-19 | Cutover at fiscal period boundary; user-driven green light | Risk minimization |
| 27 | 2026-05-19 | TalentLMS + Scribe for training | Existing pattern |
| 28 | 2026-05-19 | PRI-Books environment deployed and authenticated | Build infrastructure ready |
| 29 | 2026-05-19 | PRI-Books-Dev sandbox deployed and authenticated | Dev environment ready |
| 30 | 2026-05-19 | **Pam designated as Finance System OWNER (not consultant)** | **Mirrors Datastream ERP departmental owner pattern** |
| 31 | 2026-05-19 | **ChangeRequest must support multi-image attachments** | **Concrete evidence beats verbal description** |
| 32 | 2026-05-19 | **President is executive sponsor; rollout meeting with President and COO** | **Cascades ownership framing from top** |

---

## Development Environment Strategy

### ALM Flow

```
PRI-Books-Dev (sandbox, unmanaged)        ← Active development with Claude Code
        ↓
   Solution exported as managed
        ↓
   GitHub Actions deploys managed solution
        ↓
   PRI-Books (production, managed)        ← Production destination
```

### When PRI-Books-Test Sandbox Is Added

Before finance team UAT begins. Three-environment ALM:

```
PRI-Books-Dev → PRI-Books-Test → PRI-Books
```

### Managed Environment Implications

PRI-Books is managed:
- Solution Checker mandatory before import
- Deployment from solutions only (no direct unmanaged customization)
- Pipelines feature enforced
- Stricter maker controls

PRI-Books-Dev as unmanaged source is the correct ALM pattern.

---

## Finance System Owner — Pam

### Critical Framing: Pam OWNS This System

Mirroring the Datastream ERP departmental ownership pattern:

| App / Area | Owner |
|---|---|
| Datastream ERP — Operations | [Ops lead] |
| Datastream ERP — Transportation | [Transportation lead] |
| Datastream ERP — [Other] | [Other leads] |
| **Datastream Books — Finance** | **Pam** |

**Pam is the business owner of the finance system. IT builds the system for her. She is responsible for whether it works.**

This is not a consultative role. This is ownership.

### Why This Framing Is Critical

The historical pattern of "complain, then escalate" exists because the person feels powerless and external to the system. **Ownership removes powerlessness.**

Without ownership: "IT didn't listen to us" (victim framing, escalation incentive)
With ownership: "I haven't worked through that workflow yet" (owner framing, problem-solving incentive)

When Pam escalates to the President (Executive Sponsor), the President must reinforce ownership: "Pam, you own this. What do you need from IT to make it work?" — not "IT, why isn't this working for Pam?"

This conversation must happen with the President during the rollout meeting.

### Ownership Artifacts (Pam's Name On Them)

| Deliverable | Pam's Role |
|---|---|
| Chart of Accounts | Approves and signs off |
| Approval threshold definition | Sets the dollar amounts |
| Standard report layouts | Reviews and approves each |
| Period close runbook | Authors (with IT support) |
| User Acceptance Testing | Owns test forms and signoff |
| User documentation | Reviews and approves |
| Training rollout | Leads |
| Change Request triage (finance-side) | Triages and prioritizes |
| Vendor master cleanup approach | Reviews and signs off |

### Ownership Language

- "Pam's accounting system" — not "the new accounting system"
- "What does Pam need it to do?" — not "what do we think they need?"
- In meetings: "Pam, when do you want to deploy this?" — not "we plan to deploy this when..."

### Visible Ownership

- Pam's name in repo README as Finance System Owner
- Pam's name on the executive memo as project co-lead
- Pam's email in the user-facing app footer as the "Report an Issue" contact
- Pam mentioned by name in leadership status updates as the owner

### Engagement Structure

**Weekly 30-minute check-in:**
- Same time every week
- Agenda sent day before
- Outcomes captured in repo or CR system
- No "while I'm thinking about it" requests — redirect to CR system

**All complaints/feedback route through Change Request system:**
- Not email, not conversation, not escalation
- Multi-image attachment support required (see ChangeRequest design)
- IT commits to triage within 48 hours

**Pam triages and prioritizes** finance-side CRs herself. She closes them out when satisfied. This is part of ownership.

### Test Acceptance Form Structure

For each feature ready for finance review:

1. Feature description (provided by IT)
2. What I tested (specific scenarios)
3. What worked
4. What didn't work (specific, reproducible)
5. Severity (Blocker / Major / Minor / Cosmetic)
6. Recommendation (Accept / Accept with conditions / Reject)
7. Date and signature

### Rollout Meeting: President and COO

Before kickoff, IT meets with the President (Executive Sponsor) and COO to:

1. Confirm Datastream Books project direction (per president memo)
2. **Confirm Pam as Finance System Owner**, mirroring ERP ownership pattern
3. Confirm the President will introduce the ownership role to Pam directly
4. Confirm leadership response to escalations: reinforce ownership, not rescue
5. Review executive questionnaire and assignment of answers
6. Confirm budget envelope and timeline expectations

The President introducing ownership to Pam is critical — not IT. Endorsement from above lands differently than a tap from IT.

### Failure Mode to Watch

If Pam cannot or will not accept ownership — insisting on critic role rather than owner — this is a serious early signal. Surface to the President (Executive Sponsor) at month 1, not month 6.

### Escalation Protocol

If, after 2 months of disciplined engagement, the escalation pattern persists:

1. Document specific instances (dates, what was bypassed, where it should have gone)
2. Raise with the President (Executive Sponsor) as a process risk, not a personality complaint
3. Project should not bear the cost of an unworkable dynamic
4. Becomes an HR/management issue, not a project issue

---

## Immutability Architecture

### A. Append-Only Transaction Ledger
- Dedicated Azure SQL table: `GeneralLedgerEntries`
- `DENY UPDATE, DELETE` at SQL role level for all accounts including app service principal
- Corrections via reversing entries only

### B. Hash-Chained Records
- Every ledger row: `RowHash` = SHA-256 of (row contents + `PreviousRowHash`)
- Periodic snapshots in `LedgerIntegrityCheckpoints`
- Nightly verification job with alerting

### C. Server-Side Posting Enforcement
- All journal posting through Dataverse plugin
- Validates: debits = credits, period open, account active, post role, SoD
- Dual write to Dataverse + Azure SQL in single transaction

### D. Period Locks at Data Layer
- `FiscalPeriod` table with Status (Open / Closed / Locked)
- Plugin rejects postings to Closed or Locked periods
- Period close writes hash to `PeriodCloseAttestation`
- Reopen requires elevated role + audit event
- Locked periods cannot be reopened

### E. Segregation of Duties
- Roles: `JE Entry`, `JE Approve`, `JE Post`, `JE Void`, `Period Close`, `Period Reopen`, `Vendor Setup`, `Vendor Bank Change`, `Wire Initiate`, `Wire Approve`
- Plugin enforces `CreatedBy != ApprovedBy` for sensitive operations
- System-enforced, not honor system
- SoD matrix version-controlled in repo

### F. Comprehensive Audit Trail
- Dataverse audit log on all financial tables, long-term retention
- Redundant `AuditEvents` table in Azure SQL — append-only

### G. Time-Bound, Signed Reports
- Closed-period reports hashed and stored at close
- `ReportSnapshots` preserves figures as-of close

### H. Dev/Prod Separation
- Developers have zero direct production database access
- All production changes via approved deployment pipeline
- Deployments approved, logged, version-tagged

### I. Backup and Recovery
- Dataverse: native Microsoft backups + solution exports in git
- Azure SQL: PITR + long-term retention (7+ years)
- Recovery procedures documented and tested annually

### J. Change Management Built In
- `ChangeRequest` table within the app
- Required: business reason, desired outcome, acceptance criteria, risk assessment, rollback plan
- **Multi-image attachment support** via `ChangeRequestAttachment` related table
- **Timeline / Notes** enabled for ad-hoc files
- Plugin enforces SoD: `ApprovedBy != RequestedBy != AssignedTo`
- Permanent record

### K. AI's Role
- Generates plugin code for posting enforcement and hash chaining
- Generates test cases proving each control works
- Generates audit reports and reconciliation queries
- Reviews changes for SoD violations

---

## ChangeRequest Design — Specific Requirements

### Core Table

`ChangeRequest` with standard workflow fields (per prior spec).

### Attachment Strategy

**Hybrid approach:**

1. **`ChangeRequestAttachment` related table** (primary mechanism)
   - `Image` column (Dataverse native type)
   - `Description` column (text)
   - `CapturedDate` (auto-populated)
   - Unlimited attachments per CR
   - Use case: screenshots showing what's wrong, "before" / "after" comparisons, error messages

2. **Timeline / Notes** (catch-all)
   - Enabled on ChangeRequest table
   - For ad-hoc files: auditor letters, vendor emails forwarded, PDFs, etc.

### User Experience Requirements

- Drag-drop image attachment in form
- Paste-from-clipboard support (screenshot → paste directly)
- Mobile capture: take photo with phone camera
- Multiple images per CR
- Thumbnail previews in form
- Original-size view on click
- File size limit: ~30MB per attachment (Dataverse default); larger files go to SharePoint

### Why This Matters

"It's broken" with no evidence is unactionable.
"It's broken — here's a screenshot of the actual issue" is a real problem to solve.

Forcing concrete evidence shifts complaints into problem statements.

---

## Scope

### In Scope (v1 MVP)
- General Ledger (COA, journal entries, recurring journals, period close)
- Multi-entity ledger with inter-company transaction support
- Accounts Payable (vendors, bills, payments via NACHA generation)
- Accounts Receivable (customers, invoices, receipts, aging)
- Bank reconciliation (manual statement import)
- Fixed Assets (acquisition, depreciation, disposal)
- Financial Reporting (native: TB, BS, P&L, Cash Flow, agings)
- 1099 generation via Track1099 integration
- W-9 collection via Track1099
- Audit log / immutable transaction history
- Document attachments via SharePoint
- Change Management workflow (built into app, with image attachments)
- Approval workflows
- Integration with Datastream ERP (shared customer/vendor masters)
- Email generation via Microsoft Graph API

### Phase 2+
- AI-driven document extraction and discrepancy detection (Claude API)
- Power BI reporting
- Bill.com / Ramp / Bank API for AP payment execution
- Credit limit management and enforcement
- Customer credit risk scoring
- Mobile-optimized UI
- Limble PO replacement
- Sales tax engine (if needed)

### Out of Scope (v1)
- Budgeting & forecasting, Inventory, Project accounting, Multi-currency, Sales tax engine, Mobile data entry, Foreign currency

### Out of Scope (Ever)
- Payroll — Paylocity, permanent

---

## Architecture Decisions

### Data Storage Strategy

| Data Type | Store | Rationale |
|---|---|---|
| Chart of Accounts | Dataverse | Master data |
| Vendors / Customers | Dataverse (synced with ERP) | Shared master data |
| Entities / Companies | Dataverse | Multi-entity master |
| Journal Entry headers | Dataverse | Workflow, approval |
| Journal Entry lines (working) | Dataverse | Pre-post editing |
| Posted ledger entries | Azure SQL (append-only) | Immutability + reporting |
| Period close attestations | Azure SQL | Cryptographic hash |
| Ledger integrity checkpoints | Azure SQL | Hash-chain verification |
| Audit events | Azure SQL (append-only) | Redundant audit |
| Historical Macola data | Azure SQL | Read-only archive |
| Reporting data mart | Azure SQL | Fast aggregations |
| Report snapshots | Azure SQL | Time-bound signed reports |
| Change Requests | Dataverse | App-internal workflow |
| Change Request attachments | Dataverse (Image columns) + SharePoint (large files) | Native attachment support |
| Documents | SharePoint | Native |
| Approval requests | Dataverse | Workflow integration |

### Application Layer

- **UI:** Model-driven app + custom pages (React/Fluent UI v9) + PCF controls
- **Business logic:** Dataverse plugins (C#) — strong preference over Power Automate
- **Workflows:** Power Automate only for simple notifications and scheduled jobs outside transaction context
- **Email:** Microsoft Graph API
- **Documents:** SharePoint
- **Document AI (Phase 2):** Claude API
- **ACH:** NACHA file generation in v1

### Multi-Entity Architecture

- `Entity` master table: name, EIN, fiscal year, base currency, type
- Every transactional table includes `EntityId`
- Shared COA structure with entity-specific account activation
- Inter-company JE pairs auto-generated by plugin
- Inter-company elimination accounts on each entity's COA
- Consolidation process nets inter-company balances
- Every report includes Entity selector

---

## Approval Workflows (v1)

| Approval Type | Trigger | Approver(s) |
|---|---|---|
| Bills | Amount > $X (TBD by Pam) | Direct supervisor / designated approver |
| Journal entries | Amount > $Y (TBD by Pam) | Second reviewer (≠ creator) |
| Wire transfers | All wires | Dual approval (≠ each other) |
| New vendor setup | All new vendors | AP Manager + Controller |
| Vendor bank info changes | All changes | AP Manager + Controller + out-of-band verification |
| Period reopening | All reopens | Controller + designated executive |
| Manual JE to bank accounts | All such JEs | Controller |
| Write-offs | Amount > $Z (TBD by Pam) | Controller |
| Recurring JE setup/modification | All | Controller |

Generic `ApprovalRequest` + `ApprovalPolicy` configuration tables. Thresholds adjustable by admin.

---

## DevOps / ALM Architecture

### CI/CD Pipeline

```
Claude Code edits files in PRI-Books-Dev
         ↓
   git push to GitHub
         ↓
   GitHub Actions triggers
         ↓
   pac CLI exports solution as managed
         ↓
   Deploy managed solution to PRI-Books
```

### Required Local Installs

- Power Platform CLI: INSTALLED — v1.51.1
- All four auth profiles established and active

---

## Document AI Strategy

### Long-Term Goal
- Claude API for document extraction
- Document type detection, structured data extraction
- Two-way write: SharePoint columns + Dataverse records
- Discrepancy detection layer
- Outcome: headcount reallocation from manual validation to higher-value work

### V1 Pattern
- SharePoint stores documents (current AI Builder pattern)
- Manual entry into Datastream Books
- Sufficient for cutover

### Phase 2
- Claude API integration evaluated and built

---

## Reporting Strategy

### V1 — Native Model-Driven App

Reports: Trial Balance (per-entity + consolidated), GL Detail by Account, Balance Sheet, Income Statement, Cash Flow Statement, AR Aging, AP Aging, Cash Position, Fixed Asset Register & Depreciation, Bank Reconciliation Summary, Journal Entry Audit Trail, Vendor 1099 Summary, Change Request Log.

### Phase 2 — Power BI

Paginated reports for formal financial statements, dashboards, cross-entity analytics, comparative period analysis.

---

## Pre-Migration Projects (Parallel With Build)

1. **President + COO + IT rollout meeting:** Confirm ownership framing, Pam as System Owner, escalation handling
2. **President (Executive Sponsor) introduces ownership role to Pam directly** (not IT)
3. **COA Pre-Population:** IT proposes draft; Pam reviews and signs off
4. **Vendor Master Strategy:** "Add as needed" — Pam reviews approach
5. **Entity Documentation:** Pam coordinates with executive team
6. **Approval Threshold Definition:** Pam sets dollar amounts
7. **Macola Archive Plan:** Retention duration and access pattern
8. **Pam engagement cadence:** Weekly check-ins begin

---

## Open Questions

See `executive-questionnaire.md`.

---

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Auditor rejects custom system | Medium | Document immutability architecture; proactive disclosure at next audit |
| Build timeline slips | Medium | Hard MVP scope, no scope creep, fixed cutover date |
| Dataverse capacity costs higher than expected | Medium | Monitor capacity during build |
| Key-person dependency | Medium | Documentation standards, AI-assisted handoff |
| Macola data quality during migration | High | Extract early, profile data |
| Period close logic bugs | High | Heavy test coverage, parallel run |
| AI-generated code introduces subtle bugs | Medium | Mandatory human review of financial logic |
| Hash-chain verification missed corruption | Low | Nightly verification + alerting |
| SoD bypassed by privileged user | Medium | Enforce in plugin code, quarterly role audit |
| Cutover failure | High | Parallel run mandatory; penny-perfect reconciliation |
| Document AI accuracy insufficient for headcount story | Medium | Phase 2 only; pilot before committing |
| Leahy ACH service unavailable post-Macola | High | NACHA file generation built in v1 |
| Change management not actually used | Medium | Built into workflow such that changes can't happen without it |
| **Pam refuses ownership role** | **High** | **President (Executive Sponsor) introduces role, reinforces in escalations. Surface at month 1 if Pam resists. Not a project issue if persistent — becomes HR.** |
| **Leadership rescues Pam from ownership during escalations** | **High** | **President + COO rollout meeting sets escalation handling protocol upfront** |
| Managed env constraints surprise developer | Mitigated | PRI-Books-Dev sandbox established as proper source |

---

## AI Usage Guidelines

- AI designs the immutability architecture
- AI generates code for the controls and test cases proving them
- AI assists with: schema design, plugin code, test cases, audit narratives, anomaly detection, document extraction (Phase 2)
- AI does NOT make the system immutable — architecture does
- All financial posting logic requires human review before merge
- AI-generated SQL/plugin code requires test coverage before production

---

## Cost Comparison

Assumptions: 15 users, 5-year horizon, existing M365 base licensing.

### Business Central (Reference)
- Licensing: ~$90K (5-yr)
- Implementation: $50K–$150K
- Internal effort: $30K–$50K
- AP automation add-on: $30K–$100K
- **5-year total: $200K–$390K**

### Datastream Books
- Power Apps per-app: managed by IT
- Dataverse capacity: $5K–$10K (5-yr)
- Azure SQL: $3K–$6K (5-yr)
- Build effort: $80K–$150K
- Ongoing maintenance: $30K–$60K
- Track1099: ~$5K (5-yr)
- Claude API (Phase 2): $8K–$20K (5-yr)
- **5-year total: $130K–$255K**

### Strategic Benefits Beyond Cost
- Headcount reallocation via AI discrepancy detection (Phase 2)
- IT modernization (Lighthouse)
- Unified ERP + Finance data model

---

## Timeline (Working Estimate)

| Phase | Duration | Notes |
|---|---|---|
| Phase 0: Strategy + decisions | Complete | Files generated, environments ready, rollout meeting scheduled |
| Phase 1: Design sprint + repo setup + CI/CD + Pam onboarding | 4–6 weeks | Data model, security, controls, ALM pipeline |
| Phase 2: MVP build (GL + AP + AR + Multi-entity) | 14–18 weeks | Core posting, immutability, multi-entity, reporting, change mgmt |
| Phase 3: Extended build (FA, bank rec, NACHA, approvals, integrations) | 8–10 weeks | |
| Phase 4: Data migration + parallel run | 8–12 weeks | Minimum 1 full close cycle parallel |
| Phase 5: Cutover + hypercare | 4 weeks | At fiscal period boundary |
| **Total** | **10–14 months** | Multi-entity adds ~4 weeks vs. single-entity |

---

## Naming / Glossary

- **Datastream ERP** — existing operations model-driven app
- **Datastream Books** — this project
- **Macola** — legacy on-prem accounting being replaced
- **Leahy** — ACH service tied to Macola
- **Lighthouse** — internal IT strategic plan
- **PRI-Books** — Dataverse production environment (managed)
- **PRI-Books-Dev** — Dataverse dev sandbox (unmanaged)
- **Pam** — Finance System Owner
- **President** — Executive Sponsor
- **MVP** — General Ledger, AP, AR, basic reporting, multi-entity
- **SoD** — Segregation of Duties
- **CR** — Change Request
- **ALM** — Application Lifecycle Management
- **pac** — Power Platform CLI
- **PCF** — Power Apps Component Framework
- **COA** — Chart of Accounts
- **NACHA** — National Automated Clearing House Association
- **PITR** — Point-in-Time Restore
- **SME** — Subject Matter Expert (note: Pam is OWNER, not SME)

---

## Change Log

| Date | Change |
|---|---|
| 2026-05-19 | Document created |
| 2026-05-19 | Immutability architecture, CI/CD, cost numbers, DevOps section |
| 2026-05-19 | 20 additional considerations, per-app licensing, Claude.ai Project confirmed |
| 2026-05-19 | Document AI strategy, Track1099, Graph API, NACHA in v1, multi-entity confirmed |
| 2026-05-19 | PRI-Books env deployed and authenticated |
| 2026-05-19 | PRI-Books-Dev sandbox added; finance SME engagement protocol established |
| 2026-05-19 | **PRI-Books-Dev authenticated. Pam reframed from SME to System OWNER (mirrors ERP pattern). ChangeRequest design includes multi-image attachment support. Executive rollout meeting scheduled with President and COO. President confirmed as executive sponsor. Strategy phase complete.** |
| 2026-05-19 | **Phase 1 begun. Repo skeleton built (.gitkeep placeholders for all empty folders per repo-structure.md). Dataverse solution initialized with `Ryan McCauley` / `rm` publisher (verified against PRI-Datastream ERP via solution export from pri-dev; option-value prefix `12619` matched to ERP for cross-solution consistency). Plugin projects scaffolded (`DatastreamBooks.Plugins` net462 + `DatastreamBooks.Plugins.Tests` net48 with xUnit + FluentAssertions + FakeXrmEasy). First SQL migrations drafted: V0001 (initial schema placeholder), V0002 (`ledger.GeneralLedgerEntries` with DENY UPDATE/DELETE, hash-chain columns, indexes, and full rollback notes). First GitHub Actions workflow `deploy-dev.yml` stubbed with required secrets documented. Foundational PowerShell scripts created: setup-dev, auth-env, pull-solution, push-solution, run-sql-migration. Architecture docs authored: data-model, security-model, immutability-design. Controls docs authored: sod-matrix, approval-policies, audit-controls. All remaining CFO references in this decision log corrected to President (Executive Sponsor) following the recent sponsor change. New principle added to AGENTS.md: "Verification is mandatory, not optional" (codified after the publisher prefix had to be reverted from a guessed value to the verified ERP-matching value).** |
