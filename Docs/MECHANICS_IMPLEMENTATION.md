# Mechanics Implementation Ledger

This file is the portable record of which tabletop rules have been translated into software, how they were translated, and how to verify them. Update it in the same change as every mechanics implementation.

For the forward-looking milestone plan and architectural boundaries, see `Docs/MECHANICS_ROADMAP.md`.

## Project principles

- `SourceDocs/ESDPSAH02EN-DT_Corebook.pdf` is the primary mechanical authority.
- Supplemental PDFs are secondary authorities. A clearly stated supplemental override takes precedence for its stated scope.
- Rule behavior belongs in pure C# without `UnityEngine` dependencies. Unity scenes, input, animation, and UI consume the mechanics layer rather than owning rules.
- Randomness enters through an interface so tests, replays, saves, debugging, and simulations can reproduce outcomes.
- A mechanic is not considered implemented until its source, interpretation, limitations, and automated verification are recorded here.
- PDF page numbers below include both the printed page and PDF page where they differ.

## Architecture

| Area | Location | Portability decision |
| --- | --- | --- |
| Runtime mechanics | `Assets/ArkhamHorror/Mechanics` | Pure C# assembly with no Unity references. |
| Edit-mode tests | `Assets/ArkhamHorror/Tests/EditMode` | NUnit tests run by the Unity Test Framework. |
| Randomness boundary | `Dice/IDieRoller.cs` | Runtime may use `SystemDieRoller`; tests inject known sequences. |
| Implementation record | `Docs/MECHANICS_IMPLEMENTATION.md` | This ledger travels with source control and does not depend on an IDE. |
| Forward plan | `Docs/MECHANICS_ROADMAP.md` | Milestones, dependency order, exit criteria, and future code boundaries. |

## Implemented mechanics

### M001: Numeric d6 and derived d3

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed p. 15 (PDF p. 16), "The Dice" and "D3 Dice."

Implementation:

- `IDieRoller` defines the single runtime randomness boundary.
- `SystemDieRoller` produces inclusive results from 1 through 6 and supports an optional seed.
- `D3.FromD6` divides a d6 result by two and rounds up: 1-2 becomes 1, 3-4 becomes 2, and 5-6 becomes 3.
- Invalid physical die results are rejected instead of silently normalized.

Tests: `D3Tests` covers all six d6 faces.

### M002: Basic dice-pool lifecycle

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed pp. 15-16 (PDF pp. 16-17), "The Dice Pool," "Filling and Refilling a Dice Pool," "The Dice Pool Limit and Maximum," and "Adding Dice to a Dice Pool."

Implementation:

- A standard player pool starts with maximum, limit, and available dice all equal to six.
- Spending removes available dice.
- Refilling raises available dice to the current limit but never removes dice already above the limit.
- Directly added dice can exceed both limit and maximum without changing either value.
- Maximum, limit, and currently available dice are distinct state.

Tests: `DicePoolTests` covers player initialization, spending/refilling, dice added beyond maximum, typed spending, horror-first refills, damage, healing, straining, wounded state, and boundary behavior.

Limitations:

- Scene and turn timing decides when `Refill` is called; timing orchestration is not implemented yet.

### M003: Skill thresholds and complex-action resolution

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed p. 17 (PDF p. 18), "Skill Levels" and "Rolling Results of 1 or 6"; printed p. 21 (PDF p. 22), "Complex Actions" and "Difficult and Very Difficult Complex Actions."

Implementation:

- Skill thresholds are Bad 6+, Normal 5+, Good 4+, Amazing 3+, and Phenomenal 2+.
- Each die meeting its skill threshold is one success.
- An unmodified natural 1 always fails, even when modifiers would raise it to the threshold.
- An unmodified natural 6 always succeeds, even when modifiers would lower it below the threshold.
- Standard, difficult, and very difficult actions require one, two, and three successes respectively.
- A complex action must spend at least one die from its character's pool.
- Extra hand dice can be rolled without being removed from the character pool.
- Inputs are validated before pool state is mutated.
- Results retain all rolled dice, the results kept after hand modification, die kinds, success count, required success count, and final outcome for future UI, logging, and replay systems.

