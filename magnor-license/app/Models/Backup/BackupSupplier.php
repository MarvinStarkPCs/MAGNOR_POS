<?php

namespace App\Models\Backup;

use Illuminate\Database\Eloquent\Model;

class BackupSupplier extends Model
{
    protected $fillable = [
        'license_key',
        'company_name',
        'contact_name',
        'document_type',
        'document_number',
        'phone',
        'email',
        'address',
        'website',
        'payment_term_days',
        'notes',
        'is_active',
        'local_id',
        'synced_at',
    ];

    protected $casts = [
        'payment_term_days' => 'integer',
        'is_active' => 'boolean',
        'local_id' => 'integer',
        'synced_at' => 'datetime',
    ];
}
