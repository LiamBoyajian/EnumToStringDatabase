using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Main.addons.EnumToIcon.main;

public static class AccessIconsDb
{
    public const string DataSource = "Data Source=";

    public static string DbPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons/EnumToIcon/main/enum_to_directory.db");

    public static string GetFile(Enum @enum, int size)
    {
        using var connection = new SqliteConnection(DataSource + DbPath);
        connection.Open();

        string enumName = @enum.GetType().Name;

        using var command = connection.CreateCommand();

        command.Parameters.Add(new SqliteParameter("@size", size));
        command.Parameters.Add(new SqliteParameter("@enumName", enumName));
        Console.WriteLine(enumName);
        command.CommandText = """
                              SELECT IdToFile.Path, * 
                              FROM IdToFile 
                              JOIN ValueEnum ON IdToFile.ParentKey = ValueEnum.Key
                              WHERE IdToFile.Id = @size AND ValueEnum.ParentEnum = @enumName;
                              """;
        using var reader = command.ExecuteReader();
        if (reader.Read())
            return reader.GetString(0);
        return string.Empty;
    }
}