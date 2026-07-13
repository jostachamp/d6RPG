# Core Mechanics Roadmap

This document defines where the Arkham Horror CRPG rules live, the order in which they will be implemented, and the evidence required before a milestone is complete. It is the forward-looking plan. `MECHANICS_IMPLEMENTATION.md` is the historical ledger of rules already translated into code.

## Where the core mechanics live

The authoritative runtime rules assembly is:

```text
Assets/ArkhamHorror/Mechanics/
```

It is a pure C# assembly named `ArkhamHorror.Mechanics`. It must not reference `UnityEngine`, scenes, input, UI, animation, audio, or art assets.

The intended dependency direction is:

```text
SourceDocs -> Mechanics specification -> Pure C# rules -> Unity adapters -> Presentation
                                        -> Edit-mode tests
                                        -> Headless simulations
                                        -> Save/replay data
```

Unity-facing code will call the mechanics assembly. The mechanics assembly will never call Unity-facing code. This keeps the rules portable to tests, command-line simulations, another engine, or a future server.

## Supporting locations

| Purpose | Location |
| --- | --- |
| Runtime rules | `Assets/ArkhamHorror/Mechanics` |
| Automated rule tests | `Assets/ArkhamHorror/Tests/EditMode` |
| Completed-rule ledger and citations | `Docs/MECHANICS_IMPLEMENTATION.md` |
| Source authority | `SourceDocs` |
| Future Unity adapters | `Assets/ArkhamHorror/Unity` |
| Future headless scenario fixtures | `Assets/ArkhamHorror/Tests/Fixtures` |

The future locations are reserved architectural boundaries, not implemented features.

## Definition of done for a mechanic

A mechanic is complete only when all of the following are true:

1. Its corebook or supplemental source, printed page, and PDF page are recorded.
2. Ambiguities and CRPG-specific interpretations are written in the implementation ledger.
3. The rule is implemented in pure C# without presentation dependencies.
4. Normal behavior, boundaries, invalid inputs, and relevant rule interactions have automated tests.
5. The portable verification harness passes.
6. Unity test compilation passes; Unity Test Runner execution is recorded when the editor is available.
7. Known omissions are explicitly listed so partial implementation cannot be mistaken for complete support.

## Milestone vocabulary

Project milestones use names such as `M0`, `M1`, and `M2`. Entries such as `M001` in `MECHANICS_IMPLEMENTATION.md` are permanent rule-implementation IDs, not project milestones.

The work before M0 is the headless mechanics foundation. We do not graduate to M0 until that foundation satisfies its exit criteria.

## Headless mechanics foundation - current phase

Status: In progress.

Goal: Complete the engine-level rule primitives needed by player characters and NPCs without creating either type yet. This phase remains pure C#, deterministic, testable, and free of presentation or authored campaign content.

### Foundation A: Resolution kernel

Status: Complete.

Implemented:

- Numeric d6 and derived d3.
- Injectable and seedable randomness.
- Dice-pool maximum, limit, available dice, spending, adding, and refilling.
- Five skill thresholds and success counting.
- Standard, difficult, and very difficult actions.
- Natural 1 and natural 6 behavior.
- Advantage and disadvantage.
- Typed regular and horror dice.
- Damage, healing, wounded state, and straining state transitions.
- Trauma trigger metadata from horror dice.
- Staged rerolls before result removal, with explicit reroll grants.
- The prohibition against rerolling a horror die showing 1.
- Typed pre-roll hand removal and explicitly granted post-roll result removal.
- Complete roll-event history for replay and trauma evaluation.
- Reaction resolution using exactly one pool die while still permitting sourced extra hand dice.

Exit evidence: every core action-hand construction step can be expressed without depending on a player character, NPC, weapon, knack, injury, or scene.

### Foundation B: Consequence primitives

Status: Complete.

Scope:

- Injury generation, duplicate injuries, severity, healing requirements, and death state.
- Trauma generation, severity, session modifiers, and removal-from-play state.
- Insight as a generic resource transition, including death avoidance.
- Horror recovery transitions used by later Introspection, Counseling, and downtime actions.
- Effect duration and stacking rules without binding effects to a specific character type.

