using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

public partial class StatRegistry : Node
{
    public static StatRegistry Inst { get; private set; }
    public JsonSerializerOptions jsoptions { get; private set; }

    public override void _Ready()
    {
        Inst = this;
        jsoptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        jsoptions.Converters.Add(new CustomStatConverter());
        jsoptions.Converters.Add(new CustomVector3IConverter());
        InitFromPaths();
    }
    const string classes_path = "res://CharacterSheet/Classes/";
    const string species_path = "res://CharacterSheet/Species/";
    const string prebuiltchars_path = "res://CharacterSheet/PrebuiltChars/";
    const string equipment_path = "res://CharacterSheet/Equipment/";
    const string major_stats_path = "res://CharacterSheet/MajorStats/";
    const string minor_stats_path = "res://CharacterSheet/MinorStats/";
    public Dictionary<ClassKey, ClassSheet> Classes { get; private set; }
    public Dictionary<SpeciesKey, SpeciesSheet> Species { get; private set; }
    public Dictionary<string, CharSheet> PrebuiltChars { get; private set; }
    public Dictionary<string, Equipment> EquipmentDict { get; private set; }
    public Dictionary<BaseStatKey, StatProperties> BaseStatProperties { get; private set; }
    public Dictionary<MinorStatKey, StatProperties> MinorStatProperties { get; private set; }

    public void ReadDirIntoDict()
    {

    }
    public void InitFromPaths()
    {
        // Collect class data
        Classes = new Dictionary<ClassKey, ClassSheet>();
        foreach (string str in ResourceLoader.ListDirectory(classes_path))
        {
            if (str[^1] != '/') continue;
            string objname = str[..^1];
            if (Enum.TryParse<ClassKey>(objname, out ClassKey result))
            {
                string path = classes_path + str;
                string datapath = path + "_" + objname + ".json";
                var file = FileAccess.Open(datapath, FileAccess.ModeFlags.Read);
                GD.Print("Reading " + file.ToString() + " from path " + datapath);
                ClassSheet cs = ClassSheet.FromJSONString(file.GetAsText());
                cs.DefaultPawnScene = ResourceLoader.Load<PackedScene>(path + "_" + objname + "Obj.tscn");
                Classes.Add(result, cs);
            }
        }
        // Collect species data
        Species = new Dictionary<SpeciesKey, SpeciesSheet>();
        foreach (string str in ResourceLoader.ListDirectory(species_path))
        {
            if (str[^1] != '/') continue;
            string objname = str[..^1];
            if (Enum.TryParse<SpeciesKey>(objname, out SpeciesKey result))
            {
                string path = species_path + str;
                string datapath = path + "_" + objname + ".json";
                var file = FileAccess.Open(datapath, FileAccess.ModeFlags.Read);
                GD.Print("Reading " + file.ToString() + " from path " + datapath);
                Species.Add(result, SpeciesSheet.FromJSONString(file.GetAsText()));
            }
        }
        // Collect equipment data
        EquipmentDict = new Dictionary<string, Equipment>();
        foreach (string str in ResourceLoader.ListDirectory(equipment_path))
        {
            if (str[^1] != '/') continue;
            string objname = str[..^1];
            string path = equipment_path + str;
            string datapath = path + "_" + objname + ".json";
            var file = FileAccess.Open(datapath, FileAccess.ModeFlags.Read);
            GD.Print("Reading " + file.ToString() + " from path " + datapath);
            Equipment newsheet = Equipment.FromJSONString(file.GetAsText());
            EquipmentDict.Add(objname, newsheet);
        }
        // Collect prebuilt character data
        PrebuiltChars = new Dictionary<string, CharSheet>();
        foreach (string str in ResourceLoader.ListDirectory(prebuiltchars_path))
        {
            if (str[^1] != '/') continue;
            string objname = str[..^1];
            string path = prebuiltchars_path + str;
            string datapath = path + "_" + objname + ".json";
            var file = FileAccess.Open(datapath, FileAccess.ModeFlags.Read);
            GD.Print("Reading " + file.ToString() + " from path " + datapath);
            CharSheet newsheet = CharSheet.FromJSONString(file.GetAsText());
            newsheet.JsonSourcePath = datapath;
            PrebuiltChars.Add(objname, newsheet);
        }
    }


    public static JsonObject TxtResToJson(Resource res)
    {
        JsonObject jo = new JsonObject();
        var file = FileAccess.Open(res.ResourcePath, FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();
        jo = JsonNode.Parse(jsonText).AsObject();
        return jo;
    }
    public static JsonObject TxtResToJson(string filePath)
    {
        Resource res = ResourceLoader.Load(filePath);
        return TxtResToJson(res);
    }

    public static T FromJSONString<T>(string json_string)
    {
        return JsonSerializer.Deserialize<T>(json_string,Inst.jsoptions);
    }
    public static string ToJSONString<T>(T obj)
    {
        return JsonSerializer.Serialize(obj,Inst.jsoptions);
    }

}
