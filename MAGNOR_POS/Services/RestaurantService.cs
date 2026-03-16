using Microsoft.EntityFrameworkCore;
using MAGNOR_POS.Data;
using MAGNOR_POS.Models.Restaurant;
using MAGNOR_POS.Models.Enums;

namespace MAGNOR_POS.Services;

/// <summary>
/// Service for managing restaurant zones, tables, and orders
/// </summary>
public class RestaurantService
{
    private readonly AppDbContext _context;

    public RestaurantService(AppDbContext context)
    {
        _context = context;
    }

    #region Zones Management

    /// <summary>
    /// Get all restaurant zones with their tables
    /// </summary>
    public async Task<List<RestaurantZone>> GetAllZonesAsync()
    {
        return await _context.RestaurantZones
            .Include(z => z.Tables)
            .Where(z => z.IsActive)
            .OrderBy(z => z.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Get zone by ID
    /// </summary>
    public async Task<RestaurantZone?> GetZoneByIdAsync(int id)
    {
        return await _context.RestaurantZones
            .Include(z => z.Tables)
            .FirstOrDefaultAsync(z => z.Id == id);
    }

    /// <summary>
    /// Create a new zone
    /// </summary>
    public async Task<(bool success, string message, RestaurantZone? zone)> AddZoneAsync(RestaurantZone zone, int createdByUserId)
    {
        try
        {
            // Validate zone name is unique
            var existingZone = await _context.RestaurantZones
                .FirstOrDefaultAsync(z => z.Name == zone.Name && z.IsActive);

            if (existingZone != null)
            {
                return (false, "Ya existe una zona con este nombre", null);
            }

            // Set audit fields
            zone.CreatedAt = DateTime.Now;
            zone.CreatedBy = createdByUserId;
            zone.IsActive = true;

            _context.RestaurantZones.Add(zone);
            await _context.SaveChangesAsync();

            return (true, "Zona creada exitosamente", zone);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear la zona: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing zone
    /// </summary>
    public async Task<(bool success, string message)> UpdateZoneAsync(RestaurantZone zone, int updatedByUserId)
    {
        try
        {
            var existingZone = await _context.RestaurantZones.FindAsync(zone.Id);
            if (existingZone == null)
            {
                return (false, "Zona no encontrada");
            }

            // Validate name is unique (excluding current zone)
            var duplicateName = await _context.RestaurantZones
                .FirstOrDefaultAsync(z => z.Name == zone.Name && z.Id != zone.Id && z.IsActive);

            if (duplicateName != null)
            {
                return (false, "Ya existe una zona con este nombre");
            }

            // Update fields
            existingZone.Name = zone.Name;
            existingZone.Description = zone.Description;
            existingZone.UpdatedAt = DateTime.Now;
            existingZone.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Zona actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar la zona: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a zone (soft delete)
    /// </summary>
    public async Task<(bool success, string message)> DeleteZoneAsync(int zoneId, int deletedByUserId)
    {
        try
        {
            var zone = await _context.RestaurantZones
                .Include(z => z.Tables)
                .FirstOrDefaultAsync(z => z.Id == zoneId);

            if (zone == null)
            {
                return (false, "Zona no encontrada");
            }

            // Check if zone has active tables
            if (zone.Tables.Any(t => t.IsActive))
            {
                return (false, "No se puede eliminar una zona con mesas activas. Elimine primero las mesas.");
            }

            // Soft delete
            zone.IsActive = false;
            zone.UpdatedAt = DateTime.Now;
            zone.UpdatedBy = deletedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Zona eliminada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar la zona: {ex.Message}");
        }
    }

    #endregion

    #region Tables Management

    /// <summary>
    /// Get all tables with their zones
    /// </summary>
    public async Task<List<RestaurantTable>> GetAllTablesAsync()
    {
        return await _context.RestaurantTables
            .Include(t => t.Zone)
            .Where(t => t.IsActive)
            .OrderBy(t => t.Zone!.Name)
            .ThenBy(t => t.TableNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Get tables by zone
    /// </summary>
    public async Task<List<RestaurantTable>> GetTablesByZoneAsync(int zoneId)
    {
        return await _context.RestaurantTables
            .Where(t => t.ZoneId == zoneId && t.IsActive)
            .OrderBy(t => t.TableNumber)
            .ToListAsync();
    }

    /// <summary>
    /// Get table by ID
    /// </summary>
    public async Task<RestaurantTable?> GetTableByIdAsync(int id)
    {
        return await _context.RestaurantTables
            .Include(t => t.Zone)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Create a new table
    /// </summary>
    public async Task<(bool success, string message, RestaurantTable? table)> AddTableAsync(RestaurantTable table, int createdByUserId)
    {
        try
        {
            // Validate zone exists
            var zone = await _context.RestaurantZones.FindAsync(table.ZoneId);
            if (zone == null || !zone.IsActive)
            {
                return (false, "La zona seleccionada no existe o está inactiva", null);
            }

            // Validate table number is unique within the zone
            var existingTable = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableNumber == table.TableNumber && t.ZoneId == table.ZoneId && t.IsActive);

            if (existingTable != null)
            {
                return (false, "Ya existe una mesa con este número en esta zona", null);
            }

            // Set audit fields
            table.CreatedAt = DateTime.Now;
            table.CreatedBy = createdByUserId;
            table.IsActive = true;
            table.Status = TableStatus.Disponible;

            _context.RestaurantTables.Add(table);
            await _context.SaveChangesAsync();

            // Reload with zone
            await _context.Entry(table).Reference(t => t.Zone).LoadAsync();

            return (true, "Mesa creada exitosamente", table);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear la mesa: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Update an existing table
    /// </summary>
    public async Task<(bool success, string message)> UpdateTableAsync(RestaurantTable table, int updatedByUserId)
    {
        try
        {
            var existingTable = await _context.RestaurantTables.FindAsync(table.Id);
            if (existingTable == null)
            {
                return (false, "Mesa no encontrada");
            }

            // Validate zone exists
            var zone = await _context.RestaurantZones.FindAsync(table.ZoneId);
            if (zone == null || !zone.IsActive)
            {
                return (false, "La zona seleccionada no existe o está inactiva");
            }

            // Validate table number is unique within the zone (excluding current table)
            var duplicateNumber = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableNumber == table.TableNumber &&
                                         t.ZoneId == table.ZoneId &&
                                         t.Id != table.Id &&
                                         t.IsActive);

            if (duplicateNumber != null)
            {
                return (false, "Ya existe una mesa con este número en esta zona");
            }

            // Update fields
            existingTable.TableNumber = table.TableNumber;
            existingTable.ZoneId = table.ZoneId;
            existingTable.Capacity = table.Capacity;
            existingTable.Status = table.Status;
            existingTable.UpdatedAt = DateTime.Now;
            existingTable.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Mesa actualizada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar la mesa: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete a table (soft delete)
    /// </summary>
    public async Task<(bool success, string message)> DeleteTableAsync(int tableId, int deletedByUserId)
    {
        try
        {
            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table == null)
            {
                return (false, "Mesa no encontrada");
            }

            // Check if table is occupied
            if (table.Status == TableStatus.Ocupada)
            {
                return (false, "No se puede eliminar una mesa ocupada");
            }

            // Soft delete
            table.IsActive = false;
            table.UpdatedAt = DateTime.Now;
            table.UpdatedBy = deletedByUserId;

            await _context.SaveChangesAsync();

            return (true, "Mesa eliminada exitosamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar la mesa: {ex.Message}");
        }
    }

    /// <summary>
    /// Change table status
    /// </summary>
    public async Task<(bool success, string message)> ChangeTableStatusAsync(int tableId, TableStatus newStatus)
    {
        try
        {
            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table == null)
            {
                return (false, "Mesa no encontrada");
            }

            table.Status = newStatus;
            table.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "Estado de mesa actualizado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al cambiar estado: {ex.Message}");
        }
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get table statistics
    /// </summary>
    public async Task<(int total, int available, int occupied, int reserved)> GetTableStatisticsAsync()
    {
        var tables = await _context.RestaurantTables
            .Where(t => t.IsActive)
            .ToListAsync();

        var total = tables.Count;
        var available = tables.Count(t => t.Status == TableStatus.Disponible);
        var occupied = tables.Count(t => t.Status == TableStatus.Ocupada);
        var reserved = tables.Count(t => t.Status == TableStatus.Reservada);

        return (total, available, occupied, reserved);
    }

    #endregion
}
