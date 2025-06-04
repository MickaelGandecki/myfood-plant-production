using System.Net.Sockets;
using System.Text;
using PlantApp.Models;

namespace PlantApp.Services;

public class ZebraPrinterService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ZebraPrinterService> _logger;
    private readonly QrCodeService _qrCodeService;

    public ZebraPrinterService(IConfiguration configuration, ILogger<ZebraPrinterService> logger, QrCodeService qrCodeService)
    {
        _configuration = configuration;
        _logger = logger;
        _qrCodeService = qrCodeService;
    }

    public async Task<bool> PrintSeedingLabelAsync(SeedingEntry seeding, Plant plant)
    {
        var zpl = GenerateSeedingLabelZpl(seeding, plant);
        return await SendToPrinterAsync(zpl);
    }

    public async Task<bool> PrintOrderLabelAsync(ManufacturingOrder order, string language = "EN")
    {
        var zpl = GenerateOrderLabelZpl(order, language);
        return await SendToPrinterAsync(zpl);
    }

    private string GenerateSeedingLabelZpl(SeedingEntry seeding, Plant plant)
    {
        var qrCode = seeding.QrCode;
        var plantName = plant.NameFr;
        var latinName = plant.LatinName;
        var sowingDate = seeding.SowingDate.ToString("dd/MM/yyyy");

        var zpl = new StringBuilder();
        zpl.AppendLine("^XA");
        zpl.AppendLine("^CI28");
        zpl.AppendLine("^FO50,50^BQN,2,5^FDMM,A" + qrCode + "^FS");
        zpl.AppendLine("^FO250,50^ADN,36,20^FD" + plantName + "^FS");
        zpl.AppendLine("^FO250,100^ADN,24,12^FD" + latinName + "^FS");
        zpl.AppendLine("^FO250,150^ADN,24,12^FDSemis: " + sowingDate + "^FS");
        zpl.AppendLine("^XZ");

        return zpl.ToString();
    }

    private string GenerateOrderLabelZpl(ManufacturingOrder order, string language)
    {
        var zpl = new StringBuilder();
        zpl.AppendLine("^XA");
        zpl.AppendLine("^CI28");
        
        var title = language switch
        {
            "FR" => "COMMANDE DE PRODUCTION",
            "DE" => "PRODUKTIONSAUFTRAG",
            _ => "MANUFACTURING ORDER"
        };

        zpl.AppendLine($"^FO50,50^ADN,36,20^FD{title}^FS");
        zpl.AppendLine($"^FO50,100^ADN,24,12^FDOrder: {order.OrderKey}^FS");
        zpl.AppendLine($"^FO50,150^ADN,24,12^FDProduct: {order.ProductName}^FS");
        zpl.AppendLine($"^FO50,200^ADN,24,12^FDQuantity: {order.ProductQty}^FS");
        
        if (!string.IsNullOrEmpty(order.PartnerName))
        {
            zpl.AppendLine($"^FO50,250^ADN,24,12^FDCustomer: {order.PartnerName}^FS");
        }

        zpl.AppendLine("^XZ");
        return zpl.ToString();
    }

    private async Task<bool> SendToPrinterAsync(string zplData)
    {
        try
        {
            var printerIp = _configuration["Printer:IpAddress"];
            var printerPort = int.Parse(_configuration["Printer:Port"] ?? "9100");

            using var client = new TcpClient();
            await client.ConnectAsync(printerIp!, printerPort);
            
            using var stream = client.GetStream();
            var data = Encoding.UTF8.GetBytes(zplData);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            _logger.LogInformation("Label sent to printer successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send label to printer");
            return false;
        }
    }
}