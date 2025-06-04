namespace PlantApp.Models;

public class ScannedPlant
{
    public Guid Id { get; set; }
    public DateTime SowingDate { get; set; }
    public DateTime SeedingDate => SowingDate;
    public Guid PlantId { get; set; }
    public Plant? Plant { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "Seeded";
    public DateTime ScannedAt { get; set; }
    public DateTime LastScanned { get; set; }
    
    // Properties for OrderScanning
    public int ScanNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid SpeciesGuid { get; set; }
    public string SpeciesName { get; set; } = string.Empty;
    public string SpeciesLatin { get; set; } = string.Empty;
    public string SpeciesFullName { get; set; } = string.Empty;
    
    public string GetShortId() => Id.ToString().Substring(0, 8);
    
    public string GetQrCodeData()
    {
        return $"PLANT:{PlantId}|QTY:{Quantity}|DATE:{SeedingDate:yyyy-MM-dd}|STATUS:{Status}";
    }
    
    public static ScannedPlant? ParseQrCode(string qrCode)
    {
        if (string.IsNullOrEmpty(qrCode) || !qrCode.StartsWith("S"))
            return null;
            
        var parts = qrCode.Substring(1).Split('_');
        if (parts.Length != 2)
            return null;
            
        if (!DateTime.TryParse(parts[0], out var sowingDate))
            return null;
            
        return new ScannedPlant
        {
            Id = Guid.NewGuid(),
            SowingDate = sowingDate,
            PlantId = Guid.Parse(parts[1].PadRight(32, '0')),
            ScannedAt = DateTime.Now,
            LastScanned = DateTime.Now,
            Quantity = 1
        };
    }
}