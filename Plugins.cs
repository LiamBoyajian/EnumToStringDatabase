using Godot;

namespace Main.addons.EnumToIcon.EnumToStringDatabase;

#if TOOLS
[Tool]
public partial class DatabaseBuildPlugin : EditorPlugin
{
    public string IconDirPath = "res://main/sprites/Icons/";

    public override bool _Build()
    {
        GD.Print("Building icon database...");

        var memToDb = new MemoryToDb();
        return memToDb.InitializeFromDirectory("res://main/sprites/Icons/") > 0;
    }
}

#endif