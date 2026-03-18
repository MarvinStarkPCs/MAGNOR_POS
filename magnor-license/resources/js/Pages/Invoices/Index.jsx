import React, { useState, useMemo } from 'react';
import { Head, router } from '@inertiajs/react';

function formatDate(dateStr) {
    if (!dateStr) return '-';
    const d = new Date(dateStr);
    return d.toLocaleDateString('es-MX', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function formatCurrency(value) {
    const num = typeof value === 'string' ? parseFloat(value) : value;
    return '$' + num.toLocaleString('es-MX', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
}

function StatCard({ label, value, subtitle, color }) {
    return (
        <div className="bg-white rounded-xl shadow-sm p-5 border border-gray-100">
            <div className="text-sm text-gray-500 font-medium">{label}</div>
            <div className={'text-3xl font-bold mt-1 ' + (color || 'text-[#146e39]')}>{value}</div>
            {subtitle && <div className="text-xs text-gray-400 mt-1">{subtitle}</div>}
        </div>
    );
}

export default function InvoicesIndex({ licenseKeys, invoiceData }) {
    const [selectedKey, setSelectedKey] = useState(licenseKeys[0] || '');
    const [search, setSearch] = useState('');
    const [filterType, setFilterType] = useState('all'); // all, factus, regular
    const [expandedId, setExpandedId] = useState(null);

    const data = selectedKey ? invoiceData[selectedKey] : null;
    const stats = data?.stats;

    const searchLower = search.toLowerCase();

    const filteredInvoices = useMemo(() => {
        if (!data) return [];
        return data.invoices.filter((inv) => {
            // Search filter
            const matchesSearch =
                (inv.sale_number && inv.sale_number.toLowerCase().includes(searchLower)) ||
                (inv.customer_name && inv.customer_name.toLowerCase().includes(searchLower)) ||
                (inv.factus_number && inv.factus_number.toLowerCase().includes(searchLower)) ||
                (inv.factus_prefix && inv.factus_prefix.toLowerCase().includes(searchLower)) ||
                (inv.factus_cufe && inv.factus_cufe.toLowerCase().includes(searchLower)) ||
                (inv.cashier_name && inv.cashier_name.toLowerCase().includes(searchLower));

            // Type filter
            const hasFactus = inv.factus_number && inv.factus_number !== '';
            const matchesType =
                filterType === 'all' ||
                (filterType === 'factus' && hasFactus) ||
                (filterType === 'regular' && !hasFactus);

            return matchesSearch && matchesType;
        });
    }, [data, searchLower, filterType]);

    return (
        <>
            <Head title="Facturas" />
            <div className="min-h-screen bg-gray-50">
                {/* Header */}
                <header className="bg-white border-b border-gray-200 shadow-sm">
                    <div className="max-w-7xl mx-auto px-4 py-4 flex justify-between items-center">
                        <h1 className="text-xl font-bold text-[#146e39] tracking-tight">
                            MAGNOR - Facturas
                        </h1>
                        <div className="flex items-center gap-3">
                            <button
                                onClick={() => router.get('/dashboard')}
                                className="px-4 py-2 text-sm text-gray-600 hover:text-blue-700 hover:bg-blue-50 rounded-lg transition cursor-pointer font-medium"
                            >
                                Licencias
                            </button>
                            <button
                                onClick={() => router.get('/backup')}
                                className="px-4 py-2 text-sm text-gray-600 hover:text-[#146e39] hover:bg-green-50 rounded-lg transition cursor-pointer font-medium"
                            >
                                Backup
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
                    {licenseKeys.length === 0 ? (
                        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
                            <div className="text-gray-400 text-lg">No hay facturas disponibles.</div>
                            <div className="text-gray-300 text-sm mt-2">Las facturas apareceran cuando un POS sincronice sus datos.</div>
                        </div>
                    ) : (
                        <>
                            {/* License selector */}
                            <div className="flex flex-wrap items-center gap-4">
                                <div className="flex items-center gap-2">
                                    <label className="text-sm font-medium text-gray-700">Licencia:</label>
                                    <select
                                        value={selectedKey}
                                        onChange={(e) => { setSelectedKey(e.target.value); setSearch(''); }}
                                        className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 outline-none text-sm font-mono"
                                    >
                                        {licenseKeys.map((key) => (
                                            <option key={key} value={key}>{key}</option>
                                        ))}
                                    </select>
                                </div>
                            </div>

                            {/* Stats */}
                            {stats && (
                                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                                    <StatCard label="Total Facturas" value={stats.total_invoices} />
                                    <StatCard
                                        label="Facturas DIAN"
                                        value={stats.factus_invoices}
                                        subtitle="Con factura electronica"
                                        color="text-blue-700"
                                    />
                                    <StatCard
                                        label="Ingresos"
                                        value={formatCurrency(stats.total_revenue)}
                                        color="text-[#146e39]"
                                    />
                                    <StatCard
                                        label="Impuestos"
                                        value={formatCurrency(stats.total_tax)}
                                        color="text-orange-600"
                                    />
                                </div>
                            )}

                            {/* Filters */}
                            <div className="flex flex-wrap items-center gap-3">
                                <div className="flex gap-2">
                                    {[
                                        { key: 'all', label: 'Todas' },
                                        { key: 'factus', label: 'DIAN (Factus)' },
                                        { key: 'regular', label: 'Sin Factura Electronica' },
                                    ].map((f) => (
                                        <button
                                            key={f.key}
                                            onClick={() => setFilterType(f.key)}
                                            className={'px-4 py-2 text-sm rounded-lg font-medium transition cursor-pointer ' +
                                                (filterType === f.key
                                                    ? 'bg-[#146e39] text-white'
                                                    : 'bg-white text-gray-600 hover:bg-gray-100 border border-gray-200')
                                            }
                                        >
                                            {f.label}
                                        </button>
                                    ))}
                                </div>
                                <input
                                    type="text"
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                    placeholder="Buscar por numero, cliente, CUFE..."
                                    className="flex-1 min-w-64 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500 outline-none text-sm"
                                />
                            </div>

                            {/* Results count */}
                            <div className="text-sm text-gray-500">
                                {filteredInvoices.length} factura(s) encontrada(s)
                            </div>

                            {/* Invoices Table */}
                            <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
                                <div className="overflow-x-auto">
                                    <table className="w-full text-sm">
                                        <thead>
                                            <tr className="bg-gray-50 border-b border-gray-200">
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">No. Venta</th>
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">Factura DIAN</th>
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">Cliente</th>
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">Cajero</th>
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">Pago</th>
                                                <th className="text-right px-4 py-3 font-semibold text-gray-600">Subtotal</th>
                                                <th className="text-right px-4 py-3 font-semibold text-gray-600">IVA</th>
                                                <th className="text-right px-4 py-3 font-semibold text-gray-600">Total</th>
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">Estado</th>
                                                <th className="text-left px-4 py-3 font-semibold text-gray-600">Fecha</th>
                                                <th className="px-4 py-3"></th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-gray-100">
                                            {filteredInvoices.length === 0 && (
                                                <tr><td colSpan={11} className="text-center py-8 text-gray-400">Sin resultados.</td></tr>
                                            )}
                                            {filteredInvoices.map((inv) => {
                                                const hasFactus = inv.factus_number && inv.factus_number !== '';
                                                return (
                                                    <React.Fragment key={inv.id}>
                                                        <tr className="hover:bg-gray-50 transition">
                                                            <td className="px-4 py-3 font-mono text-xs font-medium text-gray-800">
                                                                {inv.sale_number || '-'}
                                                            </td>
                                                            <td className="px-4 py-3">
                                                                {hasFactus ? (
                                                                    <span className="inline-flex items-center gap-1">
                                                                        <span className="inline-block w-2 h-2 bg-green-500 rounded-full"></span>
                                                                        <span className="font-mono text-xs font-medium text-blue-800">
                                                                            {inv.factus_prefix}{inv.factus_number}
                                                                        </span>
                                                                    </span>
                                                                ) : (
                                                                    <span className="text-gray-400 text-xs">-</span>
                                                                )}
                                                            </td>
                                                            <td className="px-4 py-3 text-gray-600">{inv.customer_name || '-'}</td>
                                                            <td className="px-4 py-3 text-gray-500 text-xs">{inv.cashier_name || '-'}</td>
                                                            <td className="px-4 py-3 text-gray-500 text-xs">{inv.payment_type || '-'}</td>
                                                            <td className="px-4 py-3 text-right text-gray-500">{formatCurrency(inv.subtotal)}</td>
                                                            <td className="px-4 py-3 text-right text-gray-500">{formatCurrency(inv.tax_amount)}</td>
                                                            <td className="px-4 py-3 text-right font-medium text-gray-800">{formatCurrency(inv.total)}</td>
                                                            <td className="px-4 py-3">
                                                                <span className={'inline-block px-2.5 py-1 rounded-full text-xs font-medium ' +
                                                                    (inv.status === 'Completada'
                                                                        ? 'bg-green-100 text-green-800'
                                                                        : inv.status === 'Anulada'
                                                                        ? 'bg-red-100 text-red-800'
                                                                        : 'bg-gray-100 text-gray-800')
                                                                }>
                                                                    {inv.status || '-'}
                                                                </span>
                                                            </td>
                                                            <td className="px-4 py-3 text-xs text-gray-500">{formatDate(inv.sale_date)}</td>
                                                            <td className="px-4 py-3">
                                                                <button
                                                                    onClick={() => setExpandedId(expandedId === inv.id ? null : inv.id)}
                                                                    className="text-xs text-[#146e39] hover:underline cursor-pointer"
                                                                >
                                                                    {expandedId === inv.id ? 'Ocultar' : 'Detalle'}
                                                                </button>
                                                            </td>
                                                        </tr>
                                                        {expandedId === inv.id && (
                                                            <tr>
                                                                <td colSpan={11} className="px-4 py-4 bg-gray-50 border-b border-gray-200">
                                                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                                                        {/* Factus Info */}
                                                                        {hasFactus && (
                                                                            <div className="bg-white rounded-lg border border-green-200 p-4">
                                                                                <h4 className="text-sm font-bold text-[#146e39] mb-3">Factura Electronica DIAN</h4>
                                                                                <div className="space-y-2 text-xs">
                                                                                    <div className="flex justify-between">
                                                                                        <span className="text-gray-500">Numero:</span>
                                                                                        <span className="font-mono font-medium">{inv.factus_prefix}{inv.factus_number}</span>
                                                                                    </div>
                                                                                    <div className="flex justify-between">
                                                                                        <span className="text-gray-500">Estado Factus:</span>
                                                                                        <span className={'font-medium ' + (inv.factus_status === 'validated' ? 'text-green-700' : 'text-gray-700')}>
                                                                                            {inv.factus_status || '-'}
                                                                                        </span>
                                                                                    </div>
                                                                                    {inv.factus_cufe && (
                                                                                        <div>
                                                                                            <span className="text-gray-500">CUFE:</span>
                                                                                            <div className="font-mono text-[10px] text-gray-600 mt-1 break-all bg-gray-50 p-2 rounded">
                                                                                                {inv.factus_cufe}
                                                                                            </div>
                                                                                        </div>
                                                                                    )}
                                                                                </div>
                                                                            </div>
                                                                        )}

                                                                        {/* Products */}
                                                                        <div className={'bg-white rounded-lg border border-gray-200 p-4' + (hasFactus ? '' : ' md:col-span-2')}>
                                                                            <h4 className="text-sm font-bold text-gray-700 mb-3">Productos ({inv.details ? inv.details.length : 0})</h4>
                                                                            {inv.details && inv.details.length > 0 ? (
                                                                                <table className="w-full text-xs">
                                                                                    <thead>
                                                                                        <tr className="text-gray-500 border-b border-gray-100">
                                                                                            <th className="text-left py-1 pr-3">Producto</th>
                                                                                            <th className="text-right py-1 pr-3">Cant.</th>
                                                                                            <th className="text-right py-1 pr-3">P. Unit.</th>
                                                                                            <th className="text-right py-1">Total</th>
                                                                                        </tr>
                                                                                    </thead>
                                                                                    <tbody>
                                                                                        {inv.details.map((d, idx) => (
                                                                                            <tr key={idx} className="border-t border-gray-50">
                                                                                                <td className="py-1.5 pr-3 text-gray-700">{d.product_name}</td>
                                                                                                <td className="text-right py-1.5 pr-3 text-gray-600">{parseFloat(d.quantity)}</td>
                                                                                                <td className="text-right py-1.5 pr-3 text-gray-600">{formatCurrency(d.unit_price)}</td>
                                                                                                <td className="text-right py-1.5 font-medium text-gray-800">{formatCurrency(d.total)}</td>
                                                                                            </tr>
                                                                                        ))}
                                                                                    </tbody>
                                                                                </table>
                                                                            ) : (
                                                                                <div className="text-gray-400 text-xs">Sin detalle de productos</div>
                                                                            )}
                                                                        </div>
                                                                    </div>

                                                                    {inv.notes && (
                                                                        <div className="mt-3 text-xs text-gray-500">
                                                                            <span className="font-medium">Notas:</span> {inv.notes}
                                                                        </div>
                                                                    )}
                                                                </td>
                                                            </tr>
                                                        )}
                                                    </React.Fragment>
                                                );
                                            })}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </>
                    )}
                </main>
            </div>
        </>
    );
}
