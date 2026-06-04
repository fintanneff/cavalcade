using Godot;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

[GlobalClass]
public partial class StatProperties : Resource
{
    [Export] public String StatName { get; set; } = "not_named";
    [Export] public Texture2D StatIcon { get; set; }
    [Export] public int Max { get; set; } = 20;
    [Export] public int Min { get; set; } = 0;
    [Export] public bool CanBoost { get; set; } = true;

    public static StatProperties FromJson(JsonObject jo)
    {
        StatProperties statProps = new StatProperties();
        statProps.StatName = jo["StatName"].GetValue<string>();
        if (jo.ContainsKey("StatIcon")) statProps.StatIcon = null;
        if (jo.ContainsKey("Max")) statProps.Max = jo["Max"].GetValue<int>();
        if (jo.ContainsKey("Mib")) statProps.Min = jo["Min"].GetValue<int>();
        if (jo.ContainsKey("CanBoost")) statProps.CanBoost = jo["CanBeBoosted"].GetValue<bool>();
        return statProps;
    }
}