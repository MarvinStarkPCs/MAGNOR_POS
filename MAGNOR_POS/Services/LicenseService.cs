using System.IO;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MAGNOR_POS.Services
{
    public class LicenseInfo
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string HardwareId { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public DateTime ActivatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; } // null = permanente
        public DateTime LastValidated { get; set; }
        public bool IsPermanent => ExpiresAt == null;
        public bool IsExpired => ExpiresAt != null && DateTime.Now > ExpiresAt;
        public int? DaysLeft => ExpiresAt != null ? Math.Max(0, (int)Math.Ceiling((ExpiresAt.Value - DateTime.Now).TotalDays)) : null;
    }

    public class LicenseService
    {
        // ⚠️ CAMBIAR ESTA URL a tu dominio en cPanel
        private const string API_BASE_URL = "https://tudominio.com/magnor-license/api";
        private const string API_SECRET = "CAMBIAR_ESTA_CLAVE_SECRETA_AQUI";
        private const int OFFLINE_GRACE_DAYS = 7; // Días permitidos sin validar online

        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private static readonly string _licenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MAGNOR_POS",
            "license.dat"
        );

        /// <summary>
        /// Genera un ID único del hardware usando CPU, placa base y disco.
        /// </summary>
        public static string GetHardwareId()
        {
            var components = new StringBuilder();

            // CPU ID
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    components.Append(obj["ProcessorId"]?.ToString() ?? "");
                    break;
                }
            }
            catch { components.Append("CPU_UNKNOWN"); }

            // Motherboard Serial
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (var obj in searcher.Get())
                {
                    components.Append(obj["SerialNumber"]?.ToString() ?? "");
                    break;
                }
            }
            catch { components.Append("MB_UNKNOWN"); }

            // Disk Serial (disco del sistema)
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0");
                foreach (var obj in searcher.Get())
                {
                    components.Append(obj["SerialNumber"]?.ToString()?.Trim() ?? "");
                    break;
                }
            }
            catch { components.Append("DISK_UNKNOWN"); }

            // Hash SHA256
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(components.ToString()));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /// <summary>
        /// Activa una licencia en el servidor.
        /// </summary>
        public static async Task<(bool Success, string Message)> ActivateLicenseAsync(string licenseKey)
        {
            var hardwareId = GetHardwareId();

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    license_key = licenseKey.Trim().ToUpperInvariant(),
                    hardware_id = hardwareId
                });

                var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/activate.php")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Api-Secret", API_SECRET);

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);

                bool success = result.GetProperty("success").GetBoolean();
                string message = result.GetProperty("message").GetString() ?? "";

                if (success)
                {
                    // Leer fecha de expiración del servidor
                    DateTime? expiresAt = null;
                    if (result.TryGetProperty("license", out var licData) &&
                        licData.TryGetProperty("expires_at", out var expiresEl) &&
                        expiresEl.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(expiresEl.GetString(), out var parsed))
                            expiresAt = parsed;
                    }

                    var licenseInfo = new LicenseInfo
                    {
                        LicenseKey = licenseKey.Trim().ToUpperInvariant(),
                        HardwareId = hardwareId,
                        Customer = licData.ValueKind != JsonValueKind.Undefined
                            ? licData.GetProperty("customer").GetString() ?? ""
                            : "",
                        ActivatedAt = DateTime.Now,
                        ExpiresAt = expiresAt,
                        LastValidated = DateTime.Now
                    };
                    SaveLicenseLocal(licenseInfo);
                }

                return (success, message);
            }
            catch (HttpRequestException)
            {
                return (false, "No se pudo conectar al servidor de licencias. Verifique su conexión a internet.");
            }
            catch (TaskCanceledException)
            {
                return (false, "Tiempo de espera agotado al conectar con el servidor.");
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si la licencia es válida (online con fallback offline).
        /// </summary>
        public static async Task<(bool IsValid, string Message)> ValidateLicenseAsync()
        {
            var localLicense = LoadLicenseLocal();
            if (localLicense == null)
            {
                return (false, "No hay licencia activada");
            }

            // Verificar expiración local primero
            if (localLicense.IsExpired)
            {
                DeleteLicenseLocal();
                return (false, $"Su licencia expiró el {localLicense.ExpiresAt:dd/MM/yyyy}. Contacte al administrador para renovarla.");
            }

            // Intentar validar online
            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    license_key = localLicense.LicenseKey,
                    hardware_id = localLicense.HardwareId
                });

                var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/validate.php")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Api-Secret", API_SECRET);

                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);

                bool success = result.GetProperty("success").GetBoolean();
                string message = result.GetProperty("message").GetString() ?? "";

                if (success)
                {
                    // Actualizar fecha de última validación y expiración del servidor
                    localLicense.LastValidated = DateTime.Now;

                    if (result.TryGetProperty("license", out var licData) &&
                        licData.TryGetProperty("expires_at", out var expiresEl) &&
                        expiresEl.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(expiresEl.GetString(), out var parsed))
                            localLicense.ExpiresAt = parsed;
                    }

                    SaveLicenseLocal(localLicense);

                    // Mensaje con días restantes
                    string msg = "Licencia válida";
                    if (localLicense.DaysLeft != null)
                        msg += $" ({localLicense.DaysLeft} días restantes)";
                    else
                        msg += " (permanente)";

                    return (true, msg);
                }
                else
                {
                    // Licencia rechazada por el servidor -> eliminar local
                    DeleteLicenseLocal();
                    return (false, message);
                }
            }
            catch (Exception)
            {
                // Sin conexión -> validar offline con gracia
                var daysSinceValidation = (DateTime.Now - localLicense.LastValidated).TotalDays;

                if (daysSinceValidation <= OFFLINE_GRACE_DAYS)
                {
                    return (true, $"Licencia válida (modo offline, {OFFLINE_GRACE_DAYS - (int)daysSinceValidation} días restantes)");
                }
                else
                {
                    return (false, "La licencia requiere validación en línea. Conéctese a internet e intente de nuevo.");
                }
            }
        }

        /// <summary>
        /// Verifica si hay una licencia guardada localmente.
        /// </summary>
        public static bool HasLocalLicense()
        {
            return File.Exists(_licenseFilePath);
        }

        /// <summary>
        /// Obtiene la licencia local guardada.
        /// </summary>
        public static LicenseInfo? GetLocalLicense()
        {
            return LoadLicenseLocal();
        }

        // --- Persistencia local ---

        private static void SaveLicenseLocal(LicenseInfo info)
        {
            var dir = Path.GetDirectoryName(_licenseFilePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(info);
            // Encriptar con DPAPI (solo se puede leer en esta máquina/usuario)
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(json),
                Encoding.UTF8.GetBytes("MAGNOR_POS_LICENSE"),
                DataProtectionScope.CurrentUser
            );
            File.WriteAllBytes(_licenseFilePath, encrypted);
        }

        private static LicenseInfo? LoadLicenseLocal()
        {
            if (!File.Exists(_licenseFilePath))
                return null;

            try
            {
                var encrypted = File.ReadAllBytes(_licenseFilePath);
                var decrypted = ProtectedData.Unprotect(
                    encrypted,
                    Encoding.UTF8.GetBytes("MAGNOR_POS_LICENSE"),
                    DataProtectionScope.CurrentUser
                );
                var json = Encoding.UTF8.GetString(decrypted);
                return JsonSerializer.Deserialize<LicenseInfo>(json);
            }
            catch
            {
                // Archivo corrupto o de otra máquina
                return null;
            }
        }

        private static void DeleteLicenseLocal()
        {
            if (File.Exists(_licenseFilePath))
                File.Delete(_licenseFilePath);
        }
    }
}
