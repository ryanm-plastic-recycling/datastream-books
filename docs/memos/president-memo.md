# Finance System Replacement: Decision Memo

**To:** Brandon Shaw
**From:** Ryan McCauley
**Re:** Macola Replacement — Build Datastream Books vs. HoosAI or Business Central
**Date:** May 19, 2026

---

## Executive Summary

Macola is being deprecated by the vendor this calendar year. Continuing to run it unsupported is not a viable long-term position. We have two realistic paths forward:

1. **Buy:** Microsoft Dynamics 365 Business Central — vendor-supplied accounting platform
2. **Build:** Datastream Books — internally built finance application on Microsoft Dataverse, extending the proven pattern from our Datastream ERP

We recommend the **build** path. Three reasons:

1. **Lower 5-year cost** — approximately $70K–$135K savings vs. Business Central
2. **Headcount reallocation opportunity** — AI-driven document discrepancy detection can shift accounting capacity from manual validation to higher-value work
3. **Advances The Lighthouse** — post-cutover enables full decommission of our local domain controller and file server, completing our Entra ID migration

---

## Why We Are Here

- Macola end-of-support is announced for this calendar year
- Continued use is possible but exposes us to security, compliance, and operational risk with no vendor recourse
- Full-suite ERP replacements quoted at $300K–$1.7M are outside budget
- We have successfully built and deployed Datastream ERP on Microsoft Dataverse for the operations side of the business, proving the platform and our team's capability
- Estimated user base for finance: 15 (5 accountants + 10 occasional contributors)

## Option A: Business Central

### Pros
- Proven, audited GAAP-compliant accounting platform used by tens of thousands of companies
- Vendor handles compliance updates, tax tables, regulatory changes
- Built-in audit controls, period close, multi-entity, and reporting
- Implementation timeline: 4–6 months with a Microsoft partner
- External auditor acceptance is straightforward
- Reduced key-person risk

### Cons
- Licensing cost: approximately $100/user/month = $18,000/year = $90,000 over 5 years
- Limited native AI for invoice / document discrepancy detection — requires third-party AP automation add-on (Stampli, Continia, ExFlow) at $30K–$100K
- Less flexibility for custom workflows unique to our business
- Integration between BC and Datastream ERP requires ongoing maintenance
- Dependent on Microsoft's roadmap and pricing decisions
- Does not advance Lighthouse IT modernization

### 5-Year Estimated Cost
- Licensing: ~$90K
- Implementation: $50K–$150K
- AP automation add-on (required for AI document handling): $30K–$100K
- Internal effort: $30K–$50K
- **Total: ~$200K–$390K**

## Option B: Datastream Books (Internal Build)

### Pros
- **Significantly lower ongoing cost** — leverages existing Microsoft 365 licensing and Power Platform investment
- **AI-driven discrepancy detection** built natively, using best-in-class AI (Claude API) — enables headcount reallocation from manual validation
- **Unified data model with Datastream ERP** — no integration layer, no reconciliation between two systems
- **Full control** over feature roadmap, workflows, and reporting
- **Advances The Lighthouse** — cutover enables decommission of local domain controller and file server, full Entra ID migration
- Builds on proven internal capability from Datastream ERP
- Same Microsoft Entra ID authentication as the rest of the business
- AI-assisted development (Claude Code) materially reduces build time vs. traditional estimates

### Cons
- Higher execution risk — we own all defects, all controls, all compliance
- Longer time to live: 10–14 months total (vs. 4–6 for BC)
- External auditor will scrutinize controls more heavily than for a vendor system
- Key-person dependency on the internal build team
- We are responsible for tax table updates, regulatory changes, year-end forms
- Scope creep risk — finance team will request features continuously

### 5-Year Estimated Cost
- Power Apps per-app licensing: ~$5K (managed by IT)
- Dataverse capacity: $5K–$10K
- Azure SQL: $3K–$6K
- Build effort (internal labor): $80K–$150K
- Ongoing maintenance: $30K–$60K
- Track1099 (1099 filing): ~$5K
- Claude API for document AI (Phase 2): $8K–$20K
- **Total: ~$130K–$255K**

## The Strategic Value Driver: AI-Driven Document Processing

Our current accounting team is sized largely around manual cross-referencing of invoices, purchase orders, receipts, and bills against Macola records. This is necessary work, but it is also work that modern AI does well.