Tests: `ComplexActionResolverTests` covers every skill threshold, natural 1/6 overrides, every difficulty boundary, pool spending, extra hand dice, deterministic roll order, and failure without partial mutation.

Limitations:

- Knack eligibility, effect-specific usage limits, and scene-level reaction triggers are not implemented.
- The resolver supports a uniform numeric modifier across the hand. Per-die modifiers will be introduced only when a sourced rule requires them.
- Simple actions are represented by `DicePool.Spend(1)` at this stage; action classification and scene-specific exceptions are not yet modeled.

### M004: Advantage and disadvantage

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed p. 34 (PDF p. 35), "Advantage and Disadvantage."

Implementation:

- Advantage adds one regular die before rolling, then removes one lowest natural result.
- Disadvantage adds one regular die before rolling, then removes one highest natural result.
- When both apply, two regular dice are added and two distinct dice are removed: one lowest and one highest.
- `ComplexActionResult.AllRolls` preserves the complete roll while `KeptRolls` and `DieResults` expose only results remaining for success evaluation.
- Tied extrema use stable roll order. Because tied dice have the same natural result and horror consequences inspect the complete roll, this tie choice does not alter any currently implemented outcome.

Tests: `RollConditionTests` independently covers advantage, disadvantage, and their combined behavior.

### M005: Damage, healing, wounded state, and straining

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed pp. 31 and 33 (PDF pp. 32 and 34), "Damage," "Dice Pool Reduction," "Wounded," "Straining Oneself," and "Healing Damage."

Implementation:

- Each point of damage reduces the pool limit by one, to a minimum of zero.
- When the new limit is below the current number of dice, enough dice are discarded to reach the new limit.
- A zero limit exposes `IsWounded`; narrative restrictions on free actions remain an orchestration concern.
- Healing increases the limit to at most the maximum and does not refill or add dice.
- Straining restores the limit to maximum without refilling and records a pending injury.
- `ResolvePendingStrainInjury` lets turn or narrative-scene orchestration consume the pending consequence at the rule-prescribed time.
- Zero damage is a no-op and does not remove temporarily added dice above the pool limit.

Tests: `DicePoolTests` covers regular-first damage removal, damage beyond zero, healing without refill, strain without refill, pending strain injury consumption, and zero-damage behavior.

Limitations:

- Injury generation and Table 2-1 are not implemented, so resolving a pending strain injury currently hands control to a future injury service.
- Structured-turn and narrative-action timing are not yet modeled; the pool records the consequence but does not decide when a caller resolves it.

### M006: Horror dice composition and trauma trigger data

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed pp. 36-37 (PDF pp. 37-38), "Horror and the Dice Pool," "Horror Dice," and "Generating a Trauma Result."

Implementation:

- Regular and horror dice are tracked separately in a pool and retained as typed `DieRoll` values in action results.
- Horror suffered raises the horror dice limit up to the dice-pool maximum. Reducing horror never converts or removes dice already in the pool.
- Refilling adds missing horror dice first, up to the lower of the horror limit, open pool slots, and dice-pool limit; remaining slots receive regular dice.
- Damage, healing, and straining do not change the horror dice limit.
- Any generic discard removes regular dice before horror dice.
- When a pool contains horror dice, spending must use an explicit `DiceSelection`; the engine does not invent a player choice about which kind to spend.
- Every horror die that naturally rolls 1 increments `HorrorOnesRolled`, sets `TriggersTrauma`, and contributes +1 through `TraumaRollModifier`.
- Interpretation: a horror 1 triggers trauma even if advantage later removes it. The advantage rule adds and removes dice after rolling, while the horror rule triggers when a horror die generates a 1 during the action. The complete roll is therefore authoritative for trauma, while kept results are authoritative for success.

