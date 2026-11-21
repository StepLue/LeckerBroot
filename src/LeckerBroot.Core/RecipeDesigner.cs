namespace LeckerBroot.Core;

/// <summary>Ein Mehl der Mehlmischung mit seinem Anteil in %.</summary>
public class FlourPart
{
    public Ingredient Flour { get; set; } = new();
    public double Percent { get; set; }
}

/// <summary>Eine weitere Zutat mit Bäckerprozent.</summary>
public class ExtraPart
{
    public Ingredient Ingredient { get; set; } = new();
    public double Percent { get; set; }
}

/// <summary>Optionaler zweiter Sauerteig, z. B. ein Weizensauerteig neben dem Roggensauerteig.</summary>
public class SecondSourdough
{
    public bool Use { get; set; }
    public Ingredient? Flour { get; set; }

    /// <summary>Versäuerung dieses Sauerteigs in % vom Gesamtmehl.</summary>
    public double AcidificationPercent { get; set; } = 15;

    /// <summary>Anstellgut in % vom Mehl dieses Sauerteigs.</summary>
    public double StarterPercent { get; set; } = 10;

    /// <summary>Anstellgut (null = gleiches wie beim ersten Sauerteig).</summary>
    public Ingredient? Starter { get; set; }

    public SourStage Stage { get; set; } = new()
    {
        Name = "Weizensauerteig", SharePercent = 100, Ta = 200, TemperatureC = 26, Hours = 10, HoursNote = "8–12 h",
    };
}

/// <summary>Alle Eingaben des Rezept-Assistenten (Schritte 1–6). Wird im Rezept mitgespeichert.</summary>
public class RecipeDesign
{
    // Schritt 1 – Ziel
    public string Name { get; set; } = "Neues Brot";
    public Leavening Leavening { get; set; } = Leavening.Roggensauerteig;
    public string Form { get; set; } = "freigeschoben";
    public int Count { get; set; } = 1;
    public double PieceWeight { get; set; } = 1000;
    public double BakeLossPercent { get; set; } = 12;

    // Schritt 2 – Mehlmischung
    public List<FlourPart> Flours { get; set; } = new();

    // Schritt 3 – Teigausbeute
    public double Ta { get; set; } = 170;

    // Schritt 4 – Sauerteig
    public bool UseSourdough { get; set; } = true;
    public Ingredient? SourdoughFlour { get; set; }
    /// <summary>Versäuerung in % vom Gesamtmehl.</summary>
    public double AcidificationPercent { get; set; } = 35;
    public double SourdoughTa { get; set; } = 200;
    /// <summary>Anstellgut in % vom Sauerteigmehl.</summary>
    public double StarterPercent { get; set; } = 10;
    public string SourdoughDuration { get; set; } = "12–16 h";
    public string SourdoughTemperature { get; set; } = "24–26 °C";

    /// <summary>Gewählte Führungsvorlage.</summary>
    public SourdoughMethod SourdoughMethod { get; set; } = SourdoughMethod.Einstufig;

    /// <summary>Sauerteigstufen. Leer bei älteren Rezepten – dann gelten SourdoughTa/-Duration/-Temperature.</summary>
    public List<SourStage> SourStages { get; set; } = new();

    // Schritt 7–9 – Temperaturen, Ablauf, Backen
    public TemperaturePlan Temperatures { get; set; } = new();
    public List<ProcessStep> Process { get; set; } = new();
    public List<BakePhase> BakeProfile { get; set; } = new();
    public double PreheatMinutes { get; set; } = 45;
    public double PreheatTemperatureC { get; set; } = 250;
    public double CoreTemperatureC { get; set; } = 96;

    /// <summary>Optionaler zweiter Sauerteig.</summary>
    public SecondSourdough Second { get; set; } = new();

    /// <summary>Geschmacksregler 1 (sehr mild) bis 5 (sehr kräftig).</summary>
    public int FlavorLevel { get; set; } = 3;

    /// <summary>Mehl des ersten Sauerteigs (gewählt oder erstes Roggenmehl der Mischung).</summary>
    public Ingredient? ResolveSourdoughFlour()
    {
        var mix = Flours.Where(f => f.Percent > 0).ToList();
        return SourdoughFlour ?? mix.FirstOrDefault(f => f.Flour.Grain == Grain.Roggen)?.Flour ?? mix.FirstOrDefault()?.Flour;
    }

