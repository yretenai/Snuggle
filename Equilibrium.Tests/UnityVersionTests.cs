using Equilibrium.Meta;
using Equilibrium.Models;
using NUnit.Framework;

namespace Equilibrium.Tests {
    public class UnityVersionTests {
        [TestCase("2019.4.15f1", 2019, 4, 15, UnityBuildType.Final, 1, 0), TestCase("2019.4.15f1\n2", 2019, 4, 15, UnityBuildType.Final, 1, 2), TestCase("2019.4.15f1-2", 2019, 4, 15, UnityBuildType.Final, 1, 2), TestCase("5.x.x", 5, 0, 0, UnityBuildType.None, 0, 0)]
        public void ParseTest(string test, int major, int minor, int build, UnityBuildType type, int revision, int extra) {
            var expectedVersion = new UnityVersion(major, minor, build, revision, type, extra);
            var actualVersion = UnityVersion.Parse(test);
            Assert.AreEqual(expectedVersion, actualVersion);
        }

        [Test]
        public void ComparisonTest() {
            Assert.IsTrue(new UnityVersion(1) > new UnityVersion(0));
            Assert.IsTrue(new UnityVersion(1) < new UnityVersion(2));
            Assert.IsTrue(new UnityVersion(1, 2, 3, 5) > new UnityVersion(1, 2, 3, 4));
            Assert.IsTrue(new UnityVersion(1, 2, 5, 4) > new UnityVersion(1, 2, 3, 4));
            Assert.IsTrue(new UnityVersion(1, 5, 3, 4) > new UnityVersion(1, 2, 3, 4));
        }

        [Test]
        public void EqualityTest() {
            Assert.AreEqual(new UnityVersion(1), new UnityVersion(1));
            Assert.AreNotEqual(new UnityVersion(1), new UnityVersion(2));
            Assert.IsFalse(new UnityVersion(1, type: UnityBuildType.None) > new UnityVersion(1, type: UnityBuildType.Final));
            Assert.IsFalse(new UnityVersion(1, type: UnityBuildType.None) < new UnityVersion(1, type: UnityBuildType.Final));
        }
    }
}
