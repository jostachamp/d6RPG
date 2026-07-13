using System;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Interaction
{
    public enum AttackType
    {
        Melee = 0,
        Ranged = 1
    }

    public enum DefenseSkill
    {
        MeleeCombat = 0,
        Agility = 1
    }

    public enum AttackFailureReason
    {
        None = 0,
        TargetNotVisible = 1,
        TargetOutOfRange = 2,
        TargetNotReachable = 3,
        LineOfFireBlocked = 4,
        AttackerEngaged = 5,
        EngagedWeaponRequiresEngagedTarget = 6
    }

    public sealed class AttackProfile
    {
        public AttackProfile(
            AttackType attackType,
            int rangeFeet,
            int damage,
            int? injuryRating,
            bool canFireWhileEngaged = false)
        {
            if (!Enum.IsDefined(typeof(AttackType), attackType))
            {
                throw new ArgumentOutOfRangeException(nameof(attackType));
            }

            if (rangeFeet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeFeet));
            }

            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            if (injuryRating.HasValue && injuryRating.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(injuryRating));
            }

            if (canFireWhileEngaged && attackType != AttackType.Ranged)
            {
                throw new ArgumentException("Only a ranged weapon can permit firing while engaged.", nameof(canFireWhileEngaged));
            }

            AttackType = attackType;
            RangeFeet = rangeFeet;
            Damage = damage;
            InjuryRating = injuryRating;
            CanFireWhileEngaged = canFireWhileEngaged;
        }

        public AttackType AttackType { get; }

        public int RangeFeet { get; }

        public int Damage { get; }

        public int? InjuryRating { get; }

        public bool CanFireWhileEngaged { get; }
    }

    public sealed class AttackContext
    {
        public AttackContext(
            int distanceFeet,
            bool targetVisible,
            bool targetReachable,
            bool clearLineOfFire,
            bool attackerEngagedWithAnyAdversary,
            bool targetIsEngagedAdversary)
        {
            if (distanceFeet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceFeet));
            }

            DistanceFeet = distanceFeet;
            TargetVisible = targetVisible;
            TargetReachable = targetReachable;
            ClearLineOfFire = clearLineOfFire;
            AttackerEngagedWithAnyAdversary = attackerEngagedWithAnyAdversary;
            TargetIsEngagedAdversary = targetIsEngagedAdversary;
        }

        public int DistanceFeet { get; }
        public bool TargetVisible { get; }
        public bool TargetReachable { get; }
        public bool ClearLineOfFire { get; }
        public bool AttackerEngagedWithAnyAdversary { get; }
        public bool TargetIsEngagedAdversary { get; }
    }

    public sealed class AttackValidationResult
    {
        internal AttackValidationResult(AttackFailureReason failureReason, DefenseSkill defenseSkill)
        {
            FailureReason = failureReason;
            DefenseSkill = defenseSkill;
        }

        public bool IsLegal => FailureReason == AttackFailureReason.None;
        public AttackFailureReason FailureReason { get; }
        public DefenseSkill DefenseSkill { get; }
    }

    public static class AttackRules
    {
        public static AttackValidationResult Validate(AttackProfile profile, AttackContext context)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (context == null) throw new ArgumentNullException(nameof(context));

            DefenseSkill defense = profile.AttackType == AttackType.Melee
                ? DefenseSkill.MeleeCombat
                : DefenseSkill.Agility;

            if (!context.TargetVisible)
                return new AttackValidationResult(AttackFailureReason.TargetNotVisible, defense);
            if (context.DistanceFeet > profile.RangeFeet)
                return new AttackValidationResult(AttackFailureReason.TargetOutOfRange, defense);

            if (profile.AttackType == AttackType.Melee)
            {
                return new AttackValidationResult(
                    context.TargetReachable ? AttackFailureReason.None : AttackFailureReason.TargetNotReachable,
                    defense);
            }

            if (!context.ClearLineOfFire)
                return new AttackValidationResult(AttackFailureReason.LineOfFireBlocked, defense);

            if (context.AttackerEngagedWithAnyAdversary)
            {
                if (!profile.CanFireWhileEngaged)
                    return new AttackValidationResult(AttackFailureReason.AttackerEngaged, defense);
                if (!context.TargetIsEngagedAdversary)
                    return new AttackValidationResult(AttackFailureReason.EngagedWeaponRequiresEngagedTarget, defense);
            }

            return new AttackValidationResult(AttackFailureReason.None, defense);
        }

        public static AttackResolution Resolve(
            AttackProfile profile,
            ComplexActionResult attack,
            ComplexActionResult defense = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            if (attack.ActionKind != ActionKind.Complex)
                throw new ArgumentException("An attack must be a complex action.", nameof(attack));
            if (defense != null && defense.ActionKind != ActionKind.Reaction)
                throw new ArgumentException("A defense must be a reaction.", nameof(defense));

            bool hit = ReactiveEffectRules.TakesEffect(attack.Succeeded, defense != null && defense.Succeeded);
            bool inflictsInjury = hit
                && profile.InjuryRating.HasValue
                && attack.SuccessCount >= profile.InjuryRating.Value;
            return new AttackResolution(hit, hit ? profile.Damage : 0, inflictsInjury);
        }
    }

    public sealed class AttackResolution
    {
        internal AttackResolution(bool hit, int damage, bool inflictsInjury)
        {
            Hit = hit;
            Damage = damage;
            InflictsInjury = inflictsInjury;
        }

        public bool Hit { get; }
        public int Damage { get; }
        public bool InflictsInjury { get; }
    }

    public static class ReactiveEffectRules
    {
        public static bool TakesEffect(bool initiatingActionSucceeded, bool reactionSucceeded)
        {
            return initiatingActionSucceeded && !reactionSucceeded;
        }
    }
}