Datastream Books would integrate AI-driven document extraction and discrepancy detection as a Phase 2 enhancement (after the core accounting platform is live). The pattern:

- Documents enter SharePoint (existing pattern)
- AI extracts structured data — vendor, amounts, line items, dates
- System matches against POs and receipts in Datastream Books
- Clean matches auto-route through the approval workflow
- Only exceptions reach a human for review

**Outcome:** accounting team capacity shifts from manual validation to higher-value analysis, vendor management, and financial planning. This is a meaningful operational gain that Business Central does not provide natively — BC would require a separate third-party AP automation product at significant additional cost.

This is the core reason we recommend building rather than buying.

## Audit Defensibility — Built-In, Not Bolted On

A common concern with internally built financial systems is whether they can pass an external audit. Datastream Books would be designed with audit defensibility as a core architectural requirement, not an afterthought:

- **Append-only ledger** — posted transactions cannot be edited or deleted, only reversed
- **Cryptographic hash chains** on every ledger entry — any tampering is detectable
- **Server-side posting enforcement** — all controls live in code that users cannot bypass
- **Period locks** at the data layer — closed periods cannot be modified without elevated role + audit event
- **Segregation of Duties** enforced by the system, not honor code
- **Redundant audit trail** in both Dataverse and Azure SQL
- **Time-bound signed reports** — closed-period financials are hashed and pinned
- **Built-in Change Management** workflow within the app itself — every system change has a documented business reason, acceptance criteria, approver, and deployment record
- **Dev/Prod separation** — developers have zero direct production database access

This architecture is consistent with the controls auditors look for in any audited financial system.

## Secondary Strategic Benefit: The Lighthouse

A successful Datastream Books cutover triggers a series of IT modernization wins aligned to The Lighthouse strategy:

- Macola server (local SQL Server) is no longer required for active accounting
- Local file server can be decommissioned (documents in SharePoint)
- Local domain controller can be decommissioned (all auth via Entra ID)
- Headquarters becomes a true browser-only office — users can work from anywhere
- Significant reduction in on-premise infrastructure, security surface, and ongoing IT maintenance

This is not the primary justification, but it is a meaningful adjacent benefit that BC does not offer.

## Side-by-Side

| Factor | Business Central | Datastream Books |
|---|---|---|
| Time to live | 4–6 months | 10–14 months |
| 5-year total cost | $200K–$390K | $130K–$255K |
| Annual licensing | ~$18K/yr | ~$1K/yr |
| AI document processing | Add-on required ($30K+/yr) | Built-in (Phase 2) |
| Execution risk | Low | Medium-High |
| Integration with Datastream ERP | Requires connector | Native (same platform) |
| Flexibility | Constrained | Full control |
| Advances Lighthouse | No | Yes |
| Headcount reallocation potential | Limited | Strong (via AI) |

## Risk Comparison

| Risk | BC | Datastream Books |
|---|---|---|
| Cost overrun | Medium | High |
| Schedule slip | Low | Medium-High |
| Audit failure | Very Low | Medium (mitigated by control design) |
| Feature gap | Medium | Low |
| Talent loss | Low | Medium |
| Platform deprecation | Low | Low |

## Recommendation

We recommend proceeding with **Datastream Books** for the following reasons:

1. The 5-year cost advantage is material ($70K–$135K)
2. The AI discrepancy detection opportunity is a real operational gain not available natively in BC
3. The platform investment leverages our existing Datastream ERP capability
4. Cutover advances The Lighthouse strategy
5. We retain full control over the system that runs our business

We acknowledge the higher execution risk and recommend the following risk mitigations:

- Multi-entity architecture built in from day one (no retrofit)
- Audit-defensible immutability architecture by design
- Change Management workflow built into the app itself
- Parallel run with Macola before cutover, with user-driven green light
- AI document processing deferred to Phase 2, after the accounting core is proven

## Decisions Required

- [ ] Approve Datastream Books as the path forward
- [ ] Approve internal IT lead and finance SME assignments
- [ ] Authorize executive review of attached questionnaire (multi-entity structure, approval thresholds, data retention, etc.)
- [ ] Confirm budget envelope for build and ongoing operations
- [ ] Defer external auditor engagement until next regular audit cycle
- [ ] Confirm Lighthouse alignment and downstream IT modernization benefits as in scope

---

**Attached:** Executive Questionnaire (items requiring leadership input before design finalizes)

**Attached:** Executive Questionnaire (items requiring leadership input before design finalizes)
