# Datastream Books — Repository Structure

This document describes the planned structure of the `datastream-books` GitHub repository.

---

## Overview

The repository is the single source of truth for:
- All design and decision documentation
- Dataverse solution components (unpacked for version control)
- Plugin source code (C#)
- PCF control source (React/TypeScript)
- Custom page source (React)
- Azure SQL schema and migrations
- AI services (Phase 2)
- CI/CD pipeline definitions
- Setup and deployment scripts

---

## Directory Layout

```
datastream-books/
├── README.md                              ← Project overview, getting started
├── .gitignore                             ← Excludes build artifacts, secrets
│
├── .github/
│   └── workflows/                         ← GitHub Actions for CI/CD
│       ├── deploy-dev.yml                 ← Auto-deploy on feature branch push (today)
│       ├── deploy-build.yml               ← FUTURE: deploy on PR merge to main (added when PRI-Books-Build sandbox is provisioned)
│       ├── deploy-test.yml                ← FUTURE: manual deploy to test env (added when PRI-Books-Test sandbox is provisioned, before finance UAT)
│       └── deploy-prod.yml                ← FUTURE: manual deploy to prod with approval (added when first production-bound code merges)
│
├── docs/
│   ├── decisions/
│   │   └── datastream-books-decisions.md  ← Living decision log
│   │
│   ├── memos/
│   │   ├── president-memo.md              ← Decision memo for president
│   │   └── executive-questionnaire.md     ← Questions for executive leadership
│   │
│   ├── architecture/
│   │   ├── data-model.md                  ← Entity-relationship documentation
│   │   ├── security-model.md              ← Roles, SoD matrix, permissions
│   │   ├── immutability-design.md         ← Append-only ledger, hash chains
│   │   ├── multi-entity-design.md         ← Multi-entity and inter-company
│   │   ├── integration-design.md          ← Datastream ERP, SharePoint, Track1099, banks
│   │   └── diagrams/                      ← Visio / draw.io / mermaid exports
│   │       ├── data-flow.svg
│   │       ├── auth-flow.svg
│   │       └── posting-flow.svg
│   │
│   ├── controls/
│   │   ├── sod-matrix.md                  ← Segregation of Duties matrix
│   │   ├── approval-policies.md           ← Approval workflows and thresholds
│   │   └── audit-controls.md              ← Audit trail and reporting
│   │
│   ├── runbooks/
│   │   ├── disaster-recovery.md           ← DR procedures
│   │   ├── change-management.md           ← How changes flow through the system
│   │   ├── period-close.md                ← Month-end close procedures
│   │   ├── year-end-close.md              ← Annual close procedures
│   │   └── data-recovery.md               ← Restore procedures
│   │
│   ├── user-guides/
│   │   ├── ap-clerk.md                    ← AP clerk daily procedures
│   │   ├── ar-clerk.md                    ← AR clerk daily procedures
│   │   ├── controller.md                  ← Controller procedures
│   │   └── admin.md                       ← System admin procedures
│   │
│   └── reference/
│       ├── chart-of-accounts.csv          ← Starter COA, finance modifies
│       ├── fiscal-calendar.csv            ← Fiscal periods setup
│       └── reference-data.md              ← Other reference data
│
├── solution/                              ← Dataverse unpacked solution
│   ├── Other/                             ← Solution.xml, customizations.xml
│   ├── Entities/                          ← Each table as its own folder
│   │   ├── GeneralLedgerEntry/
│   │   ├── JournalEntry/
│   │   ├── JournalEntryLine/
│   │   ├── ChartOfAccount/
│   │   ├── Vendor/                        ← May be virtual to ERP env
│   │   ├── Customer/                      ← May be virtual to ERP env
│   │   ├── Entity/                        ← Legal entity master
│   │   ├── FiscalPeriod/
│   │   ├── ApprovalRequest/
│   │   ├── ApprovalPolicy/
│   │   ├── ChangeRequest/
│   │   └── ...
│   ├── WebResources/                      ← JavaScript, HTML, CSS
│   ├── Workflows/                         ← Power Automate flows (exported)
│   └── Roles/                             ← Security roles
│
├── plugins/                               ← Plugin C# source
│   ├── DatastreamBooks.Plugins/
│   │   ├── Posting/                       ← Journal entry posting logic
│   │   ├── Validation/                    ← Validation plugins
│   │   ├── PeriodLock/                    ← Period close enforcement
│   │   ├── Immutability/                  ← Hash chain, ledger writes
│   │   ├── ApprovalWorkflow/              ← Approval engine
│   │   ├── ChangeManagement/              ← Change request workflow
│   │   ├── Integration/                   ← Track1099, ERP sync, etc.
│   │   └── DatastreamBooks.Plugins.csproj
│   └── DatastreamBooks.Plugins.Tests/
│       ├── PostingTests/
│       ├── ValidationTests/
│       └── DatastreamBooks.Plugins.Tests.csproj
│
├── pcf/                                   ← PCF custom controls
│   ├── JournalEntryGrid/                  ← Custom JE entry grid
│   ├── TrialBalance/                      ← TB display control
│   └── ...
│
├── pages/                                 ← Custom pages (React)
│   ├── balance-sheet/                     ← Custom BS report page
│   ├── income-statement/                  ← Custom P&L report page
│   ├── cash-flow/                         ← Custom Cash Flow page
│   ├── multi-entity-consolidation/        ← Consolidation tool
│   └── shared/                            ← Shared components
│
├── azure-sql/                             ← Azure SQL schema & migrations
│   ├── migrations/                        ← Versioned SQL scripts (Vnnnn__description.sql)
│   │   ├── V0001__initial_schema.sql
│   │   ├── V0002__ledger_table.sql
│   │   ├── V0003__hash_chain_constraints.sql
│   │   └── ...
│   ├── views/                             ← Reporting views
│   ├── procedures/                        ← Stored procedures
│   ├── functions/                         ← Functions (hash calculation, etc.)
│   ├── jobs/                              ← Hash verification, archival jobs
│   └── roles/                             ← SQL role definitions, DENY grants
│
├── ai-services/                           ← Phase 2: Document AI
│   ├── extraction/
│   │   ├── claude-document-parser.cs      ← Claude API integration
│   │   └── extraction-orchestrator.cs
│   ├── matching/
│   │   └── discrepancy-detector.cs        ← Match logic
│   ├── prompts/                           ← Versioned prompts
│   │   ├── invoice-extraction-v1.md
│   │   ├── po-extraction-v1.md
│   │   └── ...
│   └── README.md
│
├── scripts/
│   ├── setup-dev.ps1                      ← Set up local dev machine
│   ├── auth-env.ps1                       ← Authenticate pac to environment
│   ├── pull-solution.ps1                  ← Export solution from Dataverse, unpack
│   ├── push-solution.ps1                  ← Pack solution, import to Dataverse
│   ├── deploy-plugins.ps1                 ← Build + register plugin assemblies
│   ├── run-sql-migration.ps1              ← Apply pending Azure SQL migrations
│   └── verify-integrity.ps1               ← Run hash chain verification
│
└── tests/
    ├── integration/                       ← Full system integration tests
    ├── data-fixtures/                     ← Test data sets
    └── README.md
```

---

## Branching Strategy

Solo-developer project. Work directly on `main`, no branches, no
worktrees. See AGENTS.md "Branching Policy" for the operating rule.

If the project later moves to multi-developer or multi-branch
workflows, this section and AGENTS.md must be updated together. Do
not create branches under the current rule even if older revisions
of this section suggest otherwise -- AGENTS.md is authoritative.

---

## File Conventions

### Markdown
- Use semantic line breaks (one sentence per line in source)
- Use H1 for document title only
- Use ATX-style headers (`## Heading`)
- Tables for structured comparisons
- Code blocks with language identifier

### C# (Plugins)
- One plugin class per file
- Plugin name = `<Action><Entity>Plugin` (e.g., `PostJournalEntryPlugin`)
- Unit tests in parallel folder structure
- Use `IPlugin` interface; register via solution

### SQL Migrations
- File name: `V<number>__<description>.sql` (Flyway-style)
- Each migration is idempotent or transactional
- Never modify a committed migration; always add a new one
- Include rollback comments

### Solution Components
- Solution display name: `Datastream Books`
- Solution unique name: `DatastreamBooks`
- Publisher: `Ryan McCauley` (unique name `RyanMcCauley`) — shared with PRI-Datastream ERP
- Publisher prefix: `rm`
- Customization option-value prefix: `12619` (matches ERP)
- Tables and columns: all lowercase after the prefix (`rm_journalentry`, `rm_entityid`)

---

## Initial Repo Setup

To populate the repo from this structure:

```powershell
# Clone the empty repo
cd C:\Code
git clone https://github.com/<your-org>/datastream-books.git
cd datastream-books

# Create the folder structure
New-Item -ItemType Directory -Path docs/decisions, docs/memos, docs/architecture/diagrams,
  docs/controls, docs/runbooks, docs/user-guides, docs/reference,
  solution, plugins, pcf, pages, azure-sql/migrations, azure-sql/views,
  azure-sql/procedures, azure-sql/functions, azure-sql/jobs, azure-sql/roles,
  ai-services/extraction, ai-services/matching, ai-services/prompts,
  scripts, tests, .github/workflows -Force

# Copy the markdown files into the appropriate folders
Copy-Item datastream-books-decisions.md docs/decisions/
Copy-Item president-memo.md docs/memos/
Copy-Item executive-questionnaire.md docs/memos/
Copy-Item repo-structure.md docs/

# Create initial README
# (use template below or create in Claude Code)

# Commit
git add .
git commit -m "Initial repo structure and strategy documents"
git push
```

---

## What to Expect Next (Claude Code Phase)

When we move to Claude Code, the first session will:

1. Validate this repo structure
2. Create initial `README.md`
3. Initialize the Dataverse solution (`pac solution init`)
4. Define the core data model (Entity, COA, FiscalPeriod, JournalEntry, JournalEntryLine)
5. Define the Azure SQL ledger schema (GeneralLedgerEntries with hash chain)
6. Scaffold the first plugin (posting validation)
7. Set up the first GitHub Actions workflow (deploy to PRI-Books)

Each step gets committed individually with clear messages, so the entire build history is auditable.
