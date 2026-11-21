namespace LeckerBroot.Core;

/// <summary>Kennzahlen eines Rezepts (für die gespeicherte Referenzmenge).</summary>
public class RecipeAnalysis
{
    public double TotalFlour { get; set; }
    public double RyeFlour { get; set; }
    public double TaWater { get; set; }
    public double NonTaWater { get; set; }
    public double TotalDough { get; set; }
    public double SourdoughFlour { get; set; }

    /// <summary>Roggenmehl, das in Sauerteigstufen steckt.</summary>
    public double SourRyeFlour { get; set; }
    public double Salt { get; set; }
    public double Yeast { get; set; }

    public double Ta => TotalFlour > 0 ? (TotalFlour + TaWater) / TotalFlour * 100 : 0;
    public double Hydration => TotalFlour > 0 ? TaWater / TotalFlour * 100 : 0;
    public double RyeShare => TotalFlour > 0 ? RyeFlour / TotalFlour : 0;
    public double AcidificationPercent => TotalFlour > 0 ? SourdoughFlour / TotalFlour * 100 : 0;

    /// <summary>Versäuerter Anteil des Roggenmehls in %.</summary>
    public double RyeAcidificationPercent => RyeFlour > 0 ? SourRyeFlour / RyeFlour * 100 : 0;
    public double SaltPercent => TotalFlour > 0 ? Salt / TotalFlour * 100 : 0;
    public double YeastPercent => TotalFlour > 0 ? Yeast / TotalFlour * 100 : 0;
    public BreadType BreadType => Guidelines.Classify(RyeShare, TotalFlour);

    public List<string> Warnings { get; } = new();

    /// <summary>Bäckerprozent einer Grammangabe (bezogen auf das Gesamtmehl).</summary>
    public double BakerPercent(double grams) => TotalFlour > 0 ? grams / TotalFlour * 100 : 0;
}

public static class RecipeCalculator
{
    /// <summary>Teiggewicht für Anzahl × Stückgewicht bei gegebenem Backverlust.</summary>
    public static double DoughWeight(int count, double pieceWeight, double bakeLossPercent)
    {
        var loss = Math.Clamp(bakeLossPercent, 0, 60) / 100.0;
        return Math.Max(0, count) * Math.Max(0, pieceWeight) / (1 - loss);
    }

    /// <summary>Gilt eine Zutat in dieser Stufe als Mehl/TA-relevant?</summary>
    public static bool Counts(Ingredient ing, AppSettings settings) =>
        ing.Kind != IngredientKind.Anstellgut || settings.IncludeStarterInTotals;

    public static RecipeAnalysis Analyze(Recipe recipe, AppSettings settings)
    {
        var a = new RecipeAnalysis();
        foreach (var stage in recipe.Stages)
        {
            foreach (var si in stage.Ingredients)
            {
                var ing = si.Ingredient;
                var g = Math.Max(0, si.Grams);
                a.TotalDough += g;
                if (ing.Kind == IngredientKind.Salz) a.Salt += g;
                if (ing.Kind == IngredientKind.Hefe) a.Yeast += g;
                if (!Counts(ing, settings)) continue;

                var flour = g * ing.FlourFraction;
                var water = g * ing.WaterFraction;
                a.TotalFlour += flour;
                if (ing.Grain == Grain.Roggen) a.RyeFlour += flour;
                if (stage.Type == StageType.Sauerteig)
                {
                    a.SourdoughFlour += flour;
                    if (ing.Grain == Grain.Roggen) a.SourRyeFlour += flour;
                }
                if (stage.WaterCountsToTa) a.TaWater += water;
                else a.NonTaWater += water;
            }
        }

        AddWarnings(a, recipe);
        return a;
    }

    private static void AddWarnings(RecipeAnalysis a, Recipe recipe)
    {
        if (a.TotalFlour <= 0)
        {
            a.Warnings.Add("Das Rezept enthält noch kein Mehl.");
            return;
        }

        if (!Guidelines.SaltPercent.Contains(a.SaltPercent))
            a.Warnings.Add($"Salz liegt bei {Fmt.P(a.SaltPercent)} – üblich sind 1,8–2,2 % vom Mehl.");

        var hasSour = a.SourdoughFlour > 0;
        if (a.RyeShare > Guidelines.RyeNeedsSourdough && !hasSour)
            a.Warnings.Add($"Roggenanteil {Fmt.P(a.RyeShare * 100)} ohne Sauerteig – Roggen braucht Säure, sonst wird die Krume klitschig.");

        var type = a.BreadType;
        if (hasSour)
        {
            var r = Guidelines.Acidification(type);
            if (!r.Contains(a.AcidificationPercent))
                a.Warnings.Add($"Versäuerung {Fmt.P(a.AcidificationPercent)} – Richtwert für {Names.Of(type)}: {Fmt.N(r.Min)}–{Fmt.N(r.Max)} % vom Gesamtmehl.");
        }

        if (hasSour && a.RyeShare > 0.05)
        {
            var minRye = Guidelines.MinRyeAcidificationPercent(a.RyeShare);
            if (a.RyeAcidificationPercent < minRye - 0.5)
                a.Warnings.Add($"Vom Roggenmehl sind nur {Fmt.P(a.RyeAcidificationPercent)} versäuert – bei {Fmt.P(a.RyeShare * 100)} Roggenanteil sollten es mind. ca. {Fmt.P(Math.Round(minRye))} sein, damit die Säure die Enzyme hemmt.");
        }

        var ta = Guidelines.Ta(type);
        if (!ta.Contains(a.Ta))
            a.Warnings.Add($"TA {Fmt.N(a.Ta)} – üblich für {Names.Of(type)}: {Fmt.N(ta.Min)}–{Fmt.N(ta.Max)}.");

        foreach (var stage in recipe.Stages)
            foreach (var si in stage.Ingredients)
                if (si.Grams < 0)
                    a.Warnings.Add($"Negative Menge bei „{si.Ingredient.Name}“ in {stage.Name}.");
    }
}

/// <summary>Zahlenformat für die Anzeige (deutsch, ohne Abhängigkeit von Kultur-Daten im Browser).</summary>
public static class Fmt
{
    private static string De(string s) => s.Replace('.', ',');

    /// <summary>Zahl mit höchstens einer Nachkommastelle.</summary>
    public static string N(double v) => De(Math.Round(v, 1).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Prozentwert.</summary>
    public static string P(double v) => N(v) + " %";

    /// <summary>Grammangabe: kleine Mengen mit einer Nachkommastelle, sonst ganze Gramm.</summary>
    public static string G(double grams)
    {
        var s = Math.Abs(grams) < 20
            ? Math.Round(grams, 1).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)
            : Math.Round(grams, 0).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        return De(s) + " g";
    }
}
