# Datastream Books — Reference Seed Data

> Canonical reference rows that every environment needs. Tracked in source
> so they can be re-applied to any new environment (Test, Prod, or a fresh
> Dev rebuild). Each section names its loader (script) and the conditions
> under which it should be re-run.

## Account Types (`rm_accounttype`)

The five fundamental account classifications used by every double-entry
accounting system.

| Name | Code | Short | Normal Balance | Notes |
|---|---|---|---|---|
| Asset | ASSET | Asset | Debit | Resources controlled by the entity |
| Liability | LIAB | Liab | Credit | Obligations of the entity |
| Equity | EQTY | Equity | Credit | Residual interest in the assets |
| Revenue | REV | Rev | Credit | Increases in economic benefits |
| Expense | EXP | Exp | Debit | Decreases in economic benefits |

**Choice option values** (defined on `rm_accounttype.rm_normalbalance`):

| Label | Value |
|---|---|
| Debit | 261910000 |
| Credit | 261910001 |

**Loader:** `scripts/seed-accounttypes.ps1`

**When to run:**
- Once per new environment, after the `rm_accounttype` table is deployed.
- Idempotent — existing rows (matched by name) are skipped, so it's safe
  to re-run during environment recovery.

**Applied environments:**
| Environment | Applied date | Applied by |
|---|---|---|
| PRI-Books-Dev | 2026-05-19 | Phase 3 build session (Claude Code via Web API) |

When you seed a new environment, add a row above with the date.

## Future seed data

These tables will need reference-data sections here when their loaders
are built:

- `rm_entity` — actual legal entities under the corporate umbrella (Pam +
  executive questionnaire §1 must be complete first; not committed to
  source because EIN values are sensitive and the list may be PII-adjacent).
- Initial chart of accounts (`rm_chartofaccount` — Phase 4). Pam owns the
  starter list per the decision log.
- `rm_fiscalyear` / `rm_fiscalperiod` — generated programmatically (not
  static seed data) based on each entity's fiscal-year-end month/day.

## See also

- [`../../scripts/seed-accounttypes.ps1`](../../scripts/seed-accounttypes.ps1) — the loader for the Account Types section above
- [`../architecture/data-model.md`](../architecture/data-model.md) — what each of these tables looks like in Dataverse
- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md)