Implemented:

- Complete Table 2-1 injury generation with existing-injury and external modifiers.
- Duplicate injury instances with non-stacking effects and per-instance healing.
- Unique Burned, Choking, and Sickened third-instance death.
- Medical and natural injury recovery requirements.
- Nasty Cut, Comatose, and Dire dice-pool recovery restrictions.
- Heavy Blow next-turn expiration and Dire treatment deadline state.
- Complete player-facing Table 2-2 trauma generation and immediate effects.
- Trauma session escalation, personality-trigger requirements, and Mind Undone duration extension.
- Insight spending, session refill, gain, limit changes, and permanent limit sacrifice to avoid removal.
- Introspection, Counseling, safe-week horror recovery, and injury-aware damage recovery.

Exit evidence: generic headless state can suffer and recover from damage and horror, generate sourced injuries and traumas, and resolve insight-based survival.

### Foundation C: Action, scene, and interaction primitives

Status: Complete.

Scope:

- Free, simple, complex, and reaction action contracts.
- Narrative-scene rules and repeated-action restrictions.
- Structured scenes, rounds, side turns, activation order, and surprise rounds.
- Scene entry and dice-pool refill timing.
- Aid an Ally and its narrative-scene exception.
- Engagement, range, attack, defense, damage, and hazard contracts.
- Pluggable decision requests so a caller can supply player or NPC choices without the mechanics knowing who made them.

Implemented:

- Actor-neutral scene participants identified by stable IDs, side, and dice pool.
- Narrative-scene start and entry refills, explicit scene ending, exhausted-pool detection, and scene-scoped exact-task restrictions.
- Narrative simple actions at zero dice, with the one-die Aid an Ally exception.
- Structured rounds, side turns, caller-selected first side, free activation order, pass/exhaustion gates, and turn-scoped exact-task restrictions.
- Own-turn and opposing-turn scene-entry refill timing.
- Surprise rounds with no refill for the surprised side and a configurable one- or two-die refill for the surprising side.
- Reaction declarations during the opposing side's turn, limited to one reaction per actor and stable triggering-action ID.
- Generic typed decision requests for human adapters, scripted tests, replay sources, or later NPC logic.
- Five-foot range increments and engagement as both proximity and accessibility.
- Melee and ranged attack legality, including sight, reach, line of fire, listed range, engagement restrictions, and the pistol exception.
- Melee Combat and Agility defense-reaction contracts, successful-reaction negation, damage, and weapon injury-rating thresholds.
- A generic reactive-effect contract usable by attacks, social effects, and hazards.

Exit evidence: generic actors can participate in deterministic narrative and structured scenes without those actors being defined as investigators or NPCs.

### Foundation D: Persistence and certification

Status: Complete.

Scope:

- Versioned mechanics snapshots.
- Deterministic commands and decision records.
- Random seed/state capture.
- Mechanical event log suitable for debugging and replay.
- Save migration policy for mechanics schema changes.
- Golden headless scenarios that cover actions, reactions, damage, horror, injury, trauma, recovery, and insight.

Implemented:

- Schema-versioned snapshots for shared actor dice pools, pending strain, insight, injuries, death, trauma session state, and permanent loss.
- Validated restoration that rejects malformed, duplicate, stale-version, and future-version state.
- A portable PCG random generator with exact state and stream capture.
- Immutable commands for actions, reactions, damage, healing, horror, recovery, insight, injury generation, trauma generation, and session transitions.
- Stable command IDs, recorded decision playback, ordered mechanical events, and canonical state/event fingerprints.
- Replay from an initial snapshot and random state, plus certified recording verification and tamper rejection.
- Strict adjacent-version migration registration with explicit rejection when no complete path exists.
- A golden headless scenario covering every required Foundation D interaction and a mid-scenario snapshot/resume equivalence test.
- A portable v1 schema and migration policy in `Docs/MECHANICS_SAVE_SCHEMA.md`.

Exit evidence: the headless mechanics suite can save, load, and replay representative scenarios to identical outcomes with complete rule citations. Passing this gate graduates the project to M0.

