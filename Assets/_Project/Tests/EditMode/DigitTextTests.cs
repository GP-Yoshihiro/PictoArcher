using System.Linq;
using NUnit.Framework;
using PictoArcher.Core;

namespace PictoArcher.Tests.EditMode
{
    public class DigitTextTests
    {
        [Test] public void Progress() => Assert.AreEqual("23/50", DigitText.Progress(23, 50));

        [TestCase(0f, "0:00")] [TestCase(59.9f, "0:59")] [TestCase(1425.4f, "23:45")] [TestCase(3665f, "61:05")]
        [TestCase(-3f, "0:00")] [TestCase(float.NaN, "0:00")]
        public void Duration(float seconds, string expected) => Assert.AreEqual(expected, DigitText.Duration(seconds));

        [Test] public void Count() => Assert.AreEqual("1234", DigitText.Count(1234));

        [Test] public void Outputs_HaveNoLetters() =>
            Assert.IsFalse((DigitText.Progress(1, 50) + DigitText.Duration(3665f) + DigitText.Count(-5)).Any(char.IsLetter));
    }
}
