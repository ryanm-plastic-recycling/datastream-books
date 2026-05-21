# UI Styling -- Visual Identity for Datastream Books

> Captured during Phase 7A (UI Foundation), Session S2, 2026-05-21.
> Source-of-truth document for color palette, logo, typography, and
> visual conventions Datastream Books uses. Originally framed as
> "match ERP" per decisions [§49](../decisions/datastream-books-decisions.md)
> and [§59](../decisions/datastream-books-decisions.md); investigation
> showed ERP has no custom theme (see "Headline Finding" below), so
> the operative rule is now "Books defines its own minimal theme,
> editable by Pam / accounting via CSS variables, with logo continuity
> to ERP." [§49 amendment 2026-05-21]

## Core Principle

**Every color, font, and density value is a CSS variable with a
documented default.** Pam, the accounting team, or a future operator
can rebind any value without code edits -- variables resolve at theme
load time. No hard-coded hex literals in PCF / page TypeScript /
custom-page React.

## Scope

This document captures what was extracted from the **PRI-Datastream
(ERP)** environment on 2026-05-21 and the visual identity Books has
adopted. It pins concrete CSS-variable values for the Phase 7A app
module work (S4) and all subsequent PCF / custom page work.

Out of scope: implementation mechanics for applying the theme to the
Books app module (S4). Out of scope: deep deviations from Fluent UI v9
beyond what is documented here.

## Headline Finding

The ERP app module **"Datastream - Operations"** (`rm_Dev`) has no
custom theme override. The only app setting it explicitly sets is
`AppChannel=1`. The three legacy `theme` records in PRI-Datastream are
all stock Microsoft (CRM Default Theme, CRM Blue Theme, CRM Orange
Theme); none carries a Datastream-specific palette.

The "predominantly blue" feel decision [§49](../decisions/datastream-books-decisions.md)
references is Fluent UI v9's default brand accent leaking through the
modern Unified Interface -- not a custom Datastream choice.

Consequence: "match ERP color palette" cannot be implemented as
written, because there is no ERP palette to match beyond what
Microsoft ships. Books therefore defines its own minimal theme,
sharing the PRI logo with ERP for visual continuity, but otherwise
making independent palette decisions. The original §49 intent (sister
app visual consistency) is preserved via the logo and via using
Fluent UI v9 defaults that ERP also surfaces.

## Methodology

Auth: `pac auth select --name pri-datastream` (verified -- Org ID
66a5741e-81c5-f011-95c5-6045bd06d71a).

Web API queries against
`https://datastream.crm.dynamics.com/api/data/v9.2/`:

1. `themes?$select=...` -- three legacy theme records, all
   Microsoft-shipped, no Datastream override.
2. `appmodules?$filter=uniquename eq 'rm_Dev'` -- the ERP app module
   record. Inline `appsettings` XML showed only `AppChannel=1`.
3. `webresourceset(51a84729-48b0-f011-bbd3-0022480aaebb)` -- the logo
   web resource referenced by the app module.

No mutations were made to PRI-Datastream during this session.

## ERP App Module Reference

The user-facing ERP app Books is matching:

| Field | Value |
|---|---|
| Display name | Datastream - Operations |
| Unique name | `rm_Dev` |
| App module id | `8145374b-1ec7-f011-8543-000d3a326297` |
| Solution | ERP managed (publisher `rm`) |
| Client type | 4 (Unified Interface) |
| Form factor | 1 (Desktop primary) |
| Navigation type | 0 (modern sitemap; vertical area navigator) |
| Logo web resource | `rm_PRILogoCircle20211027` (id `51a84729-48b0-f011-bbd3-0022480aaebb`) |
| App settings explicitly set | `AppChannel=1` only |
| Last published | 2025-12-16T10:53:08Z |

## Legacy Theme Records (audit-only)

These three records exist in PRI-Datastream but do not drive the
modern Unified Interface beyond residual legacy surfaces. Captured
for completeness.