    /// <summary>Mehl des zweiten Sauerteigs (gewählt oder erstes Weizen-/Dinkelmehl der Mischung).</summary>
    public Ingredient? ResolveSecondFlour()
    {
        var mix = Flours.Where(f => f.Percent > 0).ToList();
        return Second.Flour ?? mix.FirstOrDefault(f => f.Flour.Grain is Grain.Weizen or Grain.Dinkel)?.Flour ?? mix.FirstOrDefault()?.Flour;
    }

    /// <summary>Die tatsächlich verwendeten Sauerteigstufen (inkl. Umwandlung älterer Rezepte).</summary>
    public List<SourStage> EffectiveSourStages() => SourStages.Count > 0
        ? SourStages
        : new List<SourStage>
        {
            new SourStage
            {
                Name = "Sauerteig", SharePercent = 100, Ta = SourdoughTa, TemperatureC = 25,
                TemperatureNote = "", Hours = 14, HoursNote = SourdoughDuration,
            },
        };

    // Schritt 5 – Vorstufen
    public bool UsePoolish { get; set; }
    public Ingredient? PoolishFlour { get; set; }
    /// <summary>Poolish-Mehl in % vom Gesamtmehl.</summary>
    public double PoolishPercent { get; set; } = 20;
    /// <summary>Hefe in % vom Poolish-Mehl.</summary>
    public double PoolishYeastPercent { get; set; } = 0.2;

    public bool UseScald { get; set; }
    public Ingredient? ScaldFlour { get; set; }
    /// <summary>Brühstück-Mehl/Schrot in % vom Gesamtmehl.</summary>
    public double ScaldPercent { get; set; } = 10;
    /// <summary>Wasser : Mehl im Brühstück.</summary>
    public double ScaldWaterRatio { get; set; } = 2;

    public bool UseSoaker { get; set; }
    public Ingredient? SoakerSeeds { get; set; }
    /// <summary>Saaten in % vom Gesamtmehl.</summary>
    public double SoakerPercent { get; set; } = 15;
    /// <summary>Wasser : Saaten im Quellstück (zählt nicht zur TA).</summary>
    public double SoakerWaterRatio { get; set; } = 1;

    public bool UseAutolyse { get; set; }

    // Schritt 6 – Salz, Hefe, Zusätze
    public double SaltPercent { get; set; } = 2;
    public double YeastPercent { get; set; }
    public List<ExtraPart> Extras { get; set; } = new();

    // Grundzutaten (Kopien aus der Zutatendatenbank)
    public Ingredient Water { get; set; } = SeedData.Water();
    public Ingredient Salt { get; set; } = SeedData.Salt();
    public Ingredient Yeast { get; set; } = SeedData.FreshYeast();
    public Ingredient Starter { get; set; } = SeedData.RyeStarter();

    /// <summary>Brotart der Mehlmischung.</summary>
    public BreadType BreadType
    {
        get
        {
            var sum = Flours.Sum(f => Math.Max(0, f.Percent));
            if (sum <= 0) return BreadType.Unbekannt;
            var rye = Flours.Where(f => f.Flour.Grain == Grain.Roggen).Sum(f => Math.Max(0, f.Percent));
            return Guidelines.Classify(rye / sum, sum);
        }
    }
}

/// <summary>Ergebnis des Assistenten.</summary>
public class DesignResult
{
    public List<Stage> Stages { get; } = new();
    public List<string> Warnings { get; } = new();
    public double TotalFlour { get; set; }
    public double TargetDough { get; set; }
}

