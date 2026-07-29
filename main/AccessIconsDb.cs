using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Microsoft.Data.Sqlite;

namespace Main.addons.EnumToIcon.EnumToStringDatabase.main;

/**
*     Used to describe elements in the database. When passed as a param the database is checked
*      for whether any elements match the entry. Some entry values are considered wildcards,
*      and will not be searched for during queries.
*
*     Entry.Enum (null) to wildcard if method allows it
*     Entry.Size (0 >) to wildcard size; otherwise constrain search to size
*     Entry.Data (null) to wildcard; other constrain search to data
*     Entry.Copy (0 >) to wildcard; other constrain search to data
*/
public struct Entry(
    Enum @enum = Entry.EnumWildcard,
    int size = Entry.SizeWildcard,
    string data = Entry.DataWildcard,
    int copy = Entry.CopyWildcard) : IEquatable<Entry>
{
    //If this is missing the size and copy values are set to 0.
    public Entry() : this(Entry.EnumWildcard, Entry.SizeWildcard, Entry.DataWildcard, Entry.CopyWildcard)
    {
    }

    public const Enum EnumWildcard = null;
    public const int SizeWildcard = -1; // less than 0
    public const string DataWildcard = null;
    public const int CopyWildcard = -1; // less than 0

    public Enum @Enum { get; set; } = @enum;
    public string Data { get; set; } = data;
    public int Size { get; set; } = size;

    public int Copy { get; set; } = copy;

    public int GetOrdinal()
    {
        return Convert.ToInt32(@Enum);
    }

    public string GetEnumName()
    {
        return @Enum.GetType().Name;
    }

    public string GetEnumFullName()
    {
        return @Enum?.GetType().FullName;
    }

    public void GenVariables(out Enum @enum, out int size, out string data, out int copy)
    {
        @enum = @Enum;
        size = Size;
        data = Data;
        copy = Copy;
    }

    public override bool Equals(object obj)
    {
        if (obj == (object)this) return true;
        if (obj is not Entry entry) return false;
        if (!Equals(entry.Enum, Enum)) return false;
        if (entry.Size != Size) return false;
        if (entry.Copy != Copy) return false;
        if (string.CompareOrdinal(entry.Data, Data) != 0) return false;

        return true;
    }

    public bool Equals(Entry other) => Equals((object)other);

    public override int GetHashCode()
    {
        return HashCode.Combine(Enum, Data, Size, Copy);
    }

    /**
     * Alternate .equals that does compare a field if the argument entry's field is a wildcard. Will evaluaate all other fields as normal.
     */
    public bool EqualsWildcard(object obj)
    {
        if (obj == (object)this) return true;
        if (obj is not Entry entry) return false;
        if (!Equals(entry.Enum, Enum) && entry.Enum != EnumWildcard && Enum != EnumWildcard) return false;
        if (entry.Size != Size && entry.Size > SizeWildcard && Size > SizeWildcard) return false;
        if (string.CompareOrdinal(entry.Data, Data) != 0 && entry.Data != DataWildcard &&
            Data != DataWildcard) return false;
        if (entry.Copy != Copy && entry.Copy > CopyWildcard && Copy > CopyWildcard) return false;

        return true;
    }

    public override string ToString()
    {
        var result = "";
        result += $"{GetEnumFullName()}_";
        result += $"{GetOrdinal()}_";
        result += $"{Size}_";
        result += $"{Copy}_";
        result += $"{Data}";
        return result;
    }

    /**
     * Formatting like ToString without the EnumPath
     */
    public static Entry? FromString(string s)
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));

        var split = s.Split('_');
        Type type = Type.GetType(split[0] ?? "");

        if (type is null || !type.IsEnum)
            return null;

        Entry result = new Entry();

        int ordinal = Convert.ToInt32(split[1]);

        result.@Enum = (Enum)Enum.ToObject(type, ordinal);

        result.Size = Convert.ToInt32(split[2]);

        result.Copy = Convert.ToInt32(split[3]);

        result.Data = split[4];

        return result;
    }

    public static bool operator ==(Entry left, Entry right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Entry left, Entry right)
    {
        return !(left == right);
    }


    //CLONES
    public Entry Clone()
    {
        Entry result = new Entry();
        result.Enum = @Enum;
        result.Size = Size;
        result.Data = Data;
        result.Copy = Copy;
        return result;
    }

    /**
    * Returns a copy with all values wildcarded except Copy
    */
    public Entry CopyClone(int copy)
    {
        return new Entry(EnumWildcard, SizeWildcard, DataWildcard, copy);
    }

    public Entry CopyClone() => CopyClone(Copy);

    /**
     * Returns a copy with all values wildcarded except Size
     */
    public Entry SizeClone(int size)
    {
        return new Entry(EnumWildcard, size, DataWildcard, CopyWildcard);
    }

    public Entry SizeClone() => SizeClone(Size);


    /**
     * Returns a copy with all values wildcarded except Size
     */
    public Entry EnumClone(Enum @enum)
    {
        return new Entry(@enum, SizeWildcard, DataWildcard, CopyWildcard);
    }

    public Entry EnumClone() => EnumClone(Enum);

    /**
     * Returns a copy with all values wildcarded except Size
     */
    public Entry DataClone(string data)
    {
        return new Entry(EnumWildcard, SizeWildcard, data, CopyWildcard);
    }

    public Entry DataClone() => DataClone(Data);

    /**
     * Returns a copy with Enum wildcarded
     */
    public Entry EnumWildcardClone() => EnumWildcardClone(EnumWildcard);

    /**
     * Returns a copy with Enum as param
     */
    public Entry EnumWildcardClone(Enum @enum)
    {
        return new Entry(@enum, Size, Data, Copy);
    }

    /**
     * Returns a copy with size wildcarded
     */
    public Entry SizeWildcardClone() => SizeWildcardClone(SizeWildcard);

    /**
     * Returns a copy with size as param
     */
    public Entry SizeWildcardClone(int size)
    {
        return new Entry(Enum, size, Data, Copy);
    }

    /**
     * Returns a copy with data wildcarded
     */
    public Entry DataWildcardClone() => DataWildcardClone(DataWildcard);

    /**
     * Returns a copy with data as param
     */
    public Entry DataWildcardClone(string data)
    {
        return new Entry(Enum, Size, data, Copy);
    }

    /**
     * Returns a copy with copy wildcarded
     */
    public Entry CopyWildcardClone() => CopyWildcardClone(CopyWildcard);

    /**
     * Returns a copy with copy as param
     */
    public Entry CopyWildcardClone(int copy)
    {
        return new Entry(@Enum, Size, Data, copy);
    }
}