| Theme | `isdefaulttheme` | `navbarbackgroundcolor` | `headercolor` | `globallinkcolor` | `accentcolor` | `maincolor` |
|---|---|---|---|---|---|---|
| CRM Default Theme | **true** | `#000000` | `#1160B7` | `#1160B7` | `#E83D0F` | `#3B79B7` |
| CRM Blue Theme | false | `#0078D7` | `#0078D7` | `#1160B7` | `#E83D0F` | `#3B79B7` |
| CRM Orange Theme | false | `#D83B00` | `#D83B00` | `#9C2900` | `#E83D0F` | `#3B79B7` |

None have `_logoid_value` set -- the logo is on the app module, not
the theme.

## Logo Assets

Books needs two logos:

| Logo | Source | Web resource name in Books solution |
|---|---|---|
| **PRI corporate logo** | Existing -- `rm_PRILogoCircle20211027` in PRI-Datastream ERP, PNG 182x185, 12,288 bytes. To be exported and re-packaged into Books solution. | `rm_DatastreamBooksLogo` |
| **Innovation Team logo** | New asset, not yet sourced. Placeholder web resource name reserved for S4 to populate. | `rm_InnovationTeamLogo` |

**Cross-environment constraint:** PRI-Books-Dev is a different
Dataverse environment than PRI-Datastream. Web resources cannot be
cross-referenced across environments (in contrast to within-environment
table cross-solution lookups per
[`erp-pattern-notes.md`](erp-pattern-notes.md) §3). The PRI logo
binary therefore must be exported from ERP and re-packaged inside the
Books solution. S4 handles the export + packaging.

Asset hashes (to be captured during S4):

| Asset | SHA-256 | Size |
|---|---|---|
| PRI corporate logo (source from ERP) | TBD during S4 binary export | 12,288 bytes (pre-export estimate; confirm at export) |
| Innovation Team logo | TBD when sourced | TBD |

## CSS Variable Definitions

These variables are bound at theme load time. Files consuming them:

- **PCF controls** -- via `:host { --token: value; }` in the control's
  CSS, with values that pull from the document root.
- **Custom pages** -- via `:root { --token: value; }` in shared CSS
  module imported by every page.
- **Model-driven app theme record** -- the Books `theme` record (see
  "Books Theme Record" section below) binds a subset of these into the
  legacy theme surface for cases where the legacy theme still shows.

### Brand palette

```css
:root {
  /* Primary brand blue. Default is Fluent UI v9 brand blue.
     Editable -- Pam or accounting can rebind to a specific
     Datastream blue when one is selected. */
  --brand-primary: #0078D4;
  --brand-primary-hover: #106EBE;
  --brand-primary-pressed: #005A9E;

  /* Secondary blue. Retains the ERP legacy headercolor value for
     residual legacy-surface continuity. */
  --brand-secondary: #1160B7;

  /* Accent. RESERVED for warnings / high-attention finance states
     only: variance flags, period-lock warnings, audit-fail banners.
     Do NOT use on routine UI elements. */
  --brand-accent: #E83D0F;

  /* Process-positive. Used for posted JE confirmations, successful
     period-close attestations, "passed" reconciliation flags. */
  --brand-process-positive: #358717;
}
```

### Neutral palette

```css
:root {
  --surface-page: #FFFFFF;
  --surface-app-bar: #F5F5F5;
  --surface-page-header: #E0E0E0;
  --border-default: #BDC3C7;
  --text-primary: #242424;
  --text-secondary: #616161;
  --text-disabled: #A19F9D;
}
```

### Status pill palette

Driven by the JE status enum
(`rm_journalentry.rm_status`, values 261910000-261910005 per
[`data-model.md`](data-model.md)). Each status pill is two variables
(background and foreground). All editable.

```css
:root {
  --status-draft-bg:           #605E5C;
  --status-draft-fg:           #FFFFFF;

  --status-pendingapproval-bg: #F2C811;
  --status-pendingapproval-fg: #242424;

  --status-approved-bg:        #0078D4;
  --status-approved-fg:        #FFFFFF;

  --status-posted-bg:          #358717;
  --status-posted-fg:          #FFFFFF;

  --status-reversed-bg:        #8764B8;
  --status-reversed-fg:        #FFFFFF;

  --status-voided-bg:          #A4262C;
  --status-voided-fg:          #FFFFFF;
}
```

Rationale, in case anyone rebinds these later:

- **Posted = process-positive green.** Same value as
  `--brand-process-positive`. A user who also touches ERP sees the
  same "good final state" signal.
