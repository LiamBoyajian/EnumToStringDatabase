using Godot;
using Main.addons.EnumToIcon.EnumToStringDatabase.main;

namespace Main.addons.EnumToIcon.EnumToStringDatabase;


#if TOOLS
[Tool]
public partial class DatabaseBuildPlugin : EditorPlugin
{
    private const string AutoloadName = "MemoryToDb";
    private const string ScriptPath = "res://addons/EnumToIcon/EnumToStringDatabase/MemoryToDb.cs";

    public override void _EnterTree()
    {
    }

    public override bool _Build()
    {
        //OS.Alert("\nBuilding icon database...");
        //GD.PrintErr("\nBuilding icon database...");

        //MemoryToDb.Instance.ValidateIconDirectory();
        //AccessIconsDb.InitDb();
        //AddAutoloadSingleton(AutoloadName, ScriptPath);

        return true;
    }

    public override void _Ready()
    {
        //OS.Alert("\nBuilding icon database...");
    }
}

#endif