using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace PointOfSaleKata;

public class PointOfSaleShould
{
    private readonly Display display;
    private readonly PointOfSale pointOfSale;

    public PointOfSaleShould()
    {
        var catalog = new Catalog(
            new Dictionary<string, decimal>
            {
                { "12345", 7.25m },
                { "23456", 12.50m }
            });

        var shoppingCart = new ShoppingCart();

        display = new Display();

        pointOfSale = new PointOfSale(
            display,
            catalog,
            shoppingCart);
    }

    [Theory]
    [InlineData("12345", "$7.25")]
    [InlineData("23456", "$12.50")]
    public void Display_product_price_when_product_found(string barcode, string productPrice)
    {
        // When
        pointOfSale.OnBarcode(barcode);

        // Then
        display.GetText()
            .Should()
            .Be(productPrice);
    }

    [Theory]
    [InlineData("99999")]
    public void Display_error_when_product_not_found(string barcode)
    {
        // When
        pointOfSale.OnBarcode(barcode);

        // Then
        display.GetText()
            .Should()
            .Be("Error: barcode not found");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Display_error_when_empty_barcode(string barcode)
    {
        // When
        pointOfSale.OnBarcode(barcode);

        // Then
        display.GetText()
            .Should()
            .Be("Error: empty barcode");
    }

    [Fact]
    public void Display_total_price_when_no_product_scanned()
    {
        // When
        pointOfSale.Total();

        // Then
        display.GetText()
            .Should()
            .Be("Total: $0.00");
    }

    [Fact]
    public void Display_total_price_when_one_product_scanned()
    {
        // When
        pointOfSale.OnBarcode("12345");
        pointOfSale.Total();

        // Then
        display.GetText()
            .Should()
            .Be(
                "$7.25\n" +
                "Total: $7.25");
    }

    [Fact]
    public void Display_total_price_when_many_products_scanned()
    {
        // When
        pointOfSale.OnBarcode("12345");
        pointOfSale.OnBarcode("23456");
        pointOfSale.Total();

        // Then
        display.GetText()
            .Should()
            .Be(
                "$7.25\n" +
                "$12.50\n" +
                "Total: $19.75");
    }
}

public class Display
{
    private const char NewLine = '\n';
    private readonly List<string> lines = new();

    public string GetText()
        => string.Join(NewLine, lines);

    public void DisplayProductPrice(decimal productPrice)
        => lines.Add(FormatPrice(productPrice));

    public void DisplayProductNotFoundMessage()
        => lines.Add("Error: barcode not found");

    public void DisplayEmptyBarcodeMessage()
        => lines.Add("Error: empty barcode");

    public void DisplayTotalPrice(decimal totalPrice)
        => lines.Add($"Total: {FormatPrice(totalPrice)}");

    private static string FormatPrice(decimal productPrice)
        => $"${productPrice:N2}";
}

public class PointOfSale
{
    private readonly Catalog catalog;
    private readonly Display display;
    private readonly ShoppingCart shoppingCart;

    public PointOfSale(Display display, Catalog catalog, ShoppingCart shoppingCart)
    {
        this.display = display;
        this.catalog = catalog;
        this.shoppingCart = shoppingCart;
    }

    public void OnBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            display.DisplayEmptyBarcodeMessage();
            return;
        }

        var productPrice = catalog.FindProductPrice(barcode);
        if (productPrice is null)
        {
            display.DisplayProductNotFoundMessage();
            return;
        }

        shoppingCart.AddProductPrice(productPrice.Value);
        display.DisplayProductPrice(productPrice.Value);
    }

    public void Total()
        => display.DisplayTotalPrice(shoppingCart.GetTotal());
}

public class Catalog
{
    private readonly Dictionary<string, decimal> priceByBarcode;

    public Catalog(Dictionary<string, decimal> priceByBarcode)
        => this.priceByBarcode = priceByBarcode;

    public decimal? FindProductPrice(string barcode)
    {
        if (!priceByBarcode.ContainsKey(barcode))
            return null;

        return priceByBarcode[barcode];
    }
}

public class ShoppingCart
{
    private readonly List<decimal> productPrices = new();

    public void AddProductPrice(decimal productPrice)
        => productPrices.Add(productPrice);

    public decimal GetTotal()
        => productPrices.Sum();
}