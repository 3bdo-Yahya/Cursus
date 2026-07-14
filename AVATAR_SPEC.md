# Mazen emotional avatar — presets & triggers

Persistent docked face on the WebGL stage (`#avatar`). It never narrates — it only reacts to the same beats that drive the graph.

## Seven story states (locked)

| State | Halo | Brow | Eyes | Mouth | Trigger event (data / beat) |
|---|---|---|---|---|---|
| **Calm** | Indigo `#4F46E5` soft bloom | Level | Open, relaxed | Soft smile | Overview / Meet Mazen — Dashboard looks fine |
| **Worried** | Amber `#FBBF24` | Inward tilt (~12°) | Slightly narrowed | Flat / slight frown | Midterm / at-risk flag on CS211 revealed |
| **Panic** | Rose `#F43F5E` hot bloom | Strong inward tilt | Wide | Upside-down smile (distress) | Impact Analyzer cascade — 20 nodes go red |
| **Focused** | Sky `#38BDF8` | Drawn down (concentration) | Narrowed | Neutral thin line | GPA Simulator shows a reachable passing path |
| **Reassured** | Soft violet `#8B5CF6` → mint edge | Softened | Open | Small smile | AI Advisor returns a concrete, numbers-grounded plan |
| **Confident** | Mint `#34D399` | Raised slightly | Bright | Clear smile | Recovery / heal — graph re-ignites violet→green |
| **Proud** | Strong mint + indigo rim | Level | Soft closed-smile eyes | Wide smile | Progress: graduation restored on-track — *"He's going to make it."* |

Extra utility moods used only by the full defense deck (not the 7-beat story): `dormant`, `neutral`, `steady`, `off`.

## Story beat ↔ mood map (inside outline v2)

| Outline beat | Scene | Mood |
|---|---|---|
| 0 Cold | `cold` | dormant |
| 1 Intro / Problem | `intro` | worried |
| 2 Architecture | `architecture` | neutral |
| 3 Impact | `impact` | panic |
| 4 GPA | `gpa` | focused |
| 5 Progress | `progress` | focused |
| 6 Planner / heal | `planner` | reassured |
| 7 AI Advisor | `advisor` | confident |
| 8–10 Institutional | `admin`… | steady / off |
| 11 Close | `close` | proud |

There is **no separate story mode** — one linear deck only.

## Implementation

- CSS: `docs/presentation/presentation.css` — `.avatar-dock[data-mood="…"]`
- Orchestration: `docs/presentation/js/main.js` — `AVATAR_MOOD` + `setAvatarMood()`
- Cascade hooks force **Panic** on desat; heal path lifts to **Reassured** / **Confident**

## Design rule

Avatar transitions are driven by **presentation beat / cascade hooks**, not free animation loops — so it reads as “the system reacting,” not a cartoon sidekick.
