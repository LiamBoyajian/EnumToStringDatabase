using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Godot;
using Godot.Collections;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;

namespace Main.addons.EnumToIcon.EnumToStringDatabase;

/**
 * Global designed to hold file paths in memory (intended for icons)
 */
public partial class MemoryToDb : Node
{
    public List<Entry> Data;
    public Node Instance;
    public string FolderPath = "res://main/sprites/Icons/";
    public string GlobalFolderPath;

    public override void _Ready()
    {
        GlobalFolderPath = ProjectSettings.GlobalizePath(FolderPath);
        base._Ready();
        Data = new List<Entry>();
        //ValidateIconDirectory();
        Instance = this;
    }

    public string GetGlobalPathConcat(string fileName)
    {
        return ProjectSettings.GlobalizePath(FolderPath + fileName);
    }

    public void ValidateIconDirectory()
    {
        var config = new ConfigFile();

        string lastEdit = Directory.GetLastWriteTime(GlobalFolderPath).ToString("yyyy-MM-dd HH:mm:ss");

        if (String.CompareOrdinal(config.GetValue("Icons", "LastIconsEditTime").AsString(), lastEdit) != 0)
        {
            config.SetValue("Icons", "LastIconsEditTime", lastEdit);
            AccessIconsDb.ClearDatabase();
            InitializeFromDirectory(GlobalFolderPath);
        }
    }


    /**
     * Initializes
     */
    public int InitializeFromDirectory(string dir, bool recursive = true)
    {
        if (dir == null)
            return -1;
        int result = 0;

        //Files -----------------------
        using var file = Directory.EnumerateFiles(dir).GetEnumerator();
        List<Entry> tempList = new List<Entry>();
        while (file.MoveNext())
        {
            var s = file.Current?.GetBaseName();
            Type type = Type.GetType(s.Split('.')[0] ?? ""); //Potential problem TODO almost 100% this
            if (type is null || !type.IsEnum)
                continue; //TODO hitting this, so conversion isnt working

            if (Entry.FromString(type, s) is { } entry)
            {
                tempList.Add(entry);
                ++result;
            }
        }

        //Sub-dirs -----------------------
        AccessIconsDb.PutAll(tempList.GetEnumerator());

        using var subDirs = Directory.EnumerateDirectories(dir).GetEnumerator();
        while (subDirs.MoveNext())
        {
            result += InitializeFromDirectory(subDirs.Current); //Recur -----------------------
        }

        // -----------------------
        return result;
    }


    /**
     * Param: Entry with values to search for.
     *     Entry.Enum cannot be null
     *     Entry.Size 0 > to wildcard size; otherwise constrain search to size
     *     Entry.Data null to wildcard; other constrain search to data
     *
     * Queries the list and returns any found value.
     */
    public Entry? CheckData(Entry entry, bool wildcardSearch = false)
    {
        foreach (var e in Data)
        {
            if (wildcardSearch)
            {
                if (e.EqualsWildcard(entry))
                    return e.Clone();
            }
            else
            {
                if (e.Equals(entry))
                    return e.Clone();
            }
        }

        return null;
    }

    /**
     *
     * Param: Entry with values to search for.
     *     Entry.Enum cannot be null
     *     Entry.Size 0 > to wildcard size; otherwise constrain search to size
     *     Entry.Data null to wildcard; other constrain search to data
     * Param: int copies to index through matching entries (enum and size); using the copy param will always check the database
     *
     * Queries the list, if not contained, queries the database. If found, adds to the list.
     */
    public Entry? RequestData(Entry entry, bool wildcardSearch = false)
    {
        if (entry.Copy < 0) return null;
        Entry? result;
        if (entry.Copy == 0)
        {
            result = CheckData(entry, wildcardSearch);
            if (result != null)
                return result;
        }

        result = AccessIconsDb.GetEntry(entry);
        if (result == null)
            return null;

        Data.Add((Entry)result);

        return result;
    }

    /**
     * If an identical entry is not present in the database, add.
     * Does not add to the hashmap.
     */
    public bool PutData(Entry entry)
    {
        return AccessIconsDb.PutEntry(entry);
    }

    public void ClearCache()
    {
        Data.Clear();
    }


    /**
     *
     * Param: Entry with values to search for.
     *     Entry.Enum cannot be null
     *     Entry.Size 0 > to wildcard size; otherwise constrain search to size
     *     Entry.Data null to wildcard; other constrain search to data
     * Param: bool allOfType to obtain all entries matching only the enum type.
     *
     * Returns an enumerable that holds any matching entries.
     * Does not query the list.
     */
    public IEnumerable<Entry> GetEntries(Entry entry, bool allOfType = false)
    {
        return AccessIconsDb.GetAllData(entry, allOfType);
    }
}