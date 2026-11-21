using System.Globalization;
using System.Text;

namespace LeckerBroot.Core;

/// <summary>Arbeitsschritt im Ablauf nach dem Ansetzen des Hauptteigs.</summary>
public enum ProcessKind { Autolyse, Kneten, Stockgare, Formen, Stueckgare, Auskuehlen, Sonstiges }

/// <summary>Ein Schritt im Ablauf (Hauptteig bis Backen, danach Auskühlen).</summary>
public class ProcessStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public ProcessKind Kind { get; set; } = ProcessKind.Sonstiges;
    public double Minutes { get; set; }
    public string Note { get; set; } = "";
}

/// <summary>Eine Phase im Backprofil.</summary>
public class BakePhase
{
    public string Name { get; set; } = "";
    public double TemperatureC { get; set; } = 230;
    public double Minutes { get; set; } = 10;
    public bool Steam { get; set; }
    public string Note { get; set; } = "";
}

/// <summary>Temperaturen für die Berechnung der Schüttwassertemperatur.</summary>
public class TemperaturePlan
{
    public double TargetDoughC { get; set; } = 27;
    public double FlourC { get; set; } = 21;
    public double PreDoughC { get; set; } = 21;
    public double KneadWarmingC { get; set; } = 2;
}

/// <summary>Art eines Eintrags im Backtag-Plan.</summary>
public enum ScheduleKind { Vorstufe, Hauptteig, Ofen, Backen, Fertig }

/// <summary>Ein Eintrag im Backtag-Plan.</summary>
public class ScheduleItem
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public ScheduleKind Kind { get; set; }
    public bool HasDuration => End > Start;
}

public class Schedule
{
    public List<ScheduleItem> Items { get; } = new();
    public List<string> Warnings { get; } = new();
    public DateTime Begin => Items.Count > 0 ? Items.Min(i => i.Start) : DateTime.MinValue;
}

/// <summary>Richtwerte und Berechnungen für Temperaturen, Ablauf, Backen und den Backtag-Plan.</summary>
public static class Planning
{
    // ---------- Temperaturen ----------

    /// <summary>Zielteigtemperatur: Weizen 24–26 °C, Roggen/Roggenmisch 27–30 °C.</summary>
    public static double DefaultTargetDoughTemp(BreadType bt) => bt switch
    {
        BreadType.Roggenbrot or BreadType.Roggenmischbrot => 28,
        BreadType.Mischbrot => 27,
        BreadType.Weizenmischbrot => 26,
        _ => 25,
    };

    /// <summary>Erwärmung durch die Knetmaschine: Roggen wird nur gemischt, Weizen ausgeknetet.</summary>
    public static double DefaultKneadWarming(BreadType bt) => bt switch
    {
        BreadType.Roggenbrot or BreadType.Roggenmischbrot => 1,
        BreadType.Mischbrot => 2,
        _ => 4,
    };

    /// <summary>
    /// Schüttwassertemperatur nach Faustregel:
    /// ohne Vorteig 2 × Teigtemp. − Mehltemp., mit Vor-/Sauerteig 3 × Teigtemp. − Mehltemp. − Vorteigtemp.,
    /// jeweils abzüglich der Knet-Erwärmung.
    /// </summary>
    public static double WaterTemperature(TemperaturePlan t, bool hasPreDough)
    {
        var w = hasPreDough
            ? 3 * t.TargetDoughC - t.FlourC - t.PreDoughC - t.KneadWarmingC
            : 2 * t.TargetDoughC - t.FlourC - t.KneadWarmingC;
        return Math.Round(w, 0);
    }

    public static string? WaterTemperatureWarning(double water) => water switch
    {
        > 45 => "Über ca. 40–45 °C leiden Hefen und Sauerteigbakterien. Lieber Mehl und Vorstufen wärmer stellen oder die Teigtemperatur senken.",
        < 2 => "Unter 2 °C geht nicht – Eiswasser nehmen oder Mehl kühler lagern.",
        _ => null,
    };

    // ---------- Ablauf ----------

    public static string Name(ProcessKind k) => k switch
    {
        ProcessKind.Autolyse => "Autolyse",
        ProcessKind.Kneten => "Kneten",
        ProcessKind.Stockgare => "Stockgare",
        ProcessKind.Formen => "Formen",
        ProcessKind.Stueckgare => "Stückgare",
        ProcessKind.Auskuehlen => "Auskühlen",
        _ => "Sonstiges",
    };

    private static bool IsRye(BreadType bt) => bt is BreadType.Roggenbrot or BreadType.Roggenmischbrot;

