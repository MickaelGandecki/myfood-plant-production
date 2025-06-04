namespace PlantApp.Models;

public class SeedingEntry
{
    public Guid Id { get; set; }
    public DateTime SowingDate { get; set; }
    public DateTime SeedingDate => SowingDate;
    public Guid PlantId { get; set; }
    public Plant? Plant { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string QrCode => $"S{SowingDate:yyyy-MM-dd}_{PlantId.ToString().Substring(0, 8)}";
    
    public string GetShortId() => Id.ToString().Substring(0, 8);
    
    public string GetQrCodeData()
    {
        return $"PLANT:{PlantId}|QTY:{Quantity}|DATE:{SeedingDate:yyyy-MM-dd}";
    }
}