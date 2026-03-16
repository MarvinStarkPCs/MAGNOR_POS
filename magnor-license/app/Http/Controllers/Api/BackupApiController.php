<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Backup\BackupCustomer;
use App\Models\Backup\BackupProduct;
use App\Models\Backup\BackupPurchase;
use App\Models\Backup\BackupSale;
use App\Models\Backup\BackupSaleDetail;
use App\Models\Backup\BackupSupplier;
use App\Models\Backup\BackupSyncLog;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Validator;

class BackupApiController extends Controller
{
    public function sync(Request $request): JsonResponse
    {
        $validator = Validator::make($request->all(), [
            'license_key' => 'required|string|max:255',
            'customers' => 'nullable|array',
            'customers.*.local_id' => 'required|integer',
            'customers.*.full_name' => 'required|string|max:255',
            'products' => 'nullable|array',
            'products.*.local_id' => 'required|integer',
            'products.*.name' => 'required|string|max:255',
            'sales' => 'nullable|array',
            'sales.*.local_id' => 'required|integer',
            'sales.*.details' => 'nullable|array',
            'suppliers' => 'nullable|array',
            'suppliers.*.local_id' => 'required|integer',
            'suppliers.*.company_name' => 'required|string|max:255',
            'purchases' => 'nullable|array',
            'purchases.*.local_id' => 'required|integer',
        ]);

        if ($validator->fails()) {
            return response()->json([
                'success' => false,
                'message' => 'Datos de validacion invalidos.',
                'errors' => $validator->errors(),
            ], 422);
        }

        $licenseKey = $request->input('license_key');
        $now = now();
        $counts = [
            'customers' => 0,
            'products' => 0,
            'sales' => 0,
            'suppliers' => 0,
            'purchases' => 0,
        ];

        try {
            DB::beginTransaction();

            // --- Customers ---
            $customers = $request->input('customers', []);
            $customerLocalIds = collect($customers)->pluck('local_id')->toArray();

            // Delete records not in the new batch
            BackupCustomer::where('license_key', $licenseKey)
                ->when(!empty($customerLocalIds), fn($q) => $q->whereNotIn('local_id', $customerLocalIds))
                ->when(empty($customerLocalIds), fn($q) => $q)
                ->delete();

            foreach ($customers as $item) {
                BackupCustomer::updateOrCreate(
                    ['license_key' => $licenseKey, 'local_id' => $item['local_id']],
                    array_merge($this->filterFields($item, BackupCustomer::class), ['synced_at' => $now])
                );
                $counts['customers']++;
            }

            // --- Products ---
            $products = $request->input('products', []);
            $productLocalIds = collect($products)->pluck('local_id')->toArray();

            BackupProduct::where('license_key', $licenseKey)
                ->when(!empty($productLocalIds), fn($q) => $q->whereNotIn('local_id', $productLocalIds))
                ->when(empty($productLocalIds), fn($q) => $q)
                ->delete();

            foreach ($products as $item) {
                BackupProduct::updateOrCreate(
                    ['license_key' => $licenseKey, 'local_id' => $item['local_id']],
                    array_merge($this->filterFields($item, BackupProduct::class), ['synced_at' => $now])
                );
                $counts['products']++;
            }

            // --- Sales (with details) ---
            $sales = $request->input('sales', []);
            $saleLocalIds = collect($sales)->pluck('local_id')->toArray();

            // Delete sales not in the new batch (cascade deletes details)
            BackupSale::where('license_key', $licenseKey)
                ->when(!empty($saleLocalIds), fn($q) => $q->whereNotIn('local_id', $saleLocalIds))
                ->when(empty($saleLocalIds), fn($q) => $q)
                ->delete();

            foreach ($sales as $item) {
                $details = $item['details'] ?? [];
                $saleData = $this->filterFields($item, BackupSale::class);
                unset($saleData['details']);

                $sale = BackupSale::updateOrCreate(
                    ['license_key' => $licenseKey, 'local_id' => $item['local_id']],
                    array_merge($saleData, ['synced_at' => $now])
                );

                // Replace all details for this sale
                $sale->details()->delete();
                foreach ($details as $detail) {
                    $sale->details()->create($this->filterFields($detail, BackupSaleDetail::class));
                }

                $counts['sales']++;
            }

            // --- Suppliers ---
            $suppliers = $request->input('suppliers', []);
            $supplierLocalIds = collect($suppliers)->pluck('local_id')->toArray();

            BackupSupplier::where('license_key', $licenseKey)
                ->when(!empty($supplierLocalIds), fn($q) => $q->whereNotIn('local_id', $supplierLocalIds))
                ->when(empty($supplierLocalIds), fn($q) => $q)
                ->delete();

            foreach ($suppliers as $item) {
                BackupSupplier::updateOrCreate(
                    ['license_key' => $licenseKey, 'local_id' => $item['local_id']],
                    array_merge($this->filterFields($item, BackupSupplier::class), ['synced_at' => $now])
                );
                $counts['suppliers']++;
            }

            // --- Purchases ---
            $purchases = $request->input('purchases', []);
            $purchaseLocalIds = collect($purchases)->pluck('local_id')->toArray();

            BackupPurchase::where('license_key', $licenseKey)
                ->when(!empty($purchaseLocalIds), fn($q) => $q->whereNotIn('local_id', $purchaseLocalIds))
                ->when(empty($purchaseLocalIds), fn($q) => $q)
                ->delete();

            foreach ($purchases as $item) {
                BackupPurchase::updateOrCreate(
                    ['license_key' => $licenseKey, 'local_id' => $item['local_id']],
                    array_merge($this->filterFields($item, BackupPurchase::class), ['synced_at' => $now])
                );
                $counts['purchases']++;
            }

            // --- Sync Log ---
            $totalRecords = array_sum($counts);
            BackupSyncLog::create([
                'license_key' => $licenseKey,
                'sync_type' => 'full',
                'records_synced' => $totalRecords,
                'ip_address' => $request->ip(),
                'synced_at' => $now,
            ]);

            DB::commit();

            return response()->json([
                'success' => true,
                'message' => "Sincronizacion completada. {$totalRecords} registros sincronizados.",
                'synced' => $counts,
            ]);
        } catch (\Throwable $e) {
            DB::rollBack();

            return response()->json([
                'success' => false,
                'message' => 'Error durante la sincronizacion: ' . $e->getMessage(),
            ], 500);
        }
    }

