# Implementation Plan - Living Course-Map Presentation

**One linear deck** matching [presentation outline v2](../../.cursor/plans/presentation_outline_order_088ade5b.plan.md). Mazen's story is the emotional spine inside beats 1–7 — **not** a separate mode.

## What exists

| File | Role |
|------|------|
| [index.html](index.html) | Shell — beats 0–11, Mazen avatar, single nav |
| [presentation.css](presentation.css) | Tokens, glass, cascade desat, avatar moods |
| [js/stage.js](js/stage.js) | WebGL constellation (CS211 fail-seed), camera, cascade + heal |
| [js/panels.js](js/panels.js) | Dock panels to 3D anchors |
| [js/main.js](js/main.js) | Single scene list · avatar moods · hotkeys |
| [CASCADE_KPIS.md](CASCADE_KPIS.md) | Locked CS211 KPIs |
| [NARRATION.md](NARRATION.md) | Per-beat speaker lines |
| [LIVE_DEMO.md](LIVE_DEMO.md) | Hero live demo checklist |
| [AVATAR_SPEC.md](AVATAR_SPEC.md) | Mood presets mapped to outline beats |

## Preview

```bash
cd docs/presentation && python3 -m http.server 8765 --bind 127.0.0.1
# http://127.0.0.1:8765/
```

Hotkeys: `←` / `→` / `Space` · `C` cascade · `H` heal

## Beats (0–11) — only path

Cold → Intro (Abdo) → Architecture (Hussein) → Impact LIVE (Esraa) → GPA (Hazem) → Progress (Ezz) → Planner (Abdo) → AI Advisor (Hussein) → Admin (Tawfik) → Super Admin (Tawfik) → Challenges (Tawfik) → Close (Abdo)

## Locked design

1. Three.js WebGL + additive bloom  
2. One docked panel per beat ≤3 KPIs  
3. CS211 cascade signature  
4. Persistent Mazen avatar (mood follows beat)  
5. Persona: `mazen.hassan@cursus.demo` / `Demo123!`