- **Voided = red.** Only red in the palette; reserved for "this is
  dead." Do not introduce another red for a non-terminal state.
- **PendingApproval = yellow with dark text.** Yellow indicates
  mid-process / waiting; dark text on yellow meets WCAG AA contrast
  (4.5:1 minimum for body text).
- **Reversed = purple.** Terminal but neutral -- not bad, but no
  longer current. Distinguishes from Voided.
- **Draft = neutral gray.** Pre-workflow, not actively in motion.
- **Approved = brand-primary blue.** Matches "ready to act" /
  "ready to post" semantic of brand-primary.

### Typography

```css
:root {
  --font-family-base:
    'Segoe UI Variable',
    'Segoe UI',
    system-ui,
    -apple-system,
    sans-serif;
  --font-size-base: 14px;          /* Fluent UI v9 default */

  /* Tabular nums -- CRITICAL for finance. Aligns decimal columns
     in grids. Every PCF / page that displays money applies
     font-variant-numeric: tabular-nums on the numeric column. */
  --font-variant-numeric-amounts: tabular-nums;
}
```

Heading scale follows Fluent UI v9 presets (`Title3`, `Subtitle1`,
`Body1`, `Caption1`). Do not invent custom heading sizes per
[§60](../decisions/datastream-books-decisions.md).

### Density and layout

```css
:root {
  --layout-min-width: 1280px;       /* Desktop-only per §51 */
  --layout-max-content-width: 1440px;
  --density-default: comfortable;    /* Fluent UI v9 default;
                                         compact opt-in per surface */
}
```

## Books Theme Record

A `theme` record will be created in the Datastream Books solution
during S4. Minimal binding -- just the values the legacy theme entity
exposes that still leak into Unified Interface surfaces. Names below
are the legacy theme entity columns:

| Theme column | Value | Source CSS variable |
|---|---|---|
| `name` | "Datastream Books" | -- |
| `isdefaulttheme` | true (for Books solution) | -- |
| `navbarbackgroundcolor` | `#0078D4` (brand-primary) | `--brand-primary` |
| `navbarshelfcolor` | `#FFFFFF` (surface-page) | `--surface-page` |
| `headercolor` | `#1160B7` (brand-secondary) | `--brand-secondary` |
| `globallinkcolor` | `#0078D4` (brand-primary) | `--brand-primary` |
| `hoverlinkeffect` | `#E7EFF7` | (light brand-primary tint) |
| `selectedlinkeffect` | `#F8FAFC` | (very light tint) |
| `processcontrolcolor` | `#358717` (brand-process-positive) | `--brand-process-positive` |
| `accentcolor` | `#E83D0F` (brand-accent) | `--brand-accent` |
| `maincolor` | `#0078D4` (brand-primary) | `--brand-primary` |
| `controlborder` | `#BDC3C7` (border-default) | `--border-default` |
| `controlshade` | `#FFFFFF` | `--surface-page` |
| `pageheaderbackgroundcolor` | `#E0E0E0` (surface-page-header) | `--surface-page-header` |
| `backgroundcolor` | `#FFFFFF` (surface-page) | `--surface-page` |
| `defaultentitycolor` | `#666666` | -- |
| `defaultcustomentitycolor` | `#0078D4` (brand-primary) | `--brand-primary` |
| `logoid` | reference to `rm_DatastreamBooksLogo` web resource | (resource id) |
| `logotooltip` | "Datastream Books" | -- |

Books theme is **not** a Microsoft-shipped theme, so it will not appear
on the platform alongside the three stock CRM themes -- it is
solution-owned.

If/when Pam (or an accounting team operator with the System
Administrator role) wants to rebind a value, the path is either:

1. **Quick fix** -- edit the theme record in the maker portal (Settings
   > Customizations > Themes), set as default. Affects legacy
   surfaces.
2. **CSS variable** -- edit the variable default in the shared CSS
   file in Books solution; rebuild the affected PCF / custom page;
   redeploy via CI/CD. Affects PCF / custom-page surfaces.

For comprehensive rebind, do both.

## Modern UI Visual Reality

What an end user sees in Datastream Books in Unified Interface
(approximation -- subject to refinement during S4):

