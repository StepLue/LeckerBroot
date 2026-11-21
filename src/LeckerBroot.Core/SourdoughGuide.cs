using System.Globalization;

namespace LeckerBroot.Core;

/// <summary>Führungsart des Sauerteigs (Vorlage im Assistenten).</summary>
public enum SourdoughMethod { Einstufig, FallendeTemperatur, BerlinerKurzsauer, DetmolderDreistufig, Eigene }

/// <summary>Eine Sauerteigstufe. Der Anteil bezieht sich auf das gesamte Sauerteigmehl.</summary>
public class SourStage
{
    public string Name { get; set; } = "Sauerteig";

    /// <summary>Anteil am gesamten Sauerteigmehl in %.</summary>
    public double SharePercent { get; set; } = 100;

    public double Ta { get; set; } = 200;

    /// <summary>Temperatur in °C (bei fallender Führung: Starttemperatur).</summary>
    public double TemperatureC { get; set; } = 25;

    /// <summary>Zusatz zur Temperatur, z. B. „fallend auf Raumtemperatur“.</summary>
    public string TemperatureNote { get; set; } = "";

    /// <summary>Standzeit in Stunden.</summary>
    public double Hours { get; set; } = 14;

    /// <summary>Optionaler Text zur Dauer, z. B. „12–16 h“. Leer = aus Hours.</summary>
    public string HoursNote { get; set; } = "";

    public string DurationText => string.IsNullOrWhiteSpace(HoursNote) ? $"{Fmt.N(Hours)} h" : HoursNote;

    public string TemperatureText => string.IsNullOrWhiteSpace(TemperatureNote)
        ? $"{Fmt.N(TemperatureC)} °C"
        : $"{Fmt.N(TemperatureC)} °C, {TemperatureNote}";

    public SourStage Copy() => (SourStage)MemberwiseClone();
}

/// <summary>Vorschlag aus dem Geschmacksregler.</summary>
public readonly record struct FlavorSuggestion(double TemperatureC, double Ta, double StarterPercent, double Hours);

/// <summary>Wissen zur Sauerteigführung: Vorlagen, Geschmackstendenz, Reifezeit-Faustwert.</summary>
public static class SourdoughGuide
{
    public static string Name(SourdoughMethod m) => m switch
    {
        SourdoughMethod.Einstufig => "Einstufig",
        SourdoughMethod.FallendeTemperatur => "Fallende Temp.",
        SourdoughMethod.BerlinerKurzsauer => "Kurzsauer",
        SourdoughMethod.DetmolderDreistufig => "3-stufig",
        _ => "Eigene",
    };

    public static string Describe(SourdoughMethod m) => m switch
    {
        SourdoughMethod.Einstufig =>
            "Einfach und ausgewogen. Über Nacht bei Raumtemperatur – die übliche Führung für zu Hause.",
        SourdoughMethod.FallendeTemperatur =>
            "Fallende Temperatur: mit warmem Wasser (ca. 28–30 °C) ansetzen und bei Raumtemperatur abkühlen lassen. Erst Hefe und Milchsäure, später etwas Essigsäure – rund im Geschmack.",
        SourdoughMethod.BerlinerKurzsauer =>
            "Berliner Kurzsauer: warm und schnell (ca. 35 °C, 3 h). Mild, aber wenig Aroma und Triebkraft – wenn es schnell gehen muss.",
        SourdoughMethod.DetmolderDreistufig =>
            "Detmolder Dreistufenführung, vereinfacht: Anfrischsauer weckt die Hefe, fester kühler Grundsauer bildet Essigsäure, weicher warmer Vollsauer Milchsäure und Trieb. Sehr aromatisch, aber aufwendig. Anteile anpassbar.",
        _ => "Eigene Einstellungen.",
    };

