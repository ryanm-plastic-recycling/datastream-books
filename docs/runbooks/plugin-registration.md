# Plugin Registration — One-Time Per Plugin Assembly

> Walks through the first-time registration of a Dataverse plugin assembly
> + its message-processing steps into the DatastreamBooks solution on
> PRI-Books-Dev. Subsequent assembly updates flow via `pac plugin push`
> and the existing GitHub Actions deploy pipeline.

This is the Phase 6A registration runbook for `PostJournalEntryPlugin`,
written generically enough to be reused for future plugins.

## When to use

- **First push of a new plugin assembly into Dataverse.** `pac plugin push`
  requires an existing assembly GUID, so the very first registration must
  happen through the Plugin Registration Tool (PRT) or the maker portal.
- **First add of a step against a new entity.** Step rows
  (`SdkMessageProcessingStep`) plus PreImages are not currently authored
  by hand in `solution/src/Other/Customizations.xml`. PRT writes them
  into the Dataverse instance, then `pull-solution.ps1` captures the
  resulting XML for source control.

After the first registration:

- Code changes to the plugin assembly redeploy automatically — the CI
  workflow rebuilds the DLL and re-imports the solution, which contains
  the assembly + step XML pulled into `solution/src/`.
- New steps for the same assembly: register once via PRT, then
  `pull-solution.ps1` + commit.

## Prerequisites

- `pac auth select --name pri-books-dev` is active (`pac auth list` to verify).
- Local plugin DLL exists at:
  `plugins/DatastreamBooks.Plugins/bin/Release/net462/DatastreamBooks.Plugins.dll`
  Build it first with:
  ```powershell
  dotnet build plugins/DatastreamBooks.Plugins/DatastreamBooks.Plugins.csproj -c Release
  ```
- The DatastreamBooks solution exists in PRI-Books-Dev (it does — first
  bootstrapped in Phase 3).

## Steps

### 1. Launch the Plugin Registration Tool

```powershell
pac tool prt
```

In the PRT window:

1. Click **CREATE NEW CONNECTION**.
2. Deployment Type: `Microsoft 365`.
3. Display list of available organizations: check **Show Advanced**.
4. Select the org that maps to PRI-Books-Dev (`booksdev.crm.dynamics.com`).
5. Sign in with the same account `pac` is authenticated as.

### 2. Register the assembly (one time per assembly)

1. **Register → Register New Assembly** (or `Ctrl+A`).
2. Load Assembly: browse to
   `plugins/DatastreamBooks.Plugins/bin/Release/net462/DatastreamBooks.Plugins.dll`.
3. The dialog lists the plugin classes it found — confirm
   `PostJournalEntryPlugin` is present.
4. Isolation Mode: **Sandbox** (required for Dataverse online).
5. Location: **Database**.
6. Click **Register Selected Plugins**.

PRT will show the new assembly under `(Assembly) DatastreamBooks.Plugins`
with one plugin type beneath it: `DatastreamBooks.Plugins.Posting.PostJournalEntryPlugin`.

### 3. Add the assembly to the DatastreamBooks solution

In the maker portal or PRT's solution affinity step, ensure the new
assembly is added to the `DatastreamBooks` solution:

1. Power Apps maker portal → Solutions → DatastreamBooks → **Add existing → Plug-in assembly** → pick `DatastreamBooks.Plugins`.

This is what makes the assembly travel with the solution on export.

### 4. Register the 8 message-processing steps

Right-click the plugin type → **Register New Step** for each row below.