Tests: `HorrorDiceTests` and `DicePoolTests` cover typed roll preservation, multiple horror 1s, a horror 1 removed by advantage, horror-limit caps, horror-first refill, low-limit refill, explicit typed spending, and unchanged horror limit through damage/healing/strain.

Limitations:

- Table 2-2 trauma generation and trauma effects are not implemented; the resolver provides the exact trigger and modifier needed by that future service.
- Introspection, Counseling, and week-long horror recovery timing are not implemented; `ReduceHorror` is the sourced state transition those systems will call.

### M007: Staged roll modification, rerolls, and reactions

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed pp. 21-23 (PDF pp. 22-24), complex-action ordering and reactions; printed p. 34 (PDF p. 35), advantage/disadvantage result removal; printed p. 37 (PDF p. 38), horror-die reroll restriction; printed pp. 75-80, representative knack reroll grants.

Implementation:

- `ActionRollSession` represents the sourced phases explicitly: the hand is constructed, dice are rolled, granted rerolls occur, roll-condition results are removed, granted special removals occur, and the remaining results are evaluated.
- Pool dice committed to an action remain spent even if a rule removes one of those dice from the hand before rolling.
- Pre-roll removals are typed, so removing a horror die prevents that die from rolling or generating trauma.
- Rerolls must be explicitly granted when a session begins. Each successful reroll consumes one grant.
- A horror die currently showing 1 rejects a reroll without consuming the grant. A horror die rerolled into 1 becomes locked and generates the appropriate trauma modifier.
- Every initial roll and reroll is retained as a `DieRollEvent`; `AllRolls` records each die's final value before result removal.
- Advantage and disadvantage remove extrema after granted rerolls, not before them.
- Special post-roll result removals require an explicit allowance and occur after advantage/disadvantage removal.
- Reactions share the complex-action pipeline but must commit exactly one pool die. Advantage and other sourced extra hand dice do not violate that restriction because they do not come from the pool.
- Results identify whether they came from a complex action or reaction.

Tests: `ActionRollSessionTests` and `ReactionResolverTests` cover audit history, phase order, explicit grants, grant exhaustion, horror-1 locks, horror dice rerolled into 1, advantage after rerolls, pre-roll typed removal, post-roll special removal, one-die reaction spending, reaction advantage, and horror reactions.

Limitations:

- The session accepts allowances calculated by a caller. Future knack, equipment, injury, and character-state systems must determine when those allowances are earned and consumed at the campaign level.
- Enforcing only one reaction to a specific triggering action requires the future scene/action orchestrator. This layer enforces the one-pool-die rule for each individual reaction.
- Opposed outcomes such as negating an attack or social success are not part of the roll kernel; they belong to the interaction layer.

### M008: Injury generation, stacking, healing, and deadlines

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed pp. 31-35 (PDF pp. 32-36), Table 2-1, "Generating an Injury Result," "Stacking Injury Results," "Healing Injuries," and unique environmental injuries.

Implementation:

- `InjuryGenerator` produces every Table 2-1 result from a base roll, one severity point per existing injury, and sourced external modifiers.
- Generation and application are separate operations. This permits insight-limit sacrifice to intercept a fatal outcome before state is mutated.
- Generated results record the existing-injury count and are rejected if applied to stale state.
- Every nonfatal injury is an independently identified instance. Duplicate effects do not stack, but the effect remains until every matching instance is healed.
- Loss of a Sense requires an explicit Sight, Smell, or Hearing selection.
- Medical healing enforces one, two, or three required successes by injury kind.
- Natural healing enforces one-week, two-week, or unavailable recovery by injury kind.
- Nasty Cut caps increases to the dice-pool limit at 3 without reducing a limit already above 3.
- Comatose and Dire immediately reduce the dice-pool limit to zero and block recovery while present.
- Heavy Blow's temporary effect can be cleared after the next turn while the injury itself remains for severity and healing.
- Dire records whether medical treatment was received before its one-hour deadline; an untreated expired deadline is fatal.
- Burned, Choking, and Sickened are tracked as unique injuries. A third matching instance is fatal.
- `InjuryCatalog` exposes healing, natural-recovery, sense-choice, pool-ceiling, and lock metadata without UI dependencies.

