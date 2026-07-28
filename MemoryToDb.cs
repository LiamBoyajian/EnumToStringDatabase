using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using Godot;
using Godot.Collections;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;
using Main.main.scripts.core.plants;

namespace Main.addons.EnumToIcon.EnumToStringDatabase;

/**
 * Global designed to hold file paths in memory (intended for icons)
 */
public partial class MemoryToDb : Node
{
    public List<Entry> Data;
    public static Node Instance { get; private set; }
    public string FolderPath = "res://main/sprites/Icons/";
    public string[] FileExcludeTokens = { ".import" };
    public string GlobalFolderPath => GlobalPath();

    public override void _Ready()
    {
        //GlobalFolderPath = ProjectSettings.GlobalizePath(FolderPath);
        base._Ready();
        Data = new List<Entry>();
        //ValidateIconDirectory();
        Instance = this;
        AccessIconsDb.InitDb();

        ValidateIconDirectory();
        var temp = GetEntries(new Entry(), true).GetEnumerator();

        GD.Print("\nWATT: " + AccessIconsDb.DbData);
    }

    public string GetGlobalPathConcat(string fileName)
    {
        return ProjectSettings.GlobalizePath(FolderPath + fileName);
    }

    public string GlobalPath()
    {
        return ProjectSettings.GlobalizePath(FolderPath);
    }

    /**
     * param bool: uses the godot local address; or whatever local address is set
     */
    public int ValidateIconDirectory(bool local = false, string setLocalDirectory = null)
    {
        if (setLocalDirectory != null)
            FolderPath = setLocalDirectory;
        //Datetime format may not be standard
        var path = local ? FolderPath : GlobalFolderPath;

        string configPath = Path.Combine(path, "config.cfg");
        var config = new ConfigFile();
        var err = config.Load(configPath);

        bool unsynced = true;
        string lastEdit = Directory.GetLastWriteTime(GlobalPath()).ToString("yyyy-MM-dd HH:mm:ss");

        if (err == Error.Ok)
            unsynced = String.CompareOrdinal(config.GetValue("Icons", "LastIconsEditTime").AsString(), lastEdit) != 0;

        var result = -2;
        if (unsynced)
        {
            AccessIconsDb.ClearDatabase();
            result = InitializeFromDirectory(path);
            if (result > 0)
            {
                config.SetValue("Icons", "LastIconsEditTime", lastEdit);
                config.Save(configPath);
            }
        }

        return result;
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
        string[] files = Directory.GetFiles(dir);
        List<Entry> tempList = new List<Entry>();
        foreach (var f in files)
        {
            bool containsAnExclusion = false;
            foreach (var str in FileExcludeTokens)
            {
                if (f.Contains(str))
                    containsAnExclusion = true;
            }

            if (containsAnExclusion) continue;

            var fromString = f.GetFile();

            if (Entry.FromString(fromString) is { } entry)
            {
                entry.Data = f;
                tempList.Add(entry);
                ++result;
            }
        }

        //Sub-dirs -----------------------
        AccessIconsDb.PutAll(tempList.GetEnumerator());
        Data.AddRange(tempList);

        var subDirs = Directory.GetDirectories(dir);
        foreach (var d in subDirs)
        {
            if (recursive)
                result += InitializeFromDirectory(d); //Recur -----------------------
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