<?php

namespace App\Models\Backup;

use Illuminate\Database\Eloquent\Model;

class BackupSyncLog extends Model
{
    protected $fillable = [
        'license_key',
        'sync_type',
        'records_synced',
        'ip_address',
        'synced_at',
    ];

    protected $casts = [
        'records_synced' => 'integer',
        'synced_at' => 'datetime',
    ];
}