All steps:
- **Plugin**: `DatastreamBooks.Plugins.Posting.PostJournalEntryPlugin`
- **Execution Mode**: Synchronous
- **Deployment**: Server
- **Event Pipeline**: see "Stage" column
- **Run in user context**: Calling User
- **Solution**: DatastreamBooks
- **Description**: (use the row's intent)

| # | Message | Primary Entity | Stage         | Filtering Attrs                                            | Pre-Image (`PreImage`) cols                                | Purpose                                       |
|---|---------|---------------|---------------|------------------------------------------------------------|------------------------------------------------------------|-----------------------------------------------|
| 2 | Update  | rm_journalentry      | PreOperation  | `rm_status`, `rm_journaldescription`, `rm_totaldebit`, `rm_totalcredit`, `rm_approvedby_user`, `rm_fiscalperiod` | `rm_status`, `rm_createdby_user`, `rm_approvedby_user`, `rm_totaldebit`, `rm_totalcredit`, `rm_fiscalperiod`, `rm_entity`, `rm_journalentrynumber` | Status-transition guard + SoD + period check + immutability |
| 3 | Create  | rm_journalentryline  | PreOperation  | *(none)*                                                   | n/a                                                        | Block writes against locked headers           |
| 4 | Update  | rm_journalentryline  | PreOperation  | `rm_debit`, `rm_credit`, `rm_account`                      | `rm_journalentry`                                          | Block writes against locked headers           |
| 5 | Delete  | rm_journalentryline  | PreOperation  | *(none — Delete has no filter)*                            | `rm_journalentry`                                          | Block deletes against locked headers          |
| 6 | Create  | rm_journalentryline  | PostOperation | *(none)*                                                   | n/a                                                        | Recompute header totals                       |
| 7 | Update  | rm_journalentryline  | PostOperation | `rm_debit`, `rm_credit`                                    | `rm_journalentry`                                          | Recompute header totals                       |
| 8 | Delete  | rm_journalentryline  | PostOperation | *(none)*                                                   | `rm_journalentry`                                          | Recompute header totals                       |
| 9 | Delete  | rm_journalentry      | PreOperation  | *(none — fires on every create)*                           | `rm_journalentry`                                          |            |
For each step that lists a Pre-Image:

1. After saving the step, right-click the step → **Register New Image**.
2. Image Type: **PreImage** only.
3. Name and Entity Alias: **PreImage** (the literal string the plugin reads — see `PostJournalEntryPlugin.PreImageName`).
4. Parameters: tick **Pre Image**.
5. Attributes: enter the comma-separated list from the table.

### 5. Smoke test in the maker portal

Quick "did it wire up" check:

1. Power Apps → Tables → rm_journalentry → **+ New record**.
2. Pick the DEFAULT entity, fill the required fields, save as Draft.
3. Confirm `rm_journalentrynumber` was auto-stamped to `JE-DEFAULT-00000N`.
4. Add a line with rm_debit=100. Save. Reload header — `rm_totaldebit` should be 100.
5. Add a second line with rm_credit=50. Save. Reload header — `rm_totalcredit` = 50, totals not equal.
6. Try changing header status to `Approved` — should fail with the
   "out of balance" plugin error.
7. Fix the second line to rm_credit=100 (or add another credit line totalling 100). Reload header. Set the approver user lookup to a different user from the creator. Set status to Approved — should succeed.

If step 1 produces no auto-number, the Create step is not registered or
the plugin assembly didn't deploy — recheck PRT.

### 6. Pull the now-registered solution into source control

```powershell
./scripts/pull-solution.ps1
```

This re-exports DatastreamBooks from PRI-Books-Dev and unpacks it into
`solution/src/`. The unpacked tree now includes:

- `solution/src/PluginAssemblies/<assembly>/` — base-64 of the DLL plus
  metadata
- `solution/src/SdkMessageProcessingSteps/<step>/` — one per step row
- the Customizations.xml update wiring everything together

### 7. Commit + push

```powershell
git add solution/src/
git commit -m "Phase 6A: register PostJournalEntryPlugin steps in DatastreamBooks solution"
git push origin main
```

CI will now redeploy the same solution to PRI-Books-Dev with no
behavioural change — but every future code-only change to the plugin
DLL will flow through the workflow automatically because
`solution/src/PluginAssemblies/` carries the assembly + step rows.

## Why this is a runbook and not part of CI

The first-time registration creates GUIDs (assembly, plugin type, step,
image) that we don't want to invent by hand in source. PRT generates
them server-side, and `pac solution export` brings them back as
authoritative XML. Once captured, the IDs are stable and CI can
round-trip the same XML on every push.

Future plugins follow the same pattern: register once via PRT,
pull-solution, commit. New steps on an existing plugin: same pattern,
but you only register the step + image, not the assembly.

## See Also

- [`../architecture/immutability-design.md`](../architecture/immutability-design.md) §C — what the plugin enforces
- [`../controls/sod-matrix.md`](../controls/sod-matrix.md) — the SoD rule the plugin implements
- [`cicd-setup.md`](cicd-setup.md) — the deploy pipeline the registered solution flows through
- `plugins/DatastreamBooks.Plugins/Posting/PostJournalEntryPlugin.cs` — the plugin source itself, with the step registration matrix repeated at the top of the file for ground truth