/// <summary>Rechnet die Eingaben des Assistenten in Grammangaben je Stufe um.</summary>
public static class RecipeDesigner
{
    public static DesignResult Build(RecipeDesign d, AppSettings settings)
    {
        var r = new DesignResult();
        var include = settings.IncludeStarterInTotals;

        // --- Mehlmischung prüfen ---
        var flours = d.Flours.Where(f => f.Percent > 0).ToList();
        var flourSum = flours.Sum(f => f.Percent);
        if (flours.Count == 0 || flourSum <= 0)
        {
            r.Warnings.Add("Bitte in Schritt 2 mindestens ein Mehl mit Anteil eintragen.");
            return r;
        }
        if (Math.Abs(flourSum - 100) > 0.5)
            r.Warnings.Add($"Die Mehlmischung ergibt {Fmt.N(flourSum)} % statt 100 % – es wird anteilig umgerechnet.");

        // --- Gesamtmehl aus dem Zielgewicht ---
        // Teig = Mehl × k; k enthält TA, alle Zusätze in Bäcker-% und die Teile, die nicht in der TA stecken.
        var target = RecipeCalculator.DoughWeight(d.Count, d.PieceWeight, d.BakeLossPercent);
        r.TargetDough = target;
        if (target <= 0)
        {
            r.Warnings.Add("Bitte in Schritt 1 Anzahl und Stückgewicht angeben.");
            return r;
        }
        if (d.Ta < 100)
            r.Warnings.Add("Die TA muss mindestens 100 sein (100 = ohne Wasser).");

        var v = d.UseSourdough ? Math.Max(0, d.AcidificationPercent) / 100 : 0;
        var asg = Math.Max(0, d.StarterPercent) / 100;

        var k = d.Ta / 100
              + (Math.Max(0, d.SaltPercent) + Math.Max(0, d.YeastPercent) + d.Extras.Sum(e => Math.Max(0, e.Percent))) / 100;
        if (d.UseSourdough && !include) k += v * asg;
        var v2 = d.UseSourdough && d.Second.Use ? Math.Max(0, d.Second.AcidificationPercent) / 100 : 0;
        var asg2 = Math.Max(0, d.Second.StarterPercent) / 100;
        if (v2 > 0 && !include) k += v2 * asg2;
        if (d.UsePoolish) k += d.PoolishPercent / 100 * d.PoolishYeastPercent / 100;
        if (d.UseSoaker) k += d.SoakerPercent / 100 * (1 + d.SoakerWaterRatio);

        var F = target / k;
        r.TotalFlour = F;
        var waterTotal = F * (d.Ta - 100) / 100;
        var waterUsed = 0.0;

        // Bereits in Vorstufen verbrauchtes Mehl je Mehlsorte
        var flourUsed = new Dictionary<Guid, double>();
        void UseFlour(Ingredient flour, double grams)
        {
            flourUsed.TryGetValue(flour.Id, out var g);
            flourUsed[flour.Id] = g + grams;
        }
        bool InMix(Ingredient? flour) => flour != null && flours.Any(f => f.Flour.Id == flour.Id);

        // --- Schritt 4: Sauerteig ---
        if (d.UseSourdough && v > 0)
        {
            var sourFlourIng = d.ResolveSourdoughFlour() ?? flours[0].Flour;
            if (!InMix(sourFlourIng))
                r.Warnings.Add($"Das Sauerteigmehl „{sourFlourIng.Name}“ ist nicht in der Mehlmischung – bitte in Schritt 2 aufnehmen.");

            var sf = F * v;                                 // Sauerteigmehl gesamt
            var starter = sf * asg;                         // Anstellgut (kommt in die erste Stufe)
            var sourStages = d.EffectiveSourStages();
            var shareSum = sourStages.Sum(x => Math.Max(0, x.SharePercent));
            if (shareSum <= 0)
            {
                r.Warnings.Add("Die Sauerteigstufen haben keinen Mehlanteil.");
                shareSum = 100;
            }
            else if (Math.Abs(shareSum - 100) > 0.5)
            {
                r.Warnings.Add($"Die Anteile der Sauerteigstufen ergeben {Fmt.N(shareSum)} % statt 100 % – es wird anteilig umgerechnet.");
            }

            UseFlour(sourFlourIng, sf);
            string? previous = null;
            for (var i = 0; i < sourStages.Count; i++)
            {
                var s = sourStages[i];
                var stageFlour = sf * Math.Max(0, s.SharePercent) / shareSum;
                var stageWater = stageFlour * (s.Ta - 100) / 100;
                waterUsed += stageWater;
                var addFlour = stageFlour;
                var addWater = stageWater;
                if (i == 0 && include)
                {
                    addFlour -= starter * d.Starter.FlourFraction;
                    addWater -= starter * d.Starter.WaterFraction;
                    if (addFlour < -0.5 || addWater < -0.5)
                        r.Warnings.Add($"Das Anstellgut ist größer als die Stufe „{s.Name}“ – Anstellgut-% senken oder Anteil der Stufe erhöhen.");
                }

                var text = previous == null
                    ? $"Alle Zutaten verrühren (TA {Fmt.N(s.Ta)}), abgedeckt reifen lassen."
                    : $"Den kompletten {previous} mit Mehl und Wasser verrühren (TA {Fmt.N(s.Ta)}), abgedeckt reifen lassen.";

                var st = new Stage
                {
                    Name = string.IsNullOrWhiteSpace(s.Name) ? $"Sauerteig {i + 1}" : s.Name,
                    Type = StageType.Sauerteig,
                    Duration = s.DurationText,
                    Temperature = s.TemperatureText,
                    Instructions = text,
                    PlanHours = s.Hours,
                    FollowsPrevious = i > 0,
                };
                st.Ingredients.Add(new StageIngredient { Ingredient = sourFlourIng.Clone(), Grams = addFlour });
                st.Ingredients.Add(new StageIngredient { Ingredient = d.Water.Clone(), Grams = addWater });
                if (i == 0)
                    st.Ingredients.Add(new StageIngredient { Ingredient = d.Starter.Clone(), Grams = starter });
                r.Stages.Add(st);
                previous = st.Name;
            }
        }

        // --- Schritt 4b: zweiter Sauerteig ---
        if (v2 > 0)
        {
            var sec = d.Second;
            var flour2 = d.ResolveSecondFlour() ?? flours[0].Flour;
            if (!InMix(flour2))
                r.Warnings.Add($"Das Mehl des zweiten Sauerteigs „{flour2.Name}“ ist nicht in der Mehlmischung – bitte in Schritt 2 aufnehmen.");
            var starter2Ing = sec.Starter ?? d.Starter;
            var sf2 = F * v2;
            var sw2 = sf2 * (sec.Stage.Ta - 100) / 100;
            var starter2 = sf2 * asg2;
            var add2Flour = sf2;
            var add2Water = sw2;
            if (include)
            {
                add2Flour -= starter2 * starter2Ing.FlourFraction;
                add2Water -= starter2 * starter2Ing.WaterFraction;
            }
            UseFlour(flour2, sf2);
            waterUsed += sw2;

            var st2 = new Stage
            {
                Name = string.IsNullOrWhiteSpace(sec.Stage.Name) ? "Sauerteig 2" : sec.Stage.Name,
                Type = StageType.Sauerteig,
                Duration = sec.Stage.DurationText,
                Temperature = sec.Stage.TemperatureText,
                PlanHours = sec.Stage.Hours,
                Instructions = $"Alle Zutaten verrühren (TA {Fmt.N(sec.Stage.Ta)}), abgedeckt reifen lassen.",
            };
            st2.Ingredients.Add(new StageIngredient { Ingredient = flour2.Clone(), Grams = add2Flour });
            st2.Ingredients.Add(new StageIngredient { Ingredient = d.Water.Clone(), Grams = add2Water });
            st2.Ingredients.Add(new StageIngredient { Ingredient = starter2Ing.Clone(), Grams = starter2 });
            r.Stages.Add(st2);
        }

        // --- Schritt 5: Poolish ---
        if (d.UsePoolish && d.PoolishPercent > 0)
        {
            var pFlour = d.PoolishFlour ?? flours.FirstOrDefault(f => f.Flour.Grain == Grain.Weizen)?.Flour ?? flours[0].Flour;
            if (!InMix(pFlour))
                r.Warnings.Add($"Das Poolish-Mehl „{pFlour.Name}“ ist nicht in der Mehlmischung.");
            var pf = F * d.PoolishPercent / 100;
            var pw = pf; // TA 200
            var py = pf * d.PoolishYeastPercent / 100;
            UseFlour(pFlour, pf);
            waterUsed += pw;

            var st = new Stage
            {
                Name = "Poolish",
                PlanHours = 14,
                Type = StageType.Vorteig,
                Duration = "12–16 h",
                Temperature = "ca. 20 °C",
                Instructions = "Hefe im Wasser auflösen, Mehl einrühren, abgedeckt reifen lassen.",
            };
            st.Ingredients.Add(new StageIngredient { Ingredient = pFlour.Clone(), Grams = pf });
            st.Ingredients.Add(new StageIngredient { Ingredient = d.Water.Clone(), Grams = pw });
            st.Ingredients.Add(new StageIngredient { Ingredient = d.Yeast.Clone(), Grams = py });
            r.Stages.Add(st);
        }

        // --- Schritt 5: Brühstück ---
        if (d.UseScald && d.ScaldPercent > 0)
        {
            var bFlour = d.ScaldFlour ?? flours[0].Flour;
            if (!InMix(bFlour))
                r.Warnings.Add($"Das Brühstück-Mehl „{bFlour.Name}“ ist nicht in der Mehlmischung.");
            var bf = F * d.ScaldPercent / 100;
            var bw = bf * d.ScaldWaterRatio;
            UseFlour(bFlour, bf);
            waterUsed += bw;

            var st = new Stage
            {
                Name = "Brühstück",
                PlanHours = 4,
                Type = StageType.Bruehstueck,
                Duration = "mind. 4 h, besser über Nacht",
                Temperature = "abkühlen lassen",
                Instructions = "Mit kochendem Wasser übergießen, gut verrühren, abgedeckt quellen lassen.",
            };
            st.Ingredients.Add(new StageIngredient { Ingredient = bFlour.Clone(), Grams = bf });
            st.Ingredients.Add(new StageIngredient { Ingredient = d.Water.Clone(), Grams = bw });
            r.Stages.Add(st);
        }

        // --- Schritt 5: Quellstück (Wasser zählt nicht zur TA) ---
        if (d.UseSoaker && d.SoakerPercent > 0)
        {
            var seeds = d.SoakerSeeds ?? SeedData.Sunflower();
            var qs = F * d.SoakerPercent / 100;
            var qw = qs * d.SoakerWaterRatio;
            var st = new Stage
            {
                Name = "Quellstück",
                PlanHours = 10,
                Type = StageType.Quellstueck,
                WaterCountsToTa = false,
                Duration = "8–12 h",
                Temperature = "Raumtemperatur",
                Instructions = "Saaten mit kaltem Wasser übergießen und abgedeckt quellen lassen.",
            };
            st.Ingredients.Add(new StageIngredient { Ingredient = seeds.Clone(), Grams = qs });
            st.Ingredients.Add(new StageIngredient { Ingredient = d.Water.Clone(), Grams = qw });
            r.Stages.Add(st);
        }

        // --- Hauptteig ---
        var main = new Stage
        {
            Name = "Hauptteig",
            Type = StageType.Hauptteig,
            Instructions = MainInstructions(d),
        };

        foreach (var part in flours)
        {
            var need = F * part.Percent / flourSum;
            flourUsed.TryGetValue(part.Flour.Id, out var used);
            var rest = need - used;
            if (rest < -0.5)
                r.Warnings.Add($"Für „{part.Flour.Name}“ wird in den Vorstufen {Fmt.G(used)} verwendet, die Mischung sieht nur {Fmt.G(need)} vor. Anteil in Schritt 2 erhöhen oder Vorstufe verkleinern.");
            if (rest > 0.05)
                main.Ingredients.Add(new StageIngredient { Ingredient = part.Flour.Clone(), Grams = rest });
        }

        var mainWater = waterTotal - waterUsed;
        if (mainWater < -0.5)
            r.Warnings.Add($"Die Vorstufen brauchen {Fmt.G(waterUsed)} Wasser, bei TA {Fmt.N(d.Ta)} stehen nur {Fmt.G(waterTotal)} zur Verfügung. TA erhöhen oder Vorstufen verkleinern.");
        if (mainWater > 0.05)
            main.Ingredients.Add(new StageIngredient { Ingredient = d.Water.Clone(), Grams = mainWater });

        if (d.SaltPercent > 0)
            main.Ingredients.Add(new StageIngredient { Ingredient = d.Salt.Clone(), Grams = F * d.SaltPercent / 100 });
        if (d.YeastPercent > 0)
            main.Ingredients.Add(new StageIngredient { Ingredient = d.Yeast.Clone(), Grams = F * d.YeastPercent / 100 });
        foreach (var e in d.Extras.Where(e => e.Percent > 0))
            main.Ingredients.Add(new StageIngredient { Ingredient = e.Ingredient.Clone(), Grams = F * e.Percent / 100 });

        r.Stages.Add(main);
        return r;
    }

