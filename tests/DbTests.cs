using System;
using System.Collections.Generic;
using System.IO;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Main.addons.EnumToIcon.EnumToStringDatabase.tests;

[TestClass]
public class DbTests
{
    private static string _tempDbPath;
    private static SqliteConnection _connection;

    private static Entry _firstHealth16;
    private static Entry _secondHealth16;
    private static Entry _thirdHealth16;
    private static Entry _healthNeg1;
    private static Entry _null16;
    private static Entry _nullStringHealth16;
    private static Entry _health32;
    private static Entry _chlorophyll16;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
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

        _firstHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE");
        _secondHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE2");
        _thirdHealth16 = new Entry(AbstractPlant.Rt.Health, 16, "EXAMPLE3");
        _healthNeg1 = new Entry(AbstractPlant.Rt.Health, -1, "EXAMPLE4");
        _null16 = new Entry(null, 16, "EXAMPLE5");
        _nullStringHealth16 = new Entry(AbstractPlant.Rt.Health, 16, null);

        _health32 = new Entry(AbstractPlant.Rt.Health, 32, "EXAMPLE6");
        _chlorophyll16 = new Entry(AbstractPlant.Rt.Chlorophyll, 16, "EXAMPLE7");
    }

    [TestInitialize]
    public void TestInitialize()
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
    public static void ClassCleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_tempDbPath))
            File.Delete(_tempDbPath);
    }

    // ENTRY

    [TestMethod]
    public void TestToString()
    {
        Assert.AreEqual("Main.main.scripts.core.plants.AbstractPlant+Rt_0_16_0_EXAMPLE", _firstHealth16.ToString());
        Assert.AreEqual("Main.main.scripts.core.plants.AbstractPlant+Rt_1_16_0_EXAMPLE7", _chlorophyll16.ToString());
    }

    [TestMethod]
    public void TestNullDataClone()
    {
        Assert.AreEqual("Main.main.scripts.core.plants.AbstractPlant+Rt_0_16_0_",
            _firstHealth16.NullDataClone().ToString());
    }

    [TestMethod]
    public void TestEnumDataClone()
    {
        Assert.AreEqual("Main.main.scripts.core.plants.AbstractPlant+Rt_0_-1_0_",
            _firstHealth16.EnumDataClone().ToString());
    }

    [TestMethod]
    public void FromString()
    {
        Assert.AreEqual(_firstHealth16, Entry.FromString(_firstHealth16.ToString()));
    }

    [TestMethod]
    public void FromStringAlternateSize()
    {
        Assert.AreEqual(_health32.ToString(),
            Entry.FromString("Main.main.scripts.core.plants.AbstractPlant+Rt_0_32_0_EXAMPLE6")
                .ToString());
    }

    [TestMethod]
    public void FromStringAlternate()
    {
        Assert.AreEqual(_chlorophyll16.ToString(),
            Entry.FromString("Main.main.scripts.core.plants.AbstractPlant+Rt_1_16_0_EXAMPLE7")
                .ToString());
    }


    [TestMethod]
    public void FromStringNullString()
    {
        Assert.Throws<ArgumentException>(() => Entry.FromString(null));
    }

    [TestMethod]
    public void FromStringbadType()
    {
        Assert.AreEqual(null,
            Entry.FromString("Main.main.scripts.core.plants.AbstractPlant+FAKEENUMLOL_0_16_0_EXAMPLE"));
    }

    [TestMethod]
    public void TestClone()
    {
        Assert.AreEqual(_firstHealth16.ToString(), _firstHealth16.Clone().ToString());
    }

    [TestMethod]
    public void TestNullClone()
    {
        Assert.AreEqual(_firstHealth16.NullDataClone().ToString(), _firstHealth16.NullDataClone().Clone().ToString());
    }

    [TestMethod]
    public void TestEquals()
    {
        Assert.AreEqual(_firstHealth16.Clone(), _firstHealth16);
    }

    [TestMethod]
    public void TestDefaultConstructor()
    {
        Assert.AreEqual("_0_-1_0", (new Entry()).ToString());
    }

    // PUT ----------------

    [TestMethod]
    public void TestPut()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
    }

    [TestMethod]
    public void TestPutDuplicate()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(false, AccessIconsDb.PutEntry(_firstHealth16));
    }

    [TestMethod]
    public void TestPutAlternate()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
    }


    [TestMethod]
    public void TestPutNullEnum()
    {
        Assert.Throws<ArgumentException>(() => AccessIconsDb.PutEntry(_null16));
    }

    [TestMethod]
    public void TestPutSizeUnderflow()
    {
        Assert.Throws<ArgumentException>(() => AccessIconsDb.PutEntry(_healthNeg1));
    }

    [TestMethod]
    public void TestPutNullData()
    {
        Assert.Throws<ArgumentException>(() => AccessIconsDb.PutEntry(_nullStringHealth16));
    }

    [TestMethod]
    public void TestPutDuplicateValue()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(false, AccessIconsDb.PutEntry(_firstHealth16));
    }

    //PUT ALL ---------
    [TestMethod]
    public void PutAllNone()
    {
        List<Entry> temp = new List<Entry>();

        Assert.AreEqual(0, AccessIconsDb.PutAll(temp.GetEnumerator()));
    }

    [TestMethod]
    public void PutAll()
    {
        List<Entry> temp = new List<Entry>();
        temp.Add(_firstHealth16);
        temp.Add(_secondHealth16);
        temp.Add(_chlorophyll16);
        Assert.AreEqual(3, AccessIconsDb.PutAll(temp.GetEnumerator()));
        Assert.AreEqual(3, AccessIconsDb.IconEntryCount(_firstHealth16.EnumDataClone(), true));
        Assert.AreEqual(false, AccessIconsDb.PutEntry(_firstHealth16));
    }

    [TestMethod]
    public void PutAllDupe()
    {
        List<Entry> temp = new List<Entry>();
        temp.Add(_firstHealth16);
        temp.Add(_firstHealth16);
        temp.Add(_chlorophyll16);
        Assert.AreEqual(2, AccessIconsDb.PutAll(temp.GetEnumerator()));
        Assert.AreEqual(2, AccessIconsDb.IconEntryCount(_firstHealth16.EnumDataClone(), true));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
    }

    //ENTRY COUNT --------------

    [TestMethod]
    public void TestIconEntryCountZero()
    {
        Assert.AreEqual(0, AccessIconsDb.IconEntryCount(_firstHealth16));
    }

    [TestMethod]
    public void TestIconEntryCount()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));

        Assert.AreEqual(1, AccessIconsDb.IconEntryCount(_firstHealth16));
    }

    [TestMethod]
    public void TestIconEntryCountNoData()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));

        Assert.AreEqual(1, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountNoneOfTypeNoData()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(0, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountNoneOfTypeEnum()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(0, AccessIconsDb.IconEntryCount(_firstHealth16.EnumDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountEnumType()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(1, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone(), true));
    }

    [TestMethod]
    public void TestIconEntryCountNoDataConflicting()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(2, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountNoDataNoSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(3, AccessIconsDb.IconEntryCount(_firstHealth16.EnumDataClone()));
    }


    [TestMethod]
    public void TestIconEntryCountNoneOfEnumType()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(0, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountMany()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(2, AccessIconsDb.IconEntryCount(_firstHealth16.EnumDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountPartialSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(1, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone()));
    }

    [TestMethod]
    public void TestIconEntryCountAllSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(2, AccessIconsDb.IconEntryCount(_firstHealth16.NullDataClone(), true));
    }

    [TestMethod]
    public void TestIconEntryCountAllOfType()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(3, AccessIconsDb.IconEntryCount(_firstHealth16.EnumDataClone(), true));
    }

    [TestMethod]
    public void TestIconEntryCountDefaultEntry()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(3, AccessIconsDb.IconEntryCount(new Entry(), true));
    }
    // GET --------------------

    [TestMethod]
    public void TestGetFile()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(_firstHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone()));
    }

    [TestMethod]
    public void TestGetFileOverload()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(_firstHealth16.Data, AccessIconsDb.GetData(_firstHealth16.Enum));
    }

    [TestMethod]
    public void TestGetAlternate()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(_secondHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone().CloneSetCopy(1)));
    }

    [TestMethod]
    public void TestGetBothAlternates()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(_firstHealth16.Data, AccessIconsDb.GetData(_firstHealth16.Enum));
        Assert.AreEqual(_secondHealth16.Data, AccessIconsDb.GetData(_secondHealth16.Enum, 1));
    }

    [TestMethod]
    public void TestGetBothAlternatesOverload()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(_firstHealth16.Data, AccessIconsDb.GetData(_firstHealth16.Enum));
        Assert.AreEqual(_secondHealth16.Data, AccessIconsDb.GetData(_secondHealth16.Enum, 1));
    }


    [TestMethod]
    public void TestGetOtherSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));


        Assert.AreEqual(_health32.Data, AccessIconsDb.GetData(_health32.Enum, 2));
    }

    [TestMethod]
    public void TestGetMissingSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));


        Assert.AreEqual(null, AccessIconsDb.GetData(new Entry(AbstractPlant.Rt.Glucose, 32).NullDataClone()));
    }

    [TestMethod]
    public void TestGetMissingCopy()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));


        Assert.AreEqual(null, AccessIconsDb.GetData(_chlorophyll16.NullDataClone().CloneSetCopy(1)));
    }

    [TestMethod]
    public void TestGetNegCopy()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.Throws<ArgumentException>(() => AccessIconsDb.GetData(_chlorophyll16.NullDataClone().CloneSetCopy()));
    }

    // GET ENTRY --------------------

    [TestMethod]
    public void TestGetEntryNullEnumThrows()
    {
        Assert.Throws<ArgumentNullException>(() => AccessIconsDb.GetEntry(_null16));
    }

    [TestMethod]
    public void TestGetEntryNegativeCopyThrows()
    {
        Assert.Throws<ArgumentException>(() => AccessIconsDb.GetEntry(_firstHealth16.CloneSetCopy()));
    }

    [TestMethod]
    public void TestGetEntryNoMatchReturnsNull()
    {
        Assert.IsNull(AccessIconsDb.GetEntry(_firstHealth16));
    }

    [TestMethod]
    public void TestGetEntryFirstCopy()
    {
        Assert.IsTrue(AccessIconsDb.PutEntry(_firstHealth16));
        Assert.IsTrue(AccessIconsDb.PutEntry(_secondHealth16));

        Entry? result = AccessIconsDb.GetEntry(_firstHealth16.NullDataClone().CloneSetCopy(0));

        Assert.IsNotNull(result);
        Assert.AreEqual(_firstHealth16.Data, result.Value.Data);
        Assert.AreEqual(_firstHealth16.Size, result.Value.Size);
        Assert.AreEqual(_firstHealth16.Enum, result.Value.Enum);
    }

    [TestMethod]
    public void TestGetEntrySecondCopy()
    {
        Assert.IsTrue(AccessIconsDb.PutEntry(_firstHealth16));
        Assert.IsTrue(AccessIconsDb.PutEntry(_secondHealth16));

        Entry? result = AccessIconsDb.GetEntry(_firstHealth16.NullDataClone().CloneSetCopy(1));

        Assert.IsNotNull(result);
        Assert.AreEqual(_secondHealth16.Data, result.Value.Data);
        Assert.AreEqual(_secondHealth16.Size, result.Value.Size);
        Assert.AreEqual(_secondHealth16.Enum, result.Value.Enum);
    }

    [TestMethod]
    public void TestGetEntryOutOfRangeCopyReturnsNull()
    {
        Assert.IsTrue(AccessIconsDb.PutEntry(_firstHealth16));

        Entry? result = AccessIconsDb.GetEntry(_firstHealth16.NullDataClone().CloneSetCopy(5));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TestGetEntryNoSize()
    {
        Assert.IsTrue(AccessIconsDb.PutEntry(_firstHealth16));
        Assert.IsTrue(AccessIconsDb.PutEntry(_secondHealth16));

        Entry? result = AccessIconsDb.GetEntry(_firstHealth16.EnumDataClone().CloneSetCopy(0));

        Assert.IsNotNull(result);
        Assert.AreEqual(_firstHealth16.Data, result.Value.Data);
        Assert.AreEqual(_firstHealth16.Size, result.Value.Size);
        Assert.AreEqual(_firstHealth16.Enum, result.Value.Enum);
    }

    [TestMethod]
    public void TestGetEntryNoSizeSecondCopy()
    {
        Assert.IsTrue(AccessIconsDb.PutEntry(_firstHealth16));
        Assert.IsTrue(AccessIconsDb.PutEntry(_secondHealth16));

        Entry? result = AccessIconsDb.GetEntry(_firstHealth16.EnumDataClone().CloneSetCopy(1));

        Assert.IsNotNull(result);
        Assert.AreEqual(_secondHealth16.Data, result.Value.Data);
        Assert.AreEqual(_secondHealth16.Size, result.Value.Size);
        Assert.AreEqual(_secondHealth16.Enum, result.Value.Enum);
    }

    // UPDATE -------------

    [TestMethod]
    public void TestUpdate()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(1, AccessIconsDb.UpdateData(_secondHealth16));
        Assert.AreEqual(_secondHealth16.Data, AccessIconsDb.GetData(AbstractPlant.Rt.Health));
    }

    [TestMethod]
    public void TestUpdateNegSize()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(1, AccessIconsDb.UpdateData(_secondHealth16));

        Assert.Throws<ArgumentException>(() => AccessIconsDb.UpdateData(_healthNeg1));
    }

    [TestMethod]
    public void TestUpdateNegCopy()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(1, AccessIconsDb.UpdateData(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.Throws<ArgumentException>(() => AccessIconsDb.UpdateData(_secondHealth16.CloneSetCopy()));
    }

    [TestMethod]
    public void TestUpdateTwoOptions()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(1, AccessIconsDb.UpdateData(_thirdHealth16.CloneSetCopy(0)));
        Assert.AreEqual(_thirdHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone()));
        Assert.AreEqual(_secondHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone().CloneSetCopy(1)));
    }

    [TestMethod]
    public void TestUpdatCopyTwoOptions()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(1, AccessIconsDb.UpdateData(_thirdHealth16.CloneSetCopy(1)));
        Assert.AreEqual(_firstHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone()));
        Assert.AreEqual(_thirdHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone().CloneSetCopy(1)));
    }

    [TestMethod]
    public void TestUpdateNoTargetFound()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(-1, AccessIconsDb.UpdateData(_chlorophyll16));
    }

    [TestMethod]
    public void TestUpdateNoChange()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(0, AccessIconsDb.UpdateData(_firstHealth16));
    }

    [TestMethod]
    public void TestUpdateMatchingValue()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(-2, AccessIconsDb.UpdateData(_firstHealth16.CloneSetCopy(1)));
    }

    [TestMethod]
    public void TestUpdateMatchingValueSecond()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(-2, AccessIconsDb.UpdateData(_secondHealth16.CloneSetCopy(0)));
    }

    [TestMethod]
    public void TestUpdateCopyOverflow()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));

        Assert.AreEqual(0, AccessIconsDb.UpdateData(_firstHealth16.CloneSetCopy(0)));
        Assert.AreEqual(0, AccessIconsDb.UpdateData(_secondHealth16.CloneSetCopy(1)));
        Assert.AreEqual(-3, AccessIconsDb.UpdateData(_thirdHealth16.CloneSetCopy(2)));
    }

    // REMOVE ----------------
    [TestMethod]
    public void TestClearEmptyDatabase()
    {
        Assert.AreEqual(0, AccessIconsDb.ClearDatabase());
    }

    [TestMethod]
    public void TestClearDatabase()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_health32));

        Assert.AreEqual(6, AccessIconsDb.ClearDatabase());
    }

    [TestMethod]
    public void TestRemoveMissing()
    {
        Assert.AreEqual(0, AccessIconsDb.RemoveEntry(_firstHealth16));
    }

    [TestMethod]
    public void TestRemove()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(1, AccessIconsDb.RemoveEntry(_firstHealth16));
        Assert.AreEqual(_secondHealth16.Data, AccessIconsDb.GetData(_secondHealth16.NullDataClone()));
        Assert.AreEqual(_chlorophyll16.Data, AccessIconsDb.GetData(_chlorophyll16.NullDataClone()));
    }

    [TestMethod]
    public void TestRemoveAlternate()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(1, AccessIconsDb.RemoveEntry(_secondHealth16));
        Assert.AreEqual(_firstHealth16.Data, AccessIconsDb.GetData(_firstHealth16.NullDataClone()));
        Assert.AreEqual(_chlorophyll16.Data, AccessIconsDb.GetData(_chlorophyll16.NullDataClone()));
    }

    [TestMethod]
    public void TestRemoveAllOfType()
    {
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_firstHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_secondHealth16));
        Assert.AreEqual(true, AccessIconsDb.PutEntry(_chlorophyll16));

        Assert.AreEqual(3, AccessIconsDb.RemoveEntry(_firstHealth16.NullDataClone(), true));
        Assert.AreEqual(null, AccessIconsDb.GetData(_firstHealth16.NullDataClone()));
        Assert.AreEqual(null, AccessIconsDb.GetData(_chlorophyll16.NullDataClone()));
    }
}