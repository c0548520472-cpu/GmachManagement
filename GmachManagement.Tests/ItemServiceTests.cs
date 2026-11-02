using System;
using System.Collections.Generic;
using System.Text;


using GmachManagement.Core.Entities;
using GmachManagement.Core.Interfaces;
using GmachManagement.Service.Services;
using Moq;
using Xunit;

namespace GmachManagement.Tests;

public class ItemServiceTests
{
    private readonly Mock<IItemRepository> _repository = new();
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        _service = new ItemService(_repository.Object);
    }

    [Fact]
    public async Task AddAsync_TrimsNameAndDescription()
    {
        _repository
            .Setup(r => r.AddAsync(
                It.IsAny<Item>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Item item, CancellationToken _) => item);

        var result = await _service.AddAsync(
            "  מקדחה  ",
            "  מקדחה חשמלית  ");

        Assert.Equal("מקדחה", result.Name);
        Assert.Equal("מקדחה חשמלית", result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WhenItemDoesNotExist_ReturnsNull()
    {
        _repository
            .Setup(r => r.GetByIdAsync(
                999,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Item?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenItemDoesNotExist_ReturnsFalse()
    {
        _repository
            .Setup(r => r.UpdateAsync(
                It.IsAny<Item>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateAsync(
            999,
            "פריט חדש",
            "תיאור");

        Assert.False(result);
    }
}