    /// <summary>Stufen und Anstellgut-% einer Vorlage.</summary>
    public static (List<SourStage> Stages, double StarterPercent) Preset(SourdoughMethod m) => m switch
    {
        SourdoughMethod.FallendeTemperatur => (new List<SourStage>
        {
            new SourStage { Name = "Sauerteig", SharePercent = 100, Ta = 200, TemperatureC = 28, TemperatureNote = "fallend auf Raumtemperatur", Hours = 14, HoursNote = "12–16 h" },
        }, 10),
        SourdoughMethod.BerlinerKurzsauer => (new List<SourStage>
        {
            new SourStage { Name = "Sauerteig", SharePercent = 100, Ta = 200, TemperatureC = 35, Hours = 3, HoursNote = "ca. 3 h" },
        }, 20),
        SourdoughMethod.DetmolderDreistufig => (new List<SourStage>
        {
            new SourStage { Name = "Anfrischsauer", SharePercent = 10, Ta = 200, TemperatureC = 26, Hours = 6, HoursNote = "5–6 h" },
            new SourStage { Name = "Grundsauer", SharePercent = 40, Ta = 160, TemperatureC = 24, Hours = 18, HoursNote = "15–24 h" },
            new SourStage { Name = "Vollsauer", SharePercent = 50, Ta = 200, TemperatureC = 33, Hours = 3, HoursNote = "ca. 3 h" },
        }, 10),
        _ => (new List<SourStage>
        {
            new SourStage { Name = "Sauerteig", SharePercent = 100, Ta = 200, TemperatureC = 25, Hours = 14, HoursNote = "12–16 h" },
        }, 15),
    };

    // ---------- Reifezeit (Faustwert) ----------

    // Anstellgut in % vom Sauerteigmehl → Stunden bei 25 °C, TA 200.
    private static readonly (double Asg, double Hours)[] RipeningAt25 =
    {
        (1, 23), (2, 21), (5, 18), (10, 14), (20, 11), (30, 9),
    };

    public const double EstimateMinTemp = 18;
    public const double EstimateMaxTemp = 30;

    /// <summary>
    /// Grober Faustwert für die Reifezeit einer einstufigen Führung.
    /// Null außerhalb von 18–30 °C (dort passt die einfache Faustregel nicht).
    /// </summary>
    public static double? EstimateHours(double starterPercent, double temperatureC)
    {
        if (temperatureC < EstimateMinTemp || temperatureC > EstimateMaxTemp || starterPercent <= 0) return null;
        var asg = Math.Clamp(starterPercent, RipeningAt25[0].Asg, RipeningAt25[^1].Asg);
        double baseHours = RipeningAt25[^1].Hours;
        for (var i = 0; i < RipeningAt25.Length - 1; i++)
        {
            var (a1, h1) = RipeningAt25[i];
            var (a2, h2) = RipeningAt25[i + 1];
            if (asg >= a1 && asg <= a2)
            {
                baseHours = h1 + (h2 - h1) * (asg - a1) / (a2 - a1);
                break;
            }
        }
        // je 10 °C wärmer etwa halbe Zeit
        return baseHours * Math.Pow(2, (25 - temperatureC) / 10);
    }

    // ---------- Geschmack ----------

    /// <summary>
    /// Tendenz 0 (mild, milchsauer) bis 1 (kräftig, essigsauer) aus Temperatur, TA, Anstellgut und Zeit.
    /// Mehrstufige Führungen werden nach Mehlanteil gewichtet.
    /// </summary>
    public static double FlavorScore(IReadOnlyList<SourStage> stages, double starterPercent)
    {
        if (stages.Count == 0) return 0.5;
        var shareSum = stages.Sum(s => Math.Max(0, s.SharePercent));
        if (shareSum <= 0) return 0.5;

        double stagePart = 0;
        foreach (var s in stages)
        {
            var w = Math.Max(0, s.SharePercent) / shareSum;
            var temp = Clamp01((35 - s.TemperatureC) / 15);   // 35 °C mild … 20 °C kräftig
            var ta = Clamp01((220 - s.Ta) / 70);              // TA 220 mild … 150 kräftig
            var time = Clamp01((s.Hours - 3) / 21);            // 3 h mild … 24 h kräftig
            stagePart += w * (0.40 * temp + 0.30 * ta + 0.15 * time) / 0.85;
        }
        var asg = Clamp01((30 - starterPercent) / 28);          // 30 % mild … 2 % kräftig
        return Clamp01(0.82 * stagePart + 0.18 * asg);
    }

