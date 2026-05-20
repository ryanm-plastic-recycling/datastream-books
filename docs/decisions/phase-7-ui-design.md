# Phase 7: UI/UX Front-End Build

> Planning artifact. **No code is written until this phase opens.** Phase 7
> is dormant until all backend work (current Phase 6B through the expected
> Phase 11/12+ backend items) is complete and Pam has accepted the cutover
> ledger state.
>
> Numbering note: this document and the roadmap both use "Phase 7" as the
> internal label for the front-end build, parallel to the backend "Phase 7
> (Vendor/Customer Integration)" label already in the roadmap. The two
> numbering tracks are distinguished as **Phase 7 (Backend Track)** and
> **Phase 7 (UI Track)**. This document is the UI track.

## Project context

Datastream Books front-end is a hybrid: a **Dataverse model-driven app** for
list views, simple forms, admin screens, and master-data maintenance, plus
**custom React pages** for the screens where the default model-driven UI is
genuinely painful — JE entry with hybrid Excel-grid + form modes, financial
reports with universal drill-down, the multi-entity dashboard, and a few
specialty screens. Strict sequential after backend phases complete.

IT (Ryan) decides design direction without sign-off; Pam (Finance System
Owner) exercises ownership via the **Change Request system** once pages
land in dev. Pam does **NOT** review screens during construction — she
reacts after pages are usable. This matches the validated Owner framing
in [`datastream-books-decisions.md`](datastream-books-decisions.md) and
the existing CR design (multi-image attachment support per decision §31).

## UX decisions (cross-referenced to decision log)

| # | Topic | Summary |
|---|---|---|
| [§46](datastream-books-decisions.md) | UI architecture | Dataverse model-driven app + custom React pages for high-value screens |
| [§47](datastream-books-decisions.md) | Personas | 5 base finance personas; UI is role-aware with persona-specific widgets and defaults |
| [§48](datastream-books-decisions.md) | v1 screens | All 8 v1-priority screens included; partial cutover not viable |
| [§49](datastream-books-decisions.md) | Visual identity | Match Datastream ERP color palette (blue) + corner logo; competitor finance UIs for pattern inspiration |
| [§50](datastream-books-decisions.md) | JE entry | Hybrid mode — Excel-like grid for power users, form mode for clerks, same screen |
| [§51](datastream-books-decisions.md) | Mobile/tablet | Out of scope for v1; desktop only |
| [§52](datastream-books-decisions.md) | Homepage | Single shared dashboard with role-aware widgets within |
| [§53](datastream-books-decisions.md) | Notifications | In-app only for v1; email and Teams deferred |
| [§54](datastream-books-decisions.md) | Reports | All three formats equally important — on-screen, Excel, PDF |
| [§55](datastream-books-decisions.md) | Report drill-down | Universal — every figure drills to underlying transactions, for all roles |
| [§56](datastream-books-decisions.md) | Save semantics | Explicit Save button with explicit draft state. No auto-save |
| [§57](datastream-books-decisions.md) | Pam validation model | No initial design sign-off; Pam owns via CR system after pages land in dev |
| [§58](datastream-books-decisions.md) | Front-end timing | Strict sequential — UI does not begin until all backend phases complete |
| [§59](datastream-books-decisions.md) | Reference material | Datastream ERP for palette/logo; competitor finance UIs for patterns; otherwise fresh |
| [§60](datastream-books-decisions.md) | Design system | Fluent UI v9 defaults; minimal custom layer; document conventions as encountered |
| [§61](datastream-books-decisions.md) | Security roles | Finance-specific only (7 total); not aligned to ERP role structure |
| [§62](datastream-books-decisions.md) | Pam's biggest Macola pain | Navigation. Drives Phase 7A investments in global search, breadcrumbs, recent items, sitemap |

## Sub-phases

### Phase 7A: Foundation (3 weeks)

**Deliverables:**
- Sitemap design grouped by user mental model (not by Dataverse table). Reviewed against persona task flows for AP Clerk, AR Clerk, Approver, Controller, Casual Contributor.
- Global search PCF control — searches across JEs, bills, invoices, customers, vendors, accounts. Surfaces in the model-driven app shell. Addresses decision §62 (navigation pain).
- Breadcrumb component — visible on every page; clickable to ancestor pages.
- Recent items widget — last N items the current user touched, shown on the homepage and as a header dropdown.
- Role-aware homepage shell — single shared dashboard, widgets selected by current user's role(s).
- Datastream ERP visual styling extraction — color hex values, font choices, logo placement, header pattern. Captured as a styling note in this document or a sibling.
- Finance-specific security role scaffolding — the 7 Dataverse security roles per decision §61 created in PRI-Books-Dev (empty privileges, populated as later sub-phases attach pages).

**Exit criteria:**
- App shell + homepage navigable as each persona.
- Global search resolves a JE number, a vendor name, and an account number in <500ms.
- Breadcrumbs render on every model-driven app page.

### Phase 7B: Core Transactional Screens (4-5 weeks)

