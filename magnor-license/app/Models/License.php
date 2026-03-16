<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
use Carbon\Carbon;

class License extends Model
{
    protected $fillable = [
        'license_key',
        'customer_name',
        'customer_email',
        'duration_days',
        'hardware_id',
        'is_active',
        'activated_at',
        'expires_at',
    ];

    protected $casts = [
        'is_active' => 'boolean',
        'activated_at' => 'datetime',
        'expires_at' => 'datetime',
        'duration_days' => 'integer',
    ];

    protected $appends = ['status', 'days_left'];

    public function activationLogs(): HasMany
    {
        return $this->hasMany(ActivationLog::class);
    }

    // Scopes
    public function scopeActive($query)
    {
        return $query->where('is_active', true)
            ->whereNotNull('hardware_id')
            ->where(function ($q) {
                $q->whereNull('expires_at')
                  ->orWhere('expires_at', '>', now());
            });
    }

    public function scopeExpired($query)
    {
        return $query->where('is_active', true)
            ->whereNotNull('expires_at')
            ->where('expires_at', '<=', now());
    }

    public function scopeUnused($query)
    {
        return $query->where('is_active', true)->whereNull('hardware_id');
    }

    public function scopePermanent($query)
    {
        return $query->where('duration_days', 0);
    }

    // Accessors
    public function getStatusAttribute(): string
    {
        if (!$this->is_active) {
            return 'inactive';
        }

        if (is_null($this->hardware_id)) {
            return 'unused';
        }

        if ($this->duration_days > 0 && $this->expires_at && $this->expires_at->isPast()) {
            return 'expired';
        }

        return 'active';
    }

    public function getDaysLeftAttribute(): ?int
    {
        if ($this->duration_days === 0) {
            return null; // permanent
        }

        if (is_null($this->expires_at)) {
            return $this->duration_days;
        }

        $daysLeft = (int) now()->diffInDays($this->expires_at, false);
        return max(0, $daysLeft);
    }

    public static function generateKey(): string
    {
        $segments = [];
        $chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
        for ($i = 0; $i < 4; $i++) {
            $segment = '';
            for ($j = 0; $j < 5; $j++) {
                $segment .= $chars[random_int(0, strlen($chars) - 1)];
            }
            $segments[] = $segment;
        }
        return 'MAGNOR-' . implode('-', $segments);
    }
}
