<?php

use App\Http\Controllers\Api\LicenseApiController;
use Illuminate\Support\Facades\Route;

Route::middleware(\App\Http\Middleware\ValidateApiSecret::class)->group(function () {
    Route::post('/activate', [LicenseApiController::class, 'activate']);
    Route::post('/validate', [LicenseApiController::class, 'validate']);
    Route::post('/deactivate', [LicenseApiController::class, 'deactivate']);
});
