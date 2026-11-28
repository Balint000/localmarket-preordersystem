namespace localmarket_preordersystem.api.Contracts.Producers;

/// <param name="StallNumber">0 = a kiosztás törlése.</param>
/// <param name="Swap">Foglalt stand esetén a két árus helyet cserél.</param>
public sealed record AssignStallRequest(int StallNumber, bool Swap = false);
