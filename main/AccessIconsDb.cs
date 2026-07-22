using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Dapper;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;

namespace Main.addons.EnumToIcon.main;

public struct Entry(Enum @enum, int size, string data = "")
{
    public string Data { get; set; } = data;
    public int Size { get; set; } = size;
    public Enum @Enum { get; set; } = @enum;

    public int GetOrdinal()
    {
        return Convert.ToInt32(@Enum);
    }

    public string GetEnumName()
    {
        return @Enum.GetType().Name;
    }

    public void GenVariables(out Enum @enum, out int size, out string data)
    {
        @enum = @Enum;
        size = Size;
        data = Data;
    }
}

public class AccessIconsDb
{
    public static string DbData { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons/EnumToIcon/main/enum_to_directory.db");

    public AccessIconsDb(string data = null)
    {
        if (data != null)
            DbData = data;
    }

    /**
     * [Param]: An enum to check against the database
     * [Param Optional]: The size to constrain the value to
     * [Return] : the data value of the oldest entry matching the @enum type and value
     */
    public static string GetData(Enum @enum, int size = -1, int copy = 0)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(@enum, connection, size).ExecuteReader();
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

    /**
     * [Param]: An enum to check against the database
     * [Param Optional]: The size to constrain the value to
     * [Return] : the copy # of the row corresponding to both the query of enum and size values; -1 if query had no identical string
     */
    public static int HasEntry(Entry entry)
    {
        entry.GenVariables(out Enum @enum, out int size, out string data);
        if (@enum == null)
            throw new ArgumentNullException(nameof(@enum));

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        entry.Data = null;
        var reader = _iconEntries(entry, connection).ExecuteReader();
        if (reader is not { HasRows: true })
            return -1;

        int index = 0;
        while (reader.Read())
        {
            if (String.CompareOrdinal(reader.GetString(reader.GetOrdinal("Data")), data) == 0)
                return index;
            ++index;
        }

        return -1; //has at least one matching row
    }

    public static IEnumerable<(int, string)?> GetData(Entry entry, bool allOfType = false)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(entry, connection, allOfType).ExecuteReader();

        while (reader is { HasRows: true })
        {
            reader.Read();
            yield return (reader.GetInt32(reader.GetOrdinal("Size")), reader.GetString(reader.GetOrdinal("Data")));
        }
    }

    /**
     * Add the given entry into the database.
     * [Param]: entry type to check against the database (string data != null) (size >= 0)
     * [Returns]: whether the entry was added
     */
    public static bool PutEntry(Entry entry)
    {
        entry.GenVariables(out var @enum, out var size, out var data);

        if (size < 0) return false;
        if (data == null) return false;

        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();

        if (String.CompareOrdinal(_getFileAddress(@enum, connection, size), data) == 0)
            return false;

        if (connection == null) throw new Exception("Established connection was not found");

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
     *
     * returns: (0 no new changes made) (-1 if no target found) (-2 if existing entry has matching values) (-3 copy is greater than available copies)
     */
    public static int UpdateData(Entry entry, int copy = 0)
    {
        entry.GenVariables(out Enum @enum, out int size, out string data);
        if (size < 0)
            throw new InvalidOperationException("Size in entry is < 0");
        if (copy < 0)
            throw new InvalidOperationException("copy is < 0");


        using var connection = new SqliteConnection($"Data Source= {DbData};");
        connection.Open();
        var tempEntry = new Entry(entry.Enum, entry.Size, null);

        var commandCurrentData = _iconEntries(tempEntry, connection);
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
                ;
                if (String.CompareOrdinal(tempData, data) == 0)
                    return 0;
            }
            else
            {
                if (String.CompareOrdinal(tempData, data) == 0)
                    return -2;
            }
        }

        if (i < copy)
            return -3;
        if (targetId == -1)
            return -1;

        using var command = connection.CreateCommand();

        command.CommandText = """
                              UPDATE IdToFile
                              SET Data = @newData
                              WHERE Id = @id
                              """;
        command.Parameters.Add(new SqliteParameter("@newData", data));
        command.Parameters.Add(new SqliteParameter("@id", targetId));

