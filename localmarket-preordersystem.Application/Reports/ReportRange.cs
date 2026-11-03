using localmarket_preordersystem.Application.Common.Exceptions;

namespace localmarket_preordersystem.Application.Reports
{
    /// <summary>Közös időszak-feloldás: alapértelmezés az elmúlt 30 nap; a vég a nap végéig tart.</summary>
    internal static class ReportRange
    {
        public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, TimeProvider time)
        {
            var today = time.GetLocalNow().Date;
            var f = (from ?? today.AddDays(-30)).Date;
            var t = (to ?? today).Date.AddDays(1).AddTicks(-1);
            if (f > t)
                throw new ValidationException(new Dictionary<string, string[]> { ["From"] = new[] { "A kezdő dátum nem lehet a végdátum után." } });
            return (f, t);
        }
    }
}