public class AccessIconsDb
{
    public static string DbData { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons", "EnumToIcon", "EnumToStringDatabase", "main",
            "enum_to_directory.db");

    private static string SqliteDeclaration { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons", "EnumToIcon", "EnumToStringDatabase", "main",
            "db_declaration.sql");

    public static void InitDb(SqliteConnection connection = null, string path = null)
    {
        if (path != null)
            DbData = path;

        bool connectionWasNull = false;
        if (connection == null)
        {
            connectionWasNull = true;
            connection = new SqliteConnection($"Data Source={DbData};");
        }

        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = File.ReadAllText(SqliteDeclaration);
        command.ExecuteNonQuery();
        if (connectionWasNull)
            connection.Close();
    }

    public AccessIconsDb(string data = null)
    {
        if (data != null)
            DbData = data;
    }

    public static Entry? GetEntry(Entry entry)
    {
        if (entry.Enum == null)
            throw new ArgumentNullException(nameof(entry.Enum));

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _getEntries(entry, connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;
        reader.Read();

        Entry result = new Entry();
        result.Copy = reader.GetInt32(reader.GetOrdinal("Copy"));
        result.Data = reader.GetString(reader.GetOrdinal("Data"));
        result.Size = reader.GetInt32(reader.GetOrdinal("Size"));
        result.Enum = entry.Enum;
        return result;
    }

    /**
     * [Param]: An enum to check against the database
     *      entry.data is automatically set to wildcard
     */
    public static string GetData(Entry entry)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _getEntries(entry.DataWildcardClone(), connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        reader.Read();

        return reader.GetString(reader.GetOrdinal("Data"));
    }

    /**
     * [Param]: An enum to check against the database
     * [Param Optional]: The size to constrain the value to
     * [Return] : whether the table contains this entry
     */
    public static bool HasEntry(Entry entry)
    {
        entry.GenVariables(out Enum @enum, out int size, out string data, out var copy);
        if (@enum == null)
            throw new ArgumentNullException(nameof(@enum));

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        entry.Data = null;
        var reader = _getEntries(entry, connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return false;

        int index = 0;
        while (reader.Read())
        {
            if (String.CompareOrdinal(reader.GetString(reader.GetOrdinal("Data")), data) == 0)
                return true;
            ++index;
        }

        return false; //has at least one matching row
    }


    public static IEnumerable<Entry> GetAllData(Entry entry, bool allOfType = false)
    {
        if (entry.Enum == null)
            throw new ArgumentNullException(nameof(entry.Enum));
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _getEntries(entry, connection, allOfType).ExecuteReader();

        while (reader is { HasRows: true })
        {
            Entry result = new Entry();

            result.Copy = reader.GetInt32(reader.GetOrdinal("Copy"));
            result.Data = reader.GetString(reader.GetOrdinal("Data"));
            result.Size = reader.GetInt32(reader.GetOrdinal("Size"));
            result.Enum = entry.Enum;

            yield return result;
        }
    }

    public static int PutAll(IEnumerator<Entry> tempList)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        int result = 0;
        while (tempList.MoveNext())
        {
            if (_putEntry(tempList.Current, connection) >= 0)
                ++result;
        }

        return result;
    }

    /**
     * Add the given entry into the database.
     * [Param]: entry type to check against the database (string data != null) (size >= 0)
     * [Returns]: the value of the copy found (0 > if not added)
     */
    public static int PutEntry(Entry entry)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        return _putEntry(entry, connection);
    }

