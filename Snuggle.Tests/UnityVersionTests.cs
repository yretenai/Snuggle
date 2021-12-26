using NUnit.Framework;
using Snuggle.Core.Meta;
using Snuggle.Core.Models;

namespace Snuggle.Tests;


[TestFixture]
public class UnityVersionTests {
    [TestCase("2019.4.15f1", null, null, 2019, 4, 15, UnityBuildType.Final, 1, null)]
    [TestCase("2019.4.15f1\n2", null, "2019.4.15f1.2", 2019, 4, 15, UnityBuildType.Final, 1, "\n2")]
    [TestCase("2019.4.15f1-2", null, "2019.4.15f1.2", 2019, 4, 15, UnityBuildType.Final, 1, "-2")]
    [TestCase("2020.3.18f1c1", null, null, 2020, 3, 18, UnityBuildType.Final, 1, "c1")]
    [TestCase("5.x.x", "5.0.0", null, 5, 0, 0, UnityBuildType.None, 0, null)]
    public void ParseTest(string test, string? unsafeTest, string? safeTest, int major, int minor, int build, UnityBuildType type, int revision, string? extra) {
        var expectedVersion = new UnityVersion(major, minor, build, type, revision, extra);
        var actualVersion = UnityVersion.Parse(test);
        Assert.AreEqual(expectedVersion, actualVersion);
        Assert.AreEqual(unsafeTest ?? test, actualVersion.ToString());
        Assert.AreEqual(safeTest ?? unsafeTest ?? test, actualVersion.ToStringSafe());
    }

    [Test]
    public void ComparisonTest() {
        Assert.IsTrue(new UnityVersion(1) > new UnityVersion(0));
        Assert.IsTrue(new UnityVersion(1) < new UnityVersion(2));
        Assert.IsTrue(new UnityVersion(1) > 0);
        Assert.IsTrue(new UnityVersion(1, 1) > 0);
        Assert.IsTrue(new UnityVersion(1) < 2);
        Assert.IsTrue(new UnityVersion(1, 1) < 2);
        Assert.IsTrue(new UnityVersion(1, 2, 4) > new UnityVersion(1, 2, 3));
        Assert.IsTrue(new UnityVersion(1, 3, 3) > new UnityVersion(1, 2, 3));
        Assert.IsTrue(new UnityVersion(2, 2, 3) > new UnityVersion(1, 2, 3));
    }

    [Test]
    public void EqualityTest() {
        Assert.AreEqual(new UnityVersion(1), new UnityVersion(1));
        Assert.AreNotEqual(new UnityVersion(1), new UnityVersion(2));
        Assert.IsFalse(new UnityVersion(1, Type: UnityBuildType.None) > new UnityVersion(1, Type: UnityBuildType.Final));
        Assert.IsFalse(new UnityVersion(1, Type: UnityBuildType.None) < new UnityVersion(1, Type: UnityBuildType.Final));
    }
}
