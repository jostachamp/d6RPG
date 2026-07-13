using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class InjuryState
    {
        private readonly List<InjuryInstance> injuries = new List<InjuryInstance>();
        private readonly IReadOnlyList<InjuryInstance> readOnlyInjuries;
        private int nextId = 1;

        public InjuryState()
        {
            readOnlyInjuries = injuries.AsReadOnly();
        }

        internal void Restore(InjuryInstance[] restoredInjuries, bool isDead, int restoredNextId)
        {
            if (restoredInjuries == null)
            {
                throw new ArgumentNullException(nameof(restoredInjuries));
            }

            if (injuries.Count != 0 || nextId != 1)
            {
                throw new InvalidOperationException("Only a new injury state can be restored.");
            }

            int greatestId = 0;
            for (int index = 0; index < restoredInjuries.Length; index++)
            {
                InjuryInstance injury = restoredInjuries[index]
                    ?? throw new ArgumentException("A restored injury cannot be null.", nameof(restoredInjuries));
                if (injury.Id <= greatestId)
                {
                    throw new ArgumentException("Restored injury IDs must be positive and strictly increasing.", nameof(restoredInjuries));
                }

                greatestId = injury.Id;
                injuries.Add(injury);
            }

            if (restoredNextId <= greatestId)
            {
                throw new ArgumentOutOfRangeException(nameof(restoredNextId));
            }

            nextId = restoredNextId;
            IsDead = isDead;
        }

        public IReadOnlyList<InjuryInstance> Injuries => readOnlyInjuries;

        public int Count => injuries.Count;

        public bool IsDead { get; private set; }

        internal int NextId => nextId;

        public InjuryInstance Apply(
            InjuryResolution resolution,
            DicePool dicePool,
            SenseKind? lostSense = null,
            bool fatalOutcomeAvoided = false)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (dicePool == null)
            {
                throw new ArgumentNullException(nameof(dicePool));
            }

            if (resolution.ExistingInjuryModifier != Count)
            {
                throw new InvalidOperationException("The injury result was generated for a different existing injury count.");
            }

            if (resolution.IsFatal)
            {
                if (!fatalOutcomeAvoided)
                {
                    IsDead = true;
                }

                return null;
            }

            InjuryInstance instance = Add(resolution.Kind, lostSense);
            if (resolution.Kind == InjuryKind.Comatose || resolution.Kind == InjuryKind.Dire)
            {
                dicePool.SufferDamage(dicePool.Limit);
            }

            return instance;
        }

        public InjuryInstance SufferUnique(InjuryKind kind, bool fatalOutcomeAvoided = false)
        {
            if (!InjuryCatalog.IsUniqueEnvironmentalInjury(kind))
            {
                throw new ArgumentException("This method accepts only Burned, Choking, or Sickened.", nameof(kind));
            }

            if (CountOf(kind) >= 2)
            {
                if (!fatalOutcomeAvoided)
                {
                    IsDead = true;
                }

                return null;
            }

            return Add(kind, null);
        }

        public int CountOf(InjuryKind kind)
        {
            int count = 0;
            for (int index = 0; index < injuries.Count; index++)
            {
                if (injuries[index].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasActiveEffect(InjuryKind kind)
        {
            for (int index = 0; index < injuries.Count; index++)
            {
                if (injuries[index].Kind == kind
                    && (kind != InjuryKind.HeavyBlow || injuries[index].TemporaryEffectActive))
                {
                    return true;
                }
            }

            return false;
        }

        public void ResolveHeavyBlowEffectsAfterNextTurn()
        {
            for (int index = 0; index < injuries.Count; index++)
            {
                if (injuries[index].Kind == InjuryKind.HeavyBlow)
                {
                    injuries[index].TemporaryEffectActive = false;
                }
            }
        }

        public bool MarkDireInjuryTreated(int injuryId)
        {
            int index = FindIndex(injuryId);
            if (index < 0 || injuries[index].Kind != InjuryKind.Dire)
            {
                return false;
            }

            injuries[index].DireTreatmentPending = false;
            return true;
        }

        public bool ExpireDireTreatmentDeadline(int injuryId, bool fatalOutcomeAvoided = false)
        {
            int index = FindIndex(injuryId);
            if (index < 0
                || injuries[index].Kind != InjuryKind.Dire
                || !injuries[index].DireTreatmentPending)
            {
                return false;
            }

            injuries[index].DireTreatmentPending = false;
            if (!fatalOutcomeAvoided)
            {
                IsDead = true;
            }

            return true;
        }

        public bool TryHeal(int injuryId, int successes)
        {
            if (successes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(successes));
            }

            int index = FindIndex(injuryId);
            if (index < 0)
            {
                return false;
            }

            InjuryDefinition definition = InjuryCatalog.Get(injuries[index].Kind);
            if (successes < definition.HealingSuccesses)
            {
                return false;
            }

            injuries.RemoveAt(index);
            return true;
        }

        public bool TryHealNaturally(int injuryId, int weeksSinceSuffered)
        {
            if (weeksSinceSuffered < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weeksSinceSuffered));
            }

            int index = FindIndex(injuryId);
            if (index < 0)
            {
                return false;
            }

            int? requiredWeeks = InjuryCatalog.Get(injuries[index].Kind).NaturalHealingWeeks;
            if (!requiredWeeks.HasValue || weeksSinceSuffered < requiredWeeks.Value)
            {
                return false;
            }

            injuries.RemoveAt(index);
            return true;
        }

        public int GetPoolRecoveryCeiling(int poolMaximum)
        {
            if (poolMaximum < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(poolMaximum));
            }

            int ceiling = poolMaximum;
            for (int index = 0; index < injuries.Count; index++)
            {
                int? injuryCeiling = InjuryCatalog.Get(injuries[index].Kind).PoolRecoveryCeiling;
                if (injuryCeiling.HasValue)
                {
                    ceiling = Math.Min(ceiling, injuryCeiling.Value);
                }
            }

            return ceiling;
        }

        private InjuryInstance Add(InjuryKind kind, SenseKind? lostSense)
        {
            InjuryDefinition definition = InjuryCatalog.Get(kind);
            if (definition.RequiresSense != lostSense.HasValue)
            {
                throw new ArgumentException(
                    definition.RequiresSense
                        ? "Loss of a Sense requires the affected sense."
                        : "Only Loss of a Sense may specify an affected sense.",
                    nameof(lostSense));
            }

            var instance = new InjuryInstance(nextId++, kind, lostSense);
            injuries.Add(instance);
            return instance;
        }

        private int FindIndex(int injuryId)
        {
            for (int index = 0; index < injuries.Count; index++)
            {
                if (injuries[index].Id == injuryId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
