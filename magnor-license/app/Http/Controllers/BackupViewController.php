<?php

namespace App\Http\Controllers;

use App\Models\Backup\BackupCustomer;
use App\Models\Backup\BackupProduct;
use App\Models\Backup\BackupPurchase;
use App\Models\Backup\BackupSale;
use App\Models\Backup\BackupSupplier;
use App\Models\Backup\BackupSyncLog;
use Inertia\Inertia;
use Inertia\Response;

class BackupViewController extends Controller
{
    public function index(): Response
    {
        // Get all unique license keys that have any backup data
        $licenseKeys = collect()
            ->merge(BackupCustomer::distinct()->pluck('license_key'))
            ->merge(BackupProduct::distinct()->pluck('license_key'))
            ->merge(BackupSale::distinct()->pluck('license_key'))
            ->merge(BackupSupplier::distinct()->pluck('license_key'))
            ->merge(BackupPurchase::distinct()->pluck('license_key'))
            ->unique()
            ->sort()
            ->values()
            ->toArray();

        // Build data per license key
        $backupData = [];
        foreach ($licenseKeys as $key) {
            $lastSync = BackupSyncLog::where('license_key', $key)
                ->latest('synced_at')
                ->first();

            $backupData[$key] = [
                'customers' => BackupCustomer::where('license_key', $key)->get(),
                'products' => BackupProduct::where('license_key', $key)->get(),
                'sales' => BackupSale::where('license_key', $key)->with('details')->get(),
                'suppliers' => BackupSupplier::where('license_key', $key)->get(),
                'purchases' => BackupPurchase::where('license_key', $key)->get(),
                'last_sync' => $lastSync?->synced_at?->toIso8601String(),
                'stats' => [
                    'customers' => BackupCustomer::where('license_key', $key)->count(),
                    'products' => BackupProduct::where('license_key', $key)->count(),
                    'sales' => BackupSale::where('license_key', $key)->count(),
                    'suppliers' => BackupSupplier::where('license_key', $key)->count(),
                    'purchases' => BackupPurchase::where('license_key', $key)->count(),
                    'total_revenue' => (float) BackupSale::where('license_key', $key)->sum('total'),
                ],
            ];
        }

        return Inertia::render('Backup/Index', [
            'licenseKeys' => $licenseKeys,
            'backupData' => $backupData,
        ]);
    }

    public function invoices(): Response
    {
        // Get all unique license keys with sales
        $licenseKeys = BackupSale::distinct()->pluck('license_key')->sort()->values()->toArray();

        // Get invoices per license
        $invoiceData = [];
        foreach ($licenseKeys as $key) {
            $invoiceData[$key] = [
                'invoices' => BackupSale::where('license_key', $key)
                    ->with('details')
                    ->orderByDesc('sale_date')
                    ->get(),
                'stats' => [
                    'total_invoices' => BackupSale::where('license_key', $key)->count(),
                    'factus_invoices' => BackupSale::where('license_key', $key)
                        ->whereNotNull('factus_number')
                        ->where('factus_number', '!=', '')
                        ->count(),
                    'total_revenue' => (float) BackupSale::where('license_key', $key)->sum('total'),
                    'total_tax' => (float) BackupSale::where('license_key', $key)->sum('tax_amount'),
                ],
            ];
        }

        return Inertia::render('Invoices/Index', [
            'licenseKeys' => $licenseKeys,
            'invoiceData' => $invoiceData,
        ]);
    }
}
