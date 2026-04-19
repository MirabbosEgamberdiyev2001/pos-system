using FluentAssertions;
using NUnit.Framework;
using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Domain.UnitTests.Entities;

[TestFixture]
public class ProductTests
{
    [Test]
    public void Product_DefaultAmountIsZero()
    {
        var product = new Product();
        product.Amount.Should().Be(0);
    }

    [Test]
    public void Product_ProductItemsInitializedEmpty()
    {
        var product = new Product();
        product.ProductItems.Should().NotBeNull();
        product.ProductItems.Should().BeEmpty();
    }

    [Test]
    public void Product_CanSetAllProperties()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test Mahsulot",
            Barcode = "1234567890123",
            CategoryId = 2,
            Amount = 100,
            WarningAmount = 10,
            MeasurmentType = MeasurmentType.Kilogram
        };

        product.Id.Should().Be(1);
        product.Name.Should().Be("Test Mahsulot");
        product.MeasurmentType.Should().Be(MeasurmentType.Kilogram);
    }
}
