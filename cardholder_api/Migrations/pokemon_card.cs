using System;
using System.Collections.Generic;

namespace cardholder_api.Migrations;

public partial class pokemon_card
{
    public string id { get; set; } = null!;

    public string? set { get; set; }

    public string? series { get; set; }

    public string? generation { get; set; }

    public DateOnly? release_date { get; set; }

    public string? name { get; set; }

    public string? set_num { get; set; }

    public string? types { get; set; }

    public string? supertype { get; set; }

    public int? hp { get; set; }

    public string? evolvesfrom { get; set; }

    public string? evolvesto { get; set; }

    public string? rarity { get; set; }

    public string? flavortext { get; set; }
}
