using NUnit.Framework;
using Snuggle.Converters;
using Snuggle.Core;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models;
using Snuggle.Core.Models.Serialization;
using Snuggle.Core.Options;

namespace Snuggle.Tests;

[TestFixture]
public class PathFormatterTests {
    private static readonly UnityObjectInfo BlankObjectInfo = new(
        1,
        2,
        3,
        4,
        UnityClassId.Object,
        0,
        true,
        -1,
        true);

    private SerializedFile MockFile = null!;
    private SerializedObject BlankObject = null!;

    [SetUp]
    public void Setup() {
#pragma warning disable CS0618
        MockFile = new SerializedFile(SnuggleCoreOptions.Default);
#pragma warning restore CS0618
        BlankObject = new NamedObject(BlankObjectInfo, MockFile) { ObjectContainerPath = "CAB/Container/Path", Name = "BlankObject" };
        MockFile.Assets = new AssetCollection();
        MockFile.Assets.PlayerSettings = new PlayerSettings(BlankObjectInfo, MockFile) {
            ProductName = "MyGame",
            CompanyName = "MyCompany",
            ProjectName = "MyProject",
            OrganizationId = "MyId",
            BundleVersion = "1.2.3",
        };
    }

    [TestCase("/Foo/Bar/Baz", "Foo/Bar/Baz")]
    [TestCase("/Foo/{Id}/Baz", "Foo/1/Baz")]
    [TestCase("/Foo/{Type}/Baz", "Foo/Object/Baz")]
    [TestCase("/Foo/{Size}/Baz", "Foo/3/Baz")]
    [TestCase("/Foo/{Container}/Baz", "Foo/CAB/Container/Path/Baz")]
    [TestCase("/Foo/Bar/Baz.{Ext}", "Foo/Bar/Baz.bytes")]
    [TestCase("/Foo/Bar/{Name}", "Foo/Bar/BlankObject")]
    [TestCase("/Foo/{Company}/Baz", "Foo/MyCompany/Baz")]
    [TestCase("/Foo/{Organization}/Baz", "Foo/MyId/Baz")]
    [TestCase("/Foo/{Project}/Baz", "Foo/MyProject/Baz")]
    [TestCase("/Foo/{Product}/Baz", "Foo/MyGame/Baz")]
    [TestCase("/Foo/{ProductOrProject}/Baz", "Foo/MyGame/Baz")]
    [TestCase("/Foo/{Game}/Baz", "Foo/Default/Baz")]
    [TestCase("/Foo/{Version}/Baz", "Foo/1.2.3/Baz")]
    [TestCase("/Foo/{DoesNotExist}/Baz", "Foo/{DoesNotExist}/Baz")]
    [TestCase("{Project}/{Version}/{Type}/{Container}/{Name}.{Ext}", "MyProject/1.2.3/Object/CAB/Container/Path/BlankObject.bytes")]
    [TestCase("{ProductOrProject}/{Version}/{Type}/{ContainerOrName}.{Ext}", "MyGame/1.2.3/Object/CAB/Container/Path.bytes")]
    public void TestFormat(string template, string expected) {
#pragma warning disable CS0618
        Assert.AreEqual(expected, PathFormatter.Format(template, "bytes", BlankObject));
#pragma warning restore CS0618
    }
}
