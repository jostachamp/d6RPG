using ArkhamHorror.Mechanics.Dice;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class D3Tests
    {
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        public void FromD6_DividesByTwoAndRoundsUp(int d6Result, int expected)
        {
            Assert.That(D3.FromD6(d6Result), Is.EqualTo(expected));
        }
    }
}
