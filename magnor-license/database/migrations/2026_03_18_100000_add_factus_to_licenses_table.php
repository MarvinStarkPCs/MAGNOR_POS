<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('licenses', function (Blueprint $table) {
            $table->boolean('factus_enabled')->default(false)->after('expires_at');
            $table->boolean('factus_sandbox')->default(true)->after('factus_enabled');
            $table->string('factus_client_id')->nullable()->after('factus_sandbox');
            $table->string('factus_client_secret')->nullable()->after('factus_client_id');
            $table->string('factus_username')->nullable()->after('factus_client_secret');
            $table->string('factus_password')->nullable()->after('factus_username');
        });
    }

    public function down(): void
    {
        Schema::table('licenses', function (Blueprint $table) {
            $table->dropColumn([
                'factus_enabled',
                'factus_sandbox',
                'factus_client_id',
                'factus_client_secret',
                'factus_username',
                'factus_password',
            ]);
        });
    }
};
