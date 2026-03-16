<?php
// POST /api/deactivate.php
// Body JSON: { "license_key": "MAGNOR-XXXXX-..." }
// Header: X-Api-Secret
// Desvincula el hardware_id para poder reusar la licencia en otra máquina

require_once __DIR__ . '/../config.php';

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    jsonResponse(['success' => false, 'message' => 'Método no permitido'], 405);
}

validateApiSecret();

$input = json_decode(file_get_contents('php://input'), true);
$licenseKey = trim($input['license_key'] ?? '');

if (empty($licenseKey)) {
    jsonResponse(['success' => false, 'message' => 'Clave de licencia requerida'], 400);
}

try {
    $pdo = getDB();

    $stmt = $pdo->prepare("SELECT * FROM licenses WHERE license_key = ?");
    $stmt->execute([$licenseKey]);
    $license = $stmt->fetch();

    if (!$license) {
        jsonResponse(['success' => false, 'message' => 'Licencia no encontrada'], 404);
    }

    // Desvincular hardware
    $stmt = $pdo->prepare("UPDATE licenses SET hardware_id = NULL, activated_at = NULL WHERE id = ?");
    $stmt->execute([$license['id']]);

    // Log
    $stmt = $pdo->prepare("INSERT INTO activation_log (license_key, hardware_id, action, ip_address) VALUES (?, ?, 'deactivate', ?)");
    $stmt->execute([$licenseKey, $license['hardware_id'] ?? '', $_SERVER['REMOTE_ADDR'] ?? '']);

    jsonResponse(['success' => true, 'message' => 'Licencia desvinculada exitosamente']);

} catch (PDOException $e) {
    jsonResponse(['success' => false, 'message' => 'Error del servidor'], 500);
}
