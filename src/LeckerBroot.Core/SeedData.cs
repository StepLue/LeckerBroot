namespace LeckerBroot.Core;

/// <summary>Startdaten beim ersten Öffnen der App: Grundzutaten und ein Beispielrezept.</summary>
public static class SeedData
{
    // Feste Ids, damit Grundzutaten in Rezepten und Datenbank zueinander passen.
    public static readonly Guid WaterId = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000001");
    public static readonly Guid SaltId = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000002");
    public static readonly Guid FreshYeastId = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000003");
    public static readonly Guid RyeStarterId = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000004");
    public static readonly Guid SunflowerId = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000005");
    public static readonly Guid Rye1150Id = Guid.Parse("6f0b0c1e-0002-4000-8000-000000000001");
    public static readonly Guid Wheat1050Id = Guid.Parse("6f0b0c1e-0002-4000-8000-000000000002");

    public static Ingredient Water() => new() { Id = WaterId, Name = "Wasser", Kind = IngredientKind.Fluessigkeit, WaterFraction = 1 };
    public static Ingredient Salt() => new() { Id = SaltId, Name = "Salz", Kind = IngredientKind.Salz };
    public static Ingredient FreshYeast() => new() { Id = FreshYeastId, Name = "Frischhefe", Kind = IngredientKind.Hefe };
    public static Ingredient RyeStarter() => new()
    {
        Id = RyeStarterId, Name = "Anstellgut Roggen", Kind = IngredientKind.Anstellgut, Grain = Grain.Roggen,
        FlourFraction = 0.5, WaterFraction = 0.5, Notes = "TA 200 – halb Mehl, halb Wasser",
    };
    public static Ingredient Sunflower() => new() { Id = SunflowerId, Name = "Sonnenblumenkerne", Kind = IngredientKind.Saaten };

    private static Ingredient Flour(Guid id, string name, Grain grain, string type, string notes = "") => new()
    {
        Id = id, Name = name, Kind = IngredientKind.Mehl, Grain = grain, FlourType = type, FlourFraction = 1, Notes = notes,
    };

    private static Ingredient Flour(string id, string name, Grain grain, string type, string notes = "") =>
        Flour(Guid.Parse(id), name, grain, type, notes);

    public static List<Ingredient> Ingredients() => new()
    {
        // Mehle
        Flour("6f0b0c1e-0002-4000-8000-000000000010", "Weizenmehl", Grain.Weizen, "550", "Allrounder für Weißbrot und Brötchen"),
        Flour(Wheat1050Id, "Weizenmehl", Grain.Weizen, "1050", "kräftiger, etwas mehr Wasserbedarf"),
        Flour("6f0b0c1e-0002-4000-8000-000000000011", "Weizenvollkornmehl", Grain.Weizen, "Vollkorn", "hoher Wasserbedarf"),
        Flour("6f0b0c1e-0002-4000-8000-000000000012", "Roggenmehl", Grain.Roggen, "997"),
        Flour(Rye1150Id, "Roggenmehl", Grain.Roggen, "1150", "Standard für Misch- und Roggenbrote"),
        Flour("6f0b0c1e-0002-4000-8000-000000000013", "Roggenvollkornmehl", Grain.Roggen, "Vollkorn"),
        Flour("6f0b0c1e-0002-4000-8000-000000000014", "Roggenschrot", Grain.Roggen, "fein", "gut für Brühstücke"),
        Flour("6f0b0c1e-0002-4000-8000-000000000015", "Dinkelmehl", Grain.Dinkel, "630", "kurz kneten, Teig nicht zu weich"),
        Flour("6f0b0c1e-0002-4000-8000-000000000016", "Dinkelvollkornmehl", Grain.Dinkel, "Vollkorn"),

        // Grundzutaten
        Water(),
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000010"), Name = "Milch", Kind = IngredientKind.Fluessigkeit, WaterFraction = 0.87, Notes = "ca. 87 % Wasser" },
        Salt(),
        FreshYeast(),
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000011"), Name = "Trockenhefe", Kind = IngredientKind.Hefe, Notes = "⅓ der Frischhefe-Menge" },
        RyeStarter(),

        // Saaten und Sonstiges
        Sunflower(),
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000012"), Name = "Leinsaat", Kind = IngredientKind.Saaten },
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000013"), Name = "Backmalz inaktiv", Kind = IngredientKind.Sonstiges, Notes = "1–3 %, Farbe und Aroma" },
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000014"), Name = "Brotgewürz", Kind = IngredientKind.Sonstiges, Notes = "0,5–2 %" },
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000015"), Name = "Honig", Kind = IngredientKind.Sonstiges },
        new() { Id = Guid.Parse("6f0b0c1e-0001-4000-8000-000000000016"), Name = "Butter", Kind = IngredientKind.Sonstiges },
    };

    /// <summary>Beispiel aus der Planung: Roggenmischbrot 70/30, 35 % Versäuerung, TA 170.</summary>
    public static RecipeDesign ExampleDesign(List<Ingredient> db)
    {
        Ingredient Find(Guid id) => (db.FirstOrDefault(i => i.Id == id) ?? throw new InvalidOperationException("Zutat fehlt")).Clone();
        var rye = Find(Rye1150Id);
        return new RecipeDesign
        {
            Name = "Roggenmischbrot (Beispiel)",
            Leavening = Leavening.Roggensauerteig,
            Form = "freigeschoben, Gärkörbchen",
            Count = 1,
            PieceWeight = 1545,
            BakeLossPercent = 12,
            Flours = new()
            {
                new FlourPart { Flour = rye, Percent = 70 },
                new FlourPart { Flour = Find(Wheat1050Id), Percent = 30 },
            },
            Ta = 170,
            UseSourdough = true,
            SourdoughFlour = rye.Clone(),
            AcidificationPercent = 35,
            SourdoughTa = 200,
            SourdoughMethod = SourdoughMethod.Einstufig,
            SourStages = SourdoughGuide.Preset(SourdoughMethod.Einstufig).Stages,
            StarterPercent = 10,
            SaltPercent = 2,
            Water = Find(WaterId),
            Salt = Find(SaltId),
            Yeast = Find(FreshYeastId),
            Starter = Find(RyeStarterId),
        };
    }

    public static AppData Create()
    {
        var data = new AppData
        {
            Ingredients = Ingredients(),
            Pans = new List<BakingPan>
            {
                new() { Name = "Kastenform 30 cm (Beispiel)", LengthCm = 28, WidthCm = 10, HeightCm = 7.5 },
            },
        };
        var design = ExampleDesign(data.Ingredients);
        RecipeDesigner.ApplyPlanningDefaults(design, force: true);
        var example = RecipeDesigner.ToRecipe(design, data.Settings);
        example.BakeNotes = "Ofen mit Stein 45–60 min auf 250 °C vorheizen. Mit Dampf 10–15 min anbacken, Dampf ablassen, "
                          + "bei 200–210 °C fertig backen (gesamt ca. 60 min, Kerntemperatur ca. 98 °C).";
        example.Notes = "Rechenbeispiel aus der Planung – zum Ausprobieren und Anpassen.";
        example.Tags = new() { "Beispiel" };
        foreach (var stage in example.Stages.Where(s => s.Type == StageType.Hauptteig))
        {
            stage.Duration = "Teigruhe 20–30 min, Stückgare 45–75 min";
            stage.Temperature = "Teigtemperatur 27–30 °C";
        }
        data.Recipes.Add(example);
        return data;
    }
}
