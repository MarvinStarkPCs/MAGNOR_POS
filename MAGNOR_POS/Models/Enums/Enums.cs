namespace MAGNOR_POS.Models.Enums;

/// <summary>
/// Payment types for sales
/// </summary>
public enum PaymentType
{
    Efectivo = 1,
    Tarjeta = 2,
    Transferencia = 3,
    Mixto = 4
}

/// <summary>
/// Sale status
/// </summary>
public enum SaleStatus
{
    Pendiente = 1,
    Completada = 2,
    Cancelada = 3,
    Anulada = 4
}

/// <summary>
/// Customer types
/// </summary>
public enum CustomerType
{
    Minorista = 1,
    Mayorista = 2,
    Corporativo = 3
}

/// <summary>
/// Document types for identification (Colombia)
/// </summary>
public enum DocumentType
{
    CedulaCiudadania = 1,  // Cédula de Ciudadanía (CC)
    NIT = 2,               // Número de Identificación Tributaria
    CedulaExtranjeria = 3, // Cédula de Extranjería (CE)
    Pasaporte = 4,         // Pasaporte
    TarjetaIdentidad = 5,  // Tarjeta de Identidad (TI) - Menores de edad
    RegistroCivil = 6      // Registro Civil - Menores de 7 años
}

/// <summary>
/// Inventory movement types
/// </summary>
public enum InventoryMovementType
{
    Entrada = 1,
    Salida = 2,
    Ajuste = 3,
    Compra = 4,
    Venta = 5,
    Devolucion = 6,
    Transferencia = 7
}

/// <summary>
/// Purchase order status
/// </summary>
public enum PurchaseStatus
{
    Pendiente = 1,
    Recibida = 2,
    Parcial = 3,
    Cancelada = 4
}

/// <summary>
/// Cash register status
/// </summary>
public enum CashRegisterStatus
{
    Abierta = 1,
    Cerrada = 2,
    Arqueo = 3
}

/// <summary>
/// Table status for restaurant module
/// </summary>
public enum TableStatus
{
    Disponible = 1,
    Ocupada = 2,
    Reservada = 3,
    Limpieza = 4
}

/// <summary>
/// Order status for restaurant module
/// </summary>
public enum OrderStatus
{
    Pendiente = 1,
    EnPreparacion = 2,
    Lista = 3,
    Entregada = 4,
    Cancelada = 5
}

/// <summary>
/// Discount types
/// </summary>
public enum DiscountType
{
    Porcentaje = 1,
    MontoFijo = 2
}

/// <summary>
/// Unit of measure
/// </summary>
public enum UnitOfMeasure
{
    Unidad = 1,
    Kilogramo = 2,
    Gramo = 3,
    Litro = 4,
    Mililitro = 5,
    Metro = 6,
    Caja = 7,
    Paquete = 8,
    Docena = 9
}
