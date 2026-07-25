using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Godot;
using Godot.Collections;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;

namespace Main.addons.EnumToIcon.EnumToStringDatabase;

/**
 * Global designed to hold file paths in memory (intended for icons)
 */
public partial class MemoryToDb : Node
{
    public Hashtable Data;
    public Node Instance;

    public override void _Ready()
    {
        base._Ready();
        Data = new Hashtable();
        Instance = this;
    }

    /**
     * Param: Entry with values to search for.
     *     Entry.Enum cannot be null
     *     Entry.Size 0 > to wildcard size; otherwise constrain search to size
     *     Entry.Data null to wildcard; other constrain search to data
     *
     * Queries the hashtable and returns any found value.
     */
    public Entry? CheckData(Entry entry)
    {
        if (entry.Enum == null) return null;
        return (Entry?)Data[entry.GetHashCode()];
    }

    /**
     * Param: Entry with values to search for.
     *     Entry.Enum cannot be null
     *     Entry.Size 0 > to wildcard size; otherwise constrain search to size
     *     Entry.Data null to wildcard; other constrain search to data
     *
     * Queries the hashtable and returns if a value was found.
     */
    public bool HasData(Entry entry)
    {
        return Data.ContainsKey(entry.GetHashCode());
    }

    /**
     *
     * Param: Entry with values to search for.
     *     Entry.Enum cannot be null
     *     Entry.Size 0 > to wildcard size; otherwise constrain search to size
     *     Entry.Data null to wildcard; other constrain search to data
     * Param: int copies to index through matching entries (enum and size)
     *
     * Queries the hashtable, if not contained, queries the database. If found, adds to the hashtable.
     */
    public Entry? RequestData(Entry entry, int copy = 0)
    {
        if (copy < 0) return null;

        var result = CheckData(entry);
        if (result != null)
            return result;

        result = AccessIconsDb.GetEntry(entry, copy);
        if (result == null)
            return null;

        Data.Add(result.GetHashCode(), result);

        return result;
    }

    public bool ClearCache()
    {
        Data.Clear();
        return true;
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
     * Does not query the hashtable.
     */
    public IEnumerable<Entry> GetEntries(Entry entry, bool allOfType = false)
    {
        return AccessIconsDb.GetAllData(entry, allOfType);
    }
}