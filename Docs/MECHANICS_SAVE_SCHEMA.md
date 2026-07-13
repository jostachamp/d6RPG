# Mechanics Save Schema and Migration Policy

This document defines the portable persistence contract for the pure Arkham Horror mechanics assembly. It describes mechanics data, not a Unity scene, user interface, platform save slot, or authored campaign format.

## Current schema

The current mechanics snapshot version is `1`, declared by `MechanicsSnapshot.CurrentVersion`.

Version 1 stores each shared mechanics actor in ordinal ID order with:

- Dice-pool maximum, current limit, regular dice, horror dice, horror limit, and pending strain injuries.
- Insight limit and current insight.
- Injury instances in numeric ID order, including kind, lost sense, temporary Heavy Blow state, and Dire treatment deadline state.
- The next injury ID and death state.
- Trauma session modifier and Lost Forever state.

Actor IDs are identity keys and must be unique. Injury IDs are positive, strictly increasing within an actor, and lower than the stored next injury ID. Invalid enum/state combinations are rejected before restoration.

## Deterministic continuation

Certified mechanics execution requires `DeterministicDieRoller`. Its `RandomState` stores the complete PCG state and odd stream increment. Restoring both values resumes at the exact next random result.

`SystemDieRoller` remains available for non-certified callers, but its internal `System.Random` state is intentionally not treated as portable save data.

A replay recording consists of:

1. A current-version initial mechanics snapshot.
2. Initial deterministic random state.
3. Ordered immutable commands with unique stable IDs.
4. Expected final-state and event-log fingerprints.

Every command produces an ordered mechanical event. Event data includes rolled natural values and resolved outcomes so a failure can be audited without rerunning hidden presentation logic.

## Fingerprints

Snapshots and event logs have canonical, culture-independent text representations. Actor and injury ordering cannot change their result. FNV-1a 64-bit fingerprints provide fast deterministic comparison and accidental-corruption detection.

These fingerprints are replay assertions, not cryptographic signatures. A hostile or competitive environment must add authenticated storage outside the mechanics assembly.

## Migration rules

- A snapshot must never be silently interpreted as another version.
- Restoring an old snapshot requires a registered migration for every adjacent version up to the current version.
- Each migrator advances exactly one version and returns a snapshot labeled with that exact target version.
- Missing, duplicate, skipped, null, or incorrectly labeled migrations fail without partially restoring state.
- A snapshot from a newer version is rejected. Downgrades are not supported.
- Migrations must preserve stable actor, injury, command, and decision identity unless a documented schema change explicitly replaces an identity.
- Every future migration requires before/after fixtures, canonical-state assertions, and an entry in `MECHANICS_IMPLEMENTATION.md`.
- Destructive migrations require retaining the original save outside this rules assembly so recovery remains possible.

## Save boundaries

Version 1 captures the shared mutable actor mechanics required before M0. Player-character definitions, NPC definitions, inventory catalogs, authored campaign facts, Unity object references, and presentation state do not belong in this schema.

Foundation C scenes can be reconstructed deterministically from their declaration commands and actor state. A compact mid-scene snapshot will require a later schema version when M2 establishes final ownership for spatial facts and PC/NPC interaction state; version 1 does not invent that ownership early.

Wall-clock time is never read by the rules kernel. Callers record elapsed rule time and submit explicit deadline or recovery commands.

## Adapter requirements

A JSON, binary, database, cloud, or Unity save adapter may encode the public snapshot and command fields, but it must:

- Preserve integer widths and ordinal IDs exactly.
- Preserve command order and case-sensitive IDs.
- Persist random state as unsigned 64-bit values without numeric rounding.
- Validate schema version before constructing live state.
- Verify canonical fingerprints after decoding and after replay.
- Never serialize Unity instance IDs or object references into mechanics data.
