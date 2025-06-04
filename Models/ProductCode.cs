using System.Text.RegularExpressions;

namespace PlantApp.Models;

public class ProductCode
{
    public string Prefix { get; set; } = string.Empty;
    public string KitType { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public string FullCode { get; set; } = string.Empty;

    public static ProductCode? Parse(string productCode)
    {
        // Example: 508002-MIX1-012 -> Prefix: 508002, Type: MIX1, Quantity: 12
        var match = Regex.Match(productCode, @"^(\w+)-([A-Z0-9]+)-(\d{3})$");
        if (match.Success)
        {
            return new ProductCode
            {
                Prefix = match.Groups[1].Value,
                KitType = match.Groups[2].Value,
                TotalQuantity = int.Parse(match.Groups[3].Value),
                FullCode = productCode
            };
        }
        return null;
    }
}