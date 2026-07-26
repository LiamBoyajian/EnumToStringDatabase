using System;
using System.IO;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;
using static GdUnit4.Assertions;
using GdUnit4;

namespace Main.addons.EnumToIcon.EnumToStringDatabase.tests;

[TestSuite]
public class GodotTests
{
    private string _tempDbPath;
    private SqliteConnection _connection;
    private MemoryToDb _godotMemory;

    private Entry _health16;
    private Entry _secondHealth16;
    private Entry _chlorophyll16;

    [Before]
    public void ClassInitializeGd()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _connection = new SqliteConnection($"Data Source={_tempDbPath};");
        _connection.Open();

        using var command = _connection.CreateCommand();
        string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons", "EnumToIcon",
            "EnumToStringDatabase", "main",
            "db_declaration.sql");
        command.CommandText = File.ReadAllText(scriptPath);
        command.ExecuteNonQuery();

        AccessIconsDb.DbData = _tempDbPath;

        _godotMemory = AutoFree(new MemoryToDb());
        _godotMemory?._Ready();

        _health16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE");
        _secondHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE2");
        _chlorophyll16 = new Entry(AbstractPlant.Rt.Chlorophyll, 16, "EXAMPLE");
    }

    [BeforeTest]
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

    [After]
    public void ClassCleanupGd()
    {
        _connection?.Close();
        _connection?.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);
    }

    [TestCase]
    public void Test()
    {
    }

    //--------------------------------------------------
    // TESTING MemoryToDb
    //--------------------------------------------------


    [TestCase]
    public void PutData()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
    }

    [TestCase]
    public void RequestData()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(_health16);
    }

    [TestCase]
    public void CheckBatchData()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();
        AssertBool(_godotMemory.PutData(_chlorophyll16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_secondHealth16.NullDataClone())).IsEqual(_health16);
    }

    [TestCase]
    public void CheckBatchDataCopy()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();
        AssertBool(_godotMemory.PutData(_chlorophyll16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_secondHealth16.NullDataClone(), 1)).IsEqual(_health16);
    }

    [TestCase]
    public void CheckData()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();
        AssertBool(_godotMemory.PutData(_chlorophyll16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(_health16);
        AssertObject(_godotMemory.CheckData(_health16)).IsEqual(_health16);
    }
}