<?php

namespace App\Models\Backup;

use Illuminate\Database\Eloquent\Model;

class BackupProduct extends Model
{
    protected $fillable = [
        'license_key',
        'name',
        'sku',
        'barcode',
        'description',
        'sale_price',
        'purchase_price',
        'current_stock',
        'minimum_stock',
        'tax_rate',
        'image_url',
        'category_name',
        'is_active',
        'local_id',
        'synced_at',
    ];

    protected $casts = [
        'sale_price' => 'decimal:2',
        'purchase_price' => 'decimal:2',
        'current_stock' => 'decimal:2',
        'minimum_stock' => 'decimal:2',
        'tax_rate' => 'decimal:2',
        'is_active' => 'boolean',
        'local_id' => 'integer',
        'synced_at' => 'datetime',
    ];
}
