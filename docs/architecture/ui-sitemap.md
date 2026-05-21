# UI Sitemap -- Datastream Books App Module Navigation

> Captured during Phase 7A (UI Foundation), Session S3, 2026-05-21.
> Source-of-truth for the sitemap structure of the
> `Datastream Books` model-driven app module. Implements decisions
> [§52](../decisions/datastream-books-decisions.md) (single shared
> dashboard) and [§62](../decisions/datastream-books-decisions.md)
> (navigation is Pam's biggest Macola pain point).

## Core Principle

**Accounting-workflow-first, ERP-pattern-second.** ERP groups by
operations function (Plants, Machines, Lots). Books groups by what an
accountant actually does day-to-day: AP, AR, GL, period close,
reports, reference, admin. Decision: do not mimic ERP's group
structure; mimic a finance team's mental model.

## Sitemap Structure (Top-Level Groups)

| Order | Group | Purpose | Primary persona |
|---|---|---|---|
| 1 | **Home** | Landing dashboard with role-aware widgets | All |
| 2 | **Accounts Payable** | Bill entry, payments, vendor work | AP Clerk, Controller, Approver |
| 3 | **Accounts Receivable** | Invoice entry, receipts, customer work | AR Clerk, Controller |
| 4 | **General Ledger** | JE entry, recurring JEs, GL detail | Controller, Casual Contributor |
| 5 | **Period Close** | Pre-close TB review, close workflow, period lock | Controller |
| 6 | **Reports** | All reports library (BS, P&L, Cash Flow, agings, audit trail) | Controller, Approver, Auditor |
| 7 | **Reference Data** | Master data lookups (COA, vendors, customers, entities, fiscal calendar, account types/categories) | All -- read-mostly |
| 8 | **Admin** | Change requests, approval policies, audit log, security roles, system settings | System Admin, Pam |

Rationale for each group ordering choice:

- **Home first** -- per [§52](../decisions/datastream-books-decisions.md),
  single shared dashboard. Always the first item Pam clicks after sign-in.
- **AP / AR / GL in that order** -- AP and AR are higher-volume daily
  transactional flows than GL; clerks live in their respective area.
  GL precedes Period Close because adjusting journal entries are made
  in GL just before close.
- **Period Close before Reports** -- close drives reports; close is a
  workflow, reports are the artifact. Mental flow: "close the period,
  then run the reports."
- **Reports before Reference Data** -- daily / periodic use higher
  than master-data lookup. Reference Data is "look up while doing
  other things," not the destination.
- **Admin last** -- low-frequency, high-blast-radius. Should not be a
  short walk from "look up an account."

## Detailed Sitemap

### 1. Home

Single sitemap area `home`, single page (custom page in S4 stub /
S7 widget regions).

| Item | Type | Phase | Notes |
|---|---|---|---|
| Dashboard | Custom page | 7A shell, 7B+ real widgets | Single shared, role-aware. Widget regions placeholder in S7. |

### 2. Accounts Payable

Sitemap area `ap`. Entities: `rm_bill` (future), `rm_payment` (future).

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Bills | Dataverse view | Phase 8 (table) + 7B (form) | Placeholder page "Coming Phase 8 -- bill entry, approval routing, GL distribution" |
| My Approvals | Filtered view + widget | Phase 8 + 7B | Placeholder page |
| Payments | Dataverse view | Phase 8 + 7B | Placeholder page |
| AP Aging | Report link | Phase 9 + 7C | Placeholder page; cross-link to Reports group |

### 3. Accounts Receivable

Sitemap area `ar`. Entities: `rm_invoice` (future), `rm_receipt` (future).

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Invoices | Dataverse view | Phase 8 + 7B | Placeholder page |
| Receipts | Dataverse view | Phase 8 + 7B | Placeholder page |
| AR Aging | Report link | Phase 9 + 7C | Placeholder page |

### 4. General Ledger

Sitemap area `gl`. Entities: `rm_journalentry`, `rm_journalentryline`,
`rm_chartofaccount`.

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Journal Entries | Dataverse view (`rm_journalentry`) | Phase 5 (table) + 7B (form) | **Live in 7A shell** -- default Dataverse list view works today. JE-2026-001005 visible. |
| Recurring Journal Entries | Filtered view | Phase 8/9 | Placeholder page |
| GL Detail by Account | Report | Phase 9 + 7C | Placeholder page |

### 5. Period Close

Sitemap area `close`. Entities: `rm_fiscalperiod`, `rm_fiscalyear`.

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Fiscal Periods | Dataverse view (`rm_fiscalperiod`) | Phase 3 | **Live in 7A shell** -- one open period visible today. |
| Period Close Workflow | Custom page | Phase 9 + 7D | Placeholder page |
| Trial Balance Review | Report + workflow | Phase 9 + 7C | Placeholder page |

### 6. Reports

Sitemap area `reports`. No native entity -- this is a navigation
container for report pages. Reports are custom pages backed by Azure
SQL `ledger.GeneralLedgerEntries` queries (Phase 9 + 7C).

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Balance Sheet | Custom page | Phase 9 + 7C | Placeholder page |
| Income Statement (P&L) | Custom page | Phase 9 + 7C | Placeholder page |
| Cash Flow Statement | Custom page | Phase 9 + 7C | Placeholder page |
| Trial Balance | Custom page | Phase 9 + 7C | Placeholder page |
| AR Aging | Custom page | Phase 9 + 7C | Placeholder page |
| AP Aging | Custom page | Phase 9 + 7C | Placeholder page |
| JE Audit Trail | Custom page | Phase 9 + 7C | Placeholder page |
| Change Request Log | Custom page | Phase 7D | Placeholder page |
| Vendor 1099 Summary | Custom page | Phase 9 (Track1099 integration) | Placeholder page |

### 7. Reference Data

Sitemap area `reference`. Master data tables.

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Chart of Accounts | Dataverse view (`rm_chartofaccount`) | Phase 4 | **Live in 7A shell** -- 54 rows under "Default Operating Entity". |
| Entities | Dataverse view (`rm_entity`) | Phase 3 | **Live in 7A shell** -- one placeholder entity. |
| Fiscal Calendar | Dataverse view (`rm_fiscalperiod` grouped by `rm_fiscalyear`) | Phase 3 | **Live in 7A shell** |
| Account Types | Dataverse view (`rm_accounttype`) | Phase 3 | **Live in 7A shell** -- 5 rows. |
| Account Categories | Dataverse view (`rm_accountcategory`) | Phase 3 | **Live in 7A shell** |
| Vendors | Dataverse view (Books-owned table, future) | Phase 7-Backend / Phase 8 | Placeholder page |
| Customers | Dataverse view (cross-solution to ERP `rm_customer`) | Phase 7-Backend | Placeholder page |

### 8. Admin

Sitemap area `admin`. System administration and finance-system-owner
controls.

| Item | Type | Phase delivered | 7A behavior |
|---|---|---|---|
| Change Requests | Dataverse view + custom page | Phase 7D (form), table exists | Placeholder page (table exists, form in 7D) |
| Approval Policies | Dataverse view | Phase 8 | Placeholder page |
| Audit Log | Dataverse native audit + custom report | Live | Cross-link to Dataverse default audit log UI in 7A; custom report in 7C/7D |
| Security Roles | Dataverse default role admin | 7B (role scaffolding) | Cross-link to Dataverse default role admin UI |
| System Settings | Custom page | Phase 7D | Placeholder page |

## Placeholder Page Pattern

Items marked "Placeholder page" above all share the same template
(custom page, single component):

```
[Logo banner]

This area is not yet built.

Coming in <Phase X>: <one-sentence description from the relevant
phase planning document>.

If you have feedback or a request for this area, file a Change
Request: [link]
```

Single template, parameterized per item. Built once in S4 / S7,
re-used everywhere. Reduces 7A shell-build cost dramatically and gives
Pam something coherent to react to on the placeholder areas without
forcing fake widgets.

## Navigation Conventions

### Recent items

**Lean on Dataverse built-in** per Q4 resolution
([phase-7a-foundation-prompt](../runbooks/phase-7a-foundation-prompt.md)
review thread, decision in S0 prompt response).

The modern Unified Interface already surfaces a "Recently viewed"
section in the sitemap, populated automatically per-user. No custom
PCF in 7A.

Re-evaluate during 7E (CR burn-down) only if Pam files CRs that the
built-in is insufficient.

### Global search

**Platform-built-in for 7A.** Custom global search PCF is
**descoped** from 7A per the operator brief; reconsidered in 7B / 7C
when more entities exist to search across. The Unified Interface's
built-in global search (top bar) is the default for 7A.

Open question for 7C: should global search also query Azure SQL
`ledger.GeneralLedgerEntries`? Decision deferred to 7C kickoff per
[`phase-7-ui-design.md`](../decisions/phase-7-ui-design.md) open
items.

### Breadcrumbs

**Platform-built-in for 7A.** The Unified Interface already shows
breadcrumb-like back-navigation on forms. Custom breadcrumb PCF is
deferred from 7A; re-evaluated after S4 shell lands and we can judge
whether built-in is sufficient.

### Page header pattern

Every page (custom or Dataverse-native form) shows:

- Page title (top, large, `Title3` Fluent UI preset)
- Optional subtitle / context (e.g., "Entity: Default Operating
  Entity", "Period: 2026-05")
- Primary action button (right-aligned, `--brand-primary` fill)
- Secondary actions (right-aligned, ghost button style)

Form pages add the platform-default command bar above this header.

## Visibility in 7A Shell

When S4 builds the app module, the sitemap is **fully scaffolded** --
all 8 groups and all leaf items visible to the user, even those that
point to placeholder pages. Rationale: Pam (per [§57](../decisions/datastream-books-decisions.md)
shell-only demo framing) should see the full intended map even when
pages aren't built yet. Empty navigation is more confusing than full
navigation with "coming Phase X" markers.

Items currently backed by **real Dataverse views** ("Live in 7A
shell" rows above): JE list, fiscal periods, COA, entities, fiscal
calendar, account types, account categories. These work today
because their tables exist with seeded data.

Items currently backed by **placeholders** ("Placeholder page" rows
above): everything else.

## Group Icons (Fluent UI v9 icon set)

Each top-level group needs an icon for the sitemap. Recommended
mappings, all from `@fluentui/react-icons` library:

| Group | Icon name | Notes |
|---|---|---|
| Home | `Home20Regular` | Standard home icon |
| Accounts Payable | `ReceiptMoney20Regular` | Receipt with dollar sign |
| Accounts Receivable | `MoneyHand20Regular` | Hand receiving money |
| General Ledger | `BookOpen20Regular` | Open ledger book |
| Period Close | `CalendarLock20Regular` | Calendar with lock |
| Reports | `DocumentTable20Regular` | Tabular document |
| Reference Data | `Library20Regular` | Library / archive |
| Admin | `Settings20Regular` | Standard settings gear |

Icons may be substituted during S4 if the Fluent set has better
matches at that point -- Fluent ships new icons regularly.

## Group-to-Persona Visibility (Phase 7B placeholder)

Phase 7A scaffolds the sitemap without role-based hiding -- everyone
sees everything (with placeholders for unimplemented areas). Phase 7B
attaches the 7 finance-specific security roles
([§61](../decisions/datastream-books-decisions.md)) to the sitemap
and hides groups by role.

Anticipated mapping (subject to refinement when roles land in 7B):

| Role | Visible groups |
|---|---|
| Controller | All 8 groups |
| AP Clerk | Home, AP, Reference Data (Vendors), Reports (AP-related) |
| AR Clerk | Home, AR, Reference Data (Customers), Reports (AR-related) |
| Approver | Home, AP (My Approvals), AR (selective), Reports |
| Casual Contributor | Home, Reference Data (read), GL (read-only), Admin (Change Requests only) |
| System Admin | All 8 groups |
| Read-Only Auditor | Home (read-only widgets), GL (read), Reports (all), Reference Data, Admin (Audit Log only) |

## Implementation Notes for S4

- Sitemap is defined in the app module's `sitemap.xml` (unpacked
  solution form) under `solution/src/AppModules/`.
- Each group becomes a `Group` element with an `Icon` attribute
  pointing to a web resource or Fluent icon name (modern Unified
  Interface).
- Each leaf becomes a `SubArea` element pointing to a Dataverse view
  (`Entity` attribute) or a custom page (`Url` attribute).
- Placeholder pages are a single custom page parameterized via query
  string -- one component, eight URLs.
- Sitemap will be authored in maker portal initially, then pulled via
  `pull-solution.ps1` to capture the XML.

## Open Items for S4

| # | Item | Resolution path |
|---|---|---|
| N1 | Confirm Books app module does not yet exist in PRI-Books-Dev. | Verify via `pac org list-apps` or Web API at S4 kickoff. |
| N2 | Decide Innovation Team logo placement (footer vs corner watermark vs welcome card). | S4 decision. |
| N3 | Verify Fluent UI icon names are still current. | Quick `@fluentui/react-icons` package lookup at S4. |
| N4 | Author sitemap.xml or build via maker portal first? | Recommend maker portal first (faster), then `pull-solution.ps1` to capture. |
| N5 | Placeholder page parameterization mechanism. | Custom page reads `area` query param from URL; renders the per-area message. |

## References

- [`ui-styling.md`](ui-styling.md) -- CSS variables, palette, logo assets
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) §52, §57, §61, §62
- [`../decisions/phase-7-ui-design.md`](../decisions/phase-7-ui-design.md) Phase 7A deliverables
- [`data-model.md`](data-model.md) -- entity backing of each "Live in 7A shell" item
- [`security-model.md`](security-model.md) -- 7 finance-specific roles driving group visibility
- Microsoft Learn -- Create and edit the sitemap for a model-driven app: <https://learn.microsoft.com/power-apps/maker/model-driven-apps/create-edit-app-siteMap>

## Change history

| Date | Change | Source |
|---|---|---|
| 2026-05-21 | Initial draft. 8 top-level groups, accounting-workflow-first. Live-in-7A items pinned (JE list, fiscal periods, COA, entities, fiscal calendar, account types/categories). Placeholder pattern defined. Recent items / global search / breadcrumbs all defer to platform built-ins for 7A. Persona visibility deferred to 7B. | Phase 7A Session S3 |
