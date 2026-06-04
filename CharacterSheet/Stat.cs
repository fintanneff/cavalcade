using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public struct Stat
{
    public int Value { get; set; }
    public int IncreaseChance { get; set; }
    public int DecreaseChance { get; set; }
    public Stat(int v, int i, int d)
    {
        Value = v;
        IncreaseChance = i;
        DecreaseChance = d;
    }

}

public class CustomStatConverter : JsonConverter<Stat>
{
    public override Stat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Stat st = new Stat();
        string[] arr = reader.GetString().Split(" ");
        st.Value = int.Parse(arr[0]);
        st.IncreaseChance = int.Parse(arr[1]);
        st.DecreaseChance = int.Parse(arr[2]);
        return st;
    }

    public override void Write(Utf8JsonWriter writer, Stat value, JsonSerializerOptions options)
    {
        string str = $"{value.Value} {value.IncreaseChance} {value.DecreaseChance}";
        writer.WriteStringValue(str);
    }
}

public class CustomVector3IConverter : JsonConverter<Vector3I>
{
    public override Vector3I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Vector3I v3i = new Vector3I();
        string[] arr = reader.GetString().Split(" ");
        v3i.X = int.Parse(arr[0]);
        v3i.Y = int.Parse(arr[1]);
        v3i.Z = int.Parse(arr[2]);
        return v3i;
    }

    public override void Write(Utf8JsonWriter writer, Vector3I value, JsonSerializerOptions options)
    {
        string str = $"{value.X} {value.Y} {value.Z}";
        writer.WriteStringValue(str);
    }
}