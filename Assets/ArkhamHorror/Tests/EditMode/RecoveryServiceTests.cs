using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class RecoveryServiceTests
    {
        [Test]
        public void NastyCut_CapsHealingAndStrainAtThreeUntilHealed()
        {
            var pool = new DicePool(6, 2, 2);
            var injuries = new InjuryState();
            InjuryInstance cut = injuries.Apply(InjuryGenerator.FromBaseRoll(3, 0), pool);

            RecoveryService.HealDamage(pool, injuries, 100);
            Assert.That(pool.Limit, Is.EqualTo(3));

            pool.SufferDamage(1);
            RecoveryService.Strain(pool, injuries);
            Assert.That(pool.Limit, Is.EqualTo(3));

            Assert.That(injuries.TryHeal(cut.Id, 1), Is.True);
            RecoveryService.HealDamage(pool, injuries, 100);
            Assert.That(pool.Limit, Is.EqualTo(6));
        }

        [Test]
        public void NastyCut_DoesNotReduceALimitAlreadyAboveThree()
        {
            var pool = new DicePool(6, 5, 5);
            var injuries = new InjuryState();
            injuries.Apply(InjuryGenerator.FromBaseRoll(3, 0), pool);

            RecoveryService.HealDamage(pool, injuries, 1);
            RecoveryService.RecoverAllDamageNaturally(pool, injuries);

            Assert.That(pool.Limit, Is.EqualTo(5));
        }

        [Test]
        public void Comatose_ForcesZeroAndBlocksRecoveryUntilHealed()
        {
            var pool = new DicePool(6);
            var injuries = new InjuryState();
            InjuryInstance coma = injuries.Apply(InjuryGenerator.FromBaseRoll(9, 0), pool);

            Assert.That(pool.Limit, Is.Zero);
            RecoveryService.HealDamage(pool, injuries, 100);
            Assert.That(pool.Limit, Is.Zero);
            Assert.That(() => RecoveryService.Strain(pool, injuries), Throws.InvalidOperationException);

            Assert.That(injuries.TryHeal(coma.Id, 2), Is.True);
            RecoveryService.RecoverAllDamageNaturally(pool, injuries);
            Assert.That(pool.Limit, Is.EqualTo(6));
        }

        [Test]
        public void SuccessfulIntrospectionAndCounselingReduceHorrorByOne()
        {
            var pool = new DicePool(6);
            pool.SufferHorror(3);

            Assert.That(RecoveryService.ApplyIntrospection(pool, actionSucceeded: false), Is.False);
            Assert.That(pool.HorrorLimit, Is.EqualTo(3));
            Assert.That(RecoveryService.ApplyIntrospection(pool, actionSucceeded: true), Is.True);
            Assert.That(RecoveryService.ApplyCounseling(pool, actionSucceeded: true), Is.True);
            Assert.That(pool.HorrorLimit, Is.EqualTo(1));
        }

        [Test]
        public void SafeWeekClearsHorrorOnlyWhenNoHorrorWasGained()
        {
            var pool = new DicePool(6);
            pool.SufferHorror(4);

            Assert.That(RecoveryService.RecoverHorrorAfterSafeWeek(pool, horrorIncreasedDuringWeek: true), Is.False);
            Assert.That(pool.HorrorLimit, Is.EqualTo(4));
            Assert.That(RecoveryService.RecoverHorrorAfterSafeWeek(pool, horrorIncreasedDuringWeek: false), Is.True);
            Assert.That(pool.HorrorLimit, Is.Zero);
        }
    }
}
