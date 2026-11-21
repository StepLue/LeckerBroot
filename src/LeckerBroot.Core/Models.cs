namespace LeckerBroot.Core;

/// <summary>Art einer Zutat. Bestimmt, wie sie in Berechnungen behandelt wird.</summary>
public enum IngredientKind { Mehl, Fluessigkeit, Salz, Hefe, Anstellgut, Saaten, Sonstiges }

/// <summary>Getreideart eines Mehls (für die Einordnung der Brotart).</summary>
public enum Grain { Keins, Weizen, Roggen, Dinkel, Urgetreide, Sonstiges }

/// <summary>Art einer Teigstufe.</summary>
public enum StageType { Sauerteig, Vorteig, Quellstueck, Bruehstueck, Kochstueck, Hauptteig, Sonstiges }

/// <summary>Triebmittel eines Rezepts (für die Suche).</summary>
public enum Leavening { Roggensauerteig, Weizensauerteig, ExotenSauerteig, Hefe, SauerteigUndHefe }

/// <summary>Brotart nach Roggen-/Weizenanteil.</summary>
public enum BreadType { Unbekannt, Roggenbrot, Roggenmischbrot, Mischbrot, Weizenmischbrot, Weizenbrot }

/// <summary>Status eines Rezepts: in Entwicklung (frei bearbeitbar) oder fertig (nur noch Menge).</summary>
public enum RecipeStatus { Entwicklung, Fertig }

/// <summary>Eine Kastenform. Volumen aus den Innenmaßen oder – genauer – ausgelitert.</summary>
public class BakingPan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Kastenform";

    /// <summary>Innenmaße in cm (bei schrägen Wänden in halber Höhe messen).</summary>
    public double LengthCm { get; set; } = 28;
    public double WidthCm { get; set; } = 10;
    public double HeightCm { get; set; } = 7.5;

    /// <summary>Ausgelitertes Volumen in ml (randvoll Wasser, gewogen). 0 = aus den Maßen rechnen.</summary>
    public double MeasuredVolumeMl { get; set; }

    public double VolumeL => MeasuredVolumeMl > 0
        ? MeasuredVolumeMl / 1000
        : Math.Max(0, LengthCm) * Math.Max(0, WidthCm) * Math.Max(0, HeightCm) / 1000;
}

/// <summary>Eine Zutat aus der Zutatendatenbank.</summary>
public class Ingredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public IngredientKind Kind { get; set; } = IngredientKind.Sonstiges;
    public Grain Grain { get; set; } = Grain.Keins;

    /// <summary>Mehltyp, z. B. "1150" oder "Vollkorn".</summary>
    public string FlourType { get; set; } = "";

    /// <summary>Anteil, der als Mehl zählt (0..1). Mehl = 1, Anstellgut TA 200 = 0,5.</summary>
    public double FlourFraction { get; set; }

    /// <summary>Anteil, der als Flüssigkeit für die TA zählt (0..1). Wasser = 1, Milch ≈ 0,87.</summary>
    public double WaterFraction { get; set; }

    public string Notes { get; set; } = "";

    public Ingredient Clone() => (Ingredient)MemberwiseClone();
}

/// <summary>Zutat innerhalb einer Stufe. Enthält eine Kopie der Zutat, damit das Rezept für sich allein vollständig ist.</summary>
public class StageIngredient
{
    public Ingredient Ingredient { get; set; } = new();
    public double Grams { get; set; }
}

/// <summary>Eine Stufe des Rezepts, z. B. Sauerteig, Quellstück oder Hauptteig.</summary>
public class Stage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public StageType Type { get; set; } = StageType.Hauptteig;

    /// <summary>Zählt das Wasser dieser Stufe zur Teigausbeute? (Quellstück mit Saaten meist nicht.)</summary>
    public bool WaterCountsToTa { get; set; } = true;

    /// <summary>Standzeit in Stunden für den Backtag-Plan (0 = unbekannt).</summary>
    public double PlanHours { get; set; }

    /// <summary>Baut auf der vorigen Stufe auf (mehrstufiger Sauerteig) – wird dann nacheinander geplant.</summary>
    public bool FollowsPrevious { get; set; }

    public string Duration { get; set; } = "";
    public string Temperature { get; set; } = "";
    public string Instructions { get; set; } = "";
    public List<StageIngredient> Ingredients { get; set; } = new();
}

/// <summary>Ein Rezept. Die Grammangaben in den Stufen gelten für die Referenzmenge Count × PieceWeight.</summary>
public class Recipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public Leavening Leavening { get; set; } = Leavening.Roggensauerteig;
    public List<string> Tags { get; set; } = new();
    public bool Favorite { get; set; }

    public RecipeStatus Status { get; set; } = RecipeStatus.Entwicklung;

    /// <summary>Wann das Rezept als fertig markiert wurde.</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Teig je Liter Kastenform, auf dieses Rezept eingestellt („passt genau“). 0 = Richtwert der Brotart.</summary>
    public double PanDoughPerLiter { get; set; }

    /// <summary>Zuletzt gewählte Kastenform.</summary>
    public Guid? PanId { get; set; }

    public bool IsLocked => Status == RecipeStatus.Fertig;

    /// <summary>Anzahl der Teiglinge für die gespeicherten Grammangaben.</summary>
    public int Count { get; set; } = 1;

    /// <summary>Gewicht eines fertigen Brots in g für die gespeicherten Grammangaben.</summary>
    public double PieceWeight { get; set; } = 1000;

    public double BakeLossPercent { get; set; } = 12;
    public string Form { get; set; } = "";

    /// <summary>Backen als Freitext (Phase 1). Phase 2 ersetzt das durch Backprofil-Phasen.</summary>
    public string BakeNotes { get; set; } = "";

    public string Notes { get; set; } = "";
    public List<Stage> Stages { get; set; } = new();

    /// <summary>Ablauf vom Hauptteig bis zum Auskühlen.</summary>
    public List<ProcessStep> Process { get; set; } = new();

    /// <summary>Backprofil (Phasen mit Temperatur und Dauer).</summary>
    public List<BakePhase> BakeProfile { get; set; } = new();

    public double PreheatMinutes { get; set; } = 45;
    public double PreheatTemperatureC { get; set; } = 250;
    public double CoreTemperatureC { get; set; } = 96;

    /// <summary>Temperaturen für die Schüttwasser-Berechnung.</summary>
    public TemperaturePlan Temperatures { get; set; } = new();

    /// <summary>Parameter aus dem Rezept-Assistenten. Null bei von Hand eingetragenen Rezepten.</summary>
    public RecipeDesign? Design { get; set; }

    /// <summary>Individuelle Anpassung: bevorzugte Anzahl beim Anzeigen.</summary>
    public int? SavedCount { get; set; }

    /// <summary>Individuelle Anpassung: bevorzugtes Stückgewicht beim Anzeigen.</summary>
    public double? SavedPieceWeight { get; set; }

    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime Modified { get; set; } = DateTime.Now;
}

public class AppSettings
{
    /// <summary>Mehl und Wasser des Anstellguts in Gesamtmehl und TA einrechnen.</summary>
    public bool IncludeStarterInTotals { get; set; }
}

/// <summary>Alles, was die App speichert (und was als Sicherungsdatei exportiert wird).</summary>
public class AppData
{
    public int Version { get; set; } = 1;
    public List<Ingredient> Ingredients { get; set; } = new();
    public List<Recipe> Recipes { get; set; } = new();
    public List<BakingPan> Pans { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
