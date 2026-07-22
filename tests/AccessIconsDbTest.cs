using System;
using System.IO;
using Main.addons.EnumToIcon.main;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Main.addons.EnumToIcon.tests;

[TestClass]
public class AccessIconsDbTest
{
    private static string _tempDbPath;
    private static SqliteConnection _connection;
    private static Entry _basicEntry;
    private static Entry _secondEntry;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _connection = new SqliteConnection($"Data Source={_tempDbPath};");
        _connection.Open();

        using var command = _connection.CreateCommand();
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons", "EnumToIcon", "main",
            "db_declaration.sql");
        command.CommandText = File.ReadAllText(scriptPath);
        command.ExecuteNonQuery();

        AccessIconsDb.DbData = _tempDbPath;

        _basicEntry = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE");
        _secondEntry = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE2");
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);
    }

    [TestMethod]
    public void TestPut()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_basicEntry));
    }

    [TestMethod]
    public void TestPutDuplicate()
    {
        Assert.AreEqual(false, AccessIconsDb.PutEntry(_basicEntry));
    }

    [TestMethod]
    public void TestPutAlternate()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondEntry));
    }

    [TestMethod]
    public void TestIconEntryCount()
    {
        Assert.AreEqual(2, AccessIconsDb.IconEntryCount(AbstractPlant.Rt.Health, -1));
    }

    [TestMethod]
    public void TestIconEntryCountAllOfType()
    {
        Assert.AreEqual(2, AccessIconsDb.IconEntryCount(AbstractPlant.Rt.Health, true));
    }

    [TestMethod]
    public void TestGetFile()
    {
        Assert.AreEqual("EXAMPLE", AccessIconsDb.GetData(AbstractPlant.Rt.Health, 16));
    }

    [TestMethod]
    public void TestGetAlternate()
    {
        Assert.AreEqual("EXAMPLE2", AccessIconsDb.GetData(AbstractPlant.Rt.Health, 16, 1));
    }


    [TestMethod]
    public void TestPutDuplicateValue()
    {
        Assert.AreEqual(false, AccessIconsDb.PutEntry(_basicEntry));

        Assert.AreEqual("EXAMPLE", AccessIconsDb.GetData(AbstractPlant.Rt.Health, 16));

        Assert.AreEqual(true, AccessIconsDb.PutEntry(new Entry(AbstractPlant.Rt.Health, 16, "FORSEN")));

        Assert.AreEqual("EXAMPLE", AccessIconsDb.GetData(AbstractPlant.Rt.Health, 16));
    }

    [TestMethod]
    public void TestPutNewSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(new Entry(AbstractPlant.Rt.Health, 32, "FORSEN")));

        Assert.AreEqual("FORSEN", AccessIconsDb.GetData(AbstractPlant.Rt.Health, 32));
    }

    [TestMethod]
    public void TestPutNull()
    {
        Assert.AreEqual(false, AccessIconsDb.PutEntry(new Entry(AbstractPlant.Rt.Health, 64, null)));
    }

    [TestMethod]
    public void TestPutBadInt()
    {
        Assert.AreEqual(false, AccessIconsDb.PutEntry(new Entry(AbstractPlant.Rt.Health, -1, "EXAMPLE")));
    }

    // TEST ENTRYCOUNT
    [TestMethod]
    public void Test()
    {
        //Assert.AreEqual(2, AccessIconsDb.IconEntryCount(AbstractPlant.Rt.Health));
    }

    [TestMethod]
    public void TestUpdate()
    {
        Assert.AreEqual(1, AccessIconsDb.UpdateData(new Entry(_basicEntry.Enum, _basicEntry.Size, "NEWEXAMPLE")));
    }

    [TestMethod]
    public void TestUpdateTwoOptions()
    {
        Assert.AreEqual("NEWEXAMPLE",
            AccessIconsDb.GetData(_basicEntry.Enum, _basicEntry.Size,
                AccessIconsDb.HasEntry(new Entry(_basicEntry.Enum, _basicEntry.Size, "NEWEXAMPLE"))));
        Assert.AreEqual(1, AccessIconsDb.UpdateData(new Entry(_basicEntry.Enum, _basicEntry.Size, "NEWEXAMPLE2")));
        Assert.IsGreaterThan(-1, AccessIconsDb.HasEntry(new Entry(_basicEntry.Enum, _basicEntry.Size, "NEWEXAMPLE2")));
    }

    [TestMethod]
    public void TestRemoveAllOf()
    {
        Assert.AreEqual(4, AccessIconsDb.RemoveEntry(new Entry(null, -1, null)));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(new Entry(AbstractPlant.Rt.Health, 32, "FORSEN")));
        Assert.AreEqual(1, AccessIconsDb.RemoveEntry(new Entry(null, -1, null)));
    }


    [TestMethod]
    public void TestUpdateDupe()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(new Entry(_basicEntry.Enum, _basicEntry.Size, "FORSEN")));
        Assert.IsGreaterThan(-1, AccessIconsDb.HasEntry(new Entry(_basicEntry.Enum, _basicEntry.Size, "FORSEN")));
    }

    [TestMethod]
    public void TestRemove()
    {
        Assert.AreEqual(1, AccessIconsDb.RemoveEntry(new Entry(_basicEntry.Enum, _basicEntry.Size, "FORSEN")));
    }
}