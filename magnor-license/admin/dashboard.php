<?php
session_start();
if (!isset($_SESSION['admin_logged_in']) || $_SESSION['admin_logged_in'] !== true) {
    header('Location: index.php');
    exit;
}

require_once __DIR__ . '/../config.php';

$pdo = getDB();
$message = '';
$messageType = '';

// Procesar acciones
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $action = $_POST['action'] ?? '';

    if ($action === 'create') {
        $name = trim($_POST['customer_name'] ?? '');
        $email = trim($_POST['customer_email'] ?? '');
        $phone = trim($_POST['customer_phone'] ?? '');
        $notes = trim($_POST['notes'] ?? '');
        $durationDays = (int)($_POST['duration_days'] ?? 7);
        $key = generateLicenseKey();

        $stmt = $pdo->prepare("INSERT INTO licenses (license_key, customer_name, customer_email, customer_phone, duration_days, notes) VALUES (?, ?, ?, ?, ?, ?)");
        $stmt->execute([$key, $name, $email, $phone, $durationDays, $notes]);
        $durationLabel = $durationDays > 0 ? "$durationDays días" : "Permanente";
        $message = "Licencia creada: <strong>$key</strong> ($durationLabel)";
        $messageType = 'success';
    }

    if ($action === 'renew') {
        $id = (int)($_POST['license_id'] ?? 0);
        $renewDays = (int)($_POST['renew_days'] ?? 30);
        // Renovar desde hoy
        $newExpires = date('Y-m-d H:i:s', strtotime("+{$renewDays} days"));
        $stmt = $pdo->prepare("UPDATE licenses SET expires_at = ?, is_active = 1, duration_days = ? WHERE id = ?");
        $stmt->execute([$newExpires, $renewDays, $id]);
        $message = "Licencia renovada por $renewDays días (hasta " . date('d/m/Y', strtotime($newExpires)) . ")";
        $messageType = 'success';
    }

    if ($action === 'toggle') {
        $id = (int)($_POST['license_id'] ?? 0);
        $stmt = $pdo->prepare("UPDATE licenses SET is_active = NOT is_active WHERE id = ?");
        $stmt->execute([$id]);
        $message = 'Estado de licencia actualizado';
        $messageType = 'success';
    }

    if ($action === 'unbind') {
        $id = (int)($_POST['license_id'] ?? 0);
        $stmt = $pdo->prepare("UPDATE licenses SET hardware_id = NULL, activated_at = NULL WHERE id = ?");
        $stmt->execute([$id]);
        $message = 'Hardware desvinculado. La licencia puede activarse en otra máquina.';
        $messageType = 'success';
    }

    if ($action === 'delete') {
        $id = (int)($_POST['license_id'] ?? 0);
        $stmt = $pdo->prepare("DELETE FROM licenses WHERE id = ?");
        $stmt->execute([$id]);
        $message = 'Licencia eliminada';
        $messageType = 'success';
    }
}

// Buscar licencias
$search = trim($_GET['search'] ?? '');
$filter = $_GET['filter'] ?? 'all';

$sql = "SELECT * FROM licenses WHERE 1=1";
$params = [];

if (!empty($search)) {
    $sql .= " AND (license_key LIKE ? OR customer_name LIKE ? OR customer_email LIKE ?)";
    $searchParam = "%$search%";
    $params = [$searchParam, $searchParam, $searchParam];
}

if ($filter === 'active') {
    $sql .= " AND is_active = 1 AND hardware_id IS NOT NULL AND (expires_at IS NULL OR expires_at > NOW())";
} elseif ($filter === 'inactive') {
    $sql .= " AND is_active = 0";
} elseif ($filter === 'unused') {
    $sql .= " AND hardware_id IS NULL AND is_active = 1";
} elseif ($filter === 'expired') {
    $sql .= " AND expires_at IS NOT NULL AND expires_at <= NOW()";
}

$sql .= " ORDER BY created_at DESC";
$stmt = $pdo->prepare($sql);
$stmt->execute($params);
$licenses = $stmt->fetchAll();

