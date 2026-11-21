using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Service.Services;
using Moq;
using Xunit;

namespace GmachManagement.Tests;

public class LoanServiceValidationTests
{
    [Fact]
    public async Task BorrowItemAsync_RejectsPastReturnDateWithoutCallingRepository()
    {
        var repository = new Mock<ILoanRepository>(MockBehavior.Strict);
        var service = new LoanService(repository.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.BorrowItemAsync(1, 1, DateTime.UtcNow.AddDays(-1)));

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BorrowItemAsync_ValidReturnDate_DelegatesToRepository()
    {
        var repository = new Mock<ILoanRepository>();
        var expectedDate = DateTime.UtcNow.AddDays(3);
        repository.Setup(r => r.BorrowItemAsync(1, 2, expectedDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);
        var service = new LoanService(repository.Object);

        await service.BorrowItemAsync(1, 2, expectedDate);

        repository.Verify(r => r.BorrowItemAsync(1, 2, expectedDate, It.IsAny<CancellationToken>()), Times.Once);
    }
}
