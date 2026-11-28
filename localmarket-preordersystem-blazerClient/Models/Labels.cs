namespace localmarket_preordersystem.Models;

/// <summary>Az API enum-neveinek magyar megjelenítése (csak megjelenítés, üzleti szabály nincs benne).</summary>
public static class Labels
{
    public static readonly string[] OrderStatuses =
        ["Pending", "Accepted", "PartiallyFulfilled", "Ready", "PickedUp", "NotPickedUp", "Disputed", "Cancelled", "Rejected", "Expired"];

    public static readonly string[] Allergies =
        ["Gluten", "Crustaceans", "Eggs", "Fish", "Peanuts", "Soybeans", "Milk", "Nuts", "Celery", "Mustard", "Sesame", "Sulphites", "Lupin", "Molluscs"];

    public static readonly string[] PaymentMethods = ["Cash", "Card", "BankTransfer"];

    public static string OrderStatus(string s) => s switch
    {
        "Pending" => "Függőben",
        "Accepted" => "Elfogadva",
        "PartiallyFulfilled" => "Részben teljesítve",
        "Ready" => "Átvehető",
        "PickedUp" => "Átvéve",
        "NotPickedUp" => "Nem átvett",
        "Disputed" => "Vitatott",
        "Cancelled" => "Lemondva",
        "Rejected" => "Elutasítva",
        "Expired" => "Lejárt (nem igazolták vissza)",
        _ => s
    };

    /// <summary>CSS-osztály a státusz-jelvényhez (szín + szöveg együtt, nem csak szín).</summary>
    public static string OrderStatusClass(string s) => s switch
    {
        "Pending" => "badge-warn",
        "Accepted" or "PartiallyFulfilled" or "Ready" => "badge-info",
        "PickedUp" => "badge-ok",
        "Cancelled" or "Rejected" or "NotPickedUp" or "Expired" => "badge-bad",
        "Disputed" => "badge-alert",
        _ => "badge-muted"
    };

    public static string Unit(string u) => u switch
    {
        "Piece" => "db",
        "Kilogram" => "kg",
        "Gram" => "g",
        "Litre" => "l",
        "Millilitre" => "ml",
        "Decagram" => "dkg",
        "Bundle" => "csomag",
        "Box" => "doboz",
        "Pack" => "tálca/csomag",
        "Bottle" => "üveg",
        "Jar" => "befőttes üveg",
        "Bunch" => "csokor",
        "Tray" => "tálca",
        _ => u
    };

    public static string Allergy(string a) => a switch
    {
        "Gluten" => "Glutén",
        "Crustaceans" => "Rákfélék",
        "Eggs" => "Tojás",
        "Fish" => "Hal",
        "Peanuts" => "Földimogyoró",
        "Soybeans" => "Szója",
        "Milk" => "Tej",
        "Nuts" => "Diófélék",
        "Celery" => "Zeller",
        "Mustard" => "Mustár",
        "Sesame" => "Szezám",
        "Sulphites" => "Szulfitok",
        "Lupin" => "Csillagfürt",
        "Molluscs" => "Puhatestűek",
        _ => a
    };

    public static string Payment(string p) => p switch
    {
        "Cash" => "Készpénz (átvételkor)",
        "Card" => "Bankkártya",
        "BankTransfer" => "Átutalás",
        _ => p
    };

    public static string PaymentStatus(string s) => s switch
    {
        "Pending" => "fizetésre vár",
        "Completed" => "kifizetve",
        "Cancelled" => "törölve",
        "Error" => "hiba",
        "Refunded" => "visszatérítve",
        _ => s
    };

    public static string Role(string r) => r switch
    {
        "Customer" => "Vásárló",
        "Producer" => "Árus",
        "Admin" => "Adminisztrátor",
        _ => r
    };

    public static string Day(string d) => d switch
    {
        "Monday" => "hétfő",
        "Tuesday" => "kedd",
        "Wednesday" => "szerda",
        "Thursday" => "csütörtök",
        "Friday" => "péntek",
        "Saturday" => "szombat",
        "Sunday" => "vasárnap",
        _ => d
    };

    public static readonly string[] UnitKeys = ["Piece", "Kilogram", "Gram", "Decagram", "Litre", "Millilitre", "Bundle", "Box", "Pack", "Bottle", "Jar", "Bunch", "Tray"];
    public static readonly string[] DayKeys = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public static string ProducerStatus(string s) => s switch
    {
        "Pending" => "Jóváhagyásra vár",
        "Approved" => "Jóváhagyva",
        "Rejected" => "Elutasítva",
        _ => s
    };

