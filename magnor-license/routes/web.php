<?php

use App\Http\Controllers\BackupViewController;
use App\Http\Controllers\DashboardController;
use Illuminate\Support\Facades\Route;

Route::middleware('guest')->group(function () {
    Route::get('/', fn() => redirect('/login'));
    Route::get('/login', fn() => \Inertia\Inertia::render('Auth/Login'))->name('login');
    Route::post('/login', [DashboardController::class, 'login']);
});

Route::middleware('auth')->group(function () {
    Route::get('/dashboard', [DashboardController::class, 'index'])->name('dashboard');
    Route::post('/licenses', [DashboardController::class, 'store'])->name('licenses.store');
    Route::post('/licenses/{id}/toggle', [DashboardController::class, 'toggle'])->name('licenses.toggle');
    Route::post('/licenses/{id}/unbind', [DashboardController::class, 'unbind'])->name('licenses.unbind');
    Route::post('/licenses/{id}/renew', [DashboardController::class, 'renew'])->name('licenses.renew');
    Route::delete('/licenses/{id}', [DashboardController::class, 'destroy'])->name('licenses.destroy');
    Route::post('/logout', [DashboardController::class, 'logout'])->name('logout');
    Route::get('/backup', [BackupViewController::class, 'index'])->name('backup.index');
});
