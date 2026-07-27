using System;
using System.Collections.Generic;
using System.IO;
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
public struct Entry(Enum @enum = null, int size = -1, string data = null, int copy = 0)
{
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
        return @Enum.GetType().FullName;
    }

    public void GenVariables(out Enum @enum, out int size, out string data, out int copy)
    {
        @enum = @Enum;
        size = Size;
        data = Data;
        copy = Copy;
    }

    public Entry NullDataClone()
    {
        return new Entry(Enum, Size, null, copy);
    }

    public Entry EnumDataClone()
    {
        return new Entry(@Enum);
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

    /**
     * Alternate .equals that does compare a field if the argument entry's field is a wildcard. Will evaluaate all other fields as normal.
     */
    public bool EqualsWildcard(object obj)
    {
        if (obj == (object)this) return true;
        if (obj is not Entry entry) return false;
        if (!Equals(entry.Enum, Enum) && entry.Enum != null && Enum != null) return false;
        if (entry.Size != Size && entry.Size >= 0 && Size >= 0) return false;
        if (string.CompareOrdinal(entry.Data, Data) != 0 && entry.Data != null && Data != null) return false;
        if (entry.Copy != Copy && entry.Copy >= 0 && Copy >= 0) return false;

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

    /**
     * Setter
     * Optional Param: if empty will default to (wildcard) (-1)
     */
    public Entry CloneSetCopy(int copy = -1)
    {
        Entry result = Clone();
        result.Copy = copy;
        return result;
    }

    public Entry Clone()
    {
        Entry result = new Entry();
        result.Enum = @Enum;
        result.Size = Size;
        result.Data = Data;
        result.Copy = Copy;
        return result;
    }
}

public class AccessIconsDb
{
    public static string DbData { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons", "EnumToIcon", "EnumToStringDatabase", "main",
            "enum_to_directory.db");

    public AccessIconsDb(string data = null)
    {
        if (data != null)
            DbData = data;
    }

    public static string GetData(Enum @enum, int copy = 0)
    {
        if (copy < 0)
            throw new ArgumentException("copy is < 0");

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(new Entry(@enum), connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        int index = 0;
        while (reader.Read())
        {
            if (index == copy)
                return reader.GetString(reader.GetOrdinal("Data"));
            ++index;
        }

        return null;
    }

    public static Entry? GetEntry(Entry entry)
    {
        if (entry.Enum == null)
            throw new ArgumentNullException(nameof(entry.Enum));
        if (entry.Copy < 0)
            throw new ArgumentException("entry.Copy is < 0");

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(entry, connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        int index = 0;
        while (reader.Read())
        {
            if (index == entry.Copy)
            {
                Entry result = new Entry();
                result.Data = reader.GetString(reader.GetOrdinal("Data"));
                result.Size = reader.GetInt32(reader.GetOrdinal("Size"));
                result.Enum = entry.Enum;
                return result;
            }

            ++index;
        }

        return null;
    }

    /**
     * [Param]: An enum to check against the database
     */
    public static string GetData(Entry entry)
    {
        if (entry.Copy < 0)
            throw new ArgumentException("entry.Copy is < 0");

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(entry, connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        int index = 0;
        while (reader.Read())
        {
            if (index == entry.Copy)
                return reader.GetString(reader.GetOrdinal("Data"));
            ++index;
        }

        return null;
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
        var reader = _iconEntries(entry, connection).ExecuteReader();
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
        var reader = _iconEntries(entry, connection, allOfType).ExecuteReader();

        while (reader is { HasRows: true })
        {
            Entry result = new Entry();

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
            if (_putEntry(tempList.Current, connection))
                ++result;
        }

        return result;
    }

    /**
     * Add the given entry into the database.
     * [Param]: entry type to check against the database (string data != null) (size >= 0)
     * [Returns]: whether the entry was added
     */
    public static bool PutEntry(Entry entry)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        return _putEntry(entry, connection);
    }

    public static bool _putEntry(Entry entry, SqliteConnection connection)
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

        if (String.CompareOrdinal(_getFileAddress(@enum, connection, size), data) == 0)
            return false;

        var reader = _getValueEnums(@enum, connection).ExecuteReader();
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
                                      INSERT INTO IdToFile (Size, ParentKey, Data)
                                      VALUES (@size, @parentKey, @data);
                                      SELECT last_insert_rowid();
                                      """;
        commandIdToFile.Parameters.Add(new SqliteParameter("@size", size));
        commandIdToFile.Parameters.Add(new SqliteParameter("@parentKey", rowKey));
        commandIdToFile.Parameters.Add(new SqliteParameter("@data", data));

        rowKey = Convert.ToInt32(commandIdToFile.ExecuteScalar() ?? -1);
        if (rowKey < 0) throw new Exception("IdToFile row was not created");

        return true;
    }

    /**
     * param: size > 0
     * param: copy > 0
     * returns: (0 no new changes made) (-1 if no target found) (-2 if existing entry has matching values) (-3 copy is greater than available copies)
     */
    public static int UpdateData(Entry entry)
    {
        entry.GenVariables(out Enum @enum, out int size, out string data, out var copy);
        if (size < 0)
            throw new ArgumentException("Size in entry is < 0");
        if (copy < 0)
            throw new ArgumentException("copy is < 0");
        if (@enum == null)
            throw new ArgumentException("enum is null");

        using var connection = new SqliteConnection($"Data Source= {DbData};");
        connection.Open();

        var commandCurrentData = _iconEntries(entry.NullDataClone(), connection);
        var reader = commandCurrentData.ExecuteReader();

        if (!reader.HasRows)
            return -1;


        int targetId = -1;

        int i = 0;
        while (reader.Read())
        {
            var tempData = reader.GetString(reader.GetOrdinal("Data"));

            if (i == copy)
            {
                targetId = reader.GetInt32(reader.GetOrdinal("Id"));
                if (String.CompareOrdinal(tempData, data) == 0)
                    return 0;
            }
            else
            {
                if (String.CompareOrdinal(tempData, data) == 0)
                    return -2;
            }

            ++i;
        }

        if (i == copy)
            return -3;


        using var command = connection.CreateCommand();

        command.CommandText = """
                              UPDATE IdToFile
                              SET Data = @newData
                              WHERE Id = @id
                              """;
        command.Parameters.Add(new SqliteParameter("@newData", data));
        command.Parameters.Add(new SqliteParameter("@id", targetId));

        return command.ExecuteNonQuery();
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
                                                ) AND (@size = -1 OR IdToFile.Size = @size) AND (@data IS null OR IdToFile.Data = @data)
                                            )
                                        """;
            removeCommand.Parameters.Add(new SqliteParameter("@value", value));
            removeCommand.Parameters.Add(new SqliteParameter("@size", Convert.ToInt32(entry.Size)));
            removeCommand.Parameters.Add(new SqliteParameter("@data", (object)entry.Data ?? DBNull.Value));
        }

        removeCommand.Parameters.Add(new SqliteParameter("@enumName",
            (object)entry.Enum?.GetType().Name ?? DBNull.Value));


        return removeCommand.ExecuteNonQuery();
    }

    /**
     * [Param]: An enum to check against the database
     * [Param Optional]: The size to constrain the value to ( must be >= 0 )
     * [Param Overload]: An entry to cast into previous params
     *
     */
    public static int IconEntryCount(Entry entry, bool allOfType = false)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(entry, connection, allOfType).ExecuteReader();
        var result = 0;
        while (reader.HasRows && reader.Read())
        {
            result += 1;
        }

        return result;
    }

    /**
     * Searches for entries in the db that match the given entry.
     * [Return]: the joined query
     * [Param]: An enum to check against the database
     * [Param]: An established connection
     * [Param Optional]: The size to constrain the value to ( must be >= 0 )
     * [Param Optional]: Select all entries of enum (non-ordinal)
     */
    private static SqliteCommand _iconEntries(Entry entry, SqliteConnection connection, bool allOfType = false)
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
                              """;

        command.Parameters.Add(new SqliteParameter("@enum", (object)@enum?.GetType().Name ?? DBNull.Value));
        command.Parameters.Add(new SqliteParameter("@enumOrdinal", enumOrdinal));
        command.Parameters.Add(new SqliteParameter("@size", size));
        command.Parameters.Add(new SqliteParameter("@data", (object)data ?? DBNull.Value));

        return command;
    }

    /**
     * [Return]: the query
     * [Param]: An enum to check against the database
     * [Param]: An established connection
     * [Param Optional]: Select all entries of enum (non-ordinal)
     */
    private static SqliteCommand _getValueEnums(Enum @enum, SqliteConnection connection, bool allOfType = false)
    {
        if (connection == null)
            throw new ArgumentException("connection was null");

        connection.Open();

        if (@enum == null) return null;


        int enumOrdinal;
        if (!allOfType)
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
                              FROM ValueEnum
                              WHERE ValueEnum.ParentEnum = @enum AND (@enumOrdinal < 0 OR ValueEnum.Value = @enumOrdinal); 
                              """;

        command.Parameters.Add(new SqliteParameter("@enum", @enum.GetType().Name));
        command.Parameters.Add(new SqliteParameter("@enumOrdinal", enumOrdinal));

        return command;
    }

    /**
     * [Return]: the query
     * [Param]: The size to constrain the value to ( must be >= 0 )
     * [Param]: An established connection
     */
    private static SqliteCommand _getIdToFiles(int size, SqliteConnection connection)
    {
        if (connection == null)
            throw new ArgumentException("connection was null");
        if (size < 0)
            throw new ArgumentException("connection was null");

        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText = """
                              SELECT *
                              FROM IdToFile
                              WHERE IdToFile.Size = @size; 
                              """;

        command.Parameters.Add(new SqliteParameter("@size", size));

        return command;
    }

    private static string _getFileAddress(Enum @enum, SqliteConnection connection, int size = -1)
    {
        connection.Open();
        var reader = _iconEntries(new Entry(@enum, size, null), connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        reader.Read();

        return reader.GetString(reader.GetOrdinal("Data"));
    }
}