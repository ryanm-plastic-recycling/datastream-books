# Datastream Books — Segregation of Duties Matrix

> Defines which roles can perform which operations and which combinations
> are forbidden. Source of truth for the SoD checks the Dataverse plugins
> enforce. See [`../architecture/security-model.md`](../architecture/security-model.md)
> for role definitions.

## Conventions

- ✅ = role is required (one of these roles must be held by the actor)
- 🚫 = role is forbidden (the actor cannot hold this role in addition)
- — = irrelevant
- "SoD with X" = the *user GUID* holding this role at action time must
  differ from the user GUID holding role X at the prior action

## Role × Operation Matrix

| Operation | JE Entry | JE Approve | JE Post | JE Void | Period Close | Period Reopen | Vendor Setup | Vendor Bank Change | Wire Initiate | Wire Approve | Controller |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Create JE draft | ✅ | — | — | — | — | — | — | — | — | — | ✅ |
| Submit JE for approval | ✅ | — | — | — | — | — | — | — | — | — | ✅ |
| Approve JE (below threshold) | — | ✅ (SoD with creator) | — | — | — | — | — | — | — | — | ✅ |
| Approve JE (above threshold) | — | ✅ (SoD with creator) | — | — | — | — | — | — | — | — | ✅ (required) |
| Post approved JE | — | — | ✅ (SoD with approver) | — | — | — | — | — | — | — | ✅ |
| Void posted JE (reversal) | — | — | — | ✅ | — | — | — | — | — | — | ✅ |
| Close fiscal period | — | — | — | — | ✅ | — | — | — | — | — | ✅ |
| Reopen closed period | — | — | — | — | — | ✅ (SoD with closer) | — | — | — | — | ✅ |
| Lock period (terminal) | — | — | — | — | — | — | — | — | — | — | ✅ (required) |
| Create vendor | — | — | — | — | — | — | ✅ | 🚫 | — | — | ✅ |
| Edit vendor bank info | — | — | — | — | — | — | 🚫 | ✅ (dual: 2 approvers, distinct) | — | — | ✅ |
| Initiate wire transfer | — | — | — | — | — | — | — | — | ✅ | 🚫 | ✅ |
| Approve wire transfer | — | — | — | — | — | — | — | — | 🚫 | ✅ (SoD with initiator) | ✅ |

## SoD Pair Summary (the rules the plugins enforce in code)

| Rule | Plugin | Source-of-truth field comparison |
|---|---|---|
| JE creator ≠ approver | `ApproveJournalEntryPlugin` | `rm_journalentry.rm_createdby_user != rm_journalentry.rm_approvedby_user` (both lookups to systemuser; set by plugin, never by UI) |
| JE approver ≠ poster | `PostJournalEntryPlugin` | `rm_journalentry.rm_approvedby_user != rm_journalentry.rm_postedby_user` (per current approval-policies.md, may be relaxed per policy row) |
| Vendor setup ≠ vendor bank change | `EditVendorBankInfoPlugin` | distinct user GUIDs on `vendor.SetupBy` and `vendor.BankChangedBy` |
| Vendor bank change: dual approval | `ApproveVendorBankChangePlugin` | two distinct approvers, both holding `Vendor Bank Change` OR `Controller` |
| Wire initiate ≠ wire approve | `ApproveWirePlugin` | `wire.InitiatedBy != wire.ApprovedBy` |
| Period close ≠ period reopen (for the same period) | `ReopenPeriodPlugin` | `rm_closedbyuserid != currentUser` |
| ChangeRequest: requester ≠ approver ≠ assignee | `ChangeRequestApprovalPlugin` | three distinct user GUIDs |

## What Happens When a Single User Holds Both Roles

Small teams will naturally have overlap. The matrix is enforced *at the
user GUID level*, not at the role level — so a user holding both
`JE Entry` and `JE Approve` can still create a JE, but the system will
refuse to let *that same user* approve it. They must hand off to another
holder of `JE Approve`.

If no other holder exists, the correct response is **expand the role
membership**, not bypass the rule. The fact that the system makes the
problem visible at the moment of action is a feature.

## Pending: Threshold Values

The "below threshold" / "above threshold" rows depend on dollar values
set in [`approval-policies.md`](approval-policies.md). Pam owns those
values — see executive questionnaire §3.

## Quarterly Access Review

Owner: TBD (likely Pam + IT, jointly).
Cadence: quarterly.
Output: a written attestation (stored in `docs/controls/access-reviews/`
once the first review runs) listing each role's members and confirming
each is appropriate.

## See Also

- [`approval-policies.md`](approval-policies.md)
- [`audit-controls.md`](audit-controls.md)
- [`../architecture/security-model.md`](../architecture/security-model.md)
- Decision log §E (SoD architecture)
