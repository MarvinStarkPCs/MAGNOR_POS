<?php

namespace App\Models\Backup;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class BackupSaleDetail extends Model
{
    protected $fillable = [
        'backup_sale_id',
        'product_name',
        'quantity',
        'unit_price',
        'discount',
        'subtotal',
        'tax_amount',
        'total',
        'local_id',
    ];

    protected $casts = [
        'quantity' => 'decimal:2',
        'unit_price' => 'decimal:2',
        'discount' => 'decimal:2',
        'subtotal' => 'decimal:2',
        'tax_amount' => 'decimal:2',
        'total' => 'decimal:2',
        'local_id' => 'integer',
    ];

    public function sale(): BelongsTo
    {
        return $this->belongsTo(BackupSale::class, 'backup_sale_id');
    }
}