    private static TemperaturePlan Copy(TemperaturePlan t) => new()
    {
        TargetDoughC = t.TargetDoughC, FlourC = t.FlourC, PreDoughC = t.PreDoughC, KneadWarmingC = t.KneadWarmingC,
    };

    private static ProcessStep Copy(ProcessStep p) => new() { Name = p.Name, Kind = p.Kind, Minutes = p.Minutes, Note = p.Note };

    private static BakePhase Copy(BakePhase b) => new()
    {
        Name = b.Name, TemperatureC = b.TemperatureC, Minutes = b.Minutes, Steam = b.Steam, Note = b.Note,
    };

    /// <summary>Füllt Temperaturen, Ablauf und Backprofil mit Richtwerten (für neue oder ältere Entwürfe).</summary>
    public static void ApplyPlanningDefaults(RecipeDesign d, bool force = false)
    {
        var bt = d.BreadType;
        if (force || d.Process.Count == 0)
            d.Process = Planning.DefaultProcess(bt, d.UseAutolyse, d.Leavening == Leavening.Hefe);
        if (force || d.BakeProfile.Count == 0)
        {
            d.BakeProfile = Planning.DefaultBakeProfile(d.PieceWeight, bt, d.Form);
            d.CoreTemperatureC = Planning.CoreTemperature(bt);
            d.PreheatTemperatureC = d.BakeProfile.Count > 0 ? d.BakeProfile.Max(b => b.TemperatureC) : 250;
            d.PreheatMinutes = Planning.PreheatMinutes(d.Form);
        }
        if (force || d.Temperatures == null || d.Temperatures.TargetDoughC <= 0)
        {
            d.Temperatures = new TemperaturePlan
            {
                TargetDoughC = Planning.DefaultTargetDoughTemp(bt),
                KneadWarmingC = Planning.DefaultKneadWarming(bt),
            };
        }
    }

