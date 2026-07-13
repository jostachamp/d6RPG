using ArkhamHorror.Mechanics.Dice;
using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class HorrorDiceTests
    {
        [Test]
        public void HorrorOne_TriggersTraumaEvenWhenAdvantageRemovesIt()
        {
            var pool = new DicePool(2, 2, regularDice: 1, horrorDice: 1, horrorLimit: 1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(1, 6));

            ComplexActionResult result = resolver.Perform(
                pool,
                new DiceSelection(regularDice: 0, horrorDice: 1),
                SkillLevel.Bad,
                rollCondition: RollCondition.Advantage);

            Assert.That(result.DieResults, Is.EqualTo(new[] { 6 }));
            Assert.That(result.TriggersTrauma, Is.True);
            Assert.That(result.HorrorOnesRolled, Is.EqualTo(1));
            Assert.That(result.TraumaRollModifier, Is.EqualTo(1));
        }

        [Test]
        public void MultipleHorrorOnes_IncreaseTraumaRollModifier()
        {
            var pool = new DicePool(3, 3, regularDice: 0, horrorDice: 3, horrorLimit: 3);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(1, 1, 4));

            ComplexActionResult result = resolver.Perform(
                pool,
                new DiceSelection(regularDice: 0, horrorDice: 3),
                SkillLevel.Good);

            Assert.That(result.HorrorOnesRolled, Is.EqualTo(2));
            Assert.That(result.TraumaRollModifier, Is.EqualTo(2));
            Assert.That(result.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void TypedRolls_PreserveTheirKinds()
        {
            var pool = new DicePool(2, 2, regularDice: 1, horrorDice: 1, horrorLimit: 1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(5, 2));

            ComplexActionResult result = resolver.Perform(
                pool,
                new DiceSelection(regularDice: 1, horrorDice: 1),
                SkillLevel.Normal);

            Assert.That(result.AllRolls[0].Kind, Is.EqualTo(DieKind.Regular));
            Assert.That(result.AllRolls[1].Kind, Is.EqualTo(DieKind.Horror));
            Assert.That(result.AllRolls[0].NaturalResult, Is.EqualTo(5));
            Assert.That(result.AllRolls[1].NaturalResult, Is.EqualTo(2));
        }
    }
}