    /**
     * Returns the value of the copy found (0 > if not added)
     */
    public static int _putEntry(Entry entry, SqliteConnection connection)
    {
        entry.GenVariables(out var @enum, out var size, out var data, out var copy);

        if (@enum == null)
            throw new ArgumentException("data is null");
        if (size < 0)
            throw new ArgumentException("size is < 0");
        if (data == null)
            throw new ArgumentException("data is null");

        if (connection == null)
            throw new Exception("Established connection was not found");

        connection.Open();


        //This statement can be simplified into the wildcard if statement; leaving like this to save time.
        var paramEntryReader = _getEntries(entry.CopyWildcardClone(), connection).ExecuteReader();
        if (paramEntryReader.HasRows)
            return -1;


        var newCopyValue = -1;

        //Find the lowest, valid copy value
        if (copy == Entry.CopyWildcard)
        {
            int i = 0;
            using var readerCopies =
                _getEntries(entry.DataWildcardClone().CopyWildcardClone(), connection).ExecuteReader();
            while (readerCopies.Read())
            {
                var currentCopy = readerCopies.GetInt32(readerCopies.GetOrdinal("Copy"));
                if (i < currentCopy)
                {
                    newCopyValue = i;
                    break; //Valid gap in copies found
                }

                ++i;
            }

            if (newCopyValue < 0)
                newCopyValue = i;
        }
        else
        {
            if (_getEntries(entry, connection).ExecuteReader().HasRows)
                return -1; //Entry already exists
            newCopyValue = copy;
        }

        var valueEnums = connection.CreateCommand();

        valueEnums.CommandText = """
                                 SELECT *
                                 FROM ValueEnum
                                 WHERE ValueEnum.ParentEnum = @enum AND (@enumOrdinal < 0 OR ValueEnum.Value = @enumOrdinal); 
                                 """;

        valueEnums.Parameters.Add(new SqliteParameter("@enum", @enum.GetType().Name));
        valueEnums.Parameters.Add(new SqliteParameter("@enumOrdinal", Convert.ToInt32(@enum)));

        var reader = valueEnums.ExecuteReader();

        int rowKey;

        if (reader.HasRows)
        {
            reader.Read();
            rowKey = reader.GetInt32(reader.GetOrdinal("Key"));

            if (reader.Read())
                throw new InvalidOperationException(
                    "Non-user error: Database has multiple rows of the same enum ordinal");
        }
        else
        {
            using var commandValueEnum = connection.CreateCommand();


            commandValueEnum.CommandText = """
                                           INSERT INTO ValueEnum (ParentEnum, Value)
                                           VALUES (@enum, @ordinal);
                                           SELECT last_insert_rowid(); 
                                           """;
            commandValueEnum.Parameters.Add(new SqliteParameter("@enum", @enum.GetType().Name));
            commandValueEnum.Parameters.Add(new SqliteParameter("@ordinal", entry.GetOrdinal()));
            rowKey = Convert.ToInt32(commandValueEnum.ExecuteScalar() ?? -1);

            if (rowKey < 0) throw new Exception("ValueEnum row was not created");
        }

        using var commandIdToFile = connection.CreateCommand();
        commandIdToFile.CommandText = """
                                      INSERT INTO IdToFile (Size, ParentKey, Data, Copy)
                                      VALUES (@size, @parentKey, @data, @copy);
                                      SELECT last_insert_rowid();
                                      """;
        commandIdToFile.Parameters.Add(new SqliteParameter("@size", size));
        commandIdToFile.Parameters.Add(new SqliteParameter("@parentKey", rowKey));
        commandIdToFile.Parameters.Add(new SqliteParameter("@data", data));
        commandIdToFile.Parameters.Add(new SqliteParameter("@copy", newCopyValue));

        rowKey = Convert.ToInt32(commandIdToFile.ExecuteScalar() ?? -1);
        if (rowKey < 0) throw new Exception("IdToFile row was not created");

        return newCopyValue;
    }

