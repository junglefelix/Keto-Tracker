using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Keto_Tracker2.Services;

/// <summary>What the parser understood from one spoken phrase. Every field is optional.</summary>
public class VoiceCommand
{
    public DateTime Date { get; set; }
    public double? WeightKg { get; set; }
    public bool? KetoPass { get; set; }
    public bool? OmadPass { get; set; }
    public List<(TimeSpan Time, string Description)> Meals { get; } = new();
    public List<(TimeSpan Time, double Value)> Ketones { get; } = new();

    public bool IsEmpty =>
        WeightKg is null && KetoPass is null && OmadPass is null && Meals.Count == 0 && Ketones.Count == 0;

    /// <summary>Human readable summary shown in the confirmation dialog.</summary>
    public string Describe(DateTime today)
    {
        var sb = new StringBuilder();
        string day = Date == today ? "Сегодня"
                   : Date == today.AddDays(-1) ? "Вчера"
                   : Date == today.AddDays(-2) ? "Позавчера"
                   : Date.ToString("d MMMM", new CultureInfo("ru-RU"));
        sb.AppendLine($"{day} ({Date:dd.MM.yyyy}):");
        if (WeightKg is double w) sb.AppendLine($"• Вес: {w.ToString("0.##", CultureInfo.InvariantCulture)} кг");
        foreach (var m in Meals) sb.AppendLine($"• Еда в {m.Time:hh\\:mm}: {m.Description}");
        foreach (var k in Ketones) sb.AppendLine($"• Кетоны в {k.Time:hh\\:mm}: {k.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
        if (KetoPass is bool kp) sb.AppendLine($"• Кето: {(kp ? "выполнено ✓" : "нарушено ✗")}");
        if (OmadPass is bool op) sb.AppendLine($"• OMAD: {(op ? "выполнено ✓" : "нарушено ✗")}");
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Turns a recognised phrase into data using simple local rules (no network).
/// Russian first, some English. Examples:
///   "сегодня вешу 90", "мой вес 85,5", "вчера вес 86 и 3"
///   "в 15:00 ел печёнку с брокколи", "в 3 часа дня съел стейк", "вчера в 7 вечера ужин салат"
///   "кетоны 1.2", "кетоны в 8 утра 0,8"
///   "кето ок", "кето провал", "омад выполнен", "омад нарушил"
///   "today weight 90", "at 3 pm ate eggs", "ketones 1.5", "keto ok", "omad fail"
/// Several parts can be said in one phrase: "сегодня вешу 90 кето ок омад ок".
/// </summary>
public static class VoiceCommandParser
{
    private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Regex DayRx = new(@"\b(позавчера|вчера|сегодня|day before yesterday|yesterday|today)\b", Opt);

    // "85 и 5", "85 точка 5", "85 запятая 5", "85 point 5" -> 85.5
    private static readonly Regex SpokenDecimalRx =
        new(@"(\d+)\s+(?:и|точка|запятая|point|dot)\s+(\d+)", Opt);

    private static readonly Regex WeightRx = new(
        @"(?:\bвес(?:а|у|ом)?\b|\bвешу|\bвзвесил\w*|\bweigh\w*|\bweight)\D{0,20}?(\d{2,3}(?:[.,]\d{1,2})?)" +
        @"|(\d{2,3}(?:[.,]\d{1,2})?)\s*(?:кг|килограмм\w*|kg|kilo\w*)\b", Opt);

    private static readonly Regex KetoneWordRx = new(@"\b(?:кетон\w*|ketone\w*)", Opt);
    private static readonly Regex NumberRx = new(@"(?<![\d.,])\d{1,2}(?:[.,]\d{1,2})?(?![\d:])", Opt);

    // Time like "в 15:00", "в 15 00", "в 3 часа дня", "в 8 утра", "в 7:30 вечера", "at 3 pm", "15:30".
    private static readonly Regex TimeRx = new(
        @"(?:\b(?:в|во|at)\s+)?(?<!\d)(?<h>\d{1,2})(?:(?:[:.]|\s+)(?<m>[0-5]\d)(?!\d)|\s*(?:час\w*)(?:\s+(?<m2>[0-5]?\d)\s*мин\w*)?)?" +
        @"\s*(?<p>утра|дня|вечера|ночи|am|pm|a\.m\.?|p\.m\.?)?(?=\s|$|[,.!?])", Opt);

    private static readonly Regex NoonRx = new(@"\b(?:в\s+)?(полдень|полночь|noon|midnight)\b", Opt);

    private static readonly Regex MealRx = new(
        @"\b(съел[аи]?|поел[аи]?|ел[аи]?|кушал[аи]?|скушал[аи]?|перекусил[аи]?|завтрак\w*|обед\w*|ужин\w*|перекус\w*|ate|eaten|had|eat)\b\s*(?<what>.*)", Opt);

    private static readonly Regex KetoStatusRx = new(
        @"\b(?<what>кето(?!н)|keto(?!ne)|омад|omad|о\s?мад)\b\s*(?:(?:был|была|было|is|was)\s+)?(?<v>не\s+\w+|\w+)?", Opt);

    public static VoiceCommand? Parse(string? text, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var today = now.Date;
        var cmd = new VoiceCommand { Date = today };
        string s = " " + text.Trim() + " ";
        s = SpokenDecimalRx.Replace(s, "$1.$2");

        // ── Day ──
        var dm = DayRx.Match(s);
        if (dm.Success)
        {
            cmd.Date = dm.Value.ToLowerInvariant() switch
            {
                "вчера" or "yesterday" => today.AddDays(-1),
                "позавчера" or "day before yesterday" => today.AddDays(-2),
                _ => today
            };
            s = s.Remove(dm.Index, dm.Length).Insert(dm.Index, " ");
        }
        bool isToday = cmd.Date == today;
        var defaultTime = isToday ? RoundToHalfHour(now.TimeOfDay) : new TimeSpan(12, 0, 0);

        // ── Ketones (before weight so "кетоны 1.2" is never read as weight) ──
        var kw = KetoneWordRx.Match(s);
        if (kw.Success)
        {
            int from = kw.Index + kw.Length;
            string after = s.Substring(from, Math.Min(40, s.Length - from));
            // Take the time out first so "в 8 утра" is not read as the value.
            var t = FindTime(after, out var tm, requireMarker: true);
            string rest = tm is null ? after : Blank(after, tm.Index, tm.Length);
            var num = NumberRx.Match(rest);
            if (num.Success && TryNumber(num.Value, out double ketone) && ketone <= 15)
            {
                cmd.Ketones.Add((t ?? defaultTime, ketone));
                int endIdx = from + Math.Max(num.Index + num.Length, tm is null ? 0 : tm.Index + tm.Length);
                s = Blank(s, kw.Index, endIdx - kw.Index);
            }
        }

        // ── Weight ──
        var wm = WeightRx.Match(s);
        if (wm.Success)
        {
            var g = wm.Groups[1].Success ? wm.Groups[1] : wm.Groups[2];
            if (TryNumber(g.Value, out double w) && w >= 20 && w <= 350)
            {
                cmd.WeightKg = w;
                s = Blank(s, wm.Index, wm.Length);
            }
        }

        // ── Keto / OMAD status ──
        var statusStarts = new List<int>();
        foreach (Match m in KetoStatusRx.Matches(s))
        {
            var verdict = Verdict(m.Groups["v"].Value);
            if (verdict is null) continue;
            statusStarts.Add(m.Index);
            var what = m.Groups["what"].Value.ToLowerInvariant();
            if (what.StartsWith("кето") || what.StartsWith("keto")) cmd.KetoPass = verdict;
            else cmd.OmadPass = verdict;
        }
        // "сорвал кето", "нарушил омад" (verb first)
        foreach (Match m in Regex.Matches(s, @"\b(?<v>не\s+соблюд\w*|не\s+выдержал\w*|сорвал\w*|нарушил\w*|провалил\w*|соблюдал\w*|выдержал\w*|failed|broke|kept)\s+(?<what>кето(?!н)|keto(?!ne)|омад|omad|о\s?мад)\b", Opt))
        {
            var verdict = Verdict(m.Groups["v"].Value);
            if (verdict is null) continue;
            statusStarts.Add(m.Index);
            var what = m.Groups["what"].Value.ToLowerInvariant();
            if (what.StartsWith("кето") || what.StartsWith("keto")) cmd.KetoPass ??= verdict;
            else cmd.OmadPass ??= verdict;
        }

        // ── Meal ──
        var mm = MealRx.Match(s);
        if (mm.Success)
        {
            var what = mm.Groups["what"].Value;
            // Weight and ketones are already blanked out; also stop before "кето ок" / "омад провал".
            int whatStart = mm.Groups["what"].Index;
            var stop = statusStarts.Where(i => i >= whatStart).DefaultIfEmpty(-1).Min();
            if (stop >= 0) what = what.Substring(0, stop - whatStart);

            // Time may be before the verb ("в 15:00 ел ...") or inside the description ("ел ... в 15:00").
            var before = s.Substring(0, mm.Index);
            var time = FindTime(before, out _);
            if (time is null)
            {
                time = FindTime(what, out var tm);
                if (tm is not null) what = what.Remove(tm.Index, tm.Length).Insert(tm.Index, " ");
            }

            var verb = mm.Groups[1].Value.ToLowerInvariant();
            what = Regex.Replace(what, @"\s+", " ").Trim(' ', ',', '.', '!', '?');
            what = Regex.Replace(what, @"^(?:на\s+(?:завтрак|обед|ужин)|и)\s+", "", Opt);
            what = Regex.Replace(what, @"[\s,]+(?:и|а|and)$", "", Opt).Trim(' ', ',');
            // "на обед суп" / "обед: суп" - keep the meal name in the text when that is all we have.
            var mealName = Regex.Match(verb, @"^(завтрак|обед|ужин|перекус)(?:а|у|ом|е)?$");
            if (mealName.Success)
                what = string.IsNullOrEmpty(what) ? mealName.Groups[1].Value : $"{Capitalize(mealName.Groups[1].Value)}: {what}";

            if (!string.IsNullOrWhiteSpace(what))
                cmd.Meals.Add((time ?? MealDefault(verb, defaultTime), Capitalize(what)));
        }

        return cmd.IsEmpty ? null : cmd;
    }

    /// <summary>Tries each alternative from the recogniser and returns the first one that makes sense.</summary>
    public static (VoiceCommand? Command, string Text) ParseBest(IReadOnlyList<string> alternatives, DateTime now)
    {
        foreach (var alt in alternatives)
        {
            var c = Parse(alt, now);
            if (c is not null) return (c, alt);
        }
        return (null, alternatives.Count > 0 ? alternatives[0] : string.Empty);
    }

    private static TimeSpan? FindTime(string s, out Match? match, bool requireMarker = false)
    {
        match = null;
        var noon = NoonRx.Match(s);
        if (noon.Success)
        {
            match = noon;
            var v = noon.Groups[1].Value.ToLowerInvariant();
            return v is "полночь" or "midnight" ? TimeSpan.Zero : new TimeSpan(12, 0, 0);
        }

        foreach (Match m in TimeRx.Matches(s))
        {
            // A bare number is only a time when it looks like one: "в 15", "15:30", "3 часа", "8 утра".
            bool hasPrep = Regex.IsMatch(m.Value, @"^\s*(?:в|во|at)\s", Opt);
            bool hasMinutes = m.Groups["m"].Success;
            bool hasHourWord = m.Value.Contains("час", StringComparison.OrdinalIgnoreCase);
            bool hasPeriod = m.Groups["p"].Success && m.Groups["p"].Value.Length > 0;
            if (!hasPrep && !hasMinutes && !hasHourWord && !hasPeriod) continue;
            // For ketones "1.25" must stay a value, so there we need "в"/"at" or "часа"/"утра"...
            if (requireMarker && !hasPrep && !hasHourWord && !hasPeriod) continue;

            int h = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
            int min = m.Groups["m"].Success ? int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture)
                    : m.Groups["m2"].Success ? int.Parse(m.Groups["m2"].Value, CultureInfo.InvariantCulture)
                    : 0;
            var p = m.Groups["p"].Value.ToLowerInvariant();
            if ((p is "дня" or "вечера" || p.StartsWith("p")) && h < 12) h += 12;
            if ((p is "ночи" || p.StartsWith("a")) && h == 12) h = 0;
            if (h > 23 || min > 59) continue;

            match = m;
            return new TimeSpan(h, min, 0);
        }
        return null;
    }

    private static bool? Verdict(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        if (w.Length == 0) return null;
        if (w.StartsWith("не ") || w.StartsWith("нет") || w.StartsWith("провал") || w.StartsWith("сорва")
            || w.StartsWith("нарушен") || w.StartsWith("нарушил") || w.StartsWith("fail") || w.StartsWith("broke")
            || w.StartsWith("no") || w.StartsWith("плохо") || w.StartsWith("минус") || w.StartsWith("слетел"))
            return false;
        if (w.StartsWith("ок") || w.StartsWith("окей") || w.StartsWith("ok") || w.StartsWith("да") || w.StartsWith("выполн")
            || w.StartsWith("пройд") || w.StartsWith("прош") || w.StartsWith("соблюд") || w.StartsWith("выдерж")
            || w.StartsWith("хорош") || w.StartsWith("плюс") || w.StartsWith("успеш") || w.StartsWith("норм")
            || w.StartsWith("pass") || w.StartsWith("yes") || w.StartsWith("good") || w.StartsWith("done") || w.StartsWith("kept"))
            return true;
        return null;
    }

    private static TimeSpan MealDefault(string verb, TimeSpan fallback)
    {
        if (verb.StartsWith("завтрак")) return new TimeSpan(9, 0, 0);
        if (verb.StartsWith("обед")) return new TimeSpan(14, 0, 0);
        if (verb.StartsWith("ужин")) return new TimeSpan(19, 0, 0);
        return fallback;
    }

    private static bool TryNumber(string s, out double value) =>
        double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static string Blank(string s, int index, int length) =>
        s.Remove(index, length).Insert(index, new string(' ', length));

    private static TimeSpan RoundToHalfHour(TimeSpan t)
    {
        int mins = (int)Math.Round(t.TotalMinutes / 30.0) * 30;
        if (mins >= 24 * 60) mins = 23 * 60 + 30;
        return TimeSpan.FromMinutes(mins);
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0], CultureInfo.InvariantCulture) + s[1..];
}
