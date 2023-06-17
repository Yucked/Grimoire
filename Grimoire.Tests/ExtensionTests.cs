using Grimoire.Sources;

namespace Grimoire.Tests;

[TestClass]
public class ExtensionTests {
    [DataTestMethod]
    [DataRow("Ace Novel - Manga Adaptation")]
    [DataRow("Demon Slayer: Kimetsu no Yaiba ")]
    [DataRow("Haikyuu!! (New Special!)")]
    [DataRow("One-Punch Man")]
    [DataRow("“Manga Name” manga manga")]
    [DataRow("#Dense #Summer #Firstlove")]
    public void TestHashing(string input) {
        var id = input.GetIdFromName();
        var name = id.GetNameFromId();

        Assert.IsNotNull(id);
        Assert.IsTrue(input.Equals(name), $"{input}\n{name}");
    }
}