namespace PlantApp.Models;

public class ManufacturingOrder
{
    public int Id { get; set; }
    public string order_key { get; set; } = string.Empty;
    public string product_ref { get; set; } = string.Empty;
    public string product_name { get; set; } = string.Empty;
    public int product_id { get; set; }
    public double product_qty { get; set; }
    public string state { get; set; } = string.Empty;
    public string? partner_name { get; set; }
    public int partner_id { get; set; }
    public string? customer_language { get; set; }
    public DateTime? date_planned_start { get; set; }
    public DateTime? date_planned_finished { get; set; }
    public string? operator_name { get; set; }
    public List<Component> components { get; set; } = new();
    
    // Convenience properties for UI binding (PascalCase)
    public string OrderKey => order_key;
    public string ProductRef => product_ref;
    public string ProductName => product_name;
    public int ProductId => product_id;
    public double ProductQty => product_qty;
    public string State => state;
    public string? PartnerName => partner_name;
    public string? CustomerLanguage => customer_language;
    public DateTime? DatePlanned => date_planned_start;
    public List<Component> Components => components;
}

public class Component
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
}