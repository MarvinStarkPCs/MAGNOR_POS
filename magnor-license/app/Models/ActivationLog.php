<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;

class ActivationLog extends Model
{
    protected $fillable = [
        'license_id',
        'hardware_id',
        'action',
        'ip_address',
    ];

    public function license(): BelongsTo
    {
        return $this->belongsTo(License::class);
    }
}
