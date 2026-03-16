<?php
// POST /api/activate.php
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

    // Buscar la licencia
    $stmt = $pdo->prepare("SELECT * FROM licenses WHERE license_key = ?");
    $stmt->execute([$licenseKey]);
    $license = $stmt->fetch();

    if (!$license) {
        jsonResponse(['success' => false, 'message' => 'Clave de licencia no válida'], 404);
    }

    if (!$license['is_active']) {
        jsonResponse(['success' => false, 'message' => 'Esta licencia ha sido desactivada. Contacte al administrador'], 403);
    }

    // Si ya está activada en otro hardware
    if ($license['hardware_id'] !== null && $license['hardware_id'] !== $hardwareId) {
        jsonResponse(['success' => false, 'message' => 'Esta licencia ya está activada en otra máquina'], 409);
    }

    // Si ya está activada en este hardware
    if ($license['hardware_id'] === $hardwareId) {
        // Verificar si expiró
        if ($license['expires_at'] !== null && strtotime($license['expires_at']) < time()) {
            jsonResponse([
                'success' => false,
                'message' => 'Esta licencia ha expirado. Contacte al administrador para renovarla.',
                'expired' => true
            ], 403);
        }

        jsonResponse([
            'success' => true,
            'message' => 'Licencia ya activada en esta máquina',
            'license' => [
                'key' => $license['license_key'],
                'customer' => $license['customer_name'],
                'activated_at' => $license['activated_at'],
                'expires_at' => $license['expires_at'],
            ]
        ]);
    }

    // Activar la licencia - calcular fecha de expiración
    $durationDays = (int)$license['duration_days'];
    $expiresAt = $durationDays > 0
        ? date('Y-m-d H:i:s', strtotime("+{$durationDays} days"))
        : null; // 0 = licencia permanente

    $stmt = $pdo->prepare("UPDATE licenses SET hardware_id = ?, activated_at = NOW(), expires_at = ? WHERE id = ?");
    $stmt->execute([$hardwareId, $expiresAt, $license['id']]);

    // Log
    $stmt = $pdo->prepare("INSERT INTO activation_log (license_key, hardware_id, action, ip_address) VALUES (?, ?, 'activate', ?)");
    $stmt->execute([$licenseKey, $hardwareId, $_SERVER['REMOTE_ADDR'] ?? '']);

    $expiresMsg = $expiresAt ? " (válida hasta " . date('d/m/Y', strtotime($expiresAt)) . ")" : " (permanente)";
    jsonResponse([
        'success' => true,
        'message' => 'Licencia activada exitosamente' . $expiresMsg,
        'license' => [
            'key' => $license['license_key'],
            'customer' => $license['customer_name'],
            'activated_at' => date('Y-m-d H:i:s'),
            'expires_at' => $expiresAt,
        ]
    ]);

} catch (PDOException $e) {
    jsonResponse(['success' => false, 'message' => 'Error del servidor'], 500);
}