**Deliverables:**
- JE entry / edit / post — hybrid Excel-grid mode and form mode on the same screen. Power-user grid supports row paste, keyboard-only navigation, copy-line, formula-aware totals. Form mode is the clerk path: one line at a time, account lookup with picker, validation messages inline.
- AP bill entry — vendor lookup, line entry, GL distribution, attachment drag-drop.
- AP approvals queue — list of bills awaiting current user's approval, with approve/reject actions and full audit trail visible.
- AR invoice entry — customer lookup, line entry, GL distribution, attachment drag-drop.
- AR receipt entry — receipt against open invoices with auto-allocation and partial-payment support.
- Approval queue widget on homepage — counts and links to pending items for current user.

**Exit criteria:**
- AP Clerk persona can enter, route, and post a bill end-to-end against backend Phase 8 (AP/AR Core) endpoints.
- AR Clerk persona can enter and post an invoice and a receipt.
- All transactional screens honor decision §56 (explicit Save / explicit Submit-for-Approval; no auto-save).

### Phase 7C: Reports (3-4 weeks)

**Deliverables:**
- Balance Sheet — on-screen render, Excel export, PDF export. Universal drill-down per decision §55 (every figure clickable → transaction list).
- Income Statement (P&L) — same three formats, same drill-down.
- Cash Flow Statement — same three formats, same drill-down.
- Trial Balance — on-screen + Excel + PDF.
- Aging reports (AR Aging, AP Aging) — on-screen + Excel + PDF.
- Architectural decisions made at Phase 7C kickoff (currently open):
  - Drill-down implementation pattern: live queries vs cached aggregates. Decision driven by performance under multi-entity consolidated loads.
  - Excel export pipeline: XLSX with formatting (sheets per report section, frozen panes, currency formatting, totals rows in bold).
  - PDF export pipeline: server-side render with pixel-stable layout for external sharing.

**Exit criteria:**
- All five reports render on-screen, export to Excel, and export to PDF.
- Drill-down from any figure resolves to the underlying transactions list in <1s for current-period reports.
- Closed-period reports honor the [`immutability-design.md`](../architecture/immutability-design.md) §G `ReportSnapshots` mechanism — drill-down on closed periods reads from snapshot, not live.

### Phase 7D: Specialty Screens (2-3 weeks)

**Deliverables:**
- Period close + Trial Balance review — flow that walks Controller through pre-close TB review, approval, attestation hash write, and period status flip.
- Multi-entity dashboard — consolidated view across entities with entity selector and inter-company elimination preview.
- Change Request management screens — list, detail, attachment drag-drop, image paste, approval flow. Honors the existing CR design (multi-image attachments, timeline/notes).
- Vendor / Customer master maintenance — list, detail, edit forms. Customer master is read-from-ERP per the existing ERP integration pattern (decision §22 + Phase 7-Backend); vendor master is Books-owned.

**Exit criteria:**
- Controller can run period close end-to-end through the UI.
- President can open the multi-entity dashboard and see consolidated BS without IT assistance.

### Phase 7E: Refinement and CR Burn-down (2-3 weeks)

**Deliverables:**
- CR triage workflow established by Pam (and clerks once given access) starting at the close of Phase 7B, continuing through 7C/7D.
- Phase 7E is a dedicated CR burn-down — no new feature scope; only CR-driven changes against pages already in dev.
- Sweep across all v1 screens to address:
  - Inconsistencies between screens (terminology, button placement, save behavior).
  - Performance issues surfaced by Pam / clerks during real usage.
  - Documentation gaps (every screen has at least one TalentLMS module by end of Phase 7E).

**Exit criteria:**
- Open CRs against Phase 7A-7D pages either implemented or explicitly deferred to Phase 8 with Pam's signoff.
- All screens documented in TalentLMS with Scribe walkthroughs.

### Phase 7F: UAT (2-3 weeks calendar)

**Deliverables:**
- UAT test plan authored by Pam (with IT support).
- Persona-specific UAT scripts:
  - AP Clerk: enter, route, approve, post a bill; reconcile a payment.
  - AR Clerk: enter, post an invoice; apply a receipt; review aging.
  - Approver: review and approve a queue of pending bills.
  - Controller (Pam): period close, TB review, multi-entity dashboard, BS/P&L/CF reports with drill-down.
  - Casual Contributor: enter a Change Request with an attached screenshot.
- Defect log triaged daily with Pam; severity-classified (Blocker / Major / Minor / Cosmetic).
- Final signoff from Pam (per [`datastream-books-decisions.md`](datastream-books-decisions.md) "Test Acceptance Form Structure").

**Exit criteria:**
- All Blocker and Major defects resolved.
- Pam signed off on UAT.
- Open Minor/Cosmetic defects either implemented or explicitly deferred to Phase 8 with Pam's signoff.

## Total timeline

**16-20 weeks of build + UAT.** Starts only after all backend phases (the
current backend Phase 7 Vendor/Customer through the expected Phase 11/12+
items) are complete and the cutover ledger state has been accepted.

