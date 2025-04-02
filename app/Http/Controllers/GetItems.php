<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;


class GetItems extends Controller
{
    public function GetData(){
        $result = DB::select('SELECT Barcode, ItemNum, ItemName FROM ItemFile LEFT JOIN ItemBarcode ON ItemFile.ItemID = ItemBarcode.ItemID');
        return response()->json($result);
     }
}
