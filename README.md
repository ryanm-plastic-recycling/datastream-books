# Datastream Books

> Internal finance and accounting application built on Microsoft Dataverse.
> Replacement for the legacy Macola accounting system.

## Project Identity

- **Internal name:** Datastream Books
- **Purpose:** Replace Macola accounting with an internally-built finance application on Microsoft Dataverse
- **Strategic context:** Advances The Lighthouse IT modernization strategy
- **Companion system:** Datastream ERP (operations)
- **Finance System Owner:** Pam
- **Executive Sponsor:** President
- **Technical Lead:** ryanm@plastic-recycling.net
- **Technical Strategic Lead:** Ryan M (engineering + strategy + maintenance; see [§72](docs/decisions/datastream-books-decisions.md))

## Why This Exists

Macola is being deprecated by the vendor in calendar year 2026. Vendor ERP/finance replacements quoted at $300K–$1.7M are outside budget. Microsoft Business Central is viable at ~$100/user/month plus AP automation add-ons.

Datastream Books leverages our existing Power Platform investment, mirrors our proven Datastream ERP pattern, and enables AI-driven document discrepancy detection that could meaningfully reduce manual accounting work.

See `docs/memos/president-memo.md` for the full decision rationale.

## Platform

| Component | Technology |
|---|---|
| Core platform | Microsoft Dataverse (model-driven app) |
| Immutable ledger / reporting | Azure SQL |
| Authentication | Microsoft Entra ID |
| Document storage | SharePoint |
| Email | Microsoft Graph API |
| AP payment execution (v1) | NACHA file generation |
| 1099 filing | Track1099 |
| Document AI (Phase 2) | Anthropic Claude API |
| CI/CD | GitHub Actions + Power Platform Build Tools |
| Dev tooling | Power Platform CLI (`pac`) |

## Environments

| Environment | Type | URL | Purpose |
|---|---|---|---|
| PRI-Books | Production (Managed) | books.crm.dynamics.com | Production destination |
| PRI-Books-Dev | Sandbox (Unmanaged) | booksdev.crm.dynamics.com | Active development |

A `PRI-Books-Test` sandbox will be added between Dev and Prod before finance team UAT begins.

## Repository Structure

```
datastream-books/
├── README.md                     ← You are here
├── AGENTS.md                     ← Instructions for AI coding agents
├── CLAUDE.md                     ← Claude-specific extensions to AGENTS.md
├── .gitignore
├── .github/
│   └── workflows/                ← GitHub Actions CI/CD
├── docs/                         ← All project documentation
│   ├── decisions/                ← Living decision log
│   ├── memos/                    ← Stakeholder documents
│   ├── architecture/             ← Data model, security, immutability design
│   ├── controls/                 ← SoD matrix, approval policies, audit controls
│   ├── runbooks/                 ← Operational procedures
│   ├── user-guides/              ← End-user documentation
│   └── reference/                ← Reference data (COA, fiscal calendar)
├── solution/                     ← Dataverse unpacked solution
├── plugins/                      ← C# plugin source
├── pcf/                          ← PCF custom controls
├── pages/                        ← Custom pages (React)
├── azure-sql/                    ← Azure SQL schema and migrations
├── ai-services/                  ← Phase 2: document AI integration
├── scripts/                      ← PowerShell setup and deployment helpers
└── tests/                        ← Integration tests and test data
```

See `docs/repo-structure.md` for full layout details.

## Getting Started

### Prerequisites

- Windows 10/11 with PowerShell
- .NET SDK 8
- Node.js LTS (for PCF controls)
- Power Platform CLI (`pac`) — install:
  ```powershell
  dotnet tool install --global Microsoft.PowerApps.CLI.Tool
  ```
- GitHub Desktop or git CLI
- Visual Studio 2022 or VS Code

### Authenticate to Dataverse

```powershell
pac auth create --name pri-books-dev --environment https://booksdev.crm.dynamics.com
pac auth list
```

### Pull the Solution from Dev

```powershell
.\scripts\pull-solution.ps1
```

### Run Tests

```powershell
.\scripts\run-tests.ps1
```

### Deploy to Production

Deployments go through GitHub Actions. Do not deploy manually to `pri-books`.

## Key Documents

| Document | Purpose |
|---|---|
| `docs/roadmap.md` | Current phase, completed phases, future phases — read this first for state |
| `docs/decisions/datastream-books-decisions.md` | Living project decision log |
| `docs/memos/president-memo.md` | Executive decision memo |
| `docs/memos/executive-questionnaire.md` | Outstanding items requiring executive input |
| `docs/repo-structure.md` | Full repo structure reference |
| `AGENTS.md` | Instructions for AI coding agents |
| `CLAUDE.md` | Claude-specific instructions |

## Project Status

For current focus, completed work, and future phases, see **[`docs/roadmap.md`](docs/roadmap.md)** — the single source of truth for project state.

**Target cutover:** Fiscal year-end (date TBD per executive questionnaire)
**Estimated total timeline:** 10–14 months

## Contributing

This is an internal project. Changes flow:

1. Active development in `PRI-Books-Dev` (unmanaged sandbox)
2. Commit to feature branch in this repo
3. PR to `main`
4. Merge triggers GitHub Actions deployment
5. Production deployment requires manual approval

All changes that affect finance behavior require Pam's signoff via the in-app Change Request workflow.

## Contact

- **Finance System Owner:** Pam
- **Technical Lead:** ryanm@plastic-recycling.net
- **Executive Sponsor:** President

For issues, use the in-app Change Request system once deployed. Until then, contact the Technical Lead.

## License

Internal use only. Not licensed for external distribution.
