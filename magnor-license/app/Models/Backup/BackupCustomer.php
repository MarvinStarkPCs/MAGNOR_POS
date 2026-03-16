<?php

namespace App\Models\Backup;

use Illuminate\Database\Eloquent\Model;

class BackupCustomer extends Model
{
    protected $fillable = [
        'license_key',
        'full_name',
        'document_type',
        'document_number',
        'phone',
        'email',
        'address',
        'city',
        'state',
        'postal_code',
        'customer_type',
        'discount_percentage',
        'credit_limit',
        'notes',
        'is_active',
        'local_id',
        'synced_at',
    ];

    protected $casts = [
        'discount_percentage' => 'decimal:2',
        'credit_limit' => 'decimal:2',
        'is_active' => 'boolean',
        'local_id' => 'integer',
        'synced_at' => 'datetime',
    ];
}
