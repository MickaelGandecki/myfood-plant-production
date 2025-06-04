using OfficeOpenXml;
using PlantApp.Models;

namespace PlantApp.Services;

public class ExcelService
{
    private readonly PlantService _plantService;
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(PlantService plantService, ILogger<ExcelService> logger)
    {
        _plantService = plantService;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<List<SeedingEntry>> ImportSeedingsAsync(Stream excelStream)
    {
        var seedings = new List<SeedingEntry>();

        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        
        if (worksheet == null)
            throw new Exception("No worksheet found in Excel file");

        var rowCount = worksheet.Dimension?.Rows ?? 0;
        
        for (int row = 2; row <= rowCount; row++)
        {
            var dateStr = worksheet.Cells[row, 1].Value?.ToString();
            var plantName = worksheet.Cells[row, 2].Value?.ToString();
            var quantityStr = worksheet.Cells[row, 3].Value?.ToString();
            var notes = worksheet.Cells[row, 4].Value?.ToString();

            if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(plantName))
                continue;

            if (DateTime.TryParse(dateStr, out var sowingDate) && int.TryParse(quantityStr, out var quantity))
            {
                var plants = await _plantService.SearchPlantsAsync(plantName, "FR");
                var plant = plants.FirstOrDefault();
                
                if (plant != null)
                {
                    seedings.Add(new SeedingEntry
                    {
                        SowingDate = sowingDate,
                        PlantId = plant.Id,
                        Plant = plant,
                        Quantity = quantity,
                        Notes = notes
                    });
                }
            }
        }

        return seedings;
    }

    public async Task<byte[]> ExportSeedingsAsync(List<SeedingEntry> seedings)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Semis");

        worksheet.Cells[1, 1].Value = "Date";
        worksheet.Cells[1, 2].Value = "Plante";
        worksheet.Cells[1, 3].Value = "Nom Latin";
        worksheet.Cells[1, 4].Value = "Quantité";
        worksheet.Cells[1, 5].Value = "Code QR";
        worksheet.Cells[1, 6].Value = "Notes";

        using (var range = worksheet.Cells[1, 1, 1, 6])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        int row = 2;
        foreach (var seeding in seedings.OrderBy(s => s.SowingDate))
        {
            worksheet.Cells[row, 1].Value = seeding.SowingDate;
            worksheet.Cells[row, 1].Style.Numberformat.Format = "dd/MM/yyyy";
            worksheet.Cells[row, 2].Value = seeding.Plant?.NameFr;
            worksheet.Cells[row, 3].Value = seeding.Plant?.LatinName;
            worksheet.Cells[row, 4].Value = seeding.Quantity;
            worksheet.Cells[row, 5].Value = seeding.QrCode;
            worksheet.Cells[row, 6].Value = seeding.Notes;
            row++;
        }

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync();
    }
    
    public async Task<string> ExportSeedingEntriesToExcelAsync(List<SeedingEntry> seedings)
    {
        var bytes = await ExportSeedingsAsync(seedings);
        var fileName = $"seeding-entries-{DateTime.Now:yyyy-MM-dd-HH-mm}.xlsx";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        await File.WriteAllBytesAsync(filePath, bytes);
        return filePath;
    }
}