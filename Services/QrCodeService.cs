using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace PlantApp.Services;

public class QrCodeService
{
    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateQrCode(string data)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(10);
    }

    public async Task<byte[]> GenerateQrCodeAsync(string data)
    {
        return await Task.Run(() => GenerateQrCode(data));
    }
    
    public string GenerateQrCodeBase64(string data)
    {
        var qrCodeBytes = GenerateQrCode(data);
        return Convert.ToBase64String(qrCodeBytes);
    }

    public string GeneratePlantQrCode(Guid plantId, DateTime sowingDate)
    {
        return $"S{sowingDate:yyyy-MM-dd}_{plantId.ToString().Substring(0, 8)}";
    }

    public (bool IsValid, Guid? PlantId, DateTime? SowingDate) ParsePlantQrCode(string qrCode)
    {
        try
        {
            if (string.IsNullOrEmpty(qrCode) || !qrCode.StartsWith("S"))
                return (false, null, null);

            var parts = qrCode.Substring(1).Split('_');
            if (parts.Length != 2)
                return (false, null, null);

            if (!DateTime.TryParse(parts[0], out var sowingDate))
                return (false, null, null);

            var shortGuid = parts[1];
            if (shortGuid.Length < 8)
                return (false, null, null);

            return (true, null, sowingDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing QR code: {QrCode}", qrCode);
            return (false, null, null);
        }
    }
}