        //AND ParentKey IN (
        //    SELECT Id
        //FROM ValueEnum
        //WHERE 
        //    );

        return command.ExecuteNonQuery();
    }

    /**
     * [Param]: entry.Enum to remove from the database
     * [Param]: entry.Size to remove. 0 >, to remove all entries under the given enum.
     *
     */
    public static int RemoveEntry(Entry entry)
    {
        //TODO
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();

        using var removeCommand = connection.CreateCommand();


        removeCommand.CommandText = """
                                            DELETE FROM IdToFile
                                            WHERE ParentKey IN (
                                                SELECT Key
                                                FROM ValueEnum
                                                WHERE (@enumName IS null OR (ValueEnum.ParentEnum = @enumName AND ValueEnum.Value = @value)
                                                )) AND (@size = -1 OR IdToFile.Size = @size) AND (@data IS null OR IdToFile.Data = @data); 
                                    """;
        removeCommand.Parameters.Add(new SqliteParameter("@enumName",
            (object)entry.Enum?.GetType().Name ?? DBNull.Value));
        removeCommand.Parameters.Add(new SqliteParameter("@value", Convert.ToInt32(entry.Enum)));
        removeCommand.Parameters.Add(new SqliteParameter("@size", Convert.ToInt32(entry.Size)));
        removeCommand.Parameters.Add(new SqliteParameter("@data", (object)entry.Data ?? DBNull.Value));


        return removeCommand.ExecuteNonQuery();
    }


    public static int IconEntryCount(Entry entry, bool allOfType = false) =>
        IconEntryCount(entry.Enum, entry.Size, allOfType);

    public static int IconEntryCount(Enum @enum, bool allOfType = false) => IconEntryCount(@enum, -1, allOfType);

    /**
     * [Param]: An enum to check against the database
     * [Param Optional]: The size to constrain the value to ( must be >= 0 )
     * [Param Overload]: An entry to cast into previous params
     *
     */
    public static int IconEntryCount(Enum @enum, int size = -1, bool allOfType = false)
    {
        using var connection = new SqliteConnection($"Data Source={DbData};");
        connection.Open();
        var reader = _iconEntries(@enum, connection, size, allOfType).ExecuteReader();
        var result = 0;
        while (reader.HasRows && reader.Read())
        {
            result += 1;
        }

        return result;
    }

    //PRIVATE HELPER METHODS
    private static SqliteCommand _iconEntries(Enum @enum, SqliteConnection connection, int size = -1,
        bool allOfType = false) =>
        _iconEntries(new Entry(@enum, size, null), connection, allOfType);

    private static SqliteCommand _iconEntries(Enum @enum, SqliteConnection connection, bool allOfType) =>
        _iconEntries(@enum, connection, -1, allOfType);

    /**
     * [Return]: the joined query
     * [Param]: An enum to check against the database
     * [Param]: An established connection
     * [Param Optional]: The size to constrain the value to ( must be >= 0 )
     * [Param Optional]: Select all entries of enum (non-ordinal)
     */
    private static SqliteCommand _iconEntries(Entry entry, SqliteConnection connection, bool allOfType = false)
    {
        entry.GenVariables(out var @enum, out var size, out var data);

        if (connection == null)
            throw new ArgumentException("connection was null");

        connection.Open();


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
                              FROM IdToFile
                              JOIN ValueEnum ON IdToFile.ParentKey = ValueEnum.Key
                              WHERE (@enum is null OR ValueEnum.ParentEnum = @enum) AND (@enumOrdinal < 0 OR ValueEnum.Value = @enumOrdinal) AND (@size < 0 OR IdToFile.Size = @size) AND (@data IS null OR IdToFile.Data = @data); 
                              """;

        command.Parameters.Add(new SqliteParameter("@enum", (object)@enum.GetType().Name ?? DBNull.Value));
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
        var reader = _iconEntries(@enum, connection, size).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        reader.Read();

        return reader.GetString(reader.GetOrdinal("Data"));
    }
}