// Estadísticas
$stats = $pdo->query("SELECT
    COUNT(*) as total,
    SUM(CASE WHEN is_active = 1 AND hardware_id IS NOT NULL THEN 1 ELSE 0 END) as activated,
    SUM(CASE WHEN hardware_id IS NULL AND is_active = 1 THEN 1 ELSE 0 END) as unused,
    SUM(CASE WHEN is_active = 0 THEN 1 ELSE 0 END) as disabled,
    SUM(CASE WHEN expires_at IS NOT NULL AND expires_at <= NOW() THEN 1 ELSE 0 END) as expired
FROM licenses")->fetch();
?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MAGNOR - Panel de Licencias</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, sans-serif;
            background: #f0f2f5;
            color: #333;
        }
        .navbar {
            background: linear-gradient(135deg, #1a1a2e, #0f3460);
            color: #fff;
            padding: 16px 30px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        .navbar h1 { font-size: 20px; }
        .navbar a { color: #ccc; text-decoration: none; font-size: 14px; }
        .navbar a:hover { color: #fff; }

        .container { max-width: 1200px; margin: 0 auto; padding: 24px; }

        /* Stats */
        .stats {
            display: grid;
            grid-template-columns: repeat(5, 1fr);
            gap: 16px;
            margin-bottom: 24px;
        }
        .stat-card {
            background: #fff;
            border-radius: 10px;
            padding: 20px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.06);
        }
        .stat-card .number { font-size: 32px; font-weight: 700; color: #0f3460; }
        .stat-card .label { color: #888; font-size: 14px; margin-top: 4px; }

        /* Message */
        .msg {
            padding: 12px 20px;
            border-radius: 8px;
            margin-bottom: 20px;
            font-size: 14px;
        }
        .msg.success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .msg.error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }

        /* Card */
        .card {
            background: #fff;
            border-radius: 10px;
            padding: 24px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.06);
            margin-bottom: 24px;
        }
        .card h2 {
            font-size: 18px;
            margin-bottom: 16px;
            color: #1a1a2e;
        }

        /* Form */
        .form-row {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 12px;
            margin-bottom: 12px;
        }
        .form-row input, .form-row textarea {
            width: 100%;
            padding: 10px 14px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 14px;
        }
        .form-row input:focus, .form-row textarea:focus {
            outline: none;
            border-color: #0f3460;
        }

        /* Buttons */
        .btn {
            padding: 10px 20px;
            border: none;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: opacity 0.2s;
        }
        .btn:hover { opacity: 0.85; }
        .btn-primary { background: #0f3460; color: #fff; }
        .btn-sm { padding: 6px 12px; font-size: 12px; }
        .btn-warning { background: #ffc107; color: #333; }
        .btn-danger { background: #dc3545; color: #fff; }
        .btn-info { background: #17a2b8; color: #fff; }

        /* Filters */
        .filters {
            display: flex;
            gap: 8px;
            align-items: center;
            margin-bottom: 16px;
            flex-wrap: wrap;
        }
        .filters input[type="text"] {
            padding: 8px 14px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 14px;
            width: 250px;
        }
        .filter-btn {
            padding: 8px 16px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            background: #fff;
            cursor: pointer;
            font-size: 13px;
            text-decoration: none;
            color: #333;
        }
        .filter-btn.active {
            border-color: #0f3460;
            background: #0f3460;
            color: #fff;
        }

        /* Table */
        table {
            width: 100%;
            border-collapse: collapse;
        }
        table th {
            text-align: left;
            padding: 12px 10px;
            background: #f8f9fa;
            font-size: 13px;
            color: #666;
            border-bottom: 2px solid #e0e0e0;
        }
        table td {
            padding: 12px 10px;
            border-bottom: 1px solid #eee;
            font-size: 14px;
        }
        .key-cell {
            font-family: 'Consolas', 'Courier New', monospace;
            font-weight: 600;
            color: #0f3460;
            font-size: 13px;
        }
        .badge {
            display: inline-block;
            padding: 4px 10px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
        }
        .badge-green { background: #d4edda; color: #155724; }
        .badge-red { background: #f8d7da; color: #721c24; }
        .badge-gray { background: #e9ecef; color: #6c757d; }

        .actions form { display: inline; }
        .hw-id {
            font-family: monospace;
            font-size: 11px;
            color: #888;
            max-width: 120px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            display: inline-block;
        }

        @media (max-width: 768px) {
            .stats { grid-template-columns: repeat(2, 1fr); }
            .form-row { grid-template-columns: 1fr; }
            table { font-size: 12px; }
        }
    </style>
</head>
<body>
    <div class="navbar">
        <h1>MAGNOR POS - Licencias</h1>
        <div>
            <span style="margin-right:16px">Hola, <?= htmlspecialchars($_SESSION['admin_username']) ?></span>
            <a href="logout.php">Cerrar sesión</a>
        </div>
    </div>

    <div class="container">
        <!-- Stats -->
        <div class="stats">
            <div class="stat-card">
                <div class="number"><?= $stats['total'] ?? 0 ?></div>
                <div class="label">Total Licencias</div>
            </div>
            <div class="stat-card">
                <div class="number"><?= $stats['activated'] ?? 0 ?></div>
                <div class="label">Activadas</div>
            </div>
            <div class="stat-card">
                <div class="number"><?= $stats['unused'] ?? 0 ?></div>
                <div class="label">Sin Usar</div>
            </div>
            <div class="stat-card">
                <div class="number"><?= $stats['disabled'] ?? 0 ?></div>
                <div class="label">Desactivadas</div>
            </div>
            <div class="stat-card">
                <div class="number" style="color:#dc3545"><?= $stats['expired'] ?? 0 ?></div>
                <div class="label">Expiradas</div>
            </div>
        </div>

        <?php if ($message): ?>
            <div class="msg <?= $messageType ?>"><?= $message ?></div>
        <?php endif; ?>

        <!-- Crear licencia -->
        <div class="card">
            <h2>Crear Nueva Licencia</h2>
            <form method="POST">
                <input type="hidden" name="action" value="create">
                <div class="form-row">
                    <input type="text" name="customer_name" placeholder="Nombre del cliente" required>
                    <input type="email" name="customer_email" placeholder="Email (opcional)">
                    <input type="text" name="customer_phone" placeholder="Teléfono (opcional)">
                </div>
                <div class="form-row">
                    <select name="duration_days" style="padding:10px 14px;border:2px solid #e0e0e0;border-radius:8px;font-size:14px;">
                        <option value="7">7 días (prueba)</option>
                        <option value="30" selected>30 días</option>
                        <option value="90">90 días</option>
                        <option value="180">6 meses</option>
                        <option value="365">1 año</option>
                        <option value="0">Permanente</option>
                    </select>
                    <input type="text" name="notes" placeholder="Notas (opcional)">
                </div>
                <button type="submit" class="btn btn-primary" style="margin-top:8px">Generar Licencia</button>
            </form>
        </div>

        <!-- Lista de licencias -->
        <div class="card">
            <h2>Licencias</h2>

            <div class="filters">
                <form method="GET" style="display:flex;gap:8px;align-items:center;">
                    <input type="text" name="search" placeholder="Buscar por clave, nombre o email..."
                           value="<?= htmlspecialchars($search) ?>">
                    <button type="submit" class="btn btn-primary btn-sm">Buscar</button>
                </form>
                <a href="?filter=all" class="filter-btn <?= $filter === 'all' ? 'active' : '' ?>">Todas</a>
                <a href="?filter=active" class="filter-btn <?= $filter === 'active' ? 'active' : '' ?>">Activadas</a>
                <a href="?filter=unused" class="filter-btn <?= $filter === 'unused' ? 'active' : '' ?>">Sin usar</a>
                <a href="?filter=inactive" class="filter-btn <?= $filter === 'inactive' ? 'active' : '' ?>">Desactivadas</a>
                <a href="?filter=expired" class="filter-btn <?= $filter === 'expired' ? 'active' : '' ?>">Expiradas</a>
            </div>

            <div style="overflow-x:auto;">
            <table>
                <thead>
                    <tr>
                        <th>Clave</th>
                        <th>Cliente</th>
                        <th>Estado</th>
                        <th>Hardware ID</th>
                        <th>Activada</th>
                        <th>Expira</th>
                        <th>Creada</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                <?php if (empty($licenses)): ?>
                    <tr><td colspan="8" style="text-align:center;padding:30px;color:#888">No hay licencias</td></tr>
                <?php endif; ?>

                <?php foreach ($licenses as $lic): ?>
                    <tr>
                        <td class="key-cell"><?= htmlspecialchars($lic['license_key']) ?></td>
                        <td>
                            <?= htmlspecialchars($lic['customer_name'] ?: '—') ?>
                            <?php if ($lic['customer_email']): ?>
                                <br><small style="color:#888"><?= htmlspecialchars($lic['customer_email']) ?></small>
                            <?php endif; ?>
                        </td>
                        <td>
                            <?php
                                $isExpired = $lic['expires_at'] !== null && strtotime($lic['expires_at']) < time();
                            ?>
                            <?php if ($isExpired): ?>
                                <span class="badge badge-red">Expirada</span>
                            <?php elseif (!$lic['is_active']): ?>
                                <span class="badge badge-red">Desactivada</span>
                            <?php elseif ($lic['hardware_id']): ?>
                                <span class="badge badge-green">Activada</span>
                                <?php if ($lic['expires_at']):
                                    $daysLeft = (int)ceil((strtotime($lic['expires_at']) - time()) / 86400);
                                ?>
                                    <br><small style="color:<?= $daysLeft <= 3 ? '#dc3545' : '#888' ?>"><?= $daysLeft ?> días restantes</small>
                                <?php endif; ?>
                            <?php else: ?>
                                <span class="badge badge-gray">Sin usar</span>
                                <br><small style="color:#888"><?= $lic['duration_days'] > 0 ? $lic['duration_days'] . ' días' : 'Permanente' ?></small>
                            <?php endif; ?>
                        </td>
                        <td>
                            <?php if ($lic['hardware_id']): ?>
                                <span class="hw-id" title="<?= htmlspecialchars($lic['hardware_id']) ?>">
                                    <?= htmlspecialchars(substr($lic['hardware_id'], 0, 16)) ?>...
                                </span>
                            <?php else: ?>
                                <span style="color:#ccc">—</span>
                            <?php endif; ?>
                        </td>
                        <td><?= $lic['activated_at'] ? date('d/m/Y H:i', strtotime($lic['activated_at'])) : '—' ?></td>
                        <td>
                            <?php if ($lic['expires_at']): ?>
                                <span style="color:<?= $isExpired ? '#dc3545' : '#333' ?>">
                                    <?= date('d/m/Y', strtotime($lic['expires_at'])) ?>
                                </span>
                            <?php elseif ($lic['duration_days'] == 0): ?>
                                <span style="color:#28a745">Permanente</span>
                            <?php else: ?>
                                —
                            <?php endif; ?>
                        </td>
                        <td><?= date('d/m/Y', strtotime($lic['created_at'])) ?></td>
                        <td class="actions">
                            <!-- Toggle activo/inactivo -->
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="toggle">
                                <input type="hidden" name="license_id" value="<?= $lic['id'] ?>">
                                <?php if ($lic['is_active']): ?>
                                    <button type="submit" class="btn btn-sm btn-warning"
                                            onclick="return confirm('¿Desactivar esta licencia?')">Desactivar</button>
                                <?php else: ?>
                                    <button type="submit" class="btn btn-sm btn-info">Activar</button>
                                <?php endif; ?>
                            </form>

                            <!-- Desvincular hardware -->
                            <?php if ($lic['hardware_id']): ?>
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="unbind">
                                <input type="hidden" name="license_id" value="<?= $lic['id'] ?>">
                                <button type="submit" class="btn btn-sm btn-info"
                                        onclick="return confirm('¿Desvincular hardware? La licencia podrá usarse en otra máquina.')">Desvincular</button>
                            </form>
                            <?php endif; ?>

                            <!-- Renovar -->
                            <?php if ($lic['hardware_id'] && $lic['duration_days'] > 0): ?>
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="renew">
                                <input type="hidden" name="license_id" value="<?= $lic['id'] ?>">
                                <select name="renew_days" style="padding:4px;border-radius:4px;border:1px solid #ccc;font-size:11px;">
                                    <option value="7">7 días</option>
                                    <option value="30" selected>30 días</option>
                                    <option value="90">90 días</option>
                                    <option value="365">1 año</option>
                                </select>
                                <button type="submit" class="btn btn-sm" style="background:#28a745;color:#fff"
                                        onclick="return confirm('¿Renovar esta licencia?')">Renovar</button>
                            </form>
                            <?php endif; ?>

                            <!-- Eliminar -->
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="delete">
                                <input type="hidden" name="license_id" value="<?= $lic['id'] ?>">
                                <button type="submit" class="btn btn-sm btn-danger"
                                        onclick="return confirm('¿Eliminar esta licencia permanentemente?')">Eliminar</button>
                            </form>
                        </td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
            </div>
        </div>
    </div>
</body>
</html>
