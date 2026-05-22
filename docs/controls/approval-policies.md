# Datastream Books — Approval Policies

> Placeholder structure. Threshold dollar values are TBD — the Finance Lead owns these (once named)
> per the decision log and executive questionnaire §3.

## How Policies Are Implemented

A generic `rm_approvalpolicy` Dataverse table stores tuples of:

- `rm_subjecttype` (e.g., `Bill`, `JournalEntry`, `Wire`, `Writeoff`,
  `RecurringJESetup`)
- `rm_subjectcriteria` (JSON or structured fields — e.g., `amount >= 5000`)
- `rm_approverrole` (Dataverse role, e.g., `Controller`)
- `rm_numberofapprovers` (1, 2 — supports dual approval)
- `rm_sodrule` (e.g., `RequestedBy != ApprovedBy`)
- `rm_effectivefrom` / `rm_effectiveto` (so policy changes have an audit trail)

Plugins evaluating an approval (`ApproveJournalEntryPlugin`,
`ApproveBillPlugin`, `ApproveWirePlugin`, etc.) read the matching active
policy row at action time and apply its rules.

Editing approval policy rows is itself a privileged operation — see the
ChangeRequest workflow and the `Controller` role.

## Policies to Define (Values TBD by Finance Lead)

| Subject | Criteria | Approver Role(s) | # Approvers | SoD |
|---|---|---|---|---|
| Bill | `amount >= $X` | direct supervisor / designated approver | 1 | RequestedBy != ApprovedBy |
| Journal entry | `amount >= $Y` | second reviewer | 1 | CreatedBy != ApprovedBy |
| Wire transfer | ALL wires | wire-approve role | 2 | Initiator != Approver1 != Approver2 |
| New vendor setup | ALL new vendors | AP Manager + Controller | 2 | distinct |
| Vendor bank info change | ALL changes | AP Manager + Controller (+ out-of-band verification) | 2 | distinct |
| Period reopening | ALL reopens | Controller + designated executive | 2 | distinct from closer |
| Manual JE to bank accounts | ALL such JEs | Controller | 1 | CreatedBy != ApprovedBy |
| Write-offs (bad debt, AP forgiveness) | `amount >= $Z` | Controller | 1 | CreatedBy != ApprovedBy |
| Recurring JE setup / modification | ALL | Controller | 1 | CreatedBy != ApprovedBy |

Values for $X, $Y, $Z: **pending Finance Lead (executive questionnaire §3.1, §3.2, §3.3).**

## Out-of-Band Verification (Vendor Bank Change)

Vendor banking changes are a documented fraud vector. In addition to the
dual approval above, the policy requires:

1. The change request includes documentation (image attachment) of the
   vendor's bank-change letter or signed form
2. A phone call to the vendor at the *previously known* contact number
   confirming the change, recorded as a Timeline note on the
   `ChangeRequest` record (date, time, who called, who confirmed)
3. The change does not take effect until both approvers have signed off
   AND the out-of-band confirmation is recorded

Plugin enforcement: `ApproveVendorBankChangePlugin` checks for an
attached confirmation note before allowing the second approval to land.

## Audit Trail of Policy Changes

Every change to `rm_approvalpolicy` writes a row to `audit.AuditEvents`
including:
- The policy id, version, the field(s) changed, before/after values
- The user making the change
- The associated ChangeRequest id (REQUIRED — plugin rejects un-CRed
  policy changes)

## Open Questions

- Threshold values: §3.1, §3.2, §3.3 of the executive questionnaire
- Whether bill approval routes through a "direct supervisor" or a fixed
  approver pool — depends on org chart we haven't captured
- Whether write-off approval should escalate to executive sponsor above
  a higher threshold (e.g., $25K) — Finance Lead to weigh in

## See Also

- [`sod-matrix.md`](sod-matrix.md)
- [`audit-controls.md`](audit-controls.md)
- [`../memos/executive-questionnaire.md`](../memos/executive-questionnaire.md) §3
- Decision log "Approval Workflows (v1)"
