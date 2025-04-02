<?php

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Route;
use App\Http\Controllers\GetItems;

Route::get('/products', [GetItems::class, 'GetData']);