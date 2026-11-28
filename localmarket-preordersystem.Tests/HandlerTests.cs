using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.PickupSlots.CreatePickupSlot;
using localmarket_preordersystem.Application.Users.SetUserActive;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

public class SetUserActiveHandlerTests
{
    private static User Admin(int id) => User.Register("Piac", "Vezető", $"admin{id}@example.com", "hash", Role.Admin, "").WithId(id);
    private static User Customer(int id) => User.Register("Teszt", "Elek", $"vevo{id}@example.com", "hash", Role.Customer, "").WithId(id);

    private static (SetUserActiveHandler Handler, FakeAuditLog Audit, FakeUnitOfWork Uow) Build(int currentUserId, params User[] users)
    {
        var audit = new FakeAuditLog();
        var uow = new FakeUnitOfWork();
        var handler = new SetUserActiveHandler(new FakeUserRepository(users), audit, uow, new FakeCurrentUser(currentUserId, Role.Admin));
        return (handler, audit, uow);
    }

    [Fact]
    public async Task Admin_can_deactivate_and_reactivate_a_customer_and_it_is_audited()
    {
        var customer = Customer(2);
        var (handler, audit, uow) = Build(1, Admin(1), customer);

        var off = await handler.HandleAsync(new SetUserActiveCommand(2, false));
        Assert.False(off.IsActive);
        Assert.False(customer.IsActive);

        var on = await handler.HandleAsync(new SetUserActiveCommand(2, true));
        Assert.True(on.IsActive);

        Assert.Equal(new[] { "UserDeactivated", "UserActivated" }, audit.Entries.Select(e => e.Action).ToArray());
        Assert.Equal(2, uow.SaveCount);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_their_own_account()
    {
        var (handler, _, uow) = Build(1, Admin(1), Admin(3));
        await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(new SetUserActiveCommand(1, false)));
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task The_last_active_admin_cannot_be_deactivated()
    {
        // a hívó admin (1) már le van tiltva a tárolóban: rajta kívül csak a 3-as az aktív admin
        var caller = Admin(1);
        caller.Deactivate();
        var (handler, _, _) = Build(1, caller, Admin(3));
        await Assert.ThrowsAsync<ConflictException>(() => handler.HandleAsync(new SetUserActiveCommand(3, false)));
    }

    [Fact]
    public async Task Unchanged_state_is_a_no_op()
    {
        var (handler, audit, uow) = Build(1, Admin(1), Customer(2));
        var result = await handler.HandleAsync(new SetUserActiveCommand(2, true));
        Assert.True(result.IsActive);
        Assert.Empty(audit.Entries);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task Non_admin_is_forbidden()
    {
        var handler = new SetUserActiveHandler(new FakeUserRepository(Customer(2)), new FakeAuditLog(), new FakeUnitOfWork(),
            new FakeCurrentUser(2, Role.Customer));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.HandleAsync(new SetUserActiveCommand(2, false)));
    }

    [Fact]
    public async Task Unknown_user_is_not_found()
    {
        var (handler, _, _) = Build(1, Admin(1));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(new SetUserActiveCommand(99, false)));
    }
}

public class CreatePickupSlotHandlerTests
{
    private static (CreatePickupSlotHandler Handler, FakePickupSlotRepository Slots) Build()
    {
        var producer = TestData.ApprovedProducer().WithId(1);
        var existing = PickupSlot.Create(producer, TestData.Saturday, new TimeOnly(9, 0), new TimeOnly(12, 0), 5).WithId(10);
        var slots = new FakePickupSlotRepository(existing);
        var handler = new CreatePickupSlotHandler(new FakeProducerRepository(producer), slots, new FakeUnitOfWork(),
            new FakeCurrentUser(5, Role.Producer, producerId: 1), new FixedTimeProvider(TestData.Now));
        return (handler, slots);
    }

    [Fact]
    public async Task Overlapping_slot_on_the_same_day_is_rejected()
    {
        var (handler, slots) = Build();
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(new CreatePickupSlotCommand(TestData.Saturday, new TimeOnly(11, 0), new TimeOnly(13, 0), 3)));
        Assert.Single(slots.Slots);
    }

    [Fact]
    public async Task Adjacent_slot_is_allowed()
    {
        var (handler, slots) = Build();
        var dto = await handler.HandleAsync(new CreatePickupSlotCommand(TestData.Saturday, new TimeOnly(7, 0), new TimeOnly(9, 0), 3));
        Assert.Equal(new TimeOnly(7, 0), dto.StartTime);
        Assert.Equal(2, slots.Slots.Count);
    }
}

public class PickupSlotOverlapTests
{
    private static PickupSlot NineToTwelve() =>
        PickupSlot.Create(TestData.ApprovedProducer(), TestData.Saturday, new TimeOnly(9, 0), new TimeOnly(12, 0), 5);

    [Theory]
    [InlineData(8, 10, true)]   // belelóg az elejébe
    [InlineData(11, 13, true)]  // belelóg a végébe
    [InlineData(10, 11, true)]  // benne van
    [InlineData(8, 13, true)]   // körbeveszi
    [InlineData(12, 14, false)] // csak érintkezik (vége = kezdete)
    [InlineData(7, 9, false)]   // csak érintkezik (eleje)
    [InlineData(13, 15, false)] // teljesen utána
    public void Overlap_is_detected_on_the_same_day(int start, int end, bool expected)
        => Assert.Equal(expected, NineToTwelve().Overlaps(TestData.Saturday, new TimeOnly(start, 0), new TimeOnly(end, 0)));

    [Fact]
    public void Different_day_never_overlaps()
        => Assert.False(NineToTwelve().Overlaps(TestData.Saturday.AddDays(7), new TimeOnly(9, 0), new TimeOnly(12, 0)));
}
