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
        //InitFromPaths();
    }
    const string classes_path = "res://CharacterSheet/Classes/";
    const string species_path = "res://CharacterSheet/Species/";
    const string major_stats_path = "res://CharacterSheet/MajorStats/";
    const string minor_stats_path = "res://CharacterSheet/MinorStats/";
    public Dictionary<ClassKey, ClassSheet> Classes;
    public Dictionary<SpeciesKey, SpeciesSheet> Species;
    public Dictionary<BaseStatKey, StatProperties> BaseStatProperties;
    public Dictionary<MinorStatKey, StatProperties> MinorStatProperties;


    public void InitFromPaths()
    {
        foreach (string str in ResourceLoader.ListDirectory(classes_path))
        {
            string objname = str[..^1];
            if (Enum.TryParse<ClassKey>(objname, out ClassKey result))
            {
                string path = classes_path + str;
                string datapath = path + "_" + objname + ".json";
                var file = FileAccess.Open(datapath, FileAccess.ModeFlags.Read);
                Classes.Add(result, ClassSheet.FromJSONString(file.GetAsText()));
            }
        }
        foreach (string str in ResourceLoader.ListDirectory(species_path))
        {
            string objname = str[..^1];
            if (Enum.TryParse<SpeciesKey>(objname, out SpeciesKey result))
            {
                string path = species_path + str;
                string datapath = path + "_" + objname + ".json";
                var file = FileAccess.Open(datapath, FileAccess.ModeFlags.Read);
                Species.Add(result, SpeciesSheet.FromJSONString(file.GetAsText()));
            }
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
