using GmachManagement.Core.Interfaces;
using GmachManagement.Service.Services;
using Moq;
using Xunit;

namespace GmachManagement.Tests;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _repository = new();
    private readonly LoanService _service;

    public LoanServiceTests()
    {
        _service = new LoanService(_repository.Object);
    }

    [Fact]
    public async Task ChangeReturnDateAsync_RejectsPastDateWithoutWriting()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.ChangeReturnDateAsync(1, DateTime.UtcNow.AddDays(-1), CancellationToken.None));
        _repository.Verify(
            r => r.ChangeReturnDateAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeReturnDateAsync_ValidFutureDate_DelegatesToRepository()
    {
        var date = DateTime.UtcNow.AddDays(7);
        _repository.Setup(r => r.ChangeReturnDateAsync(3, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Assert.True(await _service.ChangeReturnDateAsync(3, date, CancellationToken.None));
        _repository.Verify(r => r.ChangeReturnDateAsync(3, date, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOwnerLoansAsync_DelegatesSingleFilteredRepositoryQuery()
    {
        _repository.Setup(r => r.GetOwnerLoansAsync(12, true, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GmachManagement.Core.Entities.Loan>());
        var result = await _service.GetOwnerLoansAsync(12, true, true, CancellationToken.None);
        Assert.Empty(result);
        _repository.Verify(r => r.GetOwnerLoansAsync(12, true, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BorrowItemAsync_AvailableCopy_ReturnsLoan()
    {
        var dueDate = DateTime.UtcNow.AddDays(3);
        var expected = new GmachManagement.Core.Entities.Loan
        {
            UserId = 1, ItemCopyId = 7, ExpectedReturnDate = dueDate
        };
        _repository.Setup(r => r.BorrowItemAsync(1, 7, dueDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var actual = await _service.BorrowItemAsync(1, 7, dueDate);
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task BorrowItemAsync_Conflict_PropagatesToApiMiddleware()
    {
        var dueDate = DateTime.UtcNow.AddDays(3);
        _repository.Setup(r => r.BorrowItemAsync(1, 7, dueDate, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GmachManagement.Core.Exceptions.ConcurrencyConflictException());
        await Assert.ThrowsAsync<GmachManagement.Core.Exceptions.ConcurrencyConflictException>(
            () => _service.BorrowItemAsync(1, 7, dueDate));
    }

    [Fact]
    public async Task BorrowItemAsync_WhenCopyIsUnavailable_ReturnsNull()
    {
        _repository.Setup(r => r.BorrowItemAsync(1, 7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GmachManagement.Core.Entities.Loan?)null);
        Assert.Null(await _service.BorrowItemAsync(1, 7, DateTime.UtcNow.AddDays(1)));
    }
}
