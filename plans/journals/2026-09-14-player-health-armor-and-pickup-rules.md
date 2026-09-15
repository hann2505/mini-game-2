---
title: Player health armor and pickup rules
date: 2026-09-14
summary: "Start at 100 HP and zero armor; X deals five damage, Y grants five armor, Z grants rockets."
---

# Player health armor and pickup rules

## Changes
PlayerStats and the player prefab start with 100 HP and zero armor, retaining the 50 armor capacity. Initialize defaults to zero starting armor with an explicit startArmor option for configured setups.
X deals five damage through armor first and slows movement to 60% for three seconds. The existing shield blocks both effects. Y grants five armor and retains its shield and haste. Z retains its currency rewards (+50 gold and +5 gems) and bonus pickups, and now grants the existing ten-second rocket attack previously granted by Y.

## Validation
Unity 6000.0.58f2 EditMode tests in an isolated temporary project: 140/144 passed. All health, pickup, and HUD tests passed, including partial armor damage and slowdown expiration. Four combat tests failed: two explosion animation tests, temporary missile firing timing, and held-attack timing. Repeating the combat suite with original HEAD files reproduced exactly the same four failures (13/17 passed). These pre-existing failures remain unresolved.
Scoped diff whitespace checks passed. The user's existing SampleScene changes were preserved. Both batch Unity processes exited. Test evidence is in /tmp/mini-game-pickups.YBFeuo/results.xml and baseline-results.xml.

## Review
Reviewed source, prefab wiring, and changed test expectations against the requested rules. No additional findings in the changed behavior. AgentWiki publish skipped.

> Historical work record — not durable authority. Prefer docs/specs/ADRs for current decisions.