| Surface | Look |
|---|---|
| Top app-switcher bar | White / very light gray, Microsoft Fluent default |
| App brand bar | `--surface-app-bar` (`#F5F5F5`); leftmost shows the PRI logo, then "Datastream Books" name. Innovation Team logo placement TBD in S4. |
| Sitemap (left nav) | Light gray; selected area uses `--brand-primary` (`#0078D4`) accent |
| Form body | `--surface-page` (`#FFFFFF`); section headers in `--text-primary` |
| Form headers | Subtle gray separator, `--brand-primary` (`#0078D4`) tab/section accent |
| Command bar | White, Fluent icons, `--brand-primary` on primary buttons |
| Buttons (primary) | `--brand-primary` fill, white text |
| Buttons (secondary) | White fill, `--brand-primary` text, `--brand-primary` border |
| Links | `--brand-primary` |
| Tables / lists | White; subtle row hover; selection in `--brand-primary` tint |
| Status pills | Per status pill palette above |
| Warning / variance banner | `--brand-accent` (`#E83D0F`) -- RESERVED USE |
| Process-positive banner | `--brand-process-positive` (`#358717`) |

## Header / Logo Placement

ERP places the logo leftmost in the app brand bar, immediately left
of the app name. Books mirrors:

- PRI logo: leftmost in the app brand bar, 24x24 (Fluent UI default
  brand-bar icon size; source PNG 182x185 scaled by platform).
- App name: "Datastream Books", immediately right of the PRI logo.
- App switcher / search / settings / help icons continue to the right,
  platform-controlled.
- **Innovation Team logo placement: TBD in S4.** Candidates: in the
  page footer; on the dashboard as a corner watermark; in the welcome
  card on first login. Decision deferred to S4.

## Resolved Follow-ups (formerly O1-O5)

| # | Question | Resolution |
|---|---|---|
| O1 | Books theme record yes or no? | **Yes.** Minimal, binds CSS variables + logo. Specified in "Books Theme Record" section above. |
| O2 | Lint `--brand-accent` warnings-only, or doc-only? | **Doc-only for 7A.** Reserved-use rule documented in the variable's comment. If accent-color drift surfaces in CR triage during 7B / 7C, add a lint rule then. |
| O3 | Ask Pam about status palette before 7B? | **No.** Per [§57](../decisions/datastream-books-decisions.md), no initial design sign-off. Pam reacts via CR after pages land. Palette is variable-bound -- rebind costs nothing if she does file a CR. |
| O4 | Books logo name? | **`rm_DatastreamBooksLogo`** for the PRI corporate logo. **`rm_InnovationTeamLogo`** placeholder for the new Innovation Team logo, sourced and packaged in S4. |
| O5 | Logo SHA-256 in this doc? | **Capture during S4** when binaries land on disk. Placeholder row exists in "Logo Assets" section -- S4 populates. |

## References

- [`../decisions/datastream-books-decisions.md`](../decisions/datastream-books-decisions.md) §49 (visual identity -- amended 2026-05-21 per the headline finding above), §59 (ERP as primary reference), §60 (Fluent UI v9 + minimal custom layer), §62 (Pam's navigation pain point), §51 (desktop-only v1), §57 (no initial design sign-off)
- [`../decisions/phase-7-ui-design.md`](../decisions/phase-7-ui-design.md) Phase 7A deliverables
- [`erp-pattern-notes.md`](erp-pattern-notes.md) §3 (cross-solution reference, contrast with cross-environment constraint)
- [`data-model.md`](data-model.md) (`rm_journalentry.rm_status` enum driving status pill palette)
- Microsoft Learn -- Themes for model-driven apps: <https://learn.microsoft.com/power-apps/maker/model-driven-apps/create-themes-organization-branding>
- Fluent UI v9 design tokens (consulted for default brand values, not committed to a frozen version)

## Change history

| Date | Change | Source |
|---|---|---|
| 2026-05-21 | Initial draft. Values extracted from PRI-Datastream live via Web API; finding that ERP has no custom theme reframed the doc from "match ERP" to "Books defines its own minimal theme + logo continuity." All colors converted to CSS variables. Status pill palette pinned. Two logo assets reserved (`rm_DatastreamBooksLogo`, `rm_InnovationTeamLogo`). Books theme record specified. | Phase 7A Session S2 |
