using Main.addons.EnumToIcon.EnumToStringDatabase.main;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static GdUnit4.Assertions;

namespace Main.addons.EnumToIcon.EnumToStringDatabase.tests;

[TestClass]
public class GodotTests
{
    private static string _tempDbPath;
    private static SqliteConnection _connection;
    private static MemoryToDb _godotMemory;

    private static Entry _health16;
    private static Entry _secondHealth16;
    private static Entry _chlorophyll16;

    [ClassInitialize]
    public static void ClassInitializeGd(TestContext context)
    {
        _godotMemory = AutoFree(new MemoryToDb());

        _health16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE");
        _secondHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE2");
        _chlorophyll16 = new Entry(AbstractPlant.Rt.Chlorophyll, 16, "EXAMPLE");
    }

    [TestInitialize]
    public void TestInitializeGd()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
                              DELETE FROM IdToFile;
                              DELETE FROM ValueEnum;
                              DELETE FROM sqlite_sequence WHERE name IN ('IdToFile', 'ValueEnum');
                              """;
        command.ExecuteNonQuery();
    }

    [ClassCleanup]
    public static void ClassCleanupGd()
    {
        DbTests.ClassCleanup();
    }

    [TestMethod]
    public void Test()
    {
    }

    //--------------------------------------------------
    // TESTING MemoryToDb
    //--------------------------------------------------
    [TestMethod]
    public void RequestData()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(_health16, _godotMemory.RequestData(_health16));
    }

    [TestMethod]
    public void CheckBatchData()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(_health16, _godotMemory.RequestData(_health16.NullDataClone()));
    }

    [TestMethod]
    public void CheckData()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(_health16, _godotMemory.RequestData(_health16));
        Assert.AreEqual(_health16, _godotMemory.CheckData(_health16));
    }
}