import React, { useState } from 'react';
import { router, useForm, Head, usePage } from '@inertiajs/react';

const STATUS_LABELS = {
    active: 'Activa',
    unused: 'Sin Usar',
    expired: 'Expirada',
    inactive: 'Inactiva',
};

const STATUS_COLORS = {
    active: 'bg-green-100 text-green-800',
    unused: 'bg-gray-100 text-gray-800',
    expired: 'bg-red-100 text-red-800',
    inactive: 'bg-yellow-100 text-yellow-800',
};

const DURATION_OPTIONS = [
    { value: 7, label: '7 dias' },
    { value: 30, label: '30 dias' },
    { value: 90, label: '90 dias' },
    { value: 180, label: '180 dias' },
    { value: 365, label: '365 dias' },
    { value: 0, label: 'Permanente' },
];

function formatDate(dateStr) {
    if (!dateStr) return '-';
    const d = new Date(dateStr);
    return d.toLocaleDateString('es-MX', { year: 'numeric', month: 'short', day: 'numeric' });
}

function StatCard({ label, value, color }) {
    return (
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <div className="text-sm text-gray-500 font-medium">{label}</div>
            <div className={`text-3xl font-bold mt-1 ${color}`}>{value}</div>
        </div>
    );
}

export default function Dashboard({ licenses, stats, currentFilter }) {
    const { flash } = usePage().props;
    const [showCreate, setShowCreate] = useState(false);
    const [renewId, setRenewId] = useState(null);
    const [renewDays, setRenewDays] = useState(30);
    const [factusId, setFactusId] = useState(null);
    const [factusForm, setFactusForm] = useState({
        factus_enabled: false,
        factus_sandbox: true,
        factus_client_id: '',
        factus_client_secret: '',
        factus_username: '',
        factus_password: '',
    });

    const createForm = useForm({
        customer_name: '',
        customer_email: '',
        duration_days: 30,
    });

    const handleCreate = (e) => {
        e.preventDefault();
        createForm.post('/licenses', {
            onSuccess: () => {
                setShowCreate(false);
                createForm.reset();
            },
        });
    };

    const handleFilter = (filter) => {
        router.get('/dashboard', { filter }, { preserveState: true });
    };

    const handleToggle = (id) => {
        router.post(`/licenses/${id}/toggle`);
    };

    const handleUnbind = (id) => {
        if (confirm('Desvincular hardware de esta licencia?')) {
            router.post(`/licenses/${id}/unbind`);
        }
    };

    const handleDelete = (id) => {
        if (confirm('Eliminar esta licencia permanentemente?')) {
            router.delete(`/licenses/${id}`);
        }
    };

    const handleRenew = (id) => {
        router.post(`/licenses/${id}/renew`, { duration_days: renewDays }, {
            onSuccess: () => setRenewId(null),
        });
    };

    const openFactusConfig = (lic) => {
        setFactusId(lic.id);
        setFactusForm({
            factus_enabled: lic.factus_enabled || false,
            factus_sandbox: lic.factus_sandbox !== undefined ? lic.factus_sandbox : true,
            factus_client_id: lic.factus_client_id || '',
            factus_client_secret: '',
            factus_username: lic.factus_username || '',
            factus_password: '',
        });
    };

    const handleFactusSave = (id) => {
        router.post(`/licenses/${id}/factus`, factusForm, {
            onSuccess: () => setFactusId(null),
        });
    };

    const filters = [
        { key: 'all', label: 'Todas' },
        { key: 'active', label: 'Activas' },
        { key: 'unused', label: 'Sin Usar' },
        { key: 'inactive', label: 'Inactivas' },
        { key: 'expired', label: 'Expiradas' },
    ];

    return (
        <>
            <Head title="Panel de Licencias" />
            <div className="min-h-screen bg-gray-50">
                {/* Header */}
                <header className="bg-white border-b border-gray-200 shadow-sm">
                    <div className="max-w-7xl mx-auto px-4 py-4 flex justify-between items-center">
                        <h1 className="text-xl font-bold text-blue-800 tracking-tight">
                            MAGNOR License Server
                        </h1>
                        <div className="flex items-center gap-3">
                            <button
                                onClick={() => router.get('/backup')}
                                className="px-4 py-2 text-sm text-gray-600 hover:text-[#146e39] hover:bg-green-50 rounded-lg transition cursor-pointer font-medium"
                            >
                                ☁️ Backup
                            </button>
                            <button
                                onClick={() => router.post('/logout')}
                                className="px-4 py-2 text-sm text-gray-600 hover:text-red-600 hover:bg-red-50 rounded-lg transition cursor-pointer"
                            >
                                Cerrar Sesion
                            </button>
                        </div>
                    </div>
                </header>

                <main className="max-w-7xl mx-auto px-4 py-6 space-y-6">
                    {/* Flash messages */}
                    {flash?.success && (
                        <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg">
                            {flash.success}
                        </div>
                    )}

                    {/* Stats */}
                    <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                        <StatCard label="Total" value={stats.total} color="text-blue-700" />
                        <StatCard label="Activas" value={stats.active} color="text-green-600" />
                        <StatCard label="Sin Usar" value={stats.unused} color="text-gray-600" />
                        <StatCard label="Desactivadas" value={stats.inactive} color="text-yellow-600" />
                        <StatCard label="Expiradas" value={stats.expired} color="text-red-600" />
                    </div>

                    {/* Actions bar */}
                    <div className="flex flex-wrap items-center justify-between gap-3">
                        <div className="flex flex-wrap gap-2">
                            {filters.map((f) => (
                                <button
                                    key={f.key}
                                    onClick={() => handleFilter(f.key)}
                                    className={`px-4 py-2 text-sm rounded-lg font-medium transition cursor-pointer ${
                                        currentFilter === f.key
                                            ? 'bg-blue-700 text-white'
                                            : 'bg-white text-gray-600 hover:bg-gray-100 border border-gray-200'
                                    }`}
                                >
                                    {f.label}
                                </button>
                            ))}
                        </div>
                        <button
                            onClick={() => setShowCreate(!showCreate)}
                            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white font-medium rounded-lg transition cursor-pointer text-sm"
                        >
                            + Crear Licencia
                        </button>
                    </div>

                    {/* Create form */}
                    {showCreate && (
                        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
                            <h2 className="text-lg font-semibold text-gray-800 mb-4">Nueva Licencia</h2>
                            <form onSubmit={handleCreate} className="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">
                                        Nombre del Cliente
                                    </label>
                                    <input
                                        type="text"
                                        value={createForm.data.customer_name}
                                        onChange={(e) => createForm.setData('customer_name', e.target.value)}
                                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm"
                                        placeholder="Nombre (opcional)"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">
                                        Email del Cliente
                                    </label>
                                    <input
                                        type="email"
                                        value={createForm.data.customer_email}
                                        onChange={(e) => createForm.setData('customer_email', e.target.value)}
                                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm"
                                        placeholder="email@ejemplo.com (opcional)"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-1">
                                        Duracion
                                    </label>
                                    <select
                                        value={createForm.data.duration_days}
                                        onChange={(e) => createForm.setData('duration_days', parseInt(e.target.value))}
                                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-sm"
                                    >
                                        {DURATION_OPTIONS.map((opt) => (
                                            <option key={opt.value} value={opt.value}>{opt.label}</option>
                                        ))}
                                    </select>
                                </div>
                                <div className="flex gap-2">
                                    <button
                                        type="submit"
                                        disabled={createForm.processing}
                                        className="px-5 py-2 bg-green-600 hover:bg-green-700 text-white font-medium rounded-lg transition text-sm disabled:opacity-50 cursor-pointer"
                                    >
                                        Crear
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => { setShowCreate(false); createForm.reset(); }}
                                        className="px-5 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 font-medium rounded-lg transition text-sm cursor-pointer"
                                    >
                                        Cancelar
                                    </button>
                                </div>
                            </form>
                        </div>
                    )}

                    {/* Table */}
                    <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                                <thead>
                                    <tr className="bg-gray-50 border-b border-gray-200">
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Clave</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Cliente</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Estado</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Hardware ID</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Activada</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Expira</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Creada</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Factus</th>
                                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Acciones</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-gray-100">
                                    {licenses.length === 0 && (
                                        <tr>
                                            <td colSpan="9" className="text-center py-8 text-gray-400">
                                                No se encontraron licencias.
                                            </td>
                                        </tr>
                                    )}
                                    {licenses.map((lic) => (
                                        <React.Fragment key={lic.id}>
                                        <tr className="hover:bg-gray-50 transition">
                                            <td className="px-4 py-3 font-mono text-xs text-blue-800 font-medium">
                                                {lic.license_key}
                                            </td>
                                            <td className="px-4 py-3">
                                                <div className="text-gray-800">{lic.customer_name || '-'}</div>
                                                {lic.customer_email && (
                                                    <div className="text-xs text-gray-400">{lic.customer_email}</div>
                                                )}
                                            </td>
                                            <td className="px-4 py-3">
                                                <span className={`inline-block px-2.5 py-1 rounded-full text-xs font-medium ${STATUS_COLORS[lic.status]}`}>
                                                    {STATUS_LABELS[lic.status]}
                                                </span>
                                                {lic.status === 'active' && lic.days_left !== null && (
                                                    <span className="text-xs text-gray-400 ml-1">
                                                        ({lic.days_left}d)
                                                    </span>
                                                )}
                                                {lic.status === 'active' && lic.days_left === null && (
                                                    <span className="text-xs text-gray-400 ml-1">
                                                        (Perm.)
                                                    </span>
                                                )}
                                            </td>
                                            <td className="px-4 py-3 text-xs text-gray-500 font-mono">
                                                {lic.hardware_id
                                                    ? lic.hardware_id.length > 16
                                                        ? lic.hardware_id.substring(0, 16) + '...'
                                                        : lic.hardware_id
                                                    : '-'}
                                            </td>
                                            <td className="px-4 py-3 text-xs text-gray-500">
                                                {formatDate(lic.activated_at)}
                                            </td>
                                            <td className="px-4 py-3 text-xs text-gray-500">
                                                {lic.duration_days === 0
                                                    ? 'Permanente'
                                                    : formatDate(lic.expires_at)}
                                            </td>
                                            <td className="px-4 py-3 text-xs text-gray-500">
                                                {formatDate(lic.created_at)}
                                            </td>
                                            <td className="px-4 py-3">
                                                <button
                                                    onClick={() => openFactusConfig(lic)}
                                                    className={'px-2 py-1 rounded text-xs font-medium transition cursor-pointer ' + (lic.factus_enabled ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500')}
                                                >
                                                    {lic.factus_enabled ? 'Activo' : 'No'}
                                                </button>
                                            </td>
                                            <td className="px-4 py-3">
                                                <div className="flex flex-wrap items-center gap-1">
                                                    {/* Toggle */}
                                                    <button
                                                        onClick={() => handleToggle(lic.id)}
                                                        className={`px-2 py-1 rounded text-xs font-medium transition cursor-pointer ${
                                                            lic.is_active
                                                                ? 'bg-yellow-100 text-yellow-700 hover:bg-yellow-200'
                                                                : 'bg-green-100 text-green-700 hover:bg-green-200'
                                                        }`}
                                                        title={lic.is_active ? 'Desactivar' : 'Activar'}
                                                    >
                                                        {lic.is_active ? 'Desactivar' : 'Activar'}
                                                    </button>

                                                    {/* Unbind */}
                                                    {lic.hardware_id && (
                                                        <button
                                                            onClick={() => handleUnbind(lic.id)}
                                                            className="px-2 py-1 rounded text-xs font-medium bg-blue-100 text-blue-700 hover:bg-blue-200 transition cursor-pointer"
                                                            title="Desvincular hardware"
                                                        >
                                                            Desvincular
                                                        </button>
                                                    )}

                                                    {/* Renew */}
                                                    {renewId === lic.id ? (
                                                        <div className="flex items-center gap-1">
                                                            <select
                                                                value={renewDays}
                                                                onChange={(e) => setRenewDays(parseInt(e.target.value))}
                                                                className="px-1 py-1 border border-gray-300 rounded text-xs"
                                                            >
                                                                {DURATION_OPTIONS.map((opt) => (
                                                                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                                                                ))}
                                                            </select>
                                                            <button
                                                                onClick={() => handleRenew(lic.id)}
                                                                className="px-2 py-1 rounded text-xs font-medium bg-green-600 text-white hover:bg-green-700 transition cursor-pointer"
                                                            >
                                                                OK
                                                            </button>
                                                            <button
                                                                onClick={() => setRenewId(null)}
                                                                className="px-2 py-1 rounded text-xs font-medium bg-gray-200 text-gray-600 hover:bg-gray-300 transition cursor-pointer"
                                                            >
                                                                X
                                                            </button>
                                                        </div>
                                                    ) : (
                                                        <button
                                                            onClick={() => { setRenewId(lic.id); setRenewDays(30); }}
                                                            className="px-2 py-1 rounded text-xs font-medium bg-purple-100 text-purple-700 hover:bg-purple-200 transition cursor-pointer"
                                                            title="Renovar licencia"
                                                        >
                                                            Renovar
                                                        </button>
                                                    )}

                                                    {/* Delete */}
                                                    <button
                                                        onClick={() => handleDelete(lic.id)}
                                                        className="px-2 py-1 rounded text-xs font-medium bg-red-100 text-red-700 hover:bg-red-200 transition cursor-pointer"
                                                        title="Eliminar licencia"
                                                    >
                                                        Eliminar
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                        {factusId === lic.id && (
                                            <tr>
                                                <td colSpan="9" className="px-4 py-4 bg-green-50 border-b border-green-200">
                                                    <div className="max-w-3xl">
                                                        <div className="flex items-center justify-between mb-3">
                                                            <h3 className="text-sm font-bold text-[#146e39]">Configuracion Factus - {lic.license_key}</h3>
                                                            <button onClick={() => setFactusId(null)} className="text-gray-400 hover:text-gray-600 cursor-pointer text-lg">&times;</button>
                                                        </div>
                                                        <div className="grid grid-cols-2 gap-3 mb-3">
                                                            <label className="flex items-center gap-2 text-sm">
                                                                <input
                                                                    type="checkbox"
                                                                    checked={factusForm.factus_enabled}
                                                                    onChange={(e) => setFactusForm({...factusForm, factus_enabled: e.target.checked})}
                                                                    className="rounded"
                                                                />
                                                                Facturacion electronica habilitada
                                                            </label>
                                                            <label className="flex items-center gap-2 text-sm">
                                                                <input
                                                                    type="checkbox"
                                                                    checked={factusForm.factus_sandbox}
                                                                    onChange={(e) => setFactusForm({...factusForm, factus_sandbox: e.target.checked})}
                                                                    className="rounded"
                                                                />
                                                                Modo Sandbox (pruebas)
                                                            </label>
                                                        </div>
                                                        <div className="grid grid-cols-2 gap-3 mb-3">
                                                            <div>
                                                                <label className="block text-xs font-medium text-gray-600 mb-1">Client ID</label>
                                                                <input
                                                                    type="text"
                                                                    value={factusForm.factus_client_id}
                                                                    onChange={(e) => setFactusForm({...factusForm, factus_client_id: e.target.value})}
                                                                    className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:ring-1 focus:ring-green-500 outline-none"
                                                                    placeholder="Client ID de Factus"
                                                                />
                                                            </div>
                                                            <div>
                                                                <label className="block text-xs font-medium text-gray-600 mb-1">Client Secret</label>
                                                                <input
                                                                    type="password"
                                                                    value={factusForm.factus_client_secret}
                                                                    onChange={(e) => setFactusForm({...factusForm, factus_client_secret: e.target.value})}
                                                                    className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:ring-1 focus:ring-green-500 outline-none"
                                                                    placeholder="Dejar vacio para no cambiar"
                                                                />
                                                            </div>
                                                            <div>
                                                                <label className="block text-xs font-medium text-gray-600 mb-1">Usuario (Email)</label>
                                                                <input
                                                                    type="text"
                                                                    value={factusForm.factus_username}
                                                                    onChange={(e) => setFactusForm({...factusForm, factus_username: e.target.value})}
                                                                    className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:ring-1 focus:ring-green-500 outline-none"
                                                                    placeholder="Email de Factus"
                                                                />
                                                            </div>
                                                            <div>
                                                                <label className="block text-xs font-medium text-gray-600 mb-1">Contrasena</label>
                                                                <input
                                                                    type="password"
                                                                    value={factusForm.factus_password}
                                                                    onChange={(e) => setFactusForm({...factusForm, factus_password: e.target.value})}
                                                                    className="w-full px-2 py-1.5 border border-gray-300 rounded text-sm focus:ring-1 focus:ring-green-500 outline-none"
                                                                    placeholder="Dejar vacio para no cambiar"
                                                                />
                                                            </div>
                                                        </div>
                                                        <div className="flex gap-2">
                                                            <button
                                                                onClick={() => handleFactusSave(lic.id)}
                                                                className="px-4 py-1.5 bg-[#146e39] hover:bg-green-800 text-white text-sm font-medium rounded transition cursor-pointer"
                                                            >
                                                                Guardar Factus
                                                            </button>
                                                            <button
                                                                onClick={() => setFactusId(null)}
                                                                className="px-4 py-1.5 bg-gray-200 hover:bg-gray-300 text-gray-700 text-sm font-medium rounded transition cursor-pointer"
                                                            >
                                                                Cancelar
                                                            </button>
                                                        </div>
                                                    </div>
                                                </td>
                                            </tr>
                                        )}
                                        </React.Fragment>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </main>
            </div>
        </>
    );
}
