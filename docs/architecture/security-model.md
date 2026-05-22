# Datastream Books — Security Model

> Placeholder structure. Role definitions are listed; SoD details and field-
> level security designs land here as they are designed. Cross-reference:
> [`../controls/sod-matrix.md`](../controls/sod-matrix.md).

## Authentication

- Microsoft Entra ID (decision log §5, §28)
- No local accounts in Dataverse
- pac CLI authenticates via interactive browser flow for humans; via app
  registration (federated identity preferred) for GitHub Actions

## Authorization Model

Dataverse security roles are defined in `solution/src/Roles/` (created as we
build each feature). Roles are additive: a user may hold multiple. SoD
enforcement is in plugin code (`CreatedBy != ApprovedBy`, etc.) — role
membership alone is not enough to bypass it.

### Finance-Specific Roles (per decision §61)

Phase 7 introduces 7 finance-specific Dataverse security roles. These are
**not** aligned to the Datastream ERP role structure — finance personas
(Controller, AP, AR, approver) don't map cleanly onto ERP personas
(warehouse, transportation, ops, sales), and overlaying the two would
force one team's mental model on the other. Clean separation also
simplifies SoD audits: each app's roles are reviewable in isolation.

| Role | Persona | Primary scope |
|---|---|---|
| **Controller** | Pam (today); future delegate | Holds elevated finance privileges: approve high-threshold JEs, close periods, reopen closed (not locked) periods, approve vendor bank changes, approve wires. Inherits across the SoD-enforced roles `JE Approve`, `Period Close`, `Period Reopen`. |
| **AP Clerk** | AP staff | Create and edit bills, enter vendor payments, generate NACHA file, manage AP master data updates (NOT bank info changes — separate role per SoD). |
| **AR Clerk** | AR staff | Create and edit invoices and receipts, apply receipts, aging analysis. Customer master maintenance (reads ERP `rm_customer`, writes Books-side metadata only). |
| **Approver** | Designated approver (often Pam, may be CFO or COO depending on amount + policy) | Approves bills, JEs, and vendor bank info changes above thresholds defined in [`../controls/approval-policies.md`](../controls/approval-policies.md). SoD-enforced: cannot approve own creations. |
| **Casual Contributor** | Operational staff filing CRs or referencing reports | Read-only on most surfaces; can file Change Requests with multi-image attachments; cannot view restricted fields (e.g., vendor banking info, payroll-suspense balances). |
| **System Admin** | IT (Ryan) | Privileged operations: deploy solutions, manage security roles, manage approval policies. Does **not** post JEs (separation between platform admin and finance operations). |
| **Read-Only Auditor** | External auditor at audit time | Read-only access across all financial tables, audit trails, and report snapshots. No write privileges anywhere. Time-bound activation per the audit engagement letter. |

**Detailed permissions per role are populated during Phase 7B** as
pages are designed and attached. (Originally Phase 7A per the
phase-7-ui-design.md draft; the security role scaffolding session was
moved to Phase 7B in the 2026-05-21 Phase 7A kickoff conversation
because roles only make sense once the transactional pages they attach
to exist. See [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md)
§66 for the scope rebound.) The eventual full-kickoff prompt at
[`../runbooks/phase-7a-foundation-prompt.md`](../runbooks/phase-7a-foundation-prompt.md)
still references "Phase 7A security role scaffolding session" but is
marked DRAFT and amended to reflect the §66 reality.

**Cross-reference:** The 10 SoD-purpose roles below (JE Entry, JE Approve,
JE Post, etc.) are the **enforcement-level roles** — the GUID-comparison
roles that plugins check against. The 7 finance-specific roles above are
the **persona-level roles** — the buckets users get assigned to in
practice. A user holding the persona role `Controller` gets, by
inheritance, the enforcement roles `JE Approve`, `Period Close`,
`Period Reopen`, and several others. Persona-role-to-enforcement-role
mapping is the explicit subject of the Phase 7A security role scaffolding
session, and lives in [`../controls/sod-matrix.md`](../controls/sod-matrix.md).

### Enforcement-Level Roles (SoD purposes)

From decision log §E — Segregation of Duties:

| Role | Can | Cannot |
|---|---|---|
| **JE Entry** | Create and edit JE drafts; submit for approval | Approve, Post, Void |
| **JE Approve** | Approve JEs created by others (SoD: `CreatedBy != ApprovedBy`) | Create or post JEs they approved |
| **JE Post** | Post approved JEs (SoD: `ApprovedBy != PostedBy`) | Create, approve, void |
| **JE Void** | Void a posted JE (creates a reversing entry; never deletes) | Edit posted entries |
| **Period Close** | Close an open period (writes attestation hash) | Reopen periods |
| **Period Reopen** | Reopen a closed (not locked) period (elevated; logs audit event) | Close periods (held in JE Approve / Controller role) |
| **Vendor Setup** | Create new vendor records (SoD with bank-info change) | Edit vendor banking info |
| **Vendor Bank Change** | Edit vendor banking info (SoD with vendor setup; dual-approval per ApprovalPolicy) | Create vendors |
| **Wire Initiate** | Initiate a wire transfer JE | Approve their own wires |
| **Wire Approve** | Approve a wire transfer JE (SoD: `InitiatedBy != ApprovedBy`) | Initiate wires they approve |

A separate **Controller** role inherits across `JE Approve`, `Period Close`,
`Period Reopen`, and the right to approve high-threshold approvals per
[`../controls/approval-policies.md`](../controls/approval-policies.md). The
person holding Controller is the same person in real-world terms (Pam, today),
but the *role* exists independently so the same SoD rules apply if Controller
duties are delegated.