    private static string MainInstructions(RecipeDesign d)
    {
        var rye = d.BreadType is BreadType.Roggenbrot or BreadType.Roggenmischbrot;
        var parts = new List<string>();
        if (d.UseAutolyse)
            parts.Add("Mehl und Wasser des Hauptteigs 30–60 min verquellen lassen (Autolyse), dann die übrigen Zutaten zugeben.");
        parts.Add("Alle Vorstufen und Zutaten zu einem Teig verarbeiten.");
        parts.Add(rye
            ? "Roggenbetont: nur mischen (ca. 5–8 min langsam), nicht auskneten."
            : "Weizenbetont: langsam mischen, dann auskneten bis zum Glutenfenster.");
        return string.Join(" ", parts);
    }

    /// <summary>Erzeugt ein Rezept aus dem Assistenten. Bei vorhandenem Rezept werden Id, Notizen usw. übernommen.</summary>
    public static Recipe ToRecipe(RecipeDesign d, AppSettings settings, Recipe? existing = null)
    {
        var result = Build(d, settings);
        var recipe = existing ?? new Recipe();
        recipe.Name = string.IsNullOrWhiteSpace(d.Name) ? "Neues Brot" : d.Name.Trim();
        recipe.Leavening = d.Leavening;
        recipe.Form = d.Form;
        recipe.Count = Math.Max(1, d.Count);
        recipe.PieceWeight = d.PieceWeight;
        recipe.BakeLossPercent = d.BakeLossPercent;
        recipe.Category = Names.Of(d.BreadType);
        recipe.Stages = result.Stages;
        recipe.Design = d;
        recipe.Temperatures = Copy(d.Temperatures);
        recipe.Process = d.Process.Select(x => Copy(x)).ToList();
        recipe.BakeProfile = d.BakeProfile.Select(x => Copy(x)).ToList();
        recipe.PreheatMinutes = d.PreheatMinutes;
        recipe.PreheatTemperatureC = d.PreheatTemperatureC;
        recipe.CoreTemperatureC = d.CoreTemperatureC;
        recipe.Modified = DateTime.Now;
        return recipe;
    }
}
