<?php
// POST /api/validate.php
// Body JSON: { "license_key": "MAGNOR-XXXXX-...", "hardware_id": "sha256hash" }
// Header: X-Api-Secret

require_once __DIR__ . '/../config.php';

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    jsonResponse(['success' => false, 'message' => 'Método no permitido'], 405);
}

validateApiSecret();

$input = json_decode(file_get_contents('php://input'), true);
$licenseKey = trim($input['license_key'] ?? '');
$hardwareId = trim($input['hardware_id'] ?? '');

if (empty($licenseKey) || empty($hardwareId)) {
    jsonResponse(['success' => false, 'message' => 'Clave de licencia y hardware ID son requeridos'], 400);
}

try {
    $pdo = getDB();

    $stmt = $pdo->prepare("SELECT * FROM licenses WHERE license_key = ? AND hardware_id = ?");
    $stmt->execute([$licenseKey, $hardwareId]);
    $license = $stmt->fetch();

    if (!$license) {
        jsonResponse(['success' => false, 'message' => 'Licencia no encontrada o no activada en esta máquina'], 404);
    }

    if (!$license['is_active']) {
        jsonResponse(['success' => false, 'message' => 'Licencia desactivada'], 403);
    }

    // Verificar expiración
    if ($license['expires_at'] !== null && strtotime($license['expires_at']) < time()) {
        // Marcar como inactiva automáticamente
        $stmt = $pdo->prepare("UPDATE licenses SET is_active = 0 WHERE id = ?");
        $stmt->execute([$license['id']]);

        jsonResponse([
            'success' => false,
            'message' => 'Su licencia ha expirado el ' . date('d/m/Y', strtotime($license['expires_at'])) . '. Contacte al administrador para renovarla.',
            'expired' => true
        ], 403);
    }

    // Calcular días restantes
    $daysLeft = null;
    if ($license['expires_at'] !== null) {
        $daysLeft = (int)ceil((strtotime($license['expires_at']) - time()) / 86400);
    }

    jsonResponse([
        'success' => true,
        'message' => 'Licencia válida',
        'license' => [
            'key' => $license['license_key'],
            'customer' => $license['customer_name'],
            'activated_at' => $license['activated_at'],
            'expires_at' => $license['expires_at'],
            'days_left' => $daysLeft,
            'is_active' => true,
        ]
    ]);

} catch (PDOException $e) {
    jsonResponse(['success' => false, 'message' => 'Error del servidor'], 500);
}
