<?php

namespace App\Models\Backup;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;

class BackupSale extends Model
{
    protected $fillable = [
        'license_key',
        'sale_number',
        'customer_name',
        'subtotal',
        'tax_amount',
        'discount_amount',
        'total',
        'payment_type',
        'status',
        'sale_date',
        'cashier_name',
        'notes',
        'factus_cufe',
        'factus_qr_code',
        'factus_number',
        'factus_prefix',
        'factus_status',
        'local_id',
        'synced_at',
    ];

    protected $casts = [
        'subtotal' => 'decimal:2',
        'tax_amount' => 'decimal:2',
        'discount_amount' => 'decimal:2',
        'total' => 'decimal:2',
        'sale_date' => 'datetime',
        'local_id' => 'integer',
        'synced_at' => 'datetime',
    ];

    public function details(): HasMany
    {
        return $this->hasMany(BackupSaleDetail::class);
    }
}
