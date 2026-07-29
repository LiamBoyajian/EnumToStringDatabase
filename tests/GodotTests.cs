using System;
using System.IO;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;
using static GdUnit4.Assertions;
using GdUnit4;
using Godot;

namespace Main.addons.EnumToIcon.EnumToStringDatabase.tests;

[TestSuite]
public class GodotTests
{
    private string _tempDbPath;
    private string _tempDirPath;
    private SqliteConnection _connection;
    private MemoryToDb _godotMemory;

    private Entry _health16;
    private Entry _secondHealth16;
    private Entry _thirdHealth16;
    private Entry _health32;
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

        _tempDirPath = Path.Combine(Path.GetTempPath(), "GodotTest_" + Guid.NewGuid().ToString("N")) + "\\";
        //Directory.CreateDirectory(_tempDbPath);

        _godotMemory = AutoFree(new MemoryToDb());
        _godotMemory?._Ready();
        if (_godotMemory != null)
            _godotMemory.FolderPath = _tempDirPath;

        _health16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE");
        _secondHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE2");
        _thirdHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE3");
        _health32 = new Entry(AbstractPlant.Rt.Health, 32, "EXAMPLE3");
        _chlorophyll16 = new Entry(AbstractPlant.Rt.Chlorophyll, 16, "EXAMPLE");
    }

    [BeforeTest]
    public void TestInitializeGd()
    {
        Directory.CreateDirectory(_tempDirPath);

        using var command = _connection.CreateCommand();
        command.CommandText = """
                              DELETE FROM IdToFile;
                              DELETE FROM ValueEnum;
                              DELETE FROM sqlite_sequence WHERE name IN ('IdToFile', 'ValueEnum');
                              """;
        command.ExecuteNonQuery();
    }

    [AfterTest]
    public void TearDown()
    {
        // Truncate SQLite test tables or clear static collections
        AccessIconsDb.ClearDatabase();
        _godotMemory.ClearCache();

        if (Directory.Exists(_tempDirPath))
        {
            Directory.Delete(_tempDirPath, recursive: true);
        }
    }

    [After]
    public void ClassCleanupGd()
    {
        _connection?.Close();
        _connection?.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);

        GC.Collect();
        GC.WaitForPendingFinalizers();

        if (Directory.Exists(_tempDirPath))
        {
            Directory.Delete(_tempDirPath, recursive: true);
        }
    }

    [TestCase]
    public void Test()
    {
    }

    [TestCase]
    public void CreateTestDirWorks()
    {
        File.Create(_tempDirPath + _health16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _secondHealth16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _chlorophyll16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _thirdHealth16.DataWildcardClone()).Close();
        AssertBool(File.Exists(_tempDirPath + _health16.DataWildcardClone())).IsTrue();
        AssertBool(File.Exists(_tempDirPath + _secondHealth16.DataWildcardClone())).IsTrue(); //Same as previous file
        AssertBool(File.Exists(_tempDirPath + _thirdHealth16.DataWildcardClone())).IsTrue();
        AssertBool(File.Exists(_tempDirPath + _chlorophyll16.DataWildcardClone())).IsTrue();
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

        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(_health16.CopyWildcardClone(0));
    }

    [TestCase]
    public void RequestDataCacheMiss()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        _godotMemory.ClearCache();
        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(_health16.CopyWildcardClone(0));
    }

    [TestCase]
    public void RequestDataDoesNotExist()
    {
        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(null);
    }

    [TestCase]
    public void RequestDataNegCopy()
    {
        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(null);
    }

    [TestCase]
    public void RequestDataWildcard()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_health16.DataWildcardClone())).IsEqual(_health16.CopyWildcardClone(0));
    }

    [TestCase]
    public void RequestDataWildcardAlternatives()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_secondHealth16.DataWildcardClone()))
            .IsEqual(_health16.CopyWildcardClone(0));
    }

    [TestCase]
    public void CheckData()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();
        AssertBool(_godotMemory.PutData(_chlorophyll16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_health16)).IsEqual(_health16.CopyWildcardClone(0));
        AssertObject(_godotMemory.CheckData(_health16)).IsEqual(_health16.CopyWildcardClone(0));
    }

    [TestCase]
    public void RequestBatchData()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();
        AssertBool(_godotMemory.PutData(_chlorophyll16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_secondHealth16.DataWildcardClone()))
            .IsEqual(_health16.CopyWildcardClone(0));
    }

    [TestCase]
    public void RequestBatchDataCopy()
    {
        AssertBool(_godotMemory.PutData(_health16)).IsTrue();
        AssertBool(_godotMemory.PutData(_secondHealth16)).IsTrue();
        AssertBool(_godotMemory.PutData(_chlorophyll16)).IsTrue();

        AssertObject(_godotMemory.RequestData(_secondHealth16.DataWildcardClone()))
            .IsEqual(_health16.CopyWildcardClone(0));
    }

    //Initialization

    [TestCase]
    public void DirNoChangesMade()
    {
        _godotMemory.ValidateIconDirectory(true);
        AssertInt(_godotMemory.ValidateIconDirectory(true)).IsEqual(-2);
    }

    [TestCase]
    public void InitFromDir()
    {
        File.Create(_tempDirPath + _health16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _health32.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _chlorophyll16.DataWildcardClone()).Close();

        AssertInt(_godotMemory.ValidateIconDirectory(true)).IsEqual(3);
    }

    [TestCase]
    public void InitFromDirDuplicateValues()
    {
        File.Create(_tempDirPath + _health16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _secondHealth16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _chlorophyll16.DataWildcardClone()).Close();

        AssertInt(_godotMemory.ValidateIconDirectory(true)).IsEqual(2);
    }

    [TestCase]
    public void InitFromDirExcludeTokens()
    {
        _godotMemory.FileExcludeTokens = [".import"];

        File.Create(_tempDirPath + _health16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _health32.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _chlorophyll16.DataWildcardClone() + ".import").Close();

        AssertInt(_godotMemory.ValidateIconDirectory(true)).IsEqual(2);
    }

    [TestCase]
    public void TestFromInit()
    {
        File.Create(_tempDirPath + _health16.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _health32.DataWildcardClone()).Close();
        File.Create(_tempDirPath + _chlorophyll16.DataWildcardClone()).Close();

        AssertInt(_godotMemory.ValidateIconDirectory(true)).IsEqual(3);

        AssertBool(
                _health16.DataWildcardClone().EqualsWildcard(_godotMemory.RequestData(_health16.DataWildcardClone())))
            .IsTrue();
    }
}