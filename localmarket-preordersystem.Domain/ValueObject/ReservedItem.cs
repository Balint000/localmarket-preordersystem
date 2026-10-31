namespace localmarket_preordersystem.Domain.ValueObject
{
    /// <summary>
    /// Egy rendelés által éppen lefoglalva tartott mennyiség egy termékből. Az Order adja
    /// vissza (GetHeldReservations), az Application használja a készlet felszabadításához.
    /// </summary>
    public sealed record ReservedItem(int ProductId, decimal Quantity);
}
