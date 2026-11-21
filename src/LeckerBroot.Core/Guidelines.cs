namespace LeckerBroot.Core;

/// <summary>Ein Wertebereich mit Vorschlagswert.</summary>
public readonly record struct ValueRange(double Min, double Max)
{
    public double Mid => Math.Round((Min + Max) / 2, 1);
    public bool Contains(double value) => value >= Min - 0.0001 && value <= Max + 0.0001;
}

/// <summary>Richtwerte aus der Planung (Präsentation „LeckerBroot“).</summary>
public static class Guidelines
{
    /// <summary>
    /// Einordnung nach Roggenanteil (0..1) gemäß Leitsätzen für Brot und Kleingebäck:
    /// Roggen-/Weizenbrot ab 90 %, Roggen-/Weizenmischbrot über 50 bis unter 90 %, genau 50 : 50 = Mischbrot.
    /// </summary>
    public static BreadType Classify(double ryeShare, double totalFlour)
    {
        if (totalFlour <= 0) return BreadType.Unbekannt;
        const double eps = 0.005;   // Rundung: 49,6–50,4 % gilt als 50 : 50
        var wheatShare = 1 - ryeShare;
        if (ryeShare >= 0.90 - eps / 10) return BreadType.Roggenbrot;
        if (wheatShare >= 0.90 - eps / 10) return BreadType.Weizenbrot;
        if (ryeShare > 0.50 + eps) return BreadType.Roggenmischbrot;
        if (wheatShare > 0.50 + eps) return BreadType.Weizenmischbrot;
        return BreadType.Mischbrot;
    }

    /// <summary>
    /// Versäuerung in % vom Gesamtmehl. Abgeglichen mit der eigenen Tabelle und der Versäuerungstabelle
    /// der Bäckerei backdorf (umgerechnet aufs Gesamtmehl).
    /// </summary>
    public static ValueRange Acidification(BreadType type) => type switch
    {
        BreadType.Roggenbrot => new ValueRange(30, 45),
        BreadType.Roggenmischbrot => new ValueRange(28, 40),
        BreadType.Mischbrot => new ValueRange(25, 30),
        BreadType.Weizenmischbrot => new ValueRange(18, 25),
        BreadType.Weizenbrot => new ValueRange(10, 25),
        _ => new ValueRange(10, 45),
    };

    // Roggenanteil → mindestens zu versäuernder Anteil des Roggenmehls (Tabelle backdorf, einstufige Führung).
    private static readonly (double RyeShare, double MinPercentOfRye)[] RyeAcidCurve =
    {
        (0.10, 100), (0.20, 90), (0.30, 75), (0.40, 60), (0.60, 50), (0.70, 40), (0.80, 35), (1.00, 35),
    };

    /// <summary>
    /// Wie viel Prozent des Roggenmehls mindestens im Sauerteig stecken sollten, damit genug Säure
    /// die roggeneigenen Enzyme (Amylase) hemmt. Je weniger Roggen im Brot, desto größer der Anteil.
    /// Kurze, warme Führungen (Berliner Kurzsauer) bilden wenig Säure und brauchen mehr (50–70 %).
    /// </summary>
    public static double MinRyeAcidificationPercent(double ryeShare, bool shortWarmMethod = false)
    {
        var x = Math.Clamp(ryeShare, RyeAcidCurve[0].RyeShare, RyeAcidCurve[^1].RyeShare);
        double v = RyeAcidCurve[^1].MinPercentOfRye;
        for (var i = 0; i < RyeAcidCurve.Length - 1; i++)
        {
            var (x1, y1) = RyeAcidCurve[i];
            var (x2, y2) = RyeAcidCurve[i + 1];
            if (x >= x1 && x <= x2)
            {
                v = y1 + (y2 - y1) * (x - x1) / (x2 - x1);
                break;
            }
        }
        return shortWarmMethod ? Math.Max(v, 50) : v;
    }

    /// <summary>Übliche Teigausbeute.</summary>
    public static ValueRange Ta(BreadType type) => type switch
    {
        BreadType.Roggenbrot => new ValueRange(175, 185),
        BreadType.Roggenmischbrot => new ValueRange(170, 180),
        BreadType.Mischbrot => new ValueRange(168, 175),
        BreadType.Weizenmischbrot => new ValueRange(165, 175),
        BreadType.Weizenbrot => new ValueRange(160, 172),
        _ => new ValueRange(160, 185),
    };

    /// <summary>
    /// Richtwert Teig (g) je Liter Kastenform-Volumen, offen gebacken. Luftige Weizenbrote brauchen
    /// weniger Teig pro Liter als dichte Roggen- und Vollkornbrote. Am besten pro Rezept einstellen.
    /// </summary>
    public static double PanDoughPerLiter(BreadType type) => type switch
    {
        BreadType.Roggenbrot => 600,
        BreadType.Roggenmischbrot => 550,
        BreadType.Mischbrot => 500,
        BreadType.Weizenmischbrot => 450,
        BreadType.Weizenbrot => 400,
        _ => 500,
    };

    public static readonly ValueRange SaltPercent = new(1.5, 2.5);
    public static readonly ValueRange SaltPercentTypical = new(1.8, 2.2);

    /// <summary>Ab diesem Roggenanteil braucht der Teig Säure.</summary>
    public const double RyeNeedsSourdough = 0.20;
}

/// <summary>Deutsche Anzeigenamen für die Aufzählungen.</summary>
public static class Names
{
    public static string Of(IngredientKind k) => k switch
    {
        IngredientKind.Mehl => "Mehl",
        IngredientKind.Fluessigkeit => "Flüssigkeit",
        IngredientKind.Salz => "Salz",
        IngredientKind.Hefe => "Hefe",
        IngredientKind.Anstellgut => "Anstellgut",
        IngredientKind.Saaten => "Saaten / Körner",
        _ => "Sonstiges",
    };

    public static string Of(Grain g) => g switch
    {
        Grain.Keins => "–",
        Grain.Weizen => "Weizen",
        Grain.Roggen => "Roggen",
        Grain.Dinkel => "Dinkel",
        Grain.Urgetreide => "Urgetreide",
        _ => "Sonstiges",
    };

    public static string Of(StageType t) => t switch
    {
        StageType.Sauerteig => "Sauerteig",
        StageType.Vorteig => "Vorteig",
        StageType.Quellstueck => "Quellstück",
        StageType.Bruehstueck => "Brühstück",
        StageType.Kochstueck => "Kochstück",
        StageType.Hauptteig => "Hauptteig",
        _ => "Sonstiges",
    };

    public static string Of(Leavening l) => l switch
    {
        Leavening.Roggensauerteig => "Roggensauerteig",
        Leavening.Weizensauerteig => "Weizensauerteig",
        Leavening.ExotenSauerteig => "Exoten-Sauerteig",
        Leavening.Hefe => "Hefe",
        _ => "Sauerteig + Hefe",
    };

    public static string Of(BreadType b) => b switch
    {
        BreadType.Roggenbrot => "Roggenbrot",
        BreadType.Roggenmischbrot => "Roggenmischbrot",
        BreadType.Mischbrot => "Mischbrot",
        BreadType.Weizenmischbrot => "Weizenmischbrot",
        BreadType.Weizenbrot => "Weizenbrot",
        _ => "–",
    };

    /// <summary>Anzeigename einer Zutat inkl. Mehltyp.</summary>
    public static string Display(Ingredient i) =>
        string.IsNullOrWhiteSpace(i.FlourType) || i.Name.Contains(i.FlourType, StringComparison.OrdinalIgnoreCase)
            ? i.Name
            : $"{i.Name} {i.FlourType}";
}
