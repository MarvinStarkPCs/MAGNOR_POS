<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\License;
use App\Models\ActivationLog;
use Illuminate\Http\Request;
use Carbon\Carbon;

class LicenseApiController extends Controller
{
    public function activate(Request $request)
    {
        $request->validate([
            'license_key' => 'required|string',
            'hardware_id' => 'required|string',
        ]);

        $license = License::where('license_key', $request->license_key)->first();

        if (!$license) {
            return response()->json([
                'success' => false,
                'message' => 'Licencia no encontrada.',
            ], 404);
        }

        if (!$license->is_active) {
            return response()->json([
                'success' => false,
                'message' => 'Licencia desactivada.',
            ], 403);
        }

        // If already bound to a different hardware
        if ($license->hardware_id && $license->hardware_id !== $request->hardware_id) {
            return response()->json([
                'success' => false,
                'message' => 'Licencia ya vinculada a otro equipo.',
            ], 403);
        }

        // First activation or re-activation on same hardware
        if (!$license->hardware_id) {
            $license->hardware_id = $request->hardware_id;
            $license->activated_at = now();

            if ($license->duration_days > 0) {
                $license->expires_at = now()->addDays($license->duration_days);
            } else {
                $license->expires_at = null; // permanent
            }

            $license->save();
        }

        ActivationLog::create([
            'license_id' => $license->id,
            'hardware_id' => $request->hardware_id,
            'action' => 'activate',
            'ip_address' => $request->ip(),
        ]);

        return response()->json([
            'success' => true,
            'license' => [
                'key' => $license->license_key,
                'customer' => $license->customer_name,
                'activated_at' => $license->activated_at?->toISOString(),
                'expires_at' => $license->expires_at?->toISOString(),
                'is_permanent' => $license->duration_days === 0,
            ],
        ]);
    }

    public function validate(Request $request)
    {
        $request->validate([
            'license_key' => 'required|string',
            'hardware_id' => 'required|string',
        ]);

        $license = License::where('license_key', $request->license_key)->first();

        if (!$license) {
            return response()->json([
                'success' => false,
                'valid' => false,
                'message' => 'Licencia no encontrada.',
            ], 404);
        }

        if (!$license->is_active) {
            return response()->json([
                'success' => true,
                'valid' => false,
                'message' => 'Licencia desactivada.',
            ]);
        }

        if ($license->hardware_id !== $request->hardware_id) {
            return response()->json([
                'success' => true,
                'valid' => false,
                'message' => 'Hardware no coincide.',
            ]);
        }

        // Check expiration
        if ($license->duration_days > 0 && $license->expires_at && $license->expires_at->isPast()) {
            $license->update(['is_active' => false]);

            return response()->json([
                'success' => true,
                'valid' => false,
                'message' => 'Licencia expirada.',
            ]);
        }

        ActivationLog::create([
            'license_id' => $license->id,
            'hardware_id' => $request->hardware_id,
            'action' => 'validate',
            'ip_address' => $request->ip(),
        ]);

        return response()->json([
            'success' => true,
            'valid' => true,
            'license' => [
                'key' => $license->license_key,
                'expires_at' => $license->expires_at?->toISOString(),
                'days_left' => $license->days_left,
                'is_permanent' => $license->duration_days === 0,
            ],
        ]);
    }

    public function deactivate(Request $request)
    {
        $request->validate([
            'license_key' => 'required|string',
            'hardware_id' => 'required|string',
        ]);

        $license = License::where('license_key', $request->license_key)->first();

        if (!$license) {
            return response()->json([
                'success' => false,
                'message' => 'Licencia no encontrada.',
            ], 404);
        }

        if ($license->hardware_id !== $request->hardware_id) {
            return response()->json([
                'success' => false,
                'message' => 'Hardware no coincide.',
            ], 403);
        }

        ActivationLog::create([
            'license_id' => $license->id,
            'hardware_id' => $request->hardware_id,
            'action' => 'deactivate',
            'ip_address' => $request->ip(),
        ]);

        $license->update([
            'hardware_id' => null,
            'activated_at' => null,
        ]);

        return response()->json([
            'success' => true,
            'message' => 'Licencia desvinculada exitosamente.',
        ]);
    }
}
