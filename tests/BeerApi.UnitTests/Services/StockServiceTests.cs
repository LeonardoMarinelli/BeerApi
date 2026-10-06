using AwesomeAssertions;
using BeerApi.Application.Services;
using BeerApi.Domain.Entities;
using BeerApi.Domain.Events;
using BeerApi.Domain.Exceptions;
using BeerApi.Domain.Interfaces;
using NSubstitute;

namespace BeerApi.UnitTests.Services;

public class StockServiceTests
{
    private readonly IWholesalerRepository _wholesalerRepository = Substitute.For<IWholesalerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly StockService _sut;

    public StockServiceTests()
    {
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>().Invoke());
        _sut = new StockService(_wholesalerRepository, _unitOfWork, _outbox, 10);
    }

    [Fact]
    public async Task RemoveAsync_CrossesThreshold_AddsStockLowEvent()
    {
        var stock = new WholesalerBeer { WholesalerId = 1, BeerId = 3, Quantity = 11 };
        _wholesalerRepository.GetStockEntryAsync(1, 3, Arg.Any<CancellationToken>()).Returns(stock);

        await _sut.RemoveAsync(1, 3, 2);

        stock.Quantity.Should().Be(9);
        await _wholesalerRepository.Received(1).UpdateStockEntryAsync(stock, Arg.Any<CancellationToken>());
        await _outbox.Received(1).AddAsync(
            Arg.Is<StockLowEvent>(domainEvent => domainEvent.Quantity == 9 && domainEvent.Threshold == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_StaysBelowThreshold_DoesNotRepeatAlert()
    {
        var stock = new WholesalerBeer { WholesalerId = 1, BeerId = 3, Quantity = 9 };
        _wholesalerRepository.GetStockEntryAsync(1, 3, Arg.Any<CancellationToken>()).Returns(stock);

        await _sut.RemoveAsync(1, 3, 1);

        await _outbox.DidNotReceive().AddAsync(Arg.Any<StockLowEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_ExceedsAvailableStock_ThrowsBusinessException()
    {
        _wholesalerRepository.GetStockEntryAsync(1, 3, Arg.Any<CancellationToken>())
            .Returns(new WholesalerBeer { WholesalerId = 1, BeerId = 3, Quantity = 2 });

        var act = () => _sut.RemoveAsync(1, 3, 3);

        await act.Should().ThrowAsync<BusinessException>();
        await _wholesalerRepository.DidNotReceive().UpdateStockEntryAsync(Arg.Any<WholesalerBeer>(), Arg.Any<CancellationToken>());
    }
}