    public static string FlavorText(double score) => CharacterText(score);

    public static string LevelName(int level) => level switch
    {
        1 => "sehr mild",
        2 => "mild",
        3 => "ausgewogen",
        4 => "herb",
        _ => "sehr herb",
    };

    /// <summary>Vorschlag für eine einstufige Führung je Geschmacksstufe 1 (sehr mild) bis 5 (sehr kräftig).</summary>
    public static FlavorSuggestion Suggest(int level)
    {
        var (temp, ta, asg) = Math.Clamp(level, 1, 5) switch
        {
            1 => (30.0, 210.0, 25.0),
            2 => (28.0, 200.0, 20.0),
            3 => (25.0, 200.0, 12.0),
            4 => (23.0, 180.0, 8.0),
            _ => (21.0, 160.0, 5.0),
        };
        var hours = EstimateHours(asg, temp) ?? 14;
        return new FlavorSuggestion(temp, ta, asg, Math.Round(hours));
    }

    // ---------- Geschmacksprofil: Säuremenge und Säurecharakter ----------
    //
    // Sauerteige werden fachlich über zwei Größen beschrieben: wie viel Säure entsteht
    // (Säuregrad) und wie sie zusammengesetzt ist (Milchsäure : Essigsäure).
    // Die Säuremenge hängt vor allem davon ab, wie viel Mehl versäuert wird, welches Getreide
    // (Roggen säuert stärker als Weizen, Weizenmehl puffert am wenigsten) und wie mineralstoffreich
    // das Mehl ist (Vollkorn > helle Typen). Der Charakter hängt von Temperatur, TA, Anstellgut und Zeit ab.
    // Die Faktoren sind Schätzwerte für eine Tendenz, keine Messwerte.

    /// <summary>Wie stark ein Getreide im Sauerteig säuert (Roggen = 1).</summary>
    public static double GrainFactor(Grain g) => g switch
    {
        Grain.Roggen => 1.0,
        Grain.Dinkel => 0.7,
        Grain.Urgetreide => 0.65,
        Grain.Weizen => 0.6,
        _ => 0.7,
    };