Interpretations:

- When insight avoids a fatal injury-table result or third unique injury, the fatal incoming injury is not added to the injury list. The rule specifies survival through a near-death event but does not assign a replacement injury.
- Natural-healing methods receive elapsed weeks for a specific injury. Foundation D persistence will supply timestamps rather than embedding wall-clock time in the rules kernel.

Tests: `InjuryTests` covers every table boundary, modifiers, stale results, duplicates, effect persistence, sense choice, healing requirements, natural healing, unique-injury death, fatal interception, Heavy Blow expiration, and Dire treatment deadlines.

### M009: Insight resource and fatal-outcome avoidance

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed p. 38 (PDF p. 39), "Insight" and "Decreasing Insight Limit to Survive Death"; printed p. 67, maximum insight limit.

Implementation:

- `InsightPool` keeps current insight distinct from its permanent limit.
- Session refill restores current insight to the limit.
- Spending fails without mutation when current insight is insufficient.
- Gains stop at the current limit, and permanent limit increases stop at 10.
- Avoiding death or removal permanently reduces the limit by 2 and fails when the limit is below 2.

Interpretation: when the permanent limit falls below current insight, current insight is reduced to the new limit. This maintains the rule's invariant that insight does not exceed its limit; the book does not separately describe an over-limit state.

Tests: `InsightPoolTests` covers refill, spending, insufficient spending, gains, the limit-10 cap, limit sacrifice, and current-value clamping.

### M010: Trauma generation, session escalation, and recovery transitions

Status: Implemented. Portable verification passed; Unity Test Runner execution is pending.

Source: Corebook printed pp. 36-39 (PDF pp. 37-40), horror dice, trauma generation, healing horror, Insight, and Table 2-2.

Implementation:

- `TraumaGenerator` maps all Table 2-2 ranges and combines the d6, one point per horror die showing 1, session escalation, and sourced external modifiers.
- `TraumaState` applies Shocked and Stunned pool discards, including their empty-pool session escalation.
- Overcome by Horror and Mind Undone attempt their exact insight costs and otherwise emit a negative-personality trigger requirement.
- Mind Undone always increases future trauma rolls for the session and reports when the personality effect's duration must be extended.
- `TraumaDurationRules` maps end-of-next-turn effects to end-of-scene and end-of-scene effects to end-of-session.
- Lost Forever can permanently reduce the insight limit by 2 to avoid removal; otherwise the state records permanent loss.
- Session escalation resets explicitly at session end. Trauma outcomes are not stored as persistent injuries, matching the rule that traumas have effects rather than a healing lifecycle.
- `RecoveryService` applies successful Introspection and Counseling, clears horror after an uninterrupted safe week, and performs injury-aware damage healing, natural recovery, and strain.

Boundaries:

- Personality definitions and their concrete negative effects begin in M0. This layer reports whether the effect triggers and whether Mind Undone extends it.
- The separate NPC trauma table is intentionally reserved for M1, where NPC profile rules are introduced.
- Scene restrictions, attempt limits, and the hour/week passage of time belong to Foundation C/D orchestration; this layer implements the sourced state transitions those timers call.

Tests: `TraumaTests` and `RecoveryServiceTests` cover every table boundary, dice discards, session escalation, insight choices and insufficiency, personality flags, duration extension, Lost Forever, recovery caps, Introspection, Counseling, and safe-week recovery.

### M011: Narrative and structured scene orchestration

Status: Implemented. Portable verification and test compilation passed; the active Unity editor reports no console errors.

Source: Corebook printed pp. 20-27 (PDF pp. 21-28), action types, narrative scenes, structured scenes, side turns, entering a scene, and surprise rounds.

Implementation:

