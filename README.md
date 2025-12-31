# Mayka's Holiday Homes & Cafe POS

A lightweight **WPF (.NET 8)** hotel/cafe POS for billing, room charges, food items, bill history, and **80mm thermal receipt printing**.

## Features
- Create bills with quantity controls (+ / -) and totals in **LKR**
- Add a selected room charge to the current bill
- Bills history: view past bills + items
- Print current bill and past bills (80mm receipt layout)
- Delete older bills
- Manage **Rooms** and **Foods** (CRUD)
- Admin lock/unlock to protect Rooms/Foods management

## Tech
- .NET 8 WPF (`net8.0-windows`)
- SQLite (`Microsoft.Data.Sqlite.Core`) + native bundle (`SQLitePCLRaw.bundle_e_sqlite3`)

## Run
1. Open `HotelPOS.sln` in Visual Studio 2022
2. Build and run (F5)

Or via CLI:
```bash
cd HotelPOS
dotnet build
dotnet run
```

## Printing
- Receipt format targets **80mm paper** with a **72mm printable area**.
- “Print current bill” auto-saves first, then prints with the unique number like `BILL-000123`.

## Database
- SQLite database is created under:
  - `%LocalAppData%\HotelPOS\HotelPOS.sqlite`

## Admin
- Default Admin PIN: `1234`

## Contact
- +94 71 944 7567