    /// <summary>Mineralstoffe (Mehltyp) puffern und erhöhen die Säurebildung: Typ 405 = 0,8 … Typ 1740 = 1,2, Vollkorn/Schrot = 1,2.</summary>
    public static double AshFactor(Ingredient flour)
    {
        var text = (flour.FlourType + " " + flour.Name).ToLowerInvariant();
        if (text.Contains("voll") || text.Contains("schrot")) return 1.2;
        var digits = new string(flour.FlourType.Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var type) || type <= 0) return 1.0;
        return 0.8 + Clamp01((type - 405) / 1335.0) * 0.4;
    }

    /// <summary>
    /// Wie viel Säure die Führung bildet (1 = reif nach Faustwert). Unreif weniger, länger mehr.
    /// Kurze warme Führungen (ab 32 °C, bis 5 h) bilden wenig Säure.
    /// </summary>
    public static double ProcessAcidFactor(IReadOnlyList<SourStage> stages, double starterPercent)
    {
        var total = stages.Sum(s => Math.Max(0, s.SharePercent));
        if (total <= 0) return 1;
        double f = 0, previousShare = 0;
        for (var i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            var share = Math.Max(0, s.SharePercent);
            var w = share / total;
            // in Folgestufen wirkt die vorige Stufe wie Anstellgut
            var asg = i == 0 ? starterPercent : (share > 0 ? previousShare / share * 100 : 100);
            double fi;
            if (s.TemperatureC >= 32 && s.Hours <= 5)
            {
                fi = 0.7;
            }
            else
            {
                var est = EstimateHours(asg, s.TemperatureC);
                var ratio = est is double e && e > 0 ? s.Hours / e : 1;
                fi = Math.Clamp(0.75 + 0.25 * ratio, 0.7, 1.25);
            }
            f += w * fi;
            previousShare += share;
        }
        return f;
    }

    /// <summary>Geschmacksprofil eines Rezeptentwurfs aus beiden Sauerteigen.</summary>
    public static FlavorProfile Profile(RecipeDesign d)
    {
        var parts = new List<(string Name, double Acid, double Character)>();
        if (d.UseSourdough && d.AcidificationPercent > 0)
        {
            var flour = d.ResolveSourdoughFlour();
            var stages = d.EffectiveSourStages();
            var acid = d.AcidificationPercent / 100
                       * (flour != null ? GrainFactor(flour.Grain) * AshFactor(flour) : 1)
                       * ProcessAcidFactor(stages, d.StarterPercent);
            var name = stages.Count > 1 ? "Sauerteig (mehrstufig)" : (string.IsNullOrWhiteSpace(stages[0].Name) ? "Sauerteig" : stages[0].Name);
            if (flour != null) name += $" – {Names.Display(flour)}";
            parts.Add((name, acid, FlavorScore(stages, d.StarterPercent)));
        }
        if (d.UseSourdough && d.Second.Use && d.Second.AcidificationPercent > 0)
        {
            var flour = d.ResolveSecondFlour();
            var stages = new List<SourStage> { d.Second.Stage };
            var acid = d.Second.AcidificationPercent / 100
                       * (flour != null ? GrainFactor(flour.Grain) * AshFactor(flour) : 1)
                       * ProcessAcidFactor(stages, d.Second.StarterPercent);
            var name = string.IsNullOrWhiteSpace(d.Second.Stage.Name) ? "Sauerteig 2" : d.Second.Stage.Name;
            if (flour != null) name += $" – {Names.Display(flour)}";
            parts.Add((name, acid, FlavorScore(stages, d.Second.StarterPercent)));
        }

        var profile = new FlavorProfile();
        var totalAcid = parts.Sum(p => p.Acid);
        if (totalAcid <= 0) return profile;

        profile.HasSourdough = true;
        profile.Acidity = Clamp01(totalAcid / AcidReference);
        profile.Character = Clamp01(parts.Sum(p => p.Acid * p.Character) / totalAcid);
        foreach (var p in parts)
            profile.Contributions.Add(new FlavorContribution(p.Name, p.Acid / totalAcid * 100, p.Character));
        return profile;
    }

    /// <summary>Säure-Index, der als „voll“ gilt: ca. ein Roggenvollkornbrot mit 40–45 % Versäuerung.</summary>
    public const double AcidReference = 0.5;

    public static string AcidityText(double a) => a switch
    {
        < 0.20 => "kaum säuerlich",
        < 0.40 => "leicht säuerlich",
        < 0.60 => "deutlich säuerlich",
        < 0.85 => "kräftig säuerlich",
        _ => "sehr sauer",
    };

    public static string CharacterText(double c) => c switch
    {
        < 0.25 => "sehr mild (milchsauer)",
        < 0.45 => "mild",
        < 0.62 => "ausgewogen",
        < 0.80 => "herb (mehr Essigsäure)",
        _ => "scharf-säuerlich (essigbetont)",
    };

    private static double Clamp01(double v) => Math.Clamp(v, 0, 1);

    /// <summary>Zahl für CSS (immer mit Punkt).</summary>
    public static string Css(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}

/// <summary>Anteil eines Sauerteigs an der Gesamtsäure.</summary>
public readonly record struct FlavorContribution(string Name, double AcidSharePercent, double Character);

/// <summary>Erwartetes Geschmacksprofil: wie sauer (Säuremenge) und wie die Säure schmeckt (Charakter).</summary>
public class FlavorProfile
{
    public bool HasSourdough { get; set; }

    /// <summary>0 = kaum säuerlich … 1 = sehr sauer.</summary>
    public double Acidity { get; set; }

    /// <summary>0 = mild-milchsauer … 1 = scharf-essigsauer.</summary>
    public double Character { get; set; } = 0.5;

    public List<FlavorContribution> Contributions { get; } = new();

    public string AcidityText => HasSourdough ? SourdoughGuide.AcidityText(Acidity) : "nicht säuerlich";
    public string CharacterText => HasSourdough ? SourdoughGuide.CharacterText(Character) : "–";
    public string Summary => HasSourdough ? $"{AcidityText}, {CharacterText}" : "ohne Sauerteig – nicht säuerlich";
}
