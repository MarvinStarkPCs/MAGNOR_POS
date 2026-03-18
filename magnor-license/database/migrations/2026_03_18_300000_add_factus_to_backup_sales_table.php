<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::table('backup_sales', function (Blueprint $table) {
            $table->string('factus_cufe')->nullable()->after('notes');
            $table->text('factus_qr_code')->nullable()->after('factus_cufe');
            $table->string('factus_number')->nullable()->after('factus_qr_code');
            $table->string('factus_prefix')->nullable()->after('factus_number');
            $table->string('factus_status')->nullable()->after('factus_prefix');
        });
    }

    public function down(): void
    {
        Schema::table('backup_sales', function (Blueprint $table) {
            $table->dropColumn(['factus_cufe', 'factus_qr_code', 'factus_number', 'factus_prefix', 'factus_status']);
        });
    }
};
