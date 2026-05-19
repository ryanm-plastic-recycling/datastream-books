# Datastream Books — Audit Controls

> Placeholder structure. Final audit trail design lands here as it is built.
> Cross-references [`../architecture/immutability-design.md`](../architecture/immutability-design.md)
> §F (Comprehensive Audit Trail) and §G (Time-Bound, Signed Reports).

## Audit Surfaces (Two Redundant Trails)

### 1. Dataverse Platform Audit Log

- Enabled on every financial table: `rm_journalentry`, `rm_journalentryline`,
  `rm_fiscalperiod`, `rm_chartofaccount`, `rm_entity`, vendor/customer
  tables (when added), `rm_approvalpolicy`, `rm_changerequest`.
- Captures CRUD events at the platform layer (creator, timestamp,
  before/after values for tracked fields).
- Retention: maximum allowed by the environment SKU. Quarterly export to
  a durable archive (Azure Blob Storage with immutable WORM policy) for
  long-term retention beyond the in-platform window.

### 2. Azure SQL `audit.AuditEvents` (planned V0005)

- Append-only Azure SQL table, same DENY UPDATE/DELETE pattern as
  `ledger.GeneralLedgerEntries`.
- Plugins write semantic events (richer than platform-level CRUD):
  - `PostJEAccepted`, `PostJERejected` (with reason)
  - `ApprovalGranted`, `ApprovalRejected`
  - `PeriodClosed`, `PeriodReopened` (with reason + approver)
  - `PolicyChanged` (with CR id)
  - `RoleAssignmentChanged`
  - `SoDViolationAttempted` (every blocked SoD check)
- Hash-chained the same way the ledger is, anchored on a per-day basis.

### Why Two?

An attacker who compromises the Dataverse audit log (unlikely — the
platform owner is Microsoft) would still face the SQL trail. An attacker
who compromises the SQL trail (more conceivable — internal DBA gone bad)
would still face the platform-level Dataverse trail. The redundancy is
intentional and was a design call in the decision log.

## What Gets Audited at the Application Layer

| Event | Where logged | Notes |
|---|---|---|
| JE created | Dataverse audit | Standard CRUD |
| JE submitted for approval | `audit.AuditEvents` | Plugin writes "ApprovalRequested" |
| JE approved | Both | Plugin writes "ApprovalGranted" with approver id |
| JE approval blocked by SoD | `audit.AuditEvents` | Plugin writes "SoDViolationAttempted" — visible in admin reporting |
| JE posted | Both | `audit.AuditEvents` "PostJEAccepted"; row in `ledger.GeneralLedgerEntries` |
| JE post rejected | `audit.AuditEvents` | With rejection reason (period closed, debits ≠ credits, etc.) |
| JE voided (reversed) | Both | A new reversal row in ledger + audit event |
| Period closed | Both | Plus `audit.PeriodCloseAttestation` row with close hash |
| Period reopened | Both | Plus elevated-role approver captured |
| Vendor bank info changed | Both | Plus out-of-band confirmation note check |
| Wire approved | Both | Both initiator and approvers captured |
| Approval policy modified | Both | Plus CR reference REQUIRED |
| Role assignment changed | Both | Quarterly review captures these |
| ChangeRequest opened/approved/closed | Both | Full lifecycle |

## Audit Reports (v1)

| Report | Audience | Cadence |
|---|---|---|
| Journal Entry Audit Trail | Controller / external auditor | On demand |
| SoD Violation Attempts | Controller | Monthly |
| Period Close Attestations | External auditor | On close + on demand |
| Role Assignment Roster | Controller + IT | Quarterly |
| ChangeRequest Log | Controller + IT | Monthly |
| Hash-Chain Integrity Check | IT / external auditor | Daily (automated) + on demand |
| Vendor Bank Change Log | Controller | Monthly |

Reports run from `reporting.*` views over the Azure SQL ledger/audit data
(no live queries against Dataverse for audit purposes — the SQL ledger is
the audit-of-record).

## Retention

- Dataverse audit log: in-platform max + quarterly archive to immutable
  Azure Blob (WORM, 7-year minimum)
- Azure SQL audit table: long-term retention via Azure SQL LTR (7-year
  minimum); PITR enabled on top
- Ledger: forever (no purge — append-only is forever by definition)
- Documents (SharePoint): 7-year M365 retention policy (TBD per §4.1 of
  exec questionnaire)

## Audit Access

External auditors get a time-bounded read-only Entra account with:
- `rl_app_reader` on the Azure SQL database (SELECT only on `ledger.*`,
  `audit.*`, `reporting.*`)
- A Dataverse security role that grants Read on all financial tables and
  the platform audit log
- No write rights anywhere

Access is provisioned per engagement, deprovisioned at engagement end.

## Open Questions

- §4.1: Existing retention policy? Default proposal of 7 years stands
  unless contradicted.
- §5.1–5.3: External audit scope (full / review / compilation), SOC
  requirements
- §9.1: Cyber insurance control requirements that may bind us tighter

## See Also

- [`sod-matrix.md`](sod-matrix.md)
- [`approval-policies.md`](approval-policies.md)
- [`../architecture/immutability-design.md`](../architecture/immutability-design.md) §F, §G
- Decision log §F (Comprehensive Audit Trail), §G (Time-Bound Signed Reports)