- `SceneActor` binds only a stable ID, side, and `DicePool`; it does not define an investigator or NPC.
- `NarrativeScene` refills actors at scene start and entry, exposes pool-exhaustion state, supports explicit scene termination, and limits each actor to one attempt at an exact complex task for the scene.
- General narrative simple actions cost no dice. Aid an Ally retains its explicit one-die exception.
- `StructuredScene` sequences rounds and two side turns while allowing the caller to choose the first side and activation order within a side.
- A side turn cannot end until every actor on that side has passed or exhausted their pool. Passing prevents further actions on that side turn.
- Pools refill at the start of their own side turn. An actor entering during its side's turn receives that refill immediately; one entering during the opposing turn waits.
- Exact-task restrictions reset on an actor's next side turn.
- During a surprise round, the surprised side receives no refill and the surprising side refills by one die, or two when the caller records the GM allowance. Normal refills resume in round two.
- Reactions are declared during the opposing side's turn and are limited to one per actor and triggering action.
- `DecisionRequest<T>` validates a choice against explicit legal options, while `IDecisionProvider` leaves the source of that choice outside the mechanics assembly.

Interpretations and boundaries:

- Sides are named A and B to avoid encoding investigator or NPC types. The caller assigns actors and selects the first side after applying the instigator/default-first-side rule.
- An exact task and a triggering action use caller-supplied stable IDs. Foundation D commands and event records will own their durable identity.
- Narrative scene ending by GM judgment or because every participant is done is represented by explicit `End`; automatic pool exhaustion remains separately observable.
- The rules permit the surprising side's refill to be increased from one to two by the GM. This allowance is constructor data so no hidden GM decision occurs inside the kernel.
- Decision providers are a seam, not NPC behavior. Human input, automation, and NPC choice policy remain outside the rules state machine.

Tests: `SceneTests` covers narrative and entry refills, simple-action costs, exact-task scope, side-turn refill and completion, passing, structured simple-action cost, late entry, surprise, partial refill, reactions, and decision validation.

### M012: Engagement, attacks, defenses, and reactive effects

Status: Implemented. Portable verification and test compilation passed; the active Unity editor reports no console errors.

Source: Corebook printed pp. 29-30 (PDF pp. 30-31), range, engagement, ranged attacks, melee attacks, defense reactions, damage, and injury ratings.

Implementation:

- `RangeRules` represents distances in feet, converts them to five-foot increments, and requires both five-foot proximity and easy accessibility for engagement.
- `AttackRules.Validate` checks visibility, listed range, melee reach, ranged line of fire, and ranged engagement restrictions before a roll is permitted.
- A ranged weapon may opt into the pistol exception. While engaged, that weapon can target only an engaged adversary.
- Legal melee attacks identify Melee Combat as the defense skill; legal ranged attacks identify Agility.
- `AttackRules.Resolve` requires a complex-action attack and, when supplied, a reaction defense. A successful defense negates the hit and all its effects.
- An unnegated successful attack deals the profile's damage. It inflicts an injury when attack successes meet or exceed a numeric injury rating; a missing rating represents the rulebook dash and never inflicts an injury.
- `ReactiveEffectRules` captures the shared successful-reaction-negates-effect rule without binding it to combat. This is the hazard and later social-interaction seam.

Interpretations and boundaries:

- Visibility, accessibility, reachability, line of fire, and engagement facts are explicit inputs. A later Unity spatial adapter may calculate them, but geometry and pathfinding do not belong in the rules assembly.
- Attack profiles contain only mechanics-facing values. The equipment catalog and authored weapon records are content and remain deferred.
- The current validator enforces a profile's listed range. Any equipment-specific range-band difficulty or special weapon exception must be added only when its exact source entry is implemented.
- Damage is returned as an outcome rather than directly applied to a character, because actor state definitions begin in M0 and M1. The existing consequence primitives receive that outcome later.
- Concrete falling, fire, suffocation, disease, and other hazard declarations remain data-driven callers of the shared reaction and consequence contracts; Foundation C does not author encounters or environmental content.