    /**
     *
     *
     * Returns: whether the database was updated
     */
    public static bool UpdateData(Entry entry)
    {
        entry.GenVariables(out Enum @enum, out int size, out string data, out var copy);

        if (@enum == null)
            throw new ArgumentException("enum is null");

        using var connection = new SqliteConnection($"Data Source= {DbData};");
        connection.Open();

        var commandHasData = _getEntries(entry.CopyWildcardClone(), connection);
        var readerHasData = commandHasData.ExecuteReader();
        if (readerHasData.Read())
            return false;


        var commandCurrentData = _getEntries(entry.DataWildcardClone(), connection);
        var reader = commandCurrentData.ExecuteReader();

        if (!reader.Read())
            return false; //no entries to update

        int updateTargetId = reader.GetInt32(reader.GetOrdinal("Id"));

        using var command = connection.CreateCommand();

        command.CommandText = """
                              UPDATE IdToFile
                              SET Data = @newData
                              WHERE Id = @id
                              """;
        command.Parameters.Add(new SqliteParameter("@newData", data));
        command.Parameters.Add(new SqliteParameter("@id", updateTargetId));

        return command.ExecuteNonQuery() > 0;
    }

    public static int ClearDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        using var removeCommand = connection.CreateCommand();

        removeCommand.CommandText = """
                                    DELETE FROM IdToFile;
                                    DELETE FROM ValueEnum;
                                    """;
        return removeCommand.ExecuteNonQuery();
    }

    /**
     * [Param]: entry
     *          @enum: cannot be null
     *          size: > 0 to constrain to that size
     *          data: != null to constrain to that string
     *
     * [Param]: if true selects all entries of enum and ordinal value
     * Returns: total affected rows
     */
    public static int RemoveEntry(Entry entry, bool allOfType = false)
    {
        if (entry.Enum == null)
            throw new ArgumentException("Enum is null");

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();

        using var removeCommand = connection.CreateCommand();
        var value = Convert.ToInt32(entry.Enum);

        if (allOfType)
        {
            removeCommand.CommandText = """
                                        DELETE FROM IdToFile
                                        WHERE (ParentKey IN (
                                                SELECT Key
                                                FROM ValueEnum
                                                WHERE ValueEnum.ParentEnum = @enumName
                                                )
                                            );
                                        """;
        }
        else
        {
            removeCommand.CommandText = """
                                        DELETE FROM IdToFile
                                        WHERE (ParentKey IN (
                                                SELECT Key
                                                FROM ValueEnum
                                                WHERE ValueEnum.ParentEnum = @enumName AND (@value < 0 OR ValueEnum.Value = @value)
                                                ) AND (@size = -1 OR IdToFile.Size = @size) AND (@data IS null OR IdToFile.Data = @data) AND (@copy < 0 OR IdToFile.Copy = @copy)
                                            )
                                        """;
            removeCommand.Parameters.Add(new SqliteParameter("@value", value));
            removeCommand.Parameters.Add(new SqliteParameter("@size", Convert.ToInt32(entry.Size)));
            removeCommand.Parameters.Add(new SqliteParameter("@data", (object)entry.Data ?? DBNull.Value));
            removeCommand.Parameters.Add(new SqliteParameter("@copy", entry.Copy));
        }

        removeCommand.Parameters.Add(new SqliteParameter("@enumName",
            (object)entry.Enum?.GetType().Name ?? DBNull.Value));


        return removeCommand.ExecuteNonQuery();
    }

    /**
     * [Param]: An enum to check against the database
     * [Param Optional]: The size to constrain the value to
     * [Param Overload]: An entry to cast into previous params
     *
     */
    public static int IconEntryCount(Entry entry, bool allOfType = false)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _getEntries(entry, connection, allOfType).ExecuteReader();
        var result = 0;
        while (reader.Read())
        {
            result += 1;
        }

        return result;
    }

    /**
     * Searches for entries in the db that match the given entry.
     * Returns: the joined query results
     * Param: [Entry] Searches database for entries with matching field values--excluding any wildcard fields.
     */
    private static SqliteCommand _getEntries(Entry entry, SqliteConnection connection, bool allOfType = false)
    {
        entry.GenVariables(out var @enum, out var size, out var data, out var copy);


        if (connection == null)
            throw new ArgumentException("connection was null");

        connection.Open();


        int enumOrdinal;
        if (!allOfType && @enum != null)
        {
            enumOrdinal = Convert.ToInt32(@enum);
        }
        else
        {
            enumOrdinal = -1;
        }

        var command = connection.CreateCommand();

        command.CommandText = """
                              SELECT *
                              FROM IdToFile
                              JOIN ValueEnum ON IdToFile.ParentKey = ValueEnum.Key
                              WHERE (
                                  @enum is null OR ValueEnum.ParentEnum = @enum) 
                                  AND (@enumOrdinal < 0 OR ValueEnum.Value = @enumOrdinal) 
                                  AND (@size < 0 OR IdToFile.Size = @size) 
                                  AND (@data IS null OR IdToFile.Data = @data)
                                  AND (@copy < 0 OR IdToFile.Copy = @copy);
                              """;

        command.Parameters.Add(new SqliteParameter("@enum", (object)@enum?.GetType().Name ?? DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@enumOrdinal", enumOrdinal));
        command.Parameters.Add(new SqliteParameter("@size", size));
        command.Parameters.Add(new SqliteParameter("@data", (object)data ?? DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@copy", copy));

        return command;
    }
}