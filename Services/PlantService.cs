using Newtonsoft.Json;
using PlantApp.Models;

namespace PlantApp.Services;

public class PlantService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PlantService> _logger;
    private List<Plant>? _plants;

    public PlantService(IWebHostEnvironment environment, ILogger<PlantService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<List<Plant>> GetAllPlantsAsync()
    {
        if (_plants == null)
        {
            await LoadPlantsAsync();
        }
        return _plants ?? new List<Plant>();
    }

    public async Task<Plant?> GetPlantByIdAsync(Guid id)
    {
        var plants = await GetAllPlantsAsync();
        return plants.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Plant?> GetPlantByShortIdAsync(string shortId)
    {
        var plants = await GetAllPlantsAsync();
        return plants.FirstOrDefault(p => p.Id.ToString().StartsWith(shortId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Plant>> SearchPlantsAsync(string searchTerm, string language = "EN")
    {
        var plants = await GetAllPlantsAsync();
        var lowerSearch = searchTerm.ToLower();
        
        return plants.Where(p => 
            p.GetName(language).ToLower().Contains(lowerSearch) ||
            p.LatinName.ToLower().Contains(lowerSearch) ||
            p.Id.ToString().StartsWith(lowerSearch, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    private async Task LoadPlantsAsync()
    {
        try
        {
            var jsonPath = Path.Combine(_environment.ContentRootPath, "Data", "plants-types.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(_environment.ContentRootPath, "..", "..", "plants-types.json");
            }

            if (File.Exists(jsonPath))
            {
                var json = await File.ReadAllTextAsync(jsonPath);
                _plants = JsonConvert.DeserializeObject<List<Plant>>(json);
                _logger.LogInformation("Loaded {Count} plants from JSON file", _plants?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("Plants JSON file not found at {Path}", jsonPath);
                _plants = new List<Plant>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading plants from JSON");
            _plants = new List<Plant>();
        }
    }

    public (bool IsValid, DateTime SowingDate, string SpeciesGuid) ParsePlantQRCode(string qrCode)
    {
        try
        {
            // Remove leading 'S' if present
            if (qrCode.StartsWith("S", StringComparison.OrdinalIgnoreCase))
                qrCode = qrCode.Substring(1);
                
            // Expected formats: YYYY-MM-DD_GUID or YY-MM-DD_ShortGUID
            if (string.IsNullOrWhiteSpace(qrCode))
                return (false, DateTime.MinValue, string.Empty);

            var parts = qrCode.Split('_');
            if (parts.Length != 2)
                return (false, DateTime.MinValue, string.Empty);

            // Parse date
            if (!DateTime.TryParse(parts[0], out var sowingDate))
            {
                // Try parsing YY-MM-DD format
                if (parts[0].Length == 8 && parts[0][2] == '-' && parts[0][5] == '-')
                {
                    var year = "20" + parts[0].Substring(0, 2);
                    var dateStr = year + parts[0].Substring(2);
                    if (!DateTime.TryParse(dateStr, out sowingDate))
                        return (false, DateTime.MinValue, string.Empty);
                }
                else
                {
                    return (false, DateTime.MinValue, string.Empty);
                }
            }

            // Return the GUID string as-is (could be full or short)
            return (true, sowingDate, parts[1]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing plant QR code: {QrCode}", qrCode);
            return (false, DateTime.MinValue, string.Empty);
        }
    }

    public async Task<(string Name, string Latin, string FullName)> GetPlantSpeciesAsync(string speciesGuid, string language = "EN")
    {
        Plant? plant = null;
        
        // Try to parse as full GUID first
        if (Guid.TryParse(speciesGuid, out var fullGuid))
        {
            plant = await GetPlantByIdAsync(fullGuid);
        }
        else if (speciesGuid.Length >= 8)
        {
            // Try as short GUID
            plant = await GetPlantByShortIdAsync(speciesGuid.Substring(0, 8));
        }
        
        if (plant == null)
        {
            _logger.LogWarning("Plant not found for GUID: {Guid}", speciesGuid);
            return ("Unknown", "Unknown", "Unknown");
        }

        var name = plant.GetName(language);
        var fullName = $"{name} ({plant.LatinName})";
        return (name, plant.LatinName, fullName);
    }
}