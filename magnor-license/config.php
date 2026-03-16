<?php
// ============================================
// MAGNOR POS - Sistema de Licencias
// Configuración del servidor
// ============================================

// --- Configuración de Base de Datos ---
define('DB_HOST', 'localhost');
define('DB_NAME', 'progr108_magnor_pos_licence');  // Cambiar al nombre de tu BD en cPanel
define('DB_USER', 'progr108_magnor_pos_licence');             // Cambiar al usuario de tu BD en cPanel
define('DB_PASS', 'kQ4#x)I#)S^L');                 // Cambiar a la contraseña de tu BD en cPanel

// --- Clave secreta para autenticar requests de la app ---
define('API_SECRET', 'CAMBIAR_ESTA_CLAVE_SECRETA_AQUI');

// --- Configuración general ---
define('LICENSE_PREFIX', 'MAGNOR');
date_default_timezone_set('America/Bogota');

// --- Conexión PDO ---
function getDB(): PDO {
    static $pdo = null;
    if ($pdo === null) {
        $dsn = "mysql:host=" . DB_HOST . ";dbname=" . DB_NAME . ";charset=utf8mb4";
        $pdo = new PDO($dsn, DB_USER, DB_PASS, [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
        ]);
    }
    return $pdo;
}

// --- Helpers ---
function jsonResponse(array $data, int $code = 200): void {
    http_response_code($code);
    header('Content-Type: application/json; charset=utf-8');
    echo json_encode($data, JSON_UNESCAPED_UNICODE);
    exit;
}

function generateLicenseKey(): string {
    $segments = [];
    $chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // Sin I,O,0,1 para evitar confusión
    for ($i = 0; $i < 4; $i++) {
        $segment = '';
        for ($j = 0; $j < 5; $j++) {
            $segment .= $chars[random_int(0, strlen($chars) - 1)];
        }
        $segments[] = $segment;
    }
    return LICENSE_PREFIX . '-' . implode('-', $segments);
}

function validateApiSecret(): void {
    $headers = getallheaders();
    $secret = $headers['X-Api-Secret'] ?? $headers['x-api-secret'] ?? '';
    if ($secret !== API_SECRET) {
        jsonResponse(['success' => false, 'message' => 'Acceso no autorizado'], 401);
    }
}
