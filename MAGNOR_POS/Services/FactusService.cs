using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MAGNOR_POS.Models.Enums;
using MAGNOR_POS.Models.Sales;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for integrating with Factus electronic invoicing API (DIAN Colombia)
/// </summary>
public class FactusService
{
    private static readonly HttpClient _httpClient = new();

    // Sandbox environment
    private const string SANDBOX_URL = "https://api-sandbox.factus.com.co";
    private const string PRODUCTION_URL = "https://api.factus.com.co";

    private string _accessToken = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;

    // Configuration - loaded from settings
    public string BaseUrl { get; set; } = SANDBOX_URL;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Authenticate with Factus OAuth and get access token
    /// </summary>
    public async Task<(bool Success, string Message)> AuthenticateAsync()
    {
        try
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "username", Username },
                { "password", Password }
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/oauth/token");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"Error de autenticación Factus: {response.StatusCode} - {json}");
            }

            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);
            _accessToken = tokenResponse.GetProperty("access_token").GetString() ?? "";
            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.Now.AddSeconds(expiresIn - 60); // 1 min buffer

            return (true, "Autenticación exitosa con Factus");
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión con Factus: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensure we have a valid token, refresh if expired
    /// </summary>
    private async Task<bool> EnsureAuthenticatedAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiry)
            return true;

        var result = await AuthenticateAsync();
        return result.Success;
    }

    /// <summary>
    /// Create and validate an electronic invoice with Factus (DIAN)
    /// </summary>
    public async Task<FactusInvoiceResult> CreateInvoiceAsync(Sale sale, string companyName = "", string companyNit = "")
    {
        var result = new FactusInvoiceResult();

        if (!IsEnabled)
        {
            result.Success = false;
            result.Message = "Facturación electrónica no está habilitada";
            return result;
        }

        try
        {
            if (!await EnsureAuthenticatedAsync())
            {
                result.Success = false;
                result.Message = "No se pudo autenticar con Factus";
                return result;
            }

            // Build invoice payload
            var invoice = BuildInvoicePayload(sale);
            var jsonPayload = JsonSerializer.Serialize(invoice, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/bills/validate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Extract key fields from response
                if (responseData.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("bill", out var bill))
                    {
                        result.CUFE = bill.TryGetProperty("cufe", out var cufe) ? cufe.GetString() : null;
                        result.QRCode = bill.TryGetProperty("qr_code", out var qr) ? qr.GetString() : null;
                        result.FactusNumber = bill.TryGetProperty("number", out var num) ? num.GetString() : null;
                        result.FactusPrefix = bill.TryGetProperty("prefix", out var prefix) ? prefix.GetString() : null;
                        result.Status = bill.TryGetProperty("status", out var status) ? status.GetString() : null;
                    }
                }

                result.Success = true;
                result.Message = $"Factura electrónica {result.FactusPrefix}{result.FactusNumber} creada exitosamente";
                result.FullResponse = responseJson;
            }
            else
            {
                result.Success = false;
                result.Message = $"Error Factus ({(int)response.StatusCode}): {responseJson}";
                result.FullResponse = responseJson;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error al crear factura electrónica: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Build the Factus invoice JSON payload from a Sale
    /// </summary>
    private Dictionary<string, object> BuildInvoicePayload(Sale sale)
    {
        var payload = new Dictionary<string, object>
        {
            { "document", "01" }, // 01 = Factura de venta
            { "reference_code", sale.InvoiceNumber },
            { "observation", sale.Notes ?? $"Venta POS {sale.InvoiceNumber}" },
            { "payment_form", GetFactusPaymentForm(sale.PaymentType) },
            { "payment_method_code", GetFactusPaymentMethodCode(sale.PaymentType) },
            { "operation_type", "10" }, // Standard
            { "send_email", false }
        };

        // If credit payment, add due date
        if (sale.PaymentType == PaymentType.Transferencia)
        {
            payload["payment_due_date"] = sale.SaleDate.AddDays(30).ToString("yyyy-MM-dd");
        }

        // Customer data
        var customer = BuildCustomerPayload(sale.Customer);
        payload["customer"] = customer;

        // Items
        var items = new List<Dictionary<string, object>>();
        foreach (var detail in sale.Details)
        {
            items.Add(BuildItemPayload(detail));
        }
        payload["items"] = items;

        return payload;
    }

    /// <summary>
    /// Build customer section for Factus payload
    /// </summary>
    private Dictionary<string, object?> BuildCustomerPayload(Models.Parties.Customer? customer)
    {
        if (customer == null)
        {
            // Consumidor final (generic consumer)
            return new Dictionary<string, object?>
            {
                { "identification_document_id", 6 }, // NIT
                { "identification", "222222222222" }, // Consumidor final
                { "dv", "1" },
                { "company", "Consumidor Final" },
                { "names", null },
                { "address", "N/A" },
                { "email", "consumidor@ejemplo.com" },
                { "phone", "0000000" },
                { "legal_organization_id", 2 }, // Persona Natural
                { "tribute_id", 21 }, // No responsable de IVA
                { "municipality_id", 1006 } // Bogotá default
            };
        }

        var isCompany = customer.DocumentType == DocumentType.NIT;
        var customerPayload = new Dictionary<string, object?>
        {
            { "identification_document_id", GetFactusDocumentTypeId(customer.DocumentType) },
            { "identification", customer.DocumentNumber ?? "0000000" },
            { "names", isCompany ? null : customer.FullName },
            { "company", isCompany ? customer.FullName : null },
            { "address", customer.Address ?? "N/A" },
            { "email", customer.Email ?? "sin@email.com" },
            { "phone", customer.Phone ?? "0000000" },
            { "legal_organization_id", isCompany ? 1 : 2 }, // 1=Jurídica, 2=Natural
            { "tribute_id", isCompany ? 1 : 21 }, // 1=IVA responsable, 21=No responsable
            { "municipality_id", 1006 } // Default Bogotá - TODO: map from customer city
        };

        // DV only for NIT
        if (customer.DocumentType == DocumentType.NIT)
        {
            customerPayload["dv"] = CalculateDV(customer.DocumentNumber ?? "");
        }

        return customerPayload;
    }

    /// <summary>
    /// Build item (product line) for Factus payload
    /// </summary>
    private Dictionary<string, object> BuildItemPayload(SaleDetail detail)
    {
        // Factus expects price WITH tax included
        var priceWithTax = detail.UnitPrice * (1 + detail.TaxRate);

        return new Dictionary<string, object>
        {
            { "code_reference", detail.Product?.SKU ?? detail.ProductId.ToString() },
            { "name", detail.Product?.Name ?? $"Producto {detail.ProductId}" },
            { "quantity", (int)detail.Quantity },
            { "price", Math.Round(priceWithTax, 2) },
            { "tax_rate", Math.Round(detail.TaxRate * 100, 2).ToString("F2") }, // Convert 0.19 to "19.00"
            { "discount_rate", Math.Round(detail.DiscountPercentage, 2).ToString("F2") },
            { "unit_measure_id", GetFactusUnitMeasureId(detail.Product?.UnitOfMeasure ?? UnitOfMeasure.Unidad) },
            { "standard_code_id", 1 }, // Standard adoption code
            { "is_excluded", 0 }, // Not IVA excluded
            { "tribute_id", 1 } // IVA
        };
    }

    #region Factus Code Mappings

    /// <summary>
    /// Map DocumentType enum to Factus identification_document_id
    /// </summary>
    private static int GetFactusDocumentTypeId(DocumentType? docType) => docType switch
    {
        DocumentType.CedulaCiudadania => 3,  // CC
        DocumentType.NIT => 6,               // NIT
        DocumentType.CedulaExtranjeria => 2, // CE
        DocumentType.Pasaporte => 4,         // Pasaporte
        DocumentType.TarjetaIdentidad => 3,  // TI → CC
        DocumentType.RegistroCivil => 3,     // RC → CC
        _ => 3                               // Default CC
    };

    /// <summary>
    /// Map PaymentType to Factus payment_form
    /// </summary>
    private static string GetFactusPaymentForm(PaymentType paymentType) => paymentType switch
    {
        PaymentType.Efectivo => "1",       // Contado
        PaymentType.Tarjeta => "1",        // Contado
        PaymentType.Transferencia => "2",  // Crédito
        PaymentType.Mixto => "1",          // Contado
        _ => "1"
    };

    /// <summary>
    /// Map PaymentType to Factus payment_method_code
    /// </summary>
    private static string GetFactusPaymentMethodCode(PaymentType paymentType) => paymentType switch
    {
        PaymentType.Efectivo => "10",       // Efectivo
        PaymentType.Tarjeta => "48",        // Tarjeta crédito
        PaymentType.Transferencia => "42",  // Consignación bancaria
        PaymentType.Mixto => "10",          // Efectivo (default)
        _ => "10"
    };

    /// <summary>
    /// Map UnitOfMeasure to Factus unit_measure_id
    /// </summary>
    private static int GetFactusUnitMeasureId(UnitOfMeasure unit) => unit switch
    {
        UnitOfMeasure.Unidad => 70,      // Unidad
        UnitOfMeasure.Kilogramo => 2,    // Kilogramo
        UnitOfMeasure.Gramo => 3,        // Gramo
        UnitOfMeasure.Litro => 4,        // Litro
        UnitOfMeasure.Mililitro => 5,    // Mililitro
        UnitOfMeasure.Metro => 9,        // Metro
        UnitOfMeasure.Caja => 70,        // Unidad (approx)
        UnitOfMeasure.Paquete => 70,     // Unidad (approx)
        UnitOfMeasure.Docena => 70,      // Unidad (approx)
        _ => 70
    };

    /// <summary>
    /// Calculate DV (dígito de verificación) for Colombian NIT
    /// </summary>
    private static string CalculateDV(string nit)
    {
        if (string.IsNullOrWhiteSpace(nit))
            return "0";

        // Clean NIT - only digits
        var cleanNit = new string(nit.Where(char.IsDigit).ToArray());
        if (cleanNit.Length == 0)
            return "0";

        int[] primes = { 71, 67, 59, 53, 47, 43, 41, 37, 29, 23, 19, 17, 13, 7, 3 };
        var digits = cleanNit.PadLeft(15, '0').Select(c => c - '0').ToArray();

        int sum = 0;
        for (int i = 0; i < 15; i++)
        {
            sum += digits[i] * primes[i];
        }

        int remainder = sum % 11;
        return remainder > 1 ? (11 - remainder).ToString() : remainder.ToString();
    }

    #endregion

    /// <summary>
    /// Test the connection to Factus API
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret) ||
            string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            return (false, "Faltan credenciales de Factus. Configure Client ID, Client Secret, Usuario y Contraseña.");
        }

        BaseUrl = UseSandbox ? SANDBOX_URL : PRODUCTION_URL;
        return await AuthenticateAsync();
    }
}

/// <summary>
/// Result from Factus invoice creation
/// </summary>
public class FactusInvoiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CUFE { get; set; }
    public string? QRCode { get; set; }
    public string? FactusNumber { get; set; }
    public string? FactusPrefix { get; set; }
    public string? Status { get; set; }
    public string? FullResponse { get; set; }
}