### Where SoD Lives (System-Enforced, Not Honor System)

| Rule | Enforced by | Bypass surface |
|---|---|---|
| `CreatedBy != ApprovedBy` for JEs above the approval threshold | `ApproveJournalEntryPlugin` (Dataverse) | None — plugin rejects the approve action |
| `ApprovedBy != PostedBy` for posted JEs | `PostJournalEntryPlugin` (Dataverse) | None — same |
| `VendorBankChange` dual approval | `ApproveVendorBankChangePlugin` | None — same |
| `Wire Initiate != Wire Approve` | `ApproveWirePlugin` | None — same |
| `RequestedBy != ApprovedBy != AssignedTo` for ChangeRequests | `ChangeRequestApprovalPlugin` | None — same |

If a single user holds both roles (e.g., a small team), the plugin still
fires the SoD check by comparing user GUIDs at the moment of action. The
right answer in that case is **add a second human**, not bypass the rule —
documented in the SoD matrix.

## Field-Level Security

To be designed. Initial candidates:

| Field | Restriction |
|---|---|
| `rm_entity.rm_ein` | Encrypted at rest; visible only to Controller and SystemAdmin |
| Vendor banking info | Visible only to `Vendor Bank Change` role (TBD design) |
| Posted ledger amounts in restricted accounts (e.g., payroll suspense, M&A) | TBD — driven by approval policy |

## Credential Storage — Azure Key Vault

All non-interactive credentials live in `kv-datastream-books`
(`https://kv-datastream-books.vault.azure.net/`). No plaintext credentials
in the repo, in `appsettings*`, in environment variables outside dev, or
in any chat/email channel.

Credential flow (dev today; prod once cutover):

```
                   ┌────────────────────────────────────┐
                   │      kv-datastream-books           │
                   │  (Azure RBAC, soft-delete 90d,     │
                   │   purge protection, firewall:      │
                   │   only ryanm dev IP today)         │
                   └────────────────────────────────────┘
                            ▲                ▲
                            │ Get secret     │ Set secret (rotation)
                            │ (Secrets User) │ (Administrator)
                            │                │
              ┌─────────────┴────────┐   ┌──┴────────────────────┐
              │  Phase 6B plugin     │   │  ryanm (human admin)  │
              │  runtime (uses       │   │  + automated rotation │
              │  datastream-books-   │   │  scripts (TBD)        │
              │  cicd SP)            │   │                       │
              └──────────────────────┘   └───────────────────────┘
                            │
                            │ ADO.NET conn string
                            ▼
              ┌──────────────────────────────────┐
              │  Azure SQL DatastreamBooks-Dev   │
              │  user dsb_app (least-priv)       │
              │  INSERT into ledger.*,           │
              │  DENY UPDATE/DELETE              │
              └──────────────────────────────────┘
```

Why this layer exists, and what it does **not** do:

- It does **not** protect against `priadmin` (the SQL Server admin login).
  `priadmin` maps to `dbo` and bypasses every DENY by SQL Server design.
  That risk is mitigated organizationally (see [`../runbooks/sql-account-management.md`](../runbooks/sql-account-management.md))
  and by Azure SQL auditing — *not* by Key Vault.
- It does protect against accidental credential exposure (commits, copy/
  paste into chat, screen sharing, etc.) by ensuring no plaintext copy
  of `dsb_app`'s password exists anywhere except in Vault.
- It does centralize rotation: one place to update, all consumers pick up
  the new value at next read.
- It does deliver audit defensibility: every secret read/write/list/
  delete is logged via the diagnostic setting to the shared Log Analytics
  workspace.

Operational details — RBAC, secret inventory, rotation procedures,
break-glass — live in [`../runbooks/key-vault-management.md`](../runbooks/key-vault-management.md).
Anyone touching credentials must read that runbook before acting.

## Privileged Identity Management

- No standing admin access to PRI-Books (production). Admin actions go via
  the deployment pipeline OR a time-bound elevation request through Entra PIM
  (TBD — PIM workflow design).
- `rl_admin` (Azure SQL) is held by named individuals only; assignments are
  reviewed quarterly. See [`../runbooks/change-management.md`](../runbooks/change-management.md)
  (to be authored).

## What This Document Will Cover When Complete

- Detailed role-by-permission matrix for every Dataverse table in the v1 scope
- Field-level security profiles per role
- Privilege escalation paths and the approvals required for each
- Entra group → Dataverse role mappings
- Service-principal inventory (GitHub Actions, plugin sandbox, SQL migration runner)
- Quarterly access-review process owner and cadence

## See Also

- [`data-model.md`](data-model.md)
- [`immutability-design.md`](immutability-design.md)
- [`immutability-validation.md`](immutability-validation.md) — `priadmin` bypass finding (why dsb_app exists)
- [`../runbooks/key-vault-management.md`](../runbooks/key-vault-management.md) — operational details for the credential store
- [`../runbooks/sql-account-management.md`](../runbooks/sql-account-management.md)
- [`../runbooks/cicd-setup.md`](../runbooks/cicd-setup.md)
- [`../controls/sod-matrix.md`](../controls/sod-matrix.md)
- [`../controls/approval-policies.md`](../controls/approval-policies.md)
- [`../controls/audit-controls.md`](../controls/audit-controls.md)
- Decision log §E (SoD), §H (Dev/Prod separation), §J (Change Management)
