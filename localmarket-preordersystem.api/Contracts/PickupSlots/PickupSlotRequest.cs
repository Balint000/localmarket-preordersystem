namespace localmarket_preordersystem.api.Contracts.PickupSlots;

public sealed record PickupSlotRequest(DateTime Date, TimeOnly StartTime, TimeOnly EndTime, int Capacity);
