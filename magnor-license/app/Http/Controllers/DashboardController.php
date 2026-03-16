<?php

namespace App\Http\Controllers;

use App\Models\License;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\Hash;
use Inertia\Inertia;

class DashboardController extends Controller
{
    public function index(Request $request)
    {
        $query = License::query()->latest();

        // Apply filter
        $filter = $request->get('filter', 'all');
        switch ($filter) {
            case 'active':
                $query->active();
                break;
            case 'unused':
                $query->unused();
                break;
            case 'inactive':
                $query->where('is_active', false);
                break;
            case 'expired':
                $query->expired();
                break;
        }

        $licenses = $query->get();

        // Stats (always from full dataset)
        $allLicenses = License::all();
        $stats = [
            'total' => $allLicenses->count(),
            'active' => $allLicenses->where('status', 'active')->count(),
            'unused' => $allLicenses->where('status', 'unused')->count(),
            'inactive' => $allLicenses->where('status', 'inactive')->count(),
            'expired' => $allLicenses->where('status', 'expired')->count(),
        ];

        return Inertia::render('Dashboard', [
            'licenses' => $licenses,
            'stats' => $stats,
            'currentFilter' => $filter,
        ]);
    }

    public function store(Request $request)
    {
        $request->validate([
            'customer_name' => 'nullable|string|max:255',
            'customer_email' => 'nullable|email|max:255',
            'duration_days' => 'required|integer|min:0',
        ]);

        // Generate unique key
        do {
            $key = License::generateKey();
        } while (License::where('license_key', $key)->exists());

        License::create([
            'license_key' => $key,
            'customer_name' => $request->customer_name,
            'customer_email' => $request->customer_email,
            'duration_days' => $request->duration_days,
        ]);

        return redirect()->route('dashboard')->with('success', 'Licencia creada exitosamente.');
    }

    public function toggle($id)
    {
        $license = License::findOrFail($id);
        $license->update(['is_active' => !$license->is_active]);

        return redirect()->route('dashboard');
    }

    public function unbind($id)
    {
        $license = License::findOrFail($id);
        $license->update([
            'hardware_id' => null,
            'activated_at' => null,
        ]);

        return redirect()->route('dashboard');
    }

    public function renew(Request $request, $id)
    {
        $request->validate([
            'duration_days' => 'required|integer|min:0',
        ]);

        $license = License::findOrFail($id);

        $license->duration_days = $request->duration_days;
        $license->is_active = true;

        if ($license->hardware_id) {
            // If already activated, recalculate expiration from now
            if ($request->duration_days > 0) {
                $license->expires_at = now()->addDays($request->duration_days);
            } else {
                $license->expires_at = null; // permanent
            }
        }

        $license->save();

        return redirect()->route('dashboard');
    }

    public function destroy($id)
    {
        $license = License::findOrFail($id);
        $license->delete();

        return redirect()->route('dashboard');
    }

    public function login(Request $request)
    {
        $credentials = $request->validate([
            'email' => 'required|email',
            'password' => 'required',
        ]);

        if (Auth::attempt($credentials)) {
            $request->session()->regenerate();
            return redirect()->intended('/dashboard');
        }

        return back()->withErrors([
            'email' => 'Las credenciales no coinciden.',
        ]);
    }

    public function logout(Request $request)
    {
        Auth::logout();
        $request->session()->invalidate();
        $request->session()->regenerateToken();

        return redirect('/login');
    }
}
