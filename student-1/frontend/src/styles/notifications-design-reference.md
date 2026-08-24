# Notifications UI — design reference

Source: claude.ai/design project `5afc3379-890a-4fb9-8e8d-516a388c619a`,
`Notifications.dc.html`. Tokens live in [`neobrutalism.scss`](./neobrutalism.scss).
Not yet built — this is the structural reference for Prompts 14/15.

## Shared chrome

- Top bar (68px, `border-bottom: 4px solid ink`): logo mark + "COURSEHUB" +
  "STUDENT DASHBOARD" mono badge on the left; nav links + bell icon button on
  the right. Bell shows an orange badge with `unreadCount` when > 0.
- Tab strip below the top bar: 4 tabs (`01 CENTRE`, `02 LIST`, `03 PREFS`,
  `04 DIGEST`), active tab filled ink/bg-inverted, others outlined.
- Notification "type" taxonomy used across all screens: `deadline` (orange),
  `grade` (yellow), `automation` (bg/ink), `account` (ink/bg-inverted),
  `ai` (white/ink). Unread rows get a 6px orange left border and white
  background; read rows get a transparent left border and bg background.

## Screen 1 — Notification centre (dropdown)

- Right-aligned panel, fixed width (~460px), `.nb-panel` (4px border + hard
  shadow).
- Header row: "NOTIFICATIONS" title + "MARK ALL READ" button (ink fill).
- List of up to 5 most recent notifications, each row: tag chip + timestamp
  on top, message below. Bold weight when unread.
- Footer: full-width "VIEW ALL NOTIFICATIONS →" button, routes to Screen 2.
- Component boundaries: `NotificationBell` (trigger + badge) →
  `NotificationCentreDropdown` (panel) → `NotificationRow` (shared with
  Screen 2's list, different layout variant).

## Screen 2 — Full notification list with filters

- Header row: "ALL NOTIFICATIONS" title + `{count} SHOWN` mono meta.
- Filter chip row: "ALL" + one chip per notification type, toggled
  independently; active chip is ink-filled.
- Row list, grid layout `120px 1fr 140px 130px` = tag | message+course | time
  | action. Unread rows show a "MARK READ" button; read rows show a "✓ READ"
  mono label.
- Component boundaries: `NotificationFilterChips`, `NotificationListRow`
  (reuses `NotificationRow` core, adds action column).

## Screen 3 — Preferences panel

- Title + one-line description.
- Table: header row (`TYPE` / `IN-APP` / `EMAIL`) with 4px top/bottom border,
  then one row per notification type (5 total: deadline, grade, automation,
  account, ai). Each row: label + description on the left, an ON/OFF
  segmented toggle per channel (in-app, email) on the right — toggle is a
  bordered two-cell control, active cell filled orange.
- Component boundaries: `PreferencesTable`, `ChannelToggle` (reusable
  segmented ON/OFF control, two instances per row).

## Screen 4 — AI digest

- "AI DIGEST" title.
- Digest card (`.nb-panel`, padded): "WEEKLY SUMMARY" heading + description,
  "GENERATE DIGEST" button (orange fill, label swaps to "GENERATING…" while
  pending). When a digest exists, an inset block appears below with an
  ink-filled "AI-GENERATED CONTENT — VERIFY BEFORE ACTING" disclaimer header
  and the digest text.
- "PAST DIGESTS" list below the card: date (mono, fixed width) + summary
  text per row, newest first.
- Component boundaries: `DigestCard` (generate button + disclaimer + result),
  `DigestHistoryList`.

## State shape (from the mockup's `Component` class)

- `notifications[]`: `{ id, type, course, message, time, unread }`.
- `activeFilters`: `{ [type]: boolean }`, all true by default.
- `prefs`: `{ [type]: { inApp: boolean, email: boolean } }`.
- `digestText`, `generating`, `digestHistory[]`: `{ id, date, summary }`.
