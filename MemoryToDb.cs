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
    public static MemoryToDb Instance { get; private set; }
    public string FolderPath = "res://main/sprites/Icons/";
    public string[] FileExcludeTokens = [".import"];
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
    }

    public string GetGlobalPathConcat(string fileName)
    {
        return ProjectSettings.GlobalizePath(FolderPath + fileName);
    }

    public string GlobalPath()
    {
        return ProjectSettings.GlobalizePath(FolderPath);
    }

    public static Texture2D GetTextureFromEntry(Entry entry)
    {
        var temp = Instance.RequestData(entry)?.Data;
        var tempTexture = GD.Load<Texture2D>(temp);
        return tempTexture;
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
            if (result >= 0)
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
            }
        }

        //Sub-dirs -----------------------
        result += AccessIconsDb.PutAll(tempList.GetEnumerator());

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
     * Queries the list and returns any found value.
     */
    public Entry? CheckData(Entry entry)
    {
        foreach (var e in Data)
        {
            if (e.EqualsWildcard(entry))
                return e.Clone();
        }

        return null;
    }

    /**
     *
     * Queries the list, if not contained, queries the database. If found, adds to the list.
     */
    public Entry? RequestData(Entry entry)
    {
        if (CheckData(entry) is { } result)
            return result;


        if (AccessIconsDb.GetEntry(entry) is not { } dbResult)
            return null;

        Data.Add(dbResult);

        return dbResult;
    }

    /**
     * If an identical entry is not present in the database, add.
     * Does not add to the hashmap.
     */
    public bool PutData(Entry entry)
    {
        return AccessIconsDb.PutEntry(entry) >= 0;
    }

    public void ClearCache()
    {
        Data.Clear();
    }


    /**
     *
     */
    public IEnumerable<Entry> GetEntries(Entry entry, bool allOfType = false)
    {
        return AccessIconsDb.GetAllData(entry, allOfType);
    }
}