    public static string ProducerStatusClass(string s) => s switch
    {
        "Pending" => "badge-warn",
        "Approved" => "badge-ok",
        "Rejected" => "badge-bad",
        _ => "badge-muted"
    };

    /// <summary>A termék színes "borítója" kategória alapján, hogy a kártyák ne legyenek egyformák (nincs képmező az API-ban).</summary>
    public static (string Emoji, string Css) CategoryLook(string? name)
    {
        var n = (name ?? "").ToLowerInvariant();
        if (n.Contains("zöldség")) return ("🥕", "look-green");
        if (n.Contains("gyümölcs")) return ("🍎", "look-red");
        if (n.Contains("tej") || n.Contains("sajt")) return ("🧀", "look-yellow");
        if (n.Contains("pék") || n.Contains("kenyér")) return ("🥖", "look-brown");
        if (n.Contains("hús") || n.Contains("tojás")) return ("🥚", "look-sand");
        if (n.Contains("méz") || n.Contains("lekvár")) return ("🍯", "look-amber");
        if (n.Contains("ital") || n.Contains("bor")) return ("🍷", "look-purple");
        return ("🧺", "look-green");
    }

    public static string Money(decimal v) => v.ToString("#,##0.##", Hu) + " Ft";
    public static string Qty(decimal v) => v.ToString("0.##", Hu);

    // saját számformátum (tizedesvessző, szóközös ezres tagolás), hogy ne függjön az ICU/kultúra-adatoktól
    private static readonly System.Globalization.NumberFormatInfo Hu = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = "\u00A0",
        NumberGroupSizes = [3]
    };

    private static readonly string[] Months =
        ["január", "február", "március", "április", "május", "június", "július", "augusztus", "szeptember", "október", "november", "december"];

    /// <summary>Emberi dátum: "november 28., szombat" (kultúra-adatoktól függetlenül).</summary>
    public static string DateLong(DateTime d) => $"{Months[d.Month - 1]} {d.Day}., {Day(d.DayOfWeek.ToString())}";

    /// <summary>Rövid dátum: "nov. 28." – kártyákon, chipeken.</summary>
    public static string DateShort(DateTime d) => $"{Months[d.Month - 1][..3]}. {d.Day}.";

    /// <summary>Relatív napnév, ha közel van: "ma", "holnap", különben a hét napja.</summary>
    public static string DayRelative(DateTime d)
    {
        var diff = (d.Date - DateTime.Today).Days;
        return diff switch { 0 => "ma", 1 => "holnap", _ => Day(d.DayOfWeek.ToString()) };
    }

    /// <summary>A hét napjának egybetűs/kétbetűs rövidítése a nyitvatartás-sávhoz.</summary>
    public static string DayAbbr(string d) => d switch
    {
        "Monday" => "H", "Tuesday" => "K", "Wednesday" => "Sze", "Thursday" => "Cs",
        "Friday" => "P", "Saturday" => "Szo", "Sunday" => "V", _ => d
    };

    /// <summary>Időjárás-ikon a leírás és a csapadék alapján (csak megjelenítés).</summary>
    public static string WeatherIcon(MarketWeatherInfo? w)
    {
        if (w is null) return "cloud";
        var text = (w.Description ?? "").ToLowerInvariant();
        if (text.Contains("hó") || text.Contains("havas")) return "snow";
        if (text.Contains("eső") || text.Contains("zápor") || text.Contains("zivatar") || w.PrecipitationMm is > 1) return "cloud-rain";
        if (text.Contains("részben") || text.Contains("változó")) return "cloud-sun";
        if (text.Contains("derült") || text.Contains("napos") || text.Contains("tiszta")) return "sun";
        if (text.Contains("felhős") || text.Contains("borult") || text.Contains("köd")) return "cloud";
        return "cloud-sun";
    }

    public static string PaymentIcon(string p) => p switch { "Cash" => "cash", "Card" => "card", "BankTransfer" => "bank", _ => "cash" };

    public static string PaymentHint(string p) => p switch
    {
        "Cash" => "A standnál fizetsz, átvételkor.",
        "Card" => "Bankkártyával fizetsz átvételkor.",
        "BankTransfer" => "Átutalással rendezed; az oldalon nincs online fizetés.",
        _ => ""
    };

    public static string Date(DateTime d) => d.ToString("yyyy. MM. dd.", System.Globalization.CultureInfo.InvariantCulture);
    public static string DateTimeText(DateTime d) => d.ToString("yyyy. MM. dd. HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    public static string Time(TimeOnly t) => t.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
}