    /// <summary>Standardablauf nach Brotart (Richtwerte aus der Planung).</summary>
    public static List<ProcessStep> DefaultProcess(BreadType bt, bool autolyse, bool yeastOnlyOrStrong = false)
    {
        var rye = IsRye(bt);
        var list = new List<ProcessStep>();
        if (autolyse && !rye)
            list.Add(new ProcessStep { Name = "Autolyse", Kind = ProcessKind.Autolyse, Minutes = 45, Note = "Mehl + Wasser des Hauptteigs verquellen lassen" });
        list.Add(rye
            ? new ProcessStep { Name = "Mischen", Kind = ProcessKind.Kneten, Minutes = 8, Note = "5–8 min langsam mischen, nicht auskneten" }
            : new ProcessStep { Name = "Kneten", Kind = ProcessKind.Kneten, Minutes = 12, Note = "langsam mischen, dann schnell bis zum Glutenfenster" });
        list.Add(rye
            ? new ProcessStep { Name = "Teigruhe", Kind = ProcessKind.Stockgare, Minutes = 30, Note = "20–45 min" }
            : new ProcessStep { Name = "Stockgare", Kind = ProcessKind.Stockgare, Minutes = yeastOnlyOrStrong ? 60 : 120, Note = "mit 2–3× Dehnen & Falten" });
        list.Add(new ProcessStep { Name = "Formen", Kind = ProcessKind.Formen, Minutes = 10, Note = "rund- oder langwirken, ins Gärkörbchen" });
        list.Add(rye
            ? new ProcessStep { Name = "Stückgare", Kind = ProcessKind.Stueckgare, Minutes = 60, Note = "45–75 min, bis sich Risse zeigen" }
            : new ProcessStep { Name = "Stückgare", Kind = ProcessKind.Stueckgare, Minutes = 75, Note = "45–90 min, Fingerprobe" });
        list.Add(rye
            ? new ProcessStep { Name = "Auskühlen", Kind = ProcessKind.Auskuehlen, Minutes = 12 * 60, Note = "Roggen erst nach 12–24 h anschneiden" }
            : new ProcessStep { Name = "Auskühlen", Kind = ProcessKind.Auskuehlen, Minutes = 60, Note = "auf dem Gitter" });
        return list;
    }

    // ---------- Backen ----------

    /// <summary>Kerntemperatur: Weizen/Dinkel ca. 96 °C, Roggen ca. 98 °C.</summary>
    public static double CoreTemperature(BreadType bt) => IsRye(bt) || bt == BreadType.Mischbrot ? 98 : 96;

    // Stückgewicht (g) → Backzeit (min) für Weizen-/Mischbrote; Roggen ca. 15 % länger.
    private static readonly (double Weight, double Minutes)[] BakeTimes =
    {
        (60, 18), (100, 22), (500, 40), (750, 50), (1000, 55), (1500, 70), (2000, 85),
    };

    /// <summary>Faustwert für die Gesamtbackzeit eines Brots.</summary>
    public static double BakeMinutes(double pieceWeight, BreadType bt, string form = "")
    {
        var w = Math.Clamp(pieceWeight, BakeTimes[0].Weight, BakeTimes[^1].Weight);
        double m = BakeTimes[^1].Minutes;
        for (var i = 0; i < BakeTimes.Length - 1; i++)
        {
            var (w1, m1) = BakeTimes[i];
            var (w2, m2) = BakeTimes[i + 1];
            if (w >= w1 && w <= w2)
            {
                m = m1 + (m2 - m1) * (w - w1) / (w2 - w1);
                break;
            }
        }
        if (IsRye(bt)) m *= 1.15;
        if (form.Contains("kasten", StringComparison.OrdinalIgnoreCase)) m *= 1.1;
        return Math.Round(m / 5) * 5;
    }

    /// <summary>Standard-Backprofil: heiß mit Dampf anbacken, Dampf ablassen, fallend ausbacken.</summary>
    public static List<BakePhase> DefaultBakeProfile(double pieceWeight, BreadType bt, string form = "")
    {
        var total = BakeMinutes(pieceWeight, bt, form);
        var pot = form.Contains("topf", StringComparison.OrdinalIgnoreCase);
        var small = pieceWeight < 200;
        if (pot)
        {
            var lid = Math.Min(25, Math.Round(total * 0.45));
            return new List<BakePhase>
            {
                new() { Name = "Mit Deckel", TemperatureC = 250, Minutes = lid, Steam = true, Note = "Topf mit vorheizen" },
                new() { Name = "Ohne Deckel", TemperatureC = 220, Minutes = total - lid, Note = "bis zur gewünschten Bräune" },
            };
        }
        var start = small ? 8 : 12;
        return new List<BakePhase>
        {
            new() { Name = "Anbacken", TemperatureC = small ? 240 : 250, Minutes = start, Steam = true, Note = "einschießen, sofort schwaden" },
            new() { Name = "Dampf ablassen", TemperatureC = small ? 220 : 210, Minutes = 0, Note = "Tür kurz öffnen" },
            new() { Name = "Ausbacken", TemperatureC = small ? 220 : 210, Minutes = Math.Max(5, total - start), Note = "fallend, bis Kerntemperatur erreicht" },
        };
    }