## M0: True-to-rulebook player-character creation

Status: Ready to begin; the headless mechanics foundation gate has passed.

Goal: Create legal player investigators by following the rulebook's character-creation sequence exactly, while preserving every choice and derived value in portable data.

Primary sources: Corebook Chapter 3, the character sheet supplement, the knack list supplement, and referenced starting-equipment rules.

Scope:

- Background selection and its mechanical effects.
- Personality trait selection, positive trigger, and negative trigger.
- Archetype selection and restrictions.
- Starting skill-level allocation and validation.
- Starting equipment and character advancements.
- Starting knacks, prerequisites, and derived resources.
- A step-by-step creation state machine that supports validation, backtracking, and deterministic tests.
- A serializable investigator definition separated from mutable in-play investigator state.

Exit evidence:

- Valid rulebook examples can be reproduced exactly.
- Illegal combinations and allocations are rejected with precise reasons.
- Every created value traces to a source rule or an explicit player choice.
- Creation works headlessly before any character-creation UI is attempted.

## M1: True-to-rulebook NPC creation

Status: Gated by M0.

Goal: Create legal NPC profiles using the rulebook's NPC model without forcing NPCs through player-character creation rules.

Primary sources: Corebook NPC and Game Master chapters, the NPC sheet supplement, and relevant ally/enemy profile rules.

Scope:

- NPC dice-pool sizes and profile constraints.
- Skill and capability representation appropriate to NPCs.
- Minor, major, monstrous, supernatural, ally, and adversary distinctions where defined by the rules.
- NPC knacks, attacks, defenses, horror, damage, injury, and special abilities.
- Data-driven profile construction and validation.
- A serializable NPC definition separated from mutable in-play NPC state.
- Headless fixtures that reproduce representative rulebook NPC profiles.

Exit evidence:

- Representative rulebook profiles can be reproduced without exceptions or hard-coded profile logic.
- Invalid NPC configurations are rejected with precise reasons.
- NPC definitions use the same shared mechanics primitives as investigators while retaining their rulebook-specific differences.

## M2: Player-character and NPC interaction space

Status: Gated by M1.

Goal: Exercise player investigators and NPCs together through the complete shared rules space.

Scope:

- Narrative conversations and social-scene difficulty.
- Opposed actions and reactions.
- Aid, charm, intimidation, deception, and lie detection.
- Structured scenes, initiative side order, surprise, and turn progression.
- Engagement, movement, melee attacks, ranged attacks, dodge, block, and disengage.
- Damage, healing, injuries, horror, trauma, insight, incapacitation, and death across both actor types.
- Equipment, knacks, and special abilities affecting another actor.
- A neutral decision interface supporting human input, scripted tests, and later NPC behavior logic.
- Deterministic interaction fixtures and complete event logs.

Exit evidence:

- Investigator-versus-NPC and investigator-with-NPC scenarios run headlessly from declaration to final state.
- Social and combat examples use the same action, scene, consequence, and event systems.
- No interaction rule lives in UI code or a specific character/NPC implementation.
- Golden tests prove that identical commands and random state produce identical outcomes.

## Current progression

| Phase | State | Graduation gate |
| --- | --- | --- |
| Headless mechanics foundation | Complete | Deterministic, persistent, replayable mechanics scenarios pass with source traceability. |
| M0: Player-character creation | Ready | Rulebook-legal investigators can be created and validated headlessly. |
| M1: NPC creation | Gated | Rulebook-faithful NPC profiles can be created and validated headlessly. |
| M2: Interaction space | Gated | Investigators and NPCs complete deterministic social and combat interactions. |

## Immediate implementation order

1. Begin M0 by specifying the exact rulebook character-creation sequence and data boundary.
2. Implement backgrounds, personality, archetype, skill allocation, equipment, advancements, and knacks in sourced tranches.
3. Certify legal examples and illegal combinations headlessly before creating character-creation UI.

This order may change only when a documented rule dependency requires it. Changes to the order must be recorded here and explained in the implementation ledger.
