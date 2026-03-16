<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('backup_customers', function (Blueprint $table) {
            $table->id();
            $table->string('license_key');
            $table->string('full_name');
            $table->string('document_type')->nullable();
            $table->string('document_number')->nullable();
            $table->string('phone')->nullable();
            $table->string('email')->nullable();
            $table->string('address')->nullable();
            $table->string('city')->nullable();
            $table->string('state')->nullable();
            $table->string('postal_code')->nullable();
            $table->string('customer_type')->nullable();
            $table->decimal('discount_percentage', 18, 2)->default(0);
            $table->decimal('credit_limit', 18, 2)->default(0);
            $table->text('notes')->nullable();
            $table->boolean('is_active')->default(true);
            $table->unsignedBigInteger('local_id');
            $table->timestamp('synced_at')->nullable();
            $table->timestamps();

            $table->unique(['license_key', 'local_id']);
            $table->index('license_key');
        });

        Schema::create('backup_products', function (Blueprint $table) {
            $table->id();
            $table->string('license_key');
            $table->string('name');
            $table->string('sku')->nullable();
            $table->string('barcode')->nullable();
            $table->text('description')->nullable();
            $table->decimal('sale_price', 18, 2)->default(0);
            $table->decimal('purchase_price', 18, 2)->default(0);
            $table->decimal('current_stock', 18, 2)->default(0);
            $table->decimal('minimum_stock', 18, 2)->default(0);
            $table->decimal('tax_rate', 18, 2)->default(0);
            $table->string('image_url')->nullable();
            $table->string('category_name')->nullable();
            $table->boolean('is_active')->default(true);
            $table->unsignedBigInteger('local_id');
            $table->timestamp('synced_at')->nullable();
            $table->timestamps();

            $table->unique(['license_key', 'local_id']);
            $table->index('license_key');
        });

        Schema::create('backup_sales', function (Blueprint $table) {
            $table->id();
            $table->string('license_key');
            $table->string('sale_number')->nullable();
            $table->string('customer_name')->nullable();
            $table->decimal('subtotal', 18, 2)->default(0);
            $table->decimal('tax_amount', 18, 2)->default(0);
            $table->decimal('discount_amount', 18, 2)->default(0);
            $table->decimal('total', 18, 2)->default(0);
            $table->string('payment_type')->nullable();
            $table->string('status')->nullable();
            $table->timestamp('sale_date')->nullable();
            $table->string('cashier_name')->nullable();
            $table->text('notes')->nullable();
            $table->unsignedBigInteger('local_id');
            $table->timestamp('synced_at')->nullable();
            $table->timestamps();

            $table->unique(['license_key', 'local_id']);
            $table->index('license_key');
        });

        Schema::create('backup_sale_details', function (Blueprint $table) {
            $table->id();
            $table->foreignId('backup_sale_id')->constrained('backup_sales')->cascadeOnDelete();
            $table->string('product_name');
            $table->decimal('quantity', 18, 2)->default(0);
            $table->decimal('unit_price', 18, 2)->default(0);
            $table->decimal('discount', 18, 2)->default(0);
            $table->decimal('subtotal', 18, 2)->default(0);
            $table->decimal('tax_amount', 18, 2)->default(0);
            $table->decimal('total', 18, 2)->default(0);
            $table->unsignedBigInteger('local_id')->nullable();
            $table->timestamps();
        });

        Schema::create('backup_suppliers', function (Blueprint $table) {
            $table->id();
            $table->string('license_key');
            $table->string('company_name');
            $table->string('contact_name')->nullable();
            $table->string('document_type')->nullable();
            $table->string('document_number')->nullable();
            $table->string('phone')->nullable();
            $table->string('email')->nullable();
            $table->string('address')->nullable();
            $table->string('website')->nullable();
            $table->integer('payment_term_days')->default(0);
            $table->text('notes')->nullable();
            $table->boolean('is_active')->default(true);
            $table->unsignedBigInteger('local_id');
            $table->timestamp('synced_at')->nullable();
            $table->timestamps();

            $table->unique(['license_key', 'local_id']);
            $table->index('license_key');
        });

        Schema::create('backup_purchases', function (Blueprint $table) {
            $table->id();
            $table->string('license_key');
            $table->string('purchase_number')->nullable();
            $table->string('supplier_name')->nullable();
            $table->decimal('subtotal', 18, 2)->default(0);
            $table->decimal('tax_amount', 18, 2)->default(0);
            $table->decimal('discount_amount', 18, 2)->default(0);
            $table->decimal('total', 18, 2)->default(0);
            $table->string('status')->nullable();
            $table->timestamp('purchase_date')->nullable();
            $table->text('notes')->nullable();
            $table->unsignedBigInteger('local_id');
            $table->timestamp('synced_at')->nullable();
            $table->timestamps();

            $table->unique(['license_key', 'local_id']);
            $table->index('license_key');
        });

        Schema::create('backup_sync_logs', function (Blueprint $table) {
            $table->id();
            $table->string('license_key');
            $table->string('sync_type')->default('full'); // full or partial
            $table->integer('records_synced')->default(0);
            $table->string('ip_address')->nullable();
            $table->timestamp('synced_at')->nullable();
            $table->timestamps();

            $table->index('license_key');
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('backup_sale_details');
        Schema::dropIfExists('backup_sync_logs');
        Schema::dropIfExists('backup_purchases');
        Schema::dropIfExists('backup_suppliers');
        Schema::dropIfExists('backup_sales');
        Schema::dropIfExists('backup_products');
        Schema::dropIfExists('backup_customers');
    }
};
