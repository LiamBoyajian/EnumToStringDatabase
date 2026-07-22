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

        AccessIconsDb.DbPath = _tempDbPath;
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
        Assert.AreEqual(true, AccessIconsDb.PutIcon(new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE")));
    }

    [TestMethod]
    public void TestGetFile()
    {
        Assert.AreEqual(true, AccessIconsDb.PutIcon(new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE")));

        Assert.AreEqual("EXAMPLE", AccessIconsDb.GetFileAddress(AbstractPlant.Rt.Health, 16));
    }


    [TestMethod]
    public void TestPutConfirmValue()
    {
        Assert.AreEqual(true, AccessIconsDb.PutIcon(new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE")));

        Assert.AreEqual("EXAMPLE", AccessIconsDb.GetFileAddress(AbstractPlant.Rt.Health, 16));

        Assert.AreEqual(true, AccessIconsDb.PutIcon(new Entry(AbstractPlant.Rt.Health, 16, "FORSEN")));

        Assert.AreEqual("FORSEN", AccessIconsDb.GetFileAddress(AbstractPlant.Rt.Health, 16));
    }

    [TestMethod]
    public void TestPutNull()
    {
        Assert.AreEqual(true, AccessIconsDb.PutIcon(new Entry(AbstractPlant.Rt.Health, 64, null)));
    }

    [TestMethod]
    public void TestPutBadInt()
    {
        Assert.AreEqual(false, AccessIconsDb.PutIcon(new Entry(AbstractPlant.Rt.Health, -1, "EXAMPLE")));
    }

    // TEST ENTRYCOUNT
    [TestMethod]
    public void Test()
    {
        //Assert.AreEqual(2, AccessIconsDb.IconEntryCount(AbstractPlant.Rt.Health));
    }
}