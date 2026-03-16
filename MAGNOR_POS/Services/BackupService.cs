using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MAGNOR_POS.Data;
using Microsoft.EntityFrameworkCore;

namespace MAGNOR_POS.Services;

public class BackupService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private const string API_BASE_URL = "http://magnorposlicence.northcloud.com.co/api";
    private const string API_SECRET = "MG2026-S3CR3T-K3Y-P0S";

    /// <summary>
    /// Sincroniza todos los datos locales al servidor de backup.
    /// </summary>
    public static async Task<(bool Success, string Message, BackupStats? Stats)> SyncAllAsync()
    {
        // Get license key
        var license = LicenseService.GetLocalLicense();
        if (license == null)
        {
            return (false, "No hay licencia activa. Active una licencia primero.", null);
        }

        try
        {
            using var context = new AppDbContext();

            // Collect all data
            var customers = await context.Customers
                .Select(c => new
                {
                    local_id = c.Id,
                    full_name = c.FullName,
                    document_type = c.DocumentType.ToString(),
                    document_number = c.DocumentNumber ?? "",
                    phone = c.Phone ?? "",
                    email = c.Email ?? "",
                    address = c.Address ?? "",
                    city = c.City ?? "",
                    state = c.State ?? "",
                    postal_code = c.PostalCode ?? "",
                    customer_type = c.CustomerType.ToString(),
                    discount_percentage = c.DiscountPercentage,
                    credit_limit = c.CreditLimit,
                    notes = c.Notes ?? "",
                    is_active = c.IsActive
                }).ToListAsync();

            var products = await context.Products
                .Include(p => p.Category)
                .Select(p => new
                {
                    local_id = p.Id,
                    name = p.Name,
                    sku = p.SKU,
                    barcode = p.Barcode ?? "",
                    description = p.Description ?? "",
                    sale_price = p.SalePrice,
                    purchase_price = p.PurchasePrice,
                    current_stock = p.CurrentStock,
                    minimum_stock = p.MinimumStock,
                    tax_rate = p.TaxRate,
                    image_url = p.ImageUrl ?? "",
                    category_name = p.Category != null ? p.Category.Name : "",
                    is_active = p.IsActive
                }).ToListAsync();

            var sales = await context.Sales
                .Include(s => s.Details)
                    .ThenInclude(d => d.Product)
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Select(s => new
                {
                    local_id = s.Id,
                    sale_number = s.InvoiceNumber ?? "",
                    customer_name = s.Customer != null ? s.Customer.FullName : "Consumidor Final",
                    subtotal = s.Subtotal,
                    tax_amount = s.TaxAmount,
                    discount_amount = s.DiscountAmount,
                    total = s.Total,
                    payment_type = s.PaymentType.ToString(),
                    status = s.Status.ToString(),
                    sale_date = s.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    cashier_name = s.User != null ? s.User.FullName : "Admin",
                    notes = s.Notes ?? "",
                    details = s.Details.Select(d => new
                    {
                        local_id = d.Id,
                        product_name = d.Product != null ? d.Product.Name : "Producto",
                        quantity = d.Quantity,
                        unit_price = d.UnitPrice,
                        discount = d.DiscountAmount,
                        subtotal = d.Subtotal,
                        tax_amount = d.TaxAmount,
                        total = d.Total
                    }).ToList()
                }).ToListAsync();

            var suppliers = await context.Suppliers
                .Select(s => new
                {
                    local_id = s.Id,
                    company_name = s.CompanyName,
                    contact_name = s.ContactName,
                    document_type = s.DocumentType.ToString(),
                    document_number = s.DocumentNumber ?? "",
                    phone = s.Phone ?? "",
                    email = s.Email ?? "",
                    address = s.Address ?? "",
                    website = s.Website ?? "",
                    payment_term_days = s.PaymentTermDays,
                    notes = s.Notes ?? "",
                    is_active = s.IsActive
                }).ToListAsync();

            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Select(p => new
                {
                    local_id = p.Id,
                    purchase_number = p.PurchaseNumber ?? "",
                    supplier_name = p.Supplier != null ? p.Supplier.CompanyName : "",
                    subtotal = p.Subtotal,
                    tax_amount = p.TaxAmount,
                    discount_amount = p.DiscountAmount,
                    total = p.Total,
                    status = p.Status.ToString(),
                    purchase_date = p.PurchaseDate.ToString("yyyy-MM-dd"),
                    notes = p.Notes ?? "",
                    is_active = true
                }).ToListAsync();

            // Build payload
            var payload = JsonSerializer.Serialize(new
            {
                license_key = license.LicenseKey,
                customers,
                products,
                sales,
                suppliers,
                purchases
            });

            // Send to server
            var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/backup/sync")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Api-Secret", API_SECRET);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            bool success = result.GetProperty("success").GetBoolean();
            string message = result.TryGetProperty("message", out var msgEl)
                ? msgEl.GetString() ?? ""
                : "";

            BackupStats? stats = null;
            if (success && result.TryGetProperty("synced", out var syncedEl))
            {
                stats = new BackupStats
                {
                    Customers = syncedEl.TryGetProperty("customers", out var c) ? c.GetInt32() : 0,
                    Products = syncedEl.TryGetProperty("products", out var p) ? p.GetInt32() : 0,
                    Sales = syncedEl.TryGetProperty("sales", out var s) ? s.GetInt32() : 0,
                    Suppliers = syncedEl.TryGetProperty("suppliers", out var su) ? su.GetInt32() : 0,
                    Purchases = syncedEl.TryGetProperty("purchases", out var pu) ? pu.GetInt32() : 0
                };
            }

            return (success, message, stats);
        }
        catch (HttpRequestException)
        {
            return (false, "No se pudo conectar al servidor. Verifique su conexion a internet.", null);
        }
        catch (TaskCanceledException)
        {
            return (false, "Tiempo de espera agotado. Intente de nuevo.", null);
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Consulta el estado del último backup en el servidor.
    /// </summary>
    public static async Task<(bool Success, string Message, BackupStatusInfo? Info)> GetStatusAsync()
    {
        var license = LicenseService.GetLocalLicense();
        if (license == null)
        {
            return (false, "No hay licencia activa.", null);
        }

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                license_key = license.LicenseKey
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/backup/status")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Api-Secret", API_SECRET);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            bool success = result.GetProperty("success").GetBoolean();

            if (success)
            {
                var info = new BackupStatusInfo();

                if (result.TryGetProperty("last_sync", out var lastSync) &&
                    lastSync.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(lastSync.GetString(), out var parsed))
                        info.LastSync = parsed;
                }

                if (result.TryGetProperty("counts", out var counts))
                {
                    info.Customers = counts.TryGetProperty("customers", out var c) ? c.GetInt32() : 0;
                    info.Products = counts.TryGetProperty("products", out var p) ? p.GetInt32() : 0;
                    info.Sales = counts.TryGetProperty("sales", out var s) ? s.GetInt32() : 0;
                    info.Suppliers = counts.TryGetProperty("suppliers", out var su) ? su.GetInt32() : 0;
                    info.Purchases = counts.TryGetProperty("purchases", out var pu) ? pu.GetInt32() : 0;
                }

                return (true, "Estado obtenido", info);
            }

            string message = result.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
            return (false, message, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}", null);
        }
    }
}

public class BackupStats
{
    public int Customers { get; set; }
    public int Products { get; set; }
    public int Sales { get; set; }
    public int Suppliers { get; set; }
    public int Purchases { get; set; }
    public int Total => Customers + Products + Sales + Suppliers + Purchases;
}

public class BackupStatusInfo
{
    public DateTime? LastSync { get; set; }
    public int Customers { get; set; }
    public int Products { get; set; }
    public int Sales { get; set; }
    public int Suppliers { get; set; }
    public int Purchases { get; set; }
    public int Total => Customers + Products + Sales + Suppliers + Purchases;
}
