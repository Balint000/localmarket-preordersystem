using localmarket_preordersystem.Application.Common.Exceptions;
using localmarket_preordersystem.Application.Markets.Layout;
using localmarket_preordersystem.Application.Producers.AssignStall;
using localmarket_preordersystem.Application.Producers.UpdateMyProducer;
using localmarket_preordersystem.Domain.Entity;
using localmarket_preordersystem.Domain.Exceptions;
using localmarket_preordersystem.Domain.ValueObject;

namespace localmarket_preordersystem.Tests;

/// <summary>Piac-elrendezés (felülnézeti rács) és fix standkiosztás.</summary>
public class MarketLayoutRuleTests
{
    [Fact]
    public void Valid_layout_is_created_and_sorted_by_number()
    {
        var layout = MarketLayout.Create(10, 6, [new MarketStall(2, 3, 0, 2, 2), new MarketStall(1, 0, 0, 2, 2, " hűtött ")]);
        Assert.Equal(new[] { 1, 2 }, layout.Stalls.Select(s => s.Number));
        Assert.Equal("hűtött", layout.Stalls[0].Label);
        Assert.True(layout.HasStall(2));
    }

    [Fact]
    public void Overlapping_stalls_are_rejected()
        => Assert.Throws<DomainException>(() => MarketLayout.Create(10, 6, [new MarketStall(1, 0, 0, 2, 2), new MarketStall(2, 1, 1, 2, 2)]));

    [Fact]
    public void Touching_stalls_are_allowed()
        => MarketLayout.Create(10, 6, [new MarketStall(1, 0, 0, 2, 2), new MarketStall(2, 2, 0, 2, 2)]);

    [Fact]
    public void Stall_outside_the_grid_is_rejected()
        => Assert.Throws<DomainException>(() => MarketLayout.Create(10, 6, [new MarketStall(1, 9, 0, 2, 1)]));

    [Fact]
    public void Duplicate_numbers_are_rejected()
        => Assert.Throws<DomainException>(() => MarketLayout.Create(10, 6, [new MarketStall(1, 0, 0), new MarketStall(1, 5, 5)]));

    [Theory]
    [InlineData(3, 6)]
    [InlineData(41, 6)]
    [InlineData(10, 31)]
    public void Grid_size_is_limited(int columns, int rows)
        => Assert.Throws<DomainException>(() => MarketLayout.Create(columns, rows, []));

    [Fact]
    public void Producer_can_only_get_a_stall_that_exists_in_the_layout()
    {
        var producer = TestData.ApprovedProducer();
        producer.Market.SetLayout(MarketLayout.Create(10, 6, [new MarketStall(1, 0, 0), new MarketStall(2, 2, 0)]));
        Assert.Throws<DomainException>(() => producer.AssignStall(5));
        producer.AssignStall(2);
        Assert.Equal(2, producer.StallNumber);
    }
}

public class StallAssignmentHandlerTests
{
    private static (Market Market, Producer A, Producer B) Setup()
    {
        var market = Market.Create("Piac", "Fő tér 1", [DayOfWeek.Saturday]).WithId(1);
        market.SetLayout(MarketLayout.Create(10, 6, [new MarketStall(1, 0, 0), new MarketStall(2, 2, 0), new MarketStall(3, 4, 0)]));
        var a = Producer.Register(market, "Alma Kft.", 0).WithId(10);
        var b = Producer.Register(market, "Bab Bt.", 0).WithId(11);
        a.Approve(); b.Approve();
        a.MarketId = 1; b.MarketId = 1;
        return (market, a, b);
    }

    private static AssignStallHandler Handler(params Producer[] producers)
        => new(new FakeProducerRepository(producers), new FakeAuditLog(), new FakeUnitOfWork(), new FakeCurrentUser(1, Role.Admin));

    [Fact]
    public async Task Free_stall_is_assigned()
    {
        var (_, a, b) = Setup();
        var dto = await Handler(a, b).HandleAsync(new AssignStallCommand(10, 2));
        Assert.Equal(2, dto.StallNumber);
        Assert.Equal(2, a.StallNumber);
    }

