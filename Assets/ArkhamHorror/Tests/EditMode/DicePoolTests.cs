using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class DicePoolTests
    {
        [Test]
        public void PlayerPool_StartsFilledWithSixDice()
        {
            DicePool pool = DicePool.CreatePlayerPool();

            Assert.That(pool.Maximum, Is.EqualTo(6));
            Assert.That(pool.Limit, Is.EqualTo(6));
            Assert.That(pool.AvailableDice, Is.EqualTo(6));
        }

        [Test]
        public void Spend_RemovesDiceAndRefillRestoresToLimit()
        {
            var pool = new DicePool(6, 4, 4);

            pool.Spend(3);
            pool.Refill();

            Assert.That(pool.AvailableDice, Is.EqualTo(4));
        }

        [Test]
        public void AddedDice_MayExceedLimitAndMaximum()
        {
            var pool = new DicePool(6);

            pool.Add(2);
            pool.Refill();

            Assert.That(pool.AvailableDice, Is.EqualTo(8));
            Assert.That(pool.Limit, Is.EqualTo(6));
            Assert.That(pool.Maximum, Is.EqualTo(6));
        }

        [Test]
        public void Refill_AddsHorrorDiceFirstThenRegularDice()
        {
            var pool = new DicePool(6, 6, 0, 0, 0);
            pool.SufferHorror(3);

            pool.Refill();

            Assert.That(pool.HorrorDice, Is.EqualTo(3));
            Assert.That(pool.RegularDice, Is.EqualTo(3));
        }

        [Test]
        public void SufferDamage_DiscardsRegularDiceBeforeHorrorDice()
        {
            var pool = new DicePool(6, 6, regularDice: 4, horrorDice: 2, horrorLimit: 3);

            pool.SufferDamage(3);

            Assert.That(pool.Limit, Is.EqualTo(3));
            Assert.That(pool.RegularDice, Is.EqualTo(1));
            Assert.That(pool.HorrorDice, Is.EqualTo(2));
            Assert.That(pool.HorrorLimit, Is.EqualTo(3));
        }

        [Test]
        public void HealDamage_IncreasesLimitWithoutRefillingPool()
        {
            var pool = new DicePool(6, 2, regularDice: 0, horrorDice: 2, horrorLimit: 3);

            pool.HealDamage(2);

            Assert.That(pool.Limit, Is.EqualTo(4));
            Assert.That(pool.AvailableDice, Is.EqualTo(2));
            Assert.That(pool.HorrorLimit, Is.EqualTo(3));
        }

        [Test]
        public void Strain_RestoresLimitWithoutRefillingAndSchedulesInjury()
        {
            var pool = new DicePool(6, 2, regularDice: 0, horrorDice: 2, horrorLimit: 3);

            pool.Strain();

            Assert.That(pool.Limit, Is.EqualTo(6));
            Assert.That(pool.AvailableDice, Is.EqualTo(2));
            Assert.That(pool.HorrorLimit, Is.EqualTo(3));
            Assert.That(pool.PendingStrainInjuries, Is.EqualTo(1));
            Assert.That(pool.ResolvePendingStrainInjury(), Is.True);
            Assert.That(pool.ResolvePendingStrainInjury(), Is.False);
        }

        [Test]
        public void TypedSpend_RemovesTheSelectedKinds()
        {
            var pool = new DicePool(6, 6, regularDice: 3, horrorDice: 3, horrorLimit: 3);

            pool.Spend(new DiceSelection(regularDice: 1, horrorDice: 2));

            Assert.That(pool.RegularDice, Is.EqualTo(2));
            Assert.That(pool.HorrorDice, Is.EqualTo(1));
        }

        [Test]
        public void HorrorLimit_IsCappedByPoolMaximum()
        {
            var pool = new DicePool(6);

            pool.SufferHorror(100);

            Assert.That(pool.HorrorLimit, Is.EqualTo(6));
        }

        [Test]
        public void Refill_WhenLimitIsBelowHorrorLimit_AddsOnlyHorrorDice()
        {
            var pool = new DicePool(6, 2, regularDice: 0, horrorDice: 0, horrorLimit: 3);

            pool.Refill();

            Assert.That(pool.RegularDice, Is.Zero);
            Assert.That(pool.HorrorDice, Is.EqualTo(2));
        }

        [Test]
        public void DamageToZero_MakesCharacterWoundedWithoutChangingHorrorLimit()
        {
            var pool = new DicePool(6, 2, regularDice: 0, horrorDice: 2, horrorLimit: 3);

            pool.SufferDamage(100);

            Assert.That(pool.Limit, Is.Zero);
            Assert.That(pool.AvailableDice, Is.Zero);
            Assert.That(pool.HorrorLimit, Is.EqualTo(3));
            Assert.That(pool.IsWounded, Is.True);
        }

        [Test]
        public void ReducingHorrorLimit_DoesNotConvertOrDiscardExistingHorrorDice()
        {
            var pool = new DicePool(6, 6, regularDice: 3, horrorDice: 3, horrorLimit: 3);

            pool.ReduceHorror(2);

            Assert.That(pool.HorrorLimit, Is.EqualTo(1));
            Assert.That(pool.RegularDice, Is.EqualTo(3));
            Assert.That(pool.HorrorDice, Is.EqualTo(3));
        }

        [Test]
        public void ZeroDamage_DoesNotRemoveTemporarilyAddedDice()
        {
            var pool = new DicePool(6);
            pool.Add(2);

            pool.SufferDamage(0);

            Assert.That(pool.AvailableDice, Is.EqualTo(8));
        }
    }
}