    public static double PreheatMinutes(string form) =>
        form.Contains("topf", StringComparison.OrdinalIgnoreCase) ? 45 : 45;

    // ---------- Backtag-Plan (Rückwärtsplanung) ----------

    /// <summary>
    /// Rechnet vom Zeitpunkt „Brot aus dem Ofen“ rückwärts: Backen, Ablauf des Hauptteigs, Vorstufen
    /// (Ketten mehrstufiger Sauerteige nacheinander, sonst parallel), Ofen einschalten.
    /// </summary>
    public static Schedule BuildSchedule(Recipe recipe, DateTime outOfOven, double factor, TemperaturePlan? temps = null)
    {
        var s = new Schedule();
        var bakeMinutes = recipe.BakeProfile.Sum(x => Math.Max(0, x.Minutes));
        if (bakeMinutes <= 0)
        {
            bakeMinutes = BakeMinutes(recipe.PieceWeight, BreadType.Unbekannt, recipe.Form);
            s.Warnings.Add($"Kein Backprofil – mit ca. {Fmt.N(bakeMinutes)} min Backzeit gerechnet.");
        }

        var bakeStart = outOfOven.AddMinutes(-bakeMinutes);
        var bakeDetail = recipe.BakeProfile.Count > 0
            ? string.Join(" · ", recipe.BakeProfile.Where(x => x.Minutes > 0 || x.Name.Length > 0)
                .Select(x => x.Minutes > 0 ? $"{x.Name} {Fmt.N(x.TemperatureC)} °C {Fmt.N(x.Minutes)} min{(x.Steam ? " mit Dampf" : "")}" : x.Name))
            : "";
        s.Items.Add(new ScheduleItem { Start = bakeStart, End = outOfOven, Title = "Backen", Detail = bakeDetail, Kind = ScheduleKind.Backen });

        var preheatStart = bakeStart.AddMinutes(-Math.Max(0, recipe.PreheatMinutes));
        s.Items.Add(new ScheduleItem
        {
            Start = preheatStart, End = preheatStart,
            Title = $"Ofen einschalten ({Fmt.N(recipe.PreheatTemperatureC)} °C)",
            Detail = $"{Fmt.N(recipe.PreheatMinutes)} min vorheizen, Backstein/Topf mit aufheizen",
            Kind = ScheduleKind.Ofen,
        });

        // Ablauf vor dem Backen rückwärts
        var before = recipe.Process.Where(x => x.Kind != ProcessKind.Auskuehlen).ToList();
        var hasPre = recipe.Stages.Any(x => x.Type != StageType.Hauptteig);
        var t = bakeStart;
        ScheduleItem? firstMainItem = null;
        for (var idx = before.Count - 1; idx >= 0; idx--)
        {
            var step = before[idx];
            var start = t.AddMinutes(-Math.Max(0, step.Minutes));
            var detail = step.Note;
            if (step.Kind == ProcessKind.Kneten && temps != null)
            {
                detail = $"Schüttwasser ca. {Fmt.N(WaterTemperature(temps, hasPre))} °C, Teigtemperatur {Fmt.N(temps.TargetDoughC)} °C" +
                         (string.IsNullOrWhiteSpace(step.Note) ? "" : " · " + step.Note);
            }
            var item = new ScheduleItem { Start = start, End = t, Title = step.Name, Detail = detail, Kind = ScheduleKind.Hauptteig };
            s.Items.Add(item);
            firstMainItem = item;   // am Ende der Schleife: der erste Schritt des Ablaufs
            t = start;
        }
        var mainStart = t;
        var main = recipe.Stages.FirstOrDefault(x => x.Type == StageType.Hauptteig);
        if (main != null && firstMainItem != null)
            firstMainItem.Detail = "Hauptteig: " + Ingredients(main, factor) +
                                   (string.IsNullOrWhiteSpace(firstMainItem.Detail) ? "" : " · " + firstMainItem.Detail);

        // Vorstufen: alle müssen zum Hauptteig reif sein. Folgestufen (FollowsPrevious) bauen auf der
        // vorigen Stufe auf – die vorige Stufe endet dann, wenn die Folgestufe beginnt.
        var pre = recipe.Stages.Where(x => x.Type != StageType.Hauptteig).ToList();
        DateTime? nextStart = null;
        for (var idx = pre.Count - 1; idx >= 0; idx--)
        {
            var stage = pre[idx];
            var feedsNext = idx < pre.Count - 1 && pre[idx + 1].FollowsPrevious;
            var end = feedsNext && nextStart.HasValue ? nextStart.Value : mainStart;
            var hours = stage.PlanHours;
            if (hours <= 0)
                s.Warnings.Add($"Für „{stage.Name}“ ist keine Standzeit hinterlegt – bitte im Rezept ergänzen.");
            var start = RoundDown5(end.AddHours(-Math.Max(0, hours)));
            s.Items.Add(new ScheduleItem
            {
                Start = start, End = end,
                Title = $"{stage.Name} ansetzen",
                Detail = Ingredients(stage, factor) + (string.IsNullOrWhiteSpace(stage.Temperature) ? "" : $" · {stage.Temperature}"),
                Kind = ScheduleKind.Vorstufe,
            });
            nextStart = start;
        }

        // Auskühlen nach dem Backen
        foreach (var cool in recipe.Process.Where(x => x.Kind == ProcessKind.Auskuehlen))
            s.Items.Add(new ScheduleItem
            {
                Start = outOfOven, End = outOfOven.AddMinutes(Math.Max(0, cool.Minutes)),
                Title = "Fertig gebacken – " + cool.Name, Detail = cool.Note, Kind = ScheduleKind.Fertig,
            });
        if (!recipe.Process.Any(x => x.Kind == ProcessKind.Auskuehlen))
            s.Items.Add(new ScheduleItem { Start = outOfOven, End = outOfOven, Title = "Fertig gebacken", Kind = ScheduleKind.Fertig });

        if (recipe.Process.Count == 0)
            s.Warnings.Add("Kein Ablauf hinterlegt – nur Vorstufen und Backen geplant.");

        s.Items.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Kind.CompareTo(b.Kind));
        return s;
    }

    /// <summary>Auf volle 5 Minuten abrunden (Vorstufen dürfen etwas länger stehen).</summary>
    private static DateTime RoundDown5(DateTime d) =>
        new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute - d.Minute % 5, 0, d.Kind);

    private static string Ingredients(Stage st, double factor) =>
        string.Join(", ", st.Ingredients.Select(i => $"{Fmt.G(i.Grams * factor)} {Names.Display(i.Ingredient)}"));

    // ---------- Anzeige ----------

    private static readonly string[] Days = { "So", "Mo", "Di", "Mi", "Do", "Fr", "Sa" };

    public static string Day(DateTime d) => $"{Days[(int)d.DayOfWeek]} {d.ToString("dd.MM.", CultureInfo.InvariantCulture)}";
    public static string Time(DateTime d) => d.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Duration(double minutes)
    {
        if (minutes <= 0) return "";
        if (minutes < 60) return $"{Fmt.N(minutes)} min";
        var h = Math.Floor(minutes / 60);
        var m = Math.Round(minutes - h * 60);
        return m > 0 ? $"{h} h {m} min" : $"{h} h";
    }

    // ---------- Kalender (ICS) ----------

    /// <summary>Kalenderdatei mit einem Termin je Schritt und Erinnerung zum Start.</summary>
    public static string ToIcs(Schedule schedule, string recipeName)
    {
        var sb = new StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//LeckerBroot//Backtag//DE\r\nCALSCALE:GREGORIAN\r\n");
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        foreach (var item in schedule.Items)
        {
            var end = item.HasDuration ? item.End : item.Start.AddMinutes(5);
            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append($"UID:{Guid.NewGuid()}@leckerbroot\r\n");
            sb.Append($"DTSTAMP:{stamp}\r\n");
            sb.Append($"DTSTART:{Local(item.Start)}\r\n");
            sb.Append($"DTEND:{Local(end)}\r\n");
            sb.Append($"SUMMARY:{Escape("🍞 " + item.Title)}\r\n");
            sb.Append($"DESCRIPTION:{Escape(recipeName + (string.IsNullOrWhiteSpace(item.Detail) ? "" : "\n" + item.Detail))}\r\n");
            sb.Append("BEGIN:VALARM\r\nACTION:DISPLAY\r\nTRIGGER:PT0M\r\n");
            sb.Append($"DESCRIPTION:{Escape(item.Title)}\r\n");
            sb.Append("END:VALARM\r\nEND:VEVENT\r\n");
        }
        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    private static string Local(DateTime d) => d.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
}
