using System;
using System.IO;
using Dapper;
using Main.main.scripts.core.plants;
using Microsoft.Data.Sqlite;

namespace Main.addons.EnumToIcon.main;

public struct Entry(Enum @enum, int size, string path = "")
{
    public string Path { get; set; } = path;
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
        data = Path;
    }
}

public class AccessIconsDb
{
    public static string DbPath { get; set; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons/EnumToIcon/main/enum_to_directory.db");

    public AccessIconsDb(string path = null)
    {
        if (path != null)
            DbPath = path;
    }

    public static string GetFileAddress(Enum @enum, int size = -1)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath};");
        connection.Open();
        var reader = _iconEntries(@enum, connection, size).ExecuteReader();
        if (reader is not { HasRows: true })
            return null;

        reader.Read();

        return reader.GetString(reader.GetOrdinal("Path"));
    }

    /**
     *
     */
    public static bool PutIcon(Entry entry)
    {
        entry.GenVariables(out var @enum, out var size, out var data);

        if (size < 0) return false;

        using var connection = new SqliteConnection($"Data Source={DbPath};");
        connection.Open();
        if (connection == null) throw new Exception("Established connection was not found");

        using var commandValueEnum = connection.CreateCommand(); //Reusing old connection

        commandValueEnum.CommandText = """
                                       INSERT INTO ValueEnum (ParentEnum, Value)
                                       VALUES (@enum, @ordinal);
                                       SELECT last_insert_rowid(); 
                                       """;
        commandValueEnum.Parameters.Add(new SqliteParameter("@enum", @enum.GetType().Name));
        commandValueEnum.Parameters.Add(new SqliteParameter("@ordinal", entry.GetOrdinal()));
        var newRowKey = Convert.ToInt32(commandValueEnum.ExecuteScalar() ?? -1);

        if (newRowKey < 0) throw new Exception("ValueEnum row was not created");

        using var commandIdToFile = connection.CreateCommand();

        commandIdToFile.CommandText = """
                                      INSERT INTO IdToFile (Size, ParentKey, Path)
                                      VALUES (@size, @parentKey, @path);
                                      SELECT last_insert_rowid();
                                      """;
        commandIdToFile.Parameters.Add(new SqliteParameter("@size", size));
        commandIdToFile.Parameters.Add(new SqliteParameter("@parentKey", newRowKey));
        commandIdToFile.Parameters.Add(new SqliteParameter("@path", data));

        newRowKey = Convert.ToInt32(commandIdToFile.ExecuteScalar() ?? -1);
        if (newRowKey < 0) throw new Exception("IdToFile row was not created");

        return true;
    }

    private static bool UpdateIcon(Entry entry)
    {
        throw new NotImplementedException();
        return false;
    }

    public static int IconEntryCount(Enum @enum, int size = -1)
    {
        using var connection = new SqliteConnection($"Data Source={DbPath};");
        connection.Open();
        var reader = _iconEntries(@enum, connection, size).ExecuteReader();
        var result = 0;
        while (reader.HasRows)
        {
            reader.Read();
            result += 1;
        }

        return result;
    }

    //PRIVATE HELPER METHODS

    private static SqliteCommand _iconEntries(Enum @enum, SqliteConnection connection, int size = -1)
    {
        if (connection == null)
            throw new ArgumentException("connection was null");

        connection.Open();

        if (@enum == null) return null;
        var enumOrdinal = Convert.ToInt32(@enum);

        var command = connection.CreateCommand();

        command.CommandText = """
                              SELECT *
                              FROM IdToFile
                              JOIN ValueEnum ON IdToFile.ParentKey = ValueEnum.Key
                              WHERE ValueEnum.ParentEnum = @enum AND ValueEnum.Value = @enumOrdinal AND (@size < 0 OR IdToFile.Size = @size); 
                              """;

        command.Parameters.Add(new SqliteParameter("@enum", @enum.GetType().Name));
        command.Parameters.Add(new SqliteParameter("@enumOrdinal", enumOrdinal));
        command.Parameters.Add(new SqliteParameter("@size", size));

        return command;
    }
}