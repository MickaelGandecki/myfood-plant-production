namespace PlantApp.Models;

public class Plant
{
    public Guid Id { get; set; }
    public string NameFr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameDe { get; set; } = string.Empty;
    public string LatinName { get; set; } = string.Empty;
    
    public string GetName(string language)
    {
        return language?.ToUpper() switch
        {
            "FR" => NameFr,
            "DE" => NameDe,
            _ => NameEn
        };
    }
    
    public string GetShortId() => Id.ToString().Substring(0, 8);
}