Tests: `InteractionTests` covers engagement boundaries, five-foot increments, melee legality, ranged legality, pistol targeting, required defense skills, defense negation, damage, injury thresholds, dash injury ratings, and generic reactive effects.

### M013: Versioned state, deterministic commands, and certified replay

Status: Implemented. Portable verification and all NUnit cases passed under .NET 8; Unity editor refresh is pending for this tranche.

Source relationship: This is a CRPG architecture requirement rather than a new tabletop rule. Schema v1 persists the sourced state transitions implemented in M001-M012. The rules governing the saved values retain their original corebook citations in those entries.

Implementation:

- `MechanicsState` and `MechanicsActorState` aggregate only shared mutable rule state: dice, insight, injuries, trauma, death, and removal. They do not define an investigator or NPC.
- `MechanicsSnapshot` schema version 1 captures and validates every shared actor value, including typed dice, pending strain injuries, injury identity and temporary/deadline flags, next injury identity, trauma escalation, death, and Lost Forever.
- Canonical snapshots sort actors ordinally and injuries numerically, use invariant formatting, and round-trip to identical canonical state.
- `DeterministicDieRoller` uses portable PCG-XSH-RR generation and captures both its full state and stream. Rehydration resumes at the exact next random result.
- Immutable mechanics commands cover complex actions, reactions, damage, healing, horror, recovery, insight, injury generation, trauma generation, pool refill, and session end.
- Every executed command has a unique stable ID and emits an ordered `MechanicsEvent` containing auditable natural rolls and outcome data.
- `DecisionRecord` and `RecordedDecisionProvider` reproduce explicit legal choices by stable request ID without embedding human or NPC policy.
- `ReplayEngine` restores initial state and randomness, rejects duplicate command IDs, executes commands, and returns final snapshot, random state, events, and canonical fingerprints.
- `ReplayRecording` verifies expected state and event fingerprints and rejects altered certification data.
- `SnapshotMigrationRegistry` permits only registered adjacent-version migrations, rejects incomplete paths and future versions, and validates every migrator's returned version.
- `MECHANICS_SAVE_SCHEMA.md` records schema v1, deterministic continuation, fingerprint semantics, migration policy, save boundaries, and adapter requirements.

Interpretations and boundaries:

- Certified paths require `DeterministicDieRoller`. `SystemDieRoller` is retained for callers that do not require portable continuation.
- Canonical FNV-1a fingerprints detect replay divergence and accidental corruption; they are not cryptographic authentication.
- Commands store decisions and modifiers as explicit input. They never query UI, wall-clock time, Unity objects, or NPC behavior.
- A failed replay command aborts instead of recording a partial success event. The caller retains the last certified snapshot as its transaction boundary.
- Schema v1 captures shared actor mechanics. Foundation C scene state is reconstructed from declarations and commands; compact mid-scene/spatial snapshots are deferred until M2 defines final ownership and will require a new schema version.
- The pure mechanics layer exposes validated data and canonical forms but does not choose JSON, database, cloud, or platform storage.

Tests: `PersistenceTests` covers seeded equivalence, exact random continuation, full snapshot round trips, canonical ordering, malformed state, recorded decisions, deterministic replay, certification tampering, duplicate commands, migration paths, future versions, complete golden coverage, and mid-scenario save/resume equivalence.

## Verification

Run Unity edit-mode tests from the editor's Test Runner, or headlessly:

```powershell
& '<Unity Editor path>\Editor\Unity.exe' -batchmode -nographics -projectPath '<repository root>' -runTests -testPlatform EditMode -testResults '<result path>' -quit
```

The verification record must state the Unity version, test count, pass/fail result, and any environment limitation.

Current project baseline: Unity 6000.4.11f1, Universal Render Pipeline 17.4.0, Unity Test Framework 1.6.0.

### 2026-07-13 verification record

- The pure mechanics assembly compiled independently under .NET 8 with zero warnings and zero errors.
- Eight portable behavior groups passed: complete d3 mapping, player-pool initialization, pool spending/refilling, added dice above maximum, all five skill thresholds, natural 1, natural 6, and all difficulty boundaries.
- The Unity/NUnit test sources compiled against the project's Unity-provided `nunit.framework.dll` with zero warnings and zero errors.
- The edit-mode suite defines 23 resolved NUnit cases.
- A Unity Test Runner result is still pending because Unity editor processes already had this project open, and launching a second batch-mode editor did not create an independent test result. The active editor was not interrupted or terminated.

### 2026-07-13 second-tranche verification record

- Fourteen portable behavior groups passed, including all eight original groups plus advantage/disadvantage extrema, typed dice, horror-first refill, regular-first damage removal, healing/strain without refill, and trauma triggering before result removal.
- The expanded runtime and Unity/NUnit test sources compiled under .NET 8 against the project's Unity-provided `nunit.framework.dll` with zero warnings and zero errors.
- The edit-mode suite now defines 39 resolved NUnit cases.
- Unity Test Runner execution remains pending for the same active-editor constraint recorded above.

### 2026-07-13 Foundation A completion record

- Seventeen portable behavior groups passed, adding staged rerolls, horror-1 reroll locks, one-die reactions, and typed pre-roll hand removal to the previous coverage.
- Runtime and Unity/NUnit test sources compiled under .NET 8 against the project's Unity-provided `nunit.framework.dll` with zero warnings and zero errors.
- The edit-mode suite now defines 52 resolved NUnit cases.
- Foundation A is complete at the pure rules-kernel level. Unity Test Runner execution remains pending while active Unity processes hold the project.

### 2026-07-13 Foundation B completion record

- Twenty-three portable behavior groups passed, adding injury, trauma, insight, recovery, stacking, fatal interception, and time-sensitive consequence transitions.
- Runtime and Unity/NUnit test sources compiled under .NET 8 against the project's Unity-provided `nunit.framework.dll` with zero warnings and zero errors.
- The edit-mode suite now defines 107 resolved NUnit cases.
- The user confirmed that Unity reports no compilation errors. Automated Unity Test Runner execution remains to be captured when the active editor can provide a result artifact.

### 2026-07-13 Foundation C completion record

- Twenty-six portable behavior groups passed, adding narrative scenes, structured side turns, surprise, reactions, attack legality, defense, damage, and injury thresholds.
- Runtime and Unity/NUnit test sources compiled under .NET 8 against the project's Unity-provided `nunit.framework.dll` with zero warnings and zero errors.
- The edit-mode suite now defines 130 resolved NUnit cases.
- All 64 Unity asset GUIDs in the mechanics tree are unique, and every mechanics/test asset has a matching `.meta` file.
- The user confirmed that the active Unity editor reports no console errors before this tranche. Automated Unity Test Runner result capture remains pending.

### 2026-07-13 Foundation D completion record

- Twenty-seven portable behavior groups passed, adding versioned state capture, deterministic commands, random continuation, event journaling, and certified replay.
- All 143 resolved NUnit cases executed successfully under .NET 8 against the project's Unity-provided `nunit.framework.dll`; compilation produced zero warnings and zero errors.
- The golden command scenario covers actions, reactions, damage, horror, injury, trauma, recovery, and insight.
- Saving after the sixth golden command and resuming from the captured state and random position produces the same final state and random continuation as uninterrupted execution.
- Altered certification fingerprints, duplicate command IDs, malformed snapshots, incomplete migration paths, and future-version snapshots are rejected.
- Foundation D passes its headless exit gate. The project is ready to enter M0 after Unity refresh confirms this tranche has no console errors.

## Next candidates

1. M0 character-creation sequence specification from Corebook Chapter 3 and the character sheet supplement.
2. Serializable investigator definition separated from mutable in-play state.
3. Background, personality, archetype, skill, equipment, advancement, and knack tranches with source citations.
4. Legal-example reproduction and precise rejection of illegal creation choices.

Candidate order is not a claim of implementation.
