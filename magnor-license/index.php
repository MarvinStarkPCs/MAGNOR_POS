<?php
// ============================================
// MAGNOR POS - Panel de Licencias (Unificado)
// Login + Dashboard en un solo archivo
// ============================================

session_start();
require_once __DIR__ . '/config.php';

// --- Routing ---
$page = $_GET['page'] ?? '';

// Logout
if ($page === 'logout') {
    session_destroy();
    header('Location: index.php');
    exit;
}

// --- Login ---
$loginError = '';
if (!isset($_SESSION['admin_logged_in']) || $_SESSION['admin_logged_in'] !== true) {
    if ($_SERVER['REQUEST_METHOD'] === 'POST' && ($_POST['action'] ?? '') === 'login') {
        $username = trim($_POST['username'] ?? '');
        $password = $_POST['password'] ?? '';

        if (!empty($username) && !empty($password)) {
            try {
                $pdo = getDB();
                $stmt = $pdo->prepare("SELECT * FROM admin_users WHERE username = ?");
                $stmt->execute([$username]);
                $user = $stmt->fetch();

                if ($user && password_verify($password, $user['password_hash'])) {
                    $_SESSION['admin_logged_in'] = true;
                    $_SESSION['admin_username'] = $user['username'];
                    header('Location: index.php');
                    exit;
                } else {
                    $loginError = 'Usuario o contraseña incorrectos';
                }
            } catch (PDOException $e) {
                $loginError = 'Error de conexión a la base de datos';
            }
        } else {
            $loginError = 'Complete todos los campos';
        }
    }

    // Mostrar login
    ?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MAGNOR Licencias - Admin</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .login-card {
            background: #fff;
            border-radius: 12px;
            padding: 40px;
            width: 380px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
        }
        .login-card h1 { text-align: center; color: #1a1a2e; margin-bottom: 8px; font-size: 24px; }
        .login-card .subtitle { text-align: center; color: #888; margin-bottom: 30px; font-size: 14px; }
        .form-group { margin-bottom: 20px; }
        .form-group label { display: block; margin-bottom: 6px; color: #333; font-weight: 600; font-size: 14px; }
        .form-group input {
            width: 100%; padding: 12px 16px; border: 2px solid #e0e0e0;
            border-radius: 8px; font-size: 15px; transition: border-color 0.3s;
        }
        .form-group input:focus { outline: none; border-color: #0f3460; }
        .btn-login {
            width: 100%; padding: 14px; background: #0f3460; color: #fff; border: none;
            border-radius: 8px; font-size: 16px; font-weight: 600; cursor: pointer; transition: background 0.3s;
        }
        .btn-login:hover { background: #1a1a2e; }
        .error { background: #ffe0e0; color: #c00; padding: 10px 16px; border-radius: 8px; margin-bottom: 20px; font-size: 14px; }
    </style>
</head>
<body>
    <div class="login-card">
        <h1>MAGNOR POS</h1>
        <p class="subtitle">Panel de Licencias</p>
        <?php if ($loginError): ?>
            <div class="error"><?= htmlspecialchars($loginError) ?></div>
        <?php endif; ?>
        <form method="POST">
            <input type="hidden" name="action" value="login">
            <div class="form-group">
                <label>Usuario</label>
                <input type="text" name="username" required autofocus value="<?= htmlspecialchars($_POST['username'] ?? '') ?>">
            </div>
            <div class="form-group">
                <label>Contraseña</label>
                <input type="password" name="password" required>
            </div>
            <button type="submit" class="btn-login">Iniciar Sesión</button>
        </form>
    </div>
</body>
</html>
    <?php
    exit;
}

// ============================================
// DASHBOARD (usuario autenticado)
// ============================================

$pdo = getDB();
$message = '';
$messageType = '';

// --- Configuración de admin ---
if ($page === 'config') {
    if ($_SERVER['REQUEST_METHOD'] === 'POST' && ($_POST['action'] ?? '') === 'change_password') {
        $currentPass = $_POST['current_password'] ?? '';
        $newPass = $_POST['new_password'] ?? '';
        $confirmPass = $_POST['confirm_password'] ?? '';

        $stmt = $pdo->prepare("SELECT * FROM admin_users WHERE username = ?");
        $stmt->execute([$_SESSION['admin_username']]);
        $admin = $stmt->fetch();

        if (!password_verify($currentPass, $admin['password_hash'])) {
            $message = 'Contraseña actual incorrecta';
            $messageType = 'error';
        } elseif (strlen($newPass) < 6) {
            $message = 'La nueva contraseña debe tener al menos 6 caracteres';
            $messageType = 'error';
        } elseif ($newPass !== $confirmPass) {
            $message = 'Las contraseñas no coinciden';
            $messageType = 'error';
        } else {
            $hash = password_hash($newPass, PASSWORD_BCRYPT);
            $stmt = $pdo->prepare("UPDATE admin_users SET password_hash = ? WHERE id = ?");
            $stmt->execute([$hash, $admin['id']]);
            $message = 'Contraseña actualizada correctamente';
            $messageType = 'success';
        }
    }

    if ($_SERVER['REQUEST_METHOD'] === 'POST' && ($_POST['action'] ?? '') === 'create_admin') {
        $newUser = trim($_POST['new_username'] ?? '');
        $newUserPass = $_POST['new_user_password'] ?? '';

        if (empty($newUser) || strlen($newUserPass) < 6) {
            $message = 'Usuario requerido y contraseña mínimo 6 caracteres';
            $messageType = 'error';
        } else {
            $hash = password_hash($newUserPass, PASSWORD_BCRYPT);
            $stmt = $pdo->prepare("INSERT IGNORE INTO admin_users (username, password_hash) VALUES (?, ?)");
            $stmt->execute([$newUser, $hash]);
            if ($stmt->rowCount() > 0) {
                $message = "Admin <strong>$newUser</strong> creado";
                $messageType = 'success';
            } else {
                $message = 'Ese usuario ya existe';
                $messageType = 'error';
            }
        }
    }

    if ($_SERVER['REQUEST_METHOD'] === 'POST' && ($_POST['action'] ?? '') === 'delete_admin') {
        $deleteId = (int)($_POST['admin_id'] ?? 0);
        // No permitir eliminar al propio usuario
        $stmt = $pdo->prepare("SELECT username FROM admin_users WHERE id = ?");
        $stmt->execute([$deleteId]);
        $toDelete = $stmt->fetch();
        if ($toDelete && $toDelete['username'] === $_SESSION['admin_username']) {
            $message = 'No puedes eliminar tu propio usuario';
            $messageType = 'error';
        } else {
            $stmt = $pdo->prepare("DELETE FROM admin_users WHERE id = ?");
            $stmt->execute([$deleteId]);
            $message = 'Admin eliminado';
            $messageType = 'success';
        }
    }

    // Obtener lista de admins
    $admins = $pdo->query("SELECT id, username, created_at FROM admin_users ORDER BY id")->fetchAll();
}

// --- Acciones de licencias ---
if ($_SERVER['REQUEST_METHOD'] === 'POST' && $page !== 'config') {
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

// --- Buscar licencias ---
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
    SUM(CASE WHEN is_active = 1 AND hardware_id IS NOT NULL AND (expires_at IS NULL OR expires_at > NOW()) THEN 1 ELSE 0 END) as activated,
    SUM(CASE WHEN hardware_id IS NULL AND is_active = 1 THEN 1 ELSE 0 END) as unused,
    SUM(CASE WHEN is_active = 0 THEN 1 ELSE 0 END) as disabled,
    SUM(CASE WHEN expires_at IS NOT NULL AND expires_at <= NOW() THEN 1 ELSE 0 END) as expired
FROM licenses")->fetch();

// Log de activaciones recientes
$recentLogs = [];
if ($page === 'logs') {
    $recentLogs = $pdo->query("SELECT * FROM activation_log ORDER BY created_at DESC LIMIT 100")->fetchAll();
}
?>
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MAGNOR - Panel de Licencias</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, sans-serif; background: #f0f2f5; color: #333; }

        /* Navbar */
        .navbar {
            background: linear-gradient(135deg, #1a1a2e, #0f3460);
            color: #fff; padding: 16px 30px;
            display: flex; justify-content: space-between; align-items: center;
            box-shadow: 0 2px 10px rgba(0,0,0,0.2);
        }
        .navbar h1 { font-size: 20px; }
        .navbar-links { display: flex; gap: 20px; align-items: center; }
        .navbar-links a {
            color: #aab; text-decoration: none; font-size: 14px;
            padding: 6px 14px; border-radius: 6px; transition: all 0.2s;
        }
        .navbar-links a:hover, .navbar-links a.active { color: #fff; background: rgba(255,255,255,0.15); }
        .navbar-links .user-name { color: #8899aa; font-size: 13px; }

        .container { max-width: 1300px; margin: 0 auto; padding: 24px; }

        /* Stats */
        .stats { display: grid; grid-template-columns: repeat(5, 1fr); gap: 16px; margin-bottom: 24px; }
        .stat-card {
            background: #fff; border-radius: 12px; padding: 20px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.06); text-align: center;
            transition: transform 0.2s;
        }
        .stat-card:hover { transform: translateY(-2px); }
        .stat-card .number { font-size: 36px; font-weight: 700; color: #0f3460; }
        .stat-card .label { color: #888; font-size: 13px; margin-top: 4px; text-transform: uppercase; letter-spacing: 0.5px; }

        /* Messages */
        .msg { padding: 14px 20px; border-radius: 8px; margin-bottom: 20px; font-size: 14px; }
        .msg.success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .msg.error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }

        /* Card */
        .card {
            background: #fff; border-radius: 12px; padding: 24px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.06); margin-bottom: 24px;
        }
        .card h2 { font-size: 18px; margin-bottom: 16px; color: #1a1a2e; }

        /* Form */
        .form-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px; margin-bottom: 12px; }
        .form-row input, .form-row textarea, .form-row select {
            width: 100%; padding: 10px 14px; border: 2px solid #e0e0e0;
            border-radius: 8px; font-size: 14px; transition: border-color 0.2s;
        }
        .form-row input:focus, .form-row textarea:focus, .form-row select:focus { outline: none; border-color: #0f3460; }

        /* Buttons */
        .btn {
            padding: 10px 20px; border: none; border-radius: 8px;
            font-size: 14px; font-weight: 600; cursor: pointer; transition: all 0.2s;
        }
        .btn:hover { opacity: 0.85; transform: translateY(-1px); }
        .btn-primary { background: #0f3460; color: #fff; }
        .btn-sm { padding: 6px 12px; font-size: 12px; }
        .btn-warning { background: #ffc107; color: #333; }
        .btn-danger { background: #dc3545; color: #fff; }
        .btn-info { background: #17a2b8; color: #fff; }
        .btn-success { background: #28a745; color: #fff; }

        /* Filters */
        .filters { display: flex; gap: 8px; align-items: center; margin-bottom: 16px; flex-wrap: wrap; }
        .filters input[type="text"] {
            padding: 8px 14px; border: 2px solid #e0e0e0;
            border-radius: 8px; font-size: 14px; width: 260px;
        }
        .filter-btn {
            padding: 8px 16px; border: 2px solid #e0e0e0; border-radius: 8px;
            background: #fff; cursor: pointer; font-size: 13px;
            text-decoration: none; color: #333; transition: all 0.2s;
        }
        .filter-btn:hover { border-color: #0f3460; }
        .filter-btn.active { border-color: #0f3460; background: #0f3460; color: #fff; }

        /* Table */
        table { width: 100%; border-collapse: collapse; }
        table th {
            text-align: left; padding: 12px 10px; background: #f8f9fa;
            font-size: 13px; color: #666; border-bottom: 2px solid #e0e0e0;
        }
        table td { padding: 12px 10px; border-bottom: 1px solid #eee; font-size: 14px; }
        table tr:hover td { background: #f8f9fa; }
        .key-cell { font-family: 'Consolas', 'Courier New', monospace; font-weight: 600; color: #0f3460; font-size: 13px; }
        .badge { display: inline-block; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 600; }
        .badge-green { background: #d4edda; color: #155724; }
        .badge-red { background: #f8d7da; color: #721c24; }
        .badge-gray { background: #e9ecef; color: #6c757d; }
        .badge-blue { background: #cce5ff; color: #004085; }

        .actions form { display: inline; }
        .hw-id {
            font-family: monospace; font-size: 11px; color: #888;
            max-width: 120px; overflow: hidden; text-overflow: ellipsis;
            white-space: nowrap; display: inline-block;
        }

        /* Config page */
        .config-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
        .config-grid .card { margin-bottom: 0; }

        @media (max-width: 900px) {
            .stats { grid-template-columns: repeat(2, 1fr); }
            .config-grid { grid-template-columns: 1fr; }
        }
        @media (max-width: 600px) {
            .stats { grid-template-columns: 1fr 1fr; }
            .form-row { grid-template-columns: 1fr; }
            table { font-size: 12px; }
            .navbar { flex-direction: column; gap: 10px; }
        }
    </style>
</head>
<body>
    <div class="navbar">
        <h1>MAGNOR POS - Licencias</h1>
        <div class="navbar-links">
            <a href="index.php" class="<?= $page === '' ? 'active' : '' ?>">Licencias</a>
            <a href="index.php?page=logs" class="<?= $page === 'logs' ? 'active' : '' ?>">Historial</a>
            <a href="index.php?page=config" class="<?= $page === 'config' ? 'active' : '' ?>">Configuración</a>
            <span class="user-name"><?= htmlspecialchars($_SESSION['admin_username']) ?></span>
            <a href="index.php?page=logout">Salir</a>
        </div>
    </div>

    <div class="container">
        <?php if ($message): ?>
            <div class="msg <?= $messageType ?>"><?= $message ?></div>
        <?php endif; ?>

        <?php if ($page === 'config'): ?>
        <!-- ===================== CONFIGURACIÓN ===================== -->
        <div class="config-grid">
            <div class="card">
                <h2>Cambiar Contraseña</h2>
                <form method="POST">
                    <input type="hidden" name="action" value="change_password">
                    <div class="form-row" style="grid-template-columns:1fr">
                        <input type="password" name="current_password" placeholder="Contraseña actual" required>
                    </div>
                    <div class="form-row" style="grid-template-columns:1fr">
                        <input type="password" name="new_password" placeholder="Nueva contraseña (mín. 6 caracteres)" required>
                    </div>
                    <div class="form-row" style="grid-template-columns:1fr">
                        <input type="password" name="confirm_password" placeholder="Confirmar nueva contraseña" required>
                    </div>
                    <button type="submit" class="btn btn-primary" style="margin-top:8px">Cambiar Contraseña</button>
                </form>
            </div>

            <div class="card">
                <h2>Crear Nuevo Admin</h2>
                <form method="POST">
                    <input type="hidden" name="action" value="create_admin">
                    <div class="form-row" style="grid-template-columns:1fr">
                        <input type="text" name="new_username" placeholder="Nombre de usuario" required>
                    </div>
                    <div class="form-row" style="grid-template-columns:1fr">
                        <input type="password" name="new_user_password" placeholder="Contraseña (mín. 6 caracteres)" required>
                    </div>
                    <button type="submit" class="btn btn-success" style="margin-top:8px">Crear Admin</button>
                </form>
            </div>
        </div>

        <div class="card" style="margin-top:24px">
            <h2>Administradores</h2>
            <table>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Usuario</th>
                        <th>Creado</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                <?php foreach ($admins as $adm): ?>
                    <tr>
                        <td><?= $adm['id'] ?></td>
                        <td><strong><?= htmlspecialchars($adm['username']) ?></strong>
                            <?= $adm['username'] === $_SESSION['admin_username'] ? '<span class="badge badge-blue">Tú</span>' : '' ?>
                        </td>
                        <td><?= date('d/m/Y H:i', strtotime($adm['created_at'])) ?></td>
                        <td>
                            <?php if ($adm['username'] !== $_SESSION['admin_username']): ?>
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="delete_admin">
                                <input type="hidden" name="admin_id" value="<?= $adm['id'] ?>">
                                <button type="submit" class="btn btn-sm btn-danger"
                                        onclick="return confirm('¿Eliminar admin <?= htmlspecialchars($adm['username']) ?>?')">Eliminar</button>
                            </form>
                            <?php else: ?>
                                <span style="color:#888;font-size:12px">—</span>
                            <?php endif; ?>
                        </td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
        </div>

        <div class="card">
            <h2>Información del Sistema</h2>
            <table>
                <tr><td style="font-weight:600;width:200px">PHP Version</td><td><?= phpversion() ?></td></tr>
                <tr><td style="font-weight:600">Servidor</td><td><?= htmlspecialchars($_SERVER['SERVER_SOFTWARE'] ?? 'N/A') ?></td></tr>
                <tr><td style="font-weight:600">Base de Datos</td><td><?= DB_HOST ?> / <?= DB_NAME ?></td></tr>
                <tr><td style="font-weight:600">Zona Horaria</td><td><?= date_default_timezone_get() ?></td></tr>
                <tr><td style="font-weight:600">Fecha del Servidor</td><td><?= date('d/m/Y H:i:s') ?></td></tr>
            </table>
        </div>

        <?php elseif ($page === 'logs'): ?>
        <!-- ===================== HISTORIAL ===================== -->
        <div class="card">
            <h2>Historial de Activaciones</h2>
            <div style="overflow-x:auto;">
            <table>
                <thead>
                    <tr>
                        <th>Fecha</th>
                        <th>Clave</th>
                        <th>Hardware ID</th>
                        <th>Acción</th>
                        <th>IP</th>
                    </tr>
                </thead>
                <tbody>
                <?php if (empty($recentLogs)): ?>
                    <tr><td colspan="5" style="text-align:center;padding:30px;color:#888">No hay registros</td></tr>
                <?php endif; ?>
                <?php foreach ($recentLogs as $log): ?>
                    <tr>
                        <td><?= date('d/m/Y H:i:s', strtotime($log['created_at'])) ?></td>
                        <td class="key-cell"><?= htmlspecialchars($log['license_key']) ?></td>
                        <td><span class="hw-id" title="<?= htmlspecialchars($log['hardware_id']) ?>"><?= htmlspecialchars(substr($log['hardware_id'], 0, 16)) ?>...</span></td>
                        <td><span class="badge <?= $log['action'] === 'activate' ? 'badge-green' : 'badge-red' ?>"><?= htmlspecialchars($log['action']) ?></span></td>
                        <td style="font-family:monospace;font-size:12px"><?= htmlspecialchars($log['ip_address']) ?></td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
            </div>
        </div>

        <?php else: ?>
        <!-- ===================== DASHBOARD PRINCIPAL ===================== -->

        <!-- Stats -->
        <div class="stats">
            <div class="stat-card">
                <div class="number"><?= $stats['total'] ?? 0 ?></div>
                <div class="label">Total</div>
            </div>
            <div class="stat-card">
                <div class="number" style="color:#28a745"><?= $stats['activated'] ?? 0 ?></div>
                <div class="label">Activadas</div>
            </div>
            <div class="stat-card">
                <div class="number" style="color:#6c757d"><?= $stats['unused'] ?? 0 ?></div>
                <div class="label">Sin Usar</div>
            </div>
            <div class="stat-card">
                <div class="number" style="color:#ffc107"><?= $stats['disabled'] ?? 0 ?></div>
                <div class="label">Desactivadas</div>
            </div>
            <div class="stat-card">
                <div class="number" style="color:#dc3545"><?= $stats['expired'] ?? 0 ?></div>
                <div class="label">Expiradas</div>
            </div>
        </div>

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
                    <select name="duration_days">
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
                <a href="index.php?filter=all" class="filter-btn <?= $filter === 'all' ? 'active' : '' ?>">Todas</a>
                <a href="index.php?filter=active" class="filter-btn <?= $filter === 'active' ? 'active' : '' ?>">Activadas</a>
                <a href="index.php?filter=unused" class="filter-btn <?= $filter === 'unused' ? 'active' : '' ?>">Sin usar</a>
                <a href="index.php?filter=inactive" class="filter-btn <?= $filter === 'inactive' ? 'active' : '' ?>">Desactivadas</a>
                <a href="index.php?filter=expired" class="filter-btn <?= $filter === 'expired' ? 'active' : '' ?>">Expiradas</a>
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
                            <?php if ($lic['customer_phone']): ?>
                                <br><small style="color:#888"><?= htmlspecialchars($lic['customer_phone']) ?></small>
                            <?php endif; ?>
                        </td>
                        <td>
                            <?php $isExpired = $lic['expires_at'] !== null && strtotime($lic['expires_at']) < time(); ?>
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
                                <?php elseif ($lic['duration_days'] == 0): ?>
                                    <br><small style="color:#28a745">Permanente</small>
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
                            <!-- Toggle -->
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

                            <!-- Desvincular -->
                            <?php if ($lic['hardware_id']): ?>
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="unbind">
                                <input type="hidden" name="license_id" value="<?= $lic['id'] ?>">
                                <button type="submit" class="btn btn-sm btn-info"
                                        onclick="return confirm('¿Desvincular hardware?')">Desvincular</button>
                            </form>
                            <?php endif; ?>

                            <!-- Renovar -->
                            <?php if ($lic['hardware_id'] && $lic['duration_days'] > 0): ?>
                            <form method="POST" style="display:inline">
                                <input type="hidden" name="action" value="renew">
                                <input type="hidden" name="license_id" value="<?= $lic['id'] ?>">
                                <select name="renew_days" style="padding:4px;border-radius:4px;border:1px solid #ccc;font-size:11px;">
                                    <option value="7">7d</option>
                                    <option value="30" selected>30d</option>
                                    <option value="90">90d</option>
                                    <option value="365">1a</option>
                                </select>
                                <button type="submit" class="btn btn-sm btn-success"
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

        <?php endif; ?>
    </div>
</body>
</html>