    [Fact]
    public async Task Occupied_stall_conflicts_unless_swapping()
    {
        var (_, a, b) = Setup();
        a.AssignStall(1);
        b.AssignStall(2);

        await Assert.ThrowsAsync<ConflictException>(() => Handler(a, b).HandleAsync(new AssignStallCommand(10, 2)));

        await Handler(a, b).HandleAsync(new AssignStallCommand(10, 2, Swap: true));
        Assert.Equal(2, a.StallNumber);
        Assert.Equal(1, b.StallNumber);
    }

    [Fact]
    public async Task Unknown_stall_and_empty_layout_are_rejected()
    {
        var (market, a, b) = Setup();
        await Assert.ThrowsAsync<ConflictException>(() => Handler(a, b).HandleAsync(new AssignStallCommand(10, 9)));
        market.SetLayout(MarketLayout.Empty);
        await Assert.ThrowsAsync<ConflictException>(() => Handler(a, b).HandleAsync(new AssignStallCommand(10, 1)));
    }

    [Fact]
    public async Task Zero_unassigns()
    {
        var (_, a, b) = Setup();
        a.AssignStall(3);
        await Handler(a, b).HandleAsync(new AssignStallCommand(10, 0));
        Assert.Equal(0, a.StallNumber);
    }

    [Fact]
    public async Task Saving_a_layout_unassigns_removed_and_duplicate_stalls()
    {
        var (market, a, b) = Setup();
        a.AssignStall(3);
        b.AssignStall(1);
        var c = Producer.Register(market, "Cékla Kft.", 1).WithId(12); // régi, ütköző adat: neki is az 1-es
        c.Approve(); c.MarketId = 1;

        var handler = new UpdateMarketLayoutHandler(new FakeMarketRepository(market), new FakeProducerRepository(a, b, c),
            new FakeAuditLog(), new FakeUnitOfWork(), new FakeCurrentUser(1, Role.Admin));
        var dto = await handler.HandleAsync(new UpdateMarketLayoutCommand(1, 8, 5, [new StallInput(1, 0, 0), new StallInput(2, 2, 0)]));

        Assert.Equal(0, a.StallNumber);   // a 3-as stand megszűnt
        Assert.Equal(1, b.StallNumber);   // a korábban regisztrált megtartja az 1-est
        Assert.Equal(0, c.StallNumber);   // az ütköző második kiosztás törlődik
        Assert.Equal(new[] { 10, 12 }, dto.UnassignedProducers.Select(u => u.ProducerId).OrderBy(i => i));
        Assert.Equal("Bab Bt.", dto.Stalls.Single(s => s.Number == 1).ProducerName);
    }

    [Fact]
    public async Task Producer_cannot_change_own_stall()
    {
        var (_, a, _) = Setup();
        a.AssignStall(1);
        var handler = new UpdateMyProducerHandler(new FakeProducerRepository(a), new FakeUnitOfWork(), new FakeCurrentUser(5, Role.Producer, producerId: 10));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.HandleAsync(new UpdateMyProducerCommand("Alma Kft.", 2)));
        var dto = await handler.HandleAsync(new UpdateMyProducerCommand("Alma és Társa"));
        Assert.Equal("Alma és Társa", dto.Name);
        Assert.Equal(1, dto.StallNumber);
    }
}

public class OrderPickupLocationTests
{
    [Fact]
    public void Order_dto_tells_the_customer_the_market_and_the_stall()
    {
        var producer = TestData.ApprovedProducer();
        producer.Market.SetLayout(MarketLayout.Create(10, 6, [new MarketStall(4, 0, 0, 2, 2)]));
        producer.AssignStall(4);
        var product = TestData.ProductWithStock(producer);
        var order = Order.Place(TestData.Customer(), TestData.Slot(producer), [(product, 1m)], TestData.Now);

        var dto = localmarket_preordersystem.Application.Orders.OrderDto.From(order);

        Assert.Equal(4, dto.StallNumber);
        Assert.Equal(producer.Market.Name, dto.MarketName);
    }

    [Fact]
    public void Unassigned_stall_is_null_not_zero()
    {
        var (order, _, _) = TestData.PendingOrder();
        Assert.Null(localmarket_preordersystem.Application.Orders.OrderDto.From(order).StallNumber);
    }
}
