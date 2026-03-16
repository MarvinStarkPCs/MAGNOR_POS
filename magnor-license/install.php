<?php
// ============================================
// MAGNOR POS - Instalador de Base de Datos
// Ejecutar UNA VEZ para crear las tablas
// ELIMINAR ESTE ARCHIVO DESPUÉS DE INSTALAR
// ============================================

require_once __DIR__ . '/config.php';

try {
    $pdo = getDB();

    // Crear tabla de licencias
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS licenses (
            id INT AUTO_INCREMENT PRIMARY KEY,
            license_key VARCHAR(30) NOT NULL UNIQUE,
            customer_name VARCHAR(200) NOT NULL DEFAULT '',
            customer_email VARCHAR(200) NOT NULL DEFAULT '',
            customer_phone VARCHAR(50) NOT NULL DEFAULT '',
            duration_days INT NOT NULL DEFAULT 7,
            hardware_id VARCHAR(64) DEFAULT NULL,
            activated_at DATETIME DEFAULT NULL,
            expires_at DATETIME DEFAULT NULL,
            is_active TINYINT(1) NOT NULL DEFAULT 1,
            notes TEXT DEFAULT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            INDEX idx_license_key (license_key),
            INDEX idx_hardware_id (hardware_id),
            INDEX idx_is_active (is_active)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    ");

    // Crear tabla de admin
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS admin_users (
            id INT AUTO_INCREMENT PRIMARY KEY,
            username VARCHAR(50) NOT NULL UNIQUE,
            password_hash VARCHAR(255) NOT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    ");

    // Crear tabla de log de activaciones
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS activation_log (
            id INT AUTO_INCREMENT PRIMARY KEY,
            license_key VARCHAR(30) NOT NULL,
            hardware_id VARCHAR(64) NOT NULL,
            action VARCHAR(20) NOT NULL,
            ip_address VARCHAR(45) DEFAULT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            INDEX idx_license_log (license_key)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    ");

    // Crear usuario admin por defecto (admin / admin123)
    $hash = password_hash('admin123', PASSWORD_BCRYPT);
    $stmt = $pdo->prepare("INSERT IGNORE INTO admin_users (username, password_hash) VALUES (?, ?)");
    $stmt->execute(['admin', $hash]);

    echo "<h2>✅ Instalación completada exitosamente</h2>";
    echo "<p>Tablas creadas: <strong>licenses</strong>, <strong>admin_users</strong>, <strong>activation_log</strong></p>";
    echo "<p>Usuario admin creado: <strong>admin</strong> / <strong>admin123</strong></p>";
    echo "<p style='color:red;font-weight:bold;'>⚠️ IMPORTANTE: Elimina este archivo (install.php) del servidor después de instalar.</p>";
    echo "<p><a href='admin/'>Ir al panel de administración →</a></p>";

} catch (PDOException $e) {
    echo "<h2>❌ Error de instalación</h2>";
    echo "<p>" . htmlspecialchars($e->getMessage()) . "</p>";
    echo "<p>Verifica la configuración en <code>config.php</code></p>";
}