Pam first sees UI ~7 months from project start. This trade-off was made
explicitly per decision §58 — concentrating front-end attention when it
happens vs. parallelizing build with backend. Risks captured below.

## Roles and ownership

| Role | Phase 7 responsibility |
|---|---|
| IT (Ryan) | Designs direction; builds all pages; owns sitemap, design system, security role privilege population |
| Pam (Finance System Owner) | **No design review during construction.** Uses CR system after pages land in dev. Owns UAT. Triages and prioritizes finance-side CRs. |
| AP / AR clerks | Persona-specific UAT participation in Phase 7F. May exercise pages in dev during Phase 7E refinement window. |
| President / COO / CFO | Stakeholder updates only — not design review. |

## CR workflow during Phase 7

- Pages land in dev as Phase 7 sub-phases complete (7A, 7B, 7C, 7D).
- Pam (and clerks once given access) use pages.
- File CRs via the in-app Change Request system with screenshots — honors decision §31 (multi-image attachment support).
- IT triages weekly during build (Phase 7A-7D), then daily during Phase 7E (CR burn-down) and 7F (UAT defect log).
- Phase 7E is the dedicated CR burn-down before UAT begins.

## Reference material

- **Primary visual reference:** Datastream ERP (color palette = blue, logo corner placement) — decision §49 / §59.
- **Pattern inspiration:** Business Central, NetSuite, QuickBooks, Sage Intacct where useful — decision §49 / §59.
- **Design system:** Fluent UI v9 defaults; minimal custom layer — decision §60.
- **PCF / page conventions:** [`../../AGENTS.md`](../../AGENTS.md) §Code Conventions (TypeScript / React).
- **No standalone design-system document upfront.** Conventions are documented as encountered — decision §60.

## Open architectural items needing Phase 7C kickoff design

These are deliberately deferred until Phase 7C opens so they are designed
against the actual backend ledger shape rather than ahead of it:

- **Report drill-down implementation pattern** — live queries against
  `ledger.GeneralLedgerEntries` vs. cached aggregates with on-demand
  expansion. Trade-off: live queries preserve drill-down to row-level
  transaction provenance (see [`../architecture/immutability-design.md`](../architecture/immutability-design.md) §B) but
  may be slow under multi-entity consolidated loads. Decision logged once
  benchmarked in PRI-Books-Test.
- **Excel export pipeline** — XLSX generator (server-side render). Open
  Office XML library choice (ClosedXML, EPPlus non-commercial, OpenXML
  SDK) made at Phase 7C kickoff.
- **PDF export pipeline** — server-side render. Library choice (QuestPDF,
  IronPDF, PuppeteerSharp) made at Phase 7C kickoff. Drives whether the
  pipeline is .NET-native or invokes a headless Chromium.

## Risks (cross-referenced to risk register)

See [`../risk-register.md`](../risk-register.md) for the live register.
Phase 7 risks:

| Risk | Severity | Mitigation |
|---|---|---|
| Strict sequential creates 7+ month UI invisibility | Medium | Weekly written status to Pam through Phase 6B-11; Pam's CR-based ownership model means she does not need design review during construction. Surface project status at month 1 if Pam expresses anxiety about not seeing UI. |
| Heavy CR volume in first weeks of Phase 7B | Medium | Phase 7E (CR burn-down) is built into the plan as 2-3 weeks of no-new-scope CR work. Triage weekly during 7A-7D, daily during 7E. |
| Universal report drill-down adds architectural complexity | Medium | Phase 7C kickoff has explicit decision points for live-query vs. cached-aggregate. Bench under realistic multi-entity load before committing. |
| Hybrid JE entry is the most complex screen and a long pole | Medium | Schedule JE entry as the first deliverable in Phase 7B so it has the longest runway. If 7B slips, slip 7B end-date — do not strip the hybrid mode. |
| Cutover date slips because front-end starts too late | High | Backend phases (7-Backend through 11/12+) must finish on schedule for Phase 7 to start on schedule. Backend slips compound into UI slip. Surface backend slippage to Executive Sponsor as soon as it is forecast, not after it lands. |

## Definition of Phase 7 complete

- All 8 v1 screens deployed to production (per decision §48).
- Pam signed off on UAT (per decision §54).
- Open CRs either implemented or explicitly deferred to Phase 8 with Pam's signoff.
- User documentation published in TalentLMS (per decision §27).
- Training rollout to finance team complete.

## See also

- [`datastream-books-decisions.md`](datastream-books-decisions.md) — decision log entries §46-§62 are the Phase 7 UX decisions
- [`../roadmap.md`](../roadmap.md) — phase sequencing
- [`../risk-register.md`](../risk-register.md) — live risk register
- [`../runbooks/phase-7a-foundation-prompt.md`](../runbooks/phase-7a-foundation-prompt.md) — DRAFT Claude Code prompt for the first Phase 7 session
- [`../architecture/security-model.md`](../architecture/security-model.md) — finance-specific security roles (per decision §61)
- [`../architecture/immutability-design.md`](../architecture/immutability-design.md) — report drill-down preserves row-level provenance per §B
