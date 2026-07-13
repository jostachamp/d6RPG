using ArkhamHorror.Mechanics.DynamicPool;
using ArkhamHorror.Mechanics.Interaction;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class InteractionTests
    {
        [TestCase(5, true, true)]
        [TestCase(5, false, false)]
        [TestCase(6, true, false)]
        public void Engagement_RequiresDistanceAndAccessibility(int distance, bool accessible, bool expected)
        {
            Assert.That(RangeRules.IsEngaged(distance, accessible), Is.EqualTo(expected));
        }

        [Test]
        public void Distance_IsMeasuredInFiveFootIncrementsRoundedUp()
        {
            Assert.That(RangeRules.FiveFootIncrementsFor(0), Is.Zero);
            Assert.That(RangeRules.FiveFootIncrementsFor(1), Is.EqualTo(1));
            Assert.That(RangeRules.FiveFootIncrementsFor(6), Is.EqualTo(2));
        }

        [Test]
        public void MeleeAttack_RequiresVisibleReachableTargetWithinListedRange()
        {
            var profile = new AttackProfile(AttackType.Melee, 5, 2, 2);

            AttackValidationResult legal = AttackRules.Validate(profile, Context(5));
            AttackValidationResult unreachable = AttackRules.Validate(profile, Context(5, reachable: false));
            AttackValidationResult distant = AttackRules.Validate(profile, Context(10));

            Assert.That(legal.IsLegal, Is.True);
            Assert.That(legal.DefenseSkill, Is.EqualTo(DefenseSkill.MeleeCombat));
            Assert.That(unreachable.FailureReason, Is.EqualTo(AttackFailureReason.TargetNotReachable));
            Assert.That(distant.FailureReason, Is.EqualTo(AttackFailureReason.TargetOutOfRange));
        }

        [Test]
        public void RangedAttack_RequiresSightClearLineRangeAndNoEngagement()
        {
            var profile = new AttackProfile(AttackType.Ranged, 30, 2, null);

            Assert.That(AttackRules.Validate(profile, Context(20)).IsLegal, Is.True);
            Assert.That(AttackRules.Validate(profile, Context(20, visible: false)).FailureReason,
                Is.EqualTo(AttackFailureReason.TargetNotVisible));
            Assert.That(AttackRules.Validate(profile, Context(20, clearLine: false)).FailureReason,
                Is.EqualTo(AttackFailureReason.LineOfFireBlocked));
            Assert.That(AttackRules.Validate(profile, Context(20, attackerEngaged: true)).FailureReason,
                Is.EqualTo(AttackFailureReason.AttackerEngaged));
        }

        [Test]
        public void PistolException_AllowsOnlyAnEngagedOpponentAsTargetWhileEngaged()
        {
            var pistol = new AttackProfile(AttackType.Ranged, 30, 2, 3, canFireWhileEngaged: true);

            Assert.That(AttackRules.Validate(
                pistol, Context(5, attackerEngaged: true, targetEngaged: true)).IsLegal, Is.True);
            Assert.That(AttackRules.Validate(
                pistol, Context(20, attackerEngaged: true, targetEngaged: false)).FailureReason,
                Is.EqualTo(AttackFailureReason.EngagedWeaponRequiresEngagedTarget));
        }

        [Test]
        public void SuccessfulDefenseReaction_NegatesAttackDamageAndInjury()
        {
            var profile = new AttackProfile(AttackType.Melee, 5, 3, 2);
            ComplexActionResult attack = ComplexActionResolver.Evaluate(
                new[] { 6, 6 }, SkillLevel.Bad, ActionDifficulty.Standard);
            var defensePool = new DicePool(1);
            ComplexActionResult defense = new ComplexActionResolver(new SequenceDieRoller(5)).PerformReaction(
                defensePool, new DiceSelection(1, 0), SkillLevel.Normal);

            AttackResolution result = AttackRules.Resolve(profile, attack, defense);

            Assert.That(result.Hit, Is.False);
            Assert.That(result.Damage, Is.Zero);
            Assert.That(result.InflictsInjury, Is.False);
        }

        [Test]
        public void UndefendedHit_DealsDamageAndUsesSuccessesAgainstInjuryRating()
        {
            var profile = new AttackProfile(AttackType.Melee, 5, 3, 2);
            ComplexActionResult twoSuccesses = ComplexActionResolver.Evaluate(new[] { 6, 6 }, SkillLevel.Bad);
            ComplexActionResult oneSuccess = ComplexActionResolver.Evaluate(new[] { 6 }, SkillLevel.Bad);

            AttackResolution injury = AttackRules.Resolve(profile, twoSuccesses);
            AttackResolution noInjury = AttackRules.Resolve(profile, oneSuccess);

            Assert.That(injury.Hit, Is.True);
            Assert.That(injury.Damage, Is.EqualTo(3));
            Assert.That(injury.InflictsInjury, Is.True);
            Assert.That(noInjury.InflictsInjury, Is.False);
        }

        [Test]
        public void DashInjuryRating_NeverInflictsAnInjury()
        {
            var profile = new AttackProfile(AttackType.Ranged, 30, 1, null);
            ComplexActionResult attack = ComplexActionResolver.Evaluate(new[] { 6, 6, 6 }, SkillLevel.Bad);

            Assert.That(AttackRules.Resolve(profile, attack).InflictsInjury, Is.False);
        }

        [Test]
        public void ReactiveEffectContract_NegatesAnySuccessfulInitiatingEffect()
        {
            Assert.That(ReactiveEffectRules.TakesEffect(true, true), Is.False);
            Assert.That(ReactiveEffectRules.TakesEffect(true, false), Is.True);
            Assert.That(ReactiveEffectRules.TakesEffect(false, false), Is.False);
        }

        private static AttackContext Context(
            int distance,
            bool visible = true,
            bool reachable = true,
            bool clearLine = true,
            bool attackerEngaged = false,
            bool targetEngaged = false)
        {
            return new AttackContext(
                distance, visible, reachable, clearLine, attackerEngaged, targetEngaged);
        }
    }
}
