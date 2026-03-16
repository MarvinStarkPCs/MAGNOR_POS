<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class ValidateApiSecret
{
    public function handle(Request $request, Closure $next): Response
    {
        $secret = $request->header('X-Api-Secret');

        if ($secret !== 'MG2026-S3CR3T-K3Y-P0S') {
            return response()->json([
                'success' => false,
                'message' => 'Unauthorized. Invalid API secret.',
            ], 401);
        }

        return $next($request);
    }
}