    public function status(Request $request): JsonResponse
    {
        $validator = Validator::make($request->all(), [
            'license_key' => 'required|string|max:255',
        ]);

        if ($validator->fails()) {
            return response()->json([
                'success' => false,
                'message' => 'License key es requerido.',
                'errors' => $validator->errors(),
            ], 422);
        }

        $licenseKey = $request->input('license_key');

        $lastSync = BackupSyncLog::where('license_key', $licenseKey)
            ->latest('synced_at')
            ->first();

        return response()->json([
            'success' => true,
            'license_key' => $licenseKey,
            'last_sync' => $lastSync ? $lastSync->synced_at->toIso8601String() : null,
            'counts' => [
                'customers' => BackupCustomer::where('license_key', $licenseKey)->count(),
                'products' => BackupProduct::where('license_key', $licenseKey)->count(),
                'sales' => BackupSale::where('license_key', $licenseKey)->count(),
                'suppliers' => BackupSupplier::where('license_key', $licenseKey)->count(),
                'purchases' => BackupPurchase::where('license_key', $licenseKey)->count(),
            ],
        ]);
    }

    /**
     * Filter input data to only include fields that are fillable on the model.
     */
    private function filterFields(array $data, string $modelClass): array
    {
        $fillable = (new $modelClass)->getFillable();
        return array_intersect_key($data, array_flip($fillable));
    }
}
