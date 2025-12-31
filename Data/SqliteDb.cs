using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HotelPOS.Wpf.Models;
using Microsoft.Data.Sqlite;

namespace HotelPOS.Wpf.Data
{
    public static class SqliteDb
    {
        private static readonly string DbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HotelPOS",
            "HotelPOS.sqlite");

        private static readonly string ConnectionString =
            $"Data Source={DbPath};Mode=ReadWriteCreate;";

        private static SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            return conn;
        }

        public static void EnsureDatabase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

            using var conn = OpenConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Rooms (
    RoomID INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomNumber TEXT NOT NULL UNIQUE,
    RoomType TEXT NOT NULL,
    PricePerDay NUMERIC NOT NULL,
    Status TEXT DEFAULT 'Available'
);

CREATE TABLE IF NOT EXISTS FoodItems (
    FoodID INTEGER PRIMARY KEY AUTOINCREMENT,
    FoodName TEXT NOT NULL,
    Price NUMERIC NOT NULL,
    Category TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Bills (
    BillID INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomID INTEGER NULL,
    BillDate TEXT DEFAULT CURRENT_TIMESTAMP,
    TotalAmount NUMERIC NOT NULL,
    FOREIGN KEY (RoomID) REFERENCES Rooms(RoomID)
);

CREATE TABLE IF NOT EXISTS BillItems (
    BillItemID INTEGER PRIMARY KEY AUTOINCREMENT,
    BillID INTEGER NOT NULL,
    ItemName TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice NUMERIC NOT NULL,
    TotalPrice NUMERIC NOT NULL,
    FOREIGN KEY (BillID) REFERENCES Bills(BillID) ON DELETE CASCADE
);
";
                cmd.ExecuteNonQuery();
            }

            // Seed if empty
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(1) FROM Rooms;";
                var roomsCount = Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
                if (roomsCount != 0)
                {
                    EnsureRoomsConfigured(conn);
                    return;
                }
            }

            using var tx = conn.BeginTransaction();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
INSERT INTO Rooms (RoomNumber, RoomType, PricePerDay, Status) VALUES
('101', 'Non/AC Family Room', 100.00, 'Available'),
('102', 'Non/AC Family Room', 100.00, 'Available'),
('103', 'Non/AC Triple Room', 150.00, 'Occupied'),
('104', 'Non/AC Triple Room', 150.00, 'Available'),
('105', 'Outside Family Room', 250.00, 'Available'),
('106', 'Outside Family Room', 250.00, 'Available');

INSERT INTO FoodItems (FoodName, Price, Category) VALUES
('Biryani', 250.00, 'Lunch'),
('Butter Chicken', 280.00, 'Lunch'),
('Paneer Tikka', 200.00, 'Snacks'),
('Dal Makhani', 150.00, 'Lunch'),
('Naan', 50.00, 'Breakfast'),
('Coffee', 80.00, 'Drinks'),
('Tea', 40.00, 'Drinks'),
('Samosa', 30.00, 'Snacks'),
('Tandoori Chicken', 320.00, 'Lunch'),
('Ice Cream', 100.00, 'Snacks');
";
                cmd.ExecuteNonQuery();
            }
            tx.Commit();

            EnsureRoomsConfigured(conn);
        }

        private static void EnsureRoomsConfigured(SqliteConnection conn)
        {
            // Keep existing prices/status, but normalize RoomType names.
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE Rooms
SET RoomType = CASE
    WHEN RoomNumber IN ('101','102') THEN 'Non/AC Family Room'
    WHEN RoomNumber IN ('103','104') THEN 'Non/AC Triple Room'
    WHEN RoomNumber IN ('105','106') THEN 'Outside Family Room'
    ELSE RoomType
END
WHERE RoomNumber IN ('101','102','103','104','105','106');";
                cmd.ExecuteNonQuery();
            }

            // Ensure seeded room numbers exist if someone deleted them.
            using (var tx = conn.BeginTransaction())
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
INSERT OR IGNORE INTO Rooms (RoomNumber, RoomType, PricePerDay, Status) VALUES
('101', 'Non/AC Family Room', 100.00, 'Available'),
('102', 'Non/AC Family Room', 100.00, 'Available'),
('103', 'Non/AC Triple Room', 150.00, 'Available'),
('104', 'Non/AC Triple Room', 150.00, 'Available'),
('105', 'Outside Family Room', 250.00, 'Available'),
('106', 'Outside Family Room', 250.00, 'Available');";
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
        }

        public static List<Room> LoadRooms()
        {
            var list = new List<Room>();
            using var conn = OpenConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT RoomID, RoomNumber, RoomType, PricePerDay, Status FROM Rooms ORDER BY RoomNumber;";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Room
                {
                    RoomID = r.GetInt32(0),
                    RoomNumber = r.GetString(1),
                    RoomType = r.GetString(2),
                    PricePerDay = r.GetDecimal(3),
                    Status = r.IsDBNull(4) ? "Available" : r.GetString(4)
                });
            }
            return list;
        }

        public static int AddRoom(string? roomNumber, string roomType, decimal pricePerDay, string status)
        {
            if (string.IsNullOrWhiteSpace(roomType)) throw new ArgumentException("Room type is required.", nameof(roomType));
            if (pricePerDay < 0) throw new ArgumentOutOfRangeException(nameof(pricePerDay));
            status = string.IsNullOrWhiteSpace(status) ? "Available" : status.Trim();

            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            var number = string.IsNullOrWhiteSpace(roomNumber)
                ? GetNextRoomNumber(conn)
                : roomNumber.Trim();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
INSERT INTO Rooms (RoomNumber, RoomType, PricePerDay, Status)
VALUES ($no, $type, $price, $status);";
                cmd.Parameters.AddWithValue("$no", number);
                cmd.Parameters.AddWithValue("$type", roomType.Trim());
                cmd.Parameters.AddWithValue("$price", pricePerDay);
                cmd.Parameters.AddWithValue("$status", status);
                cmd.ExecuteNonQuery();
            }

            int id;
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT last_insert_rowid();";
                id = Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
            }

            tx.Commit();
            return id;
        }

        public static void UpdateRoom(int roomId, string? roomNumber, string roomType, decimal pricePerDay, string status)
        {
            if (roomId <= 0) throw new ArgumentOutOfRangeException(nameof(roomId));
            if (string.IsNullOrWhiteSpace(roomType)) throw new ArgumentException("Room type is required.", nameof(roomType));
            if (pricePerDay < 0) throw new ArgumentOutOfRangeException(nameof(pricePerDay));
            status = string.IsNullOrWhiteSpace(status) ? "Available" : status.Trim();

            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE Rooms
SET RoomNumber = $no,
    RoomType = $type,
    PricePerDay = $price,
    Status = $status
WHERE RoomID = $id;";
            cmd.Parameters.AddWithValue("$id", roomId);
            cmd.Parameters.AddWithValue("$no", (roomNumber ?? "").Trim());
            cmd.Parameters.AddWithValue("$type", roomType.Trim());
            cmd.Parameters.AddWithValue("$price", pricePerDay);
            cmd.Parameters.AddWithValue("$status", status);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteRoom(int roomId)
        {
            if (roomId <= 0) throw new ArgumentOutOfRangeException(nameof(roomId));
            using var conn = OpenConnection();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(1) FROM Bills WHERE RoomID = $id;";
                cmd.Parameters.AddWithValue("$id", roomId);
                var used = Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
                if (used > 0)
                {
                    throw new InvalidOperationException("Cannot delete this room because it is used in existing bills.");
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Rooms WHERE RoomID = $id;";
                cmd.Parameters.AddWithValue("$id", roomId);
                cmd.ExecuteNonQuery();
            }
        }

        private static string GetNextRoomNumber(SqliteConnection conn)
        {
            // Try to generate the next numeric code; fallback to a timestamp-based code.
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT RoomNumber FROM Rooms;";

            var max = 0;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var s = r.IsDBNull(0) ? "" : r.GetString(0);
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    if (n > max) max = n;
                }
            }

            if (max <= 0) max = 100;
            return (max + 1).ToString(CultureInfo.InvariantCulture);
        }

        public static List<FoodItem> LoadFoodItems()
        {
            var list = new List<FoodItem>();
            using var conn = OpenConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT FoodID, FoodName, Price, Category FROM FoodItems ORDER BY Category, FoodName;";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new FoodItem
                {
                    FoodID = r.GetInt32(0),
                    FoodName = r.GetString(1),
                    Price = r.GetDecimal(2),
                    Category = r.GetString(3)
                });
            }
            return list;
        }

        public static int AddFoodItem(string foodName, decimal price, string category)
        {
            if (string.IsNullOrWhiteSpace(foodName)) throw new ArgumentException("Food name is required.", nameof(foodName));
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));

            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
INSERT INTO FoodItems (FoodName, Price, Category)
VALUES ($name, $price, $cat);";
                cmd.Parameters.AddWithValue("$name", foodName.Trim());
                cmd.Parameters.AddWithValue("$price", price);
                cmd.Parameters.AddWithValue("$cat", category.Trim());
                cmd.ExecuteNonQuery();
            }

            int id;
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT last_insert_rowid();";
                id = Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
            }

            tx.Commit();
            return id;
        }

        public static void UpdateFoodItem(int foodId, string foodName, decimal price, string category)
        {
            if (foodId <= 0) throw new ArgumentOutOfRangeException(nameof(foodId));
            if (string.IsNullOrWhiteSpace(foodName)) throw new ArgumentException("Food name is required.", nameof(foodName));
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
            if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));

            using var conn = OpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE FoodItems
SET FoodName = $name,
    Price = $price,
    Category = $cat
WHERE FoodID = $id;";
            cmd.Parameters.AddWithValue("$id", foodId);
            cmd.Parameters.AddWithValue("$name", foodName.Trim());
            cmd.Parameters.AddWithValue("$price", price);
            cmd.Parameters.AddWithValue("$cat", category.Trim());
            cmd.ExecuteNonQuery();
        }

        public static void DeleteFoodItem(int foodId)
        {
            if (foodId <= 0) throw new ArgumentOutOfRangeException(nameof(foodId));
            using var conn = OpenConnection();

            string? foodName;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT FoodName FROM FoodItems WHERE FoodID = $id;";
                cmd.Parameters.AddWithValue("$id", foodId);
                foodName = cmd.ExecuteScalar() as string;
            }

            if (!string.IsNullOrWhiteSpace(foodName))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(1) FROM BillItems WHERE ItemName = $name;";
                cmd.Parameters.AddWithValue("$name", foodName);
                var used = Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture);
                if (used > 0)
                {
                    throw new InvalidOperationException("Cannot delete this food because it is used in existing bills.");
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM FoodItems WHERE FoodID = $id;";
                cmd.Parameters.AddWithValue("$id", foodId);
                cmd.ExecuteNonQuery();
            }
        }

        public static long SaveBill(int? roomId, decimal totalAmount, IReadOnlyList<BillItem> items)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            long billId;
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO Bills (RoomID, BillDate, TotalAmount) VALUES ($roomId, CURRENT_TIMESTAMP, $total);";
                cmd.Parameters.AddWithValue("$roomId", (object?)roomId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$total", totalAmount);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT last_insert_rowid();";
                billId = (long)(cmd.ExecuteScalar() ?? 0L);
            }

            foreach (var it in items)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
INSERT INTO BillItems (BillID, ItemName, Quantity, UnitPrice, TotalPrice)
VALUES ($billId, $name, $qty, $unit, $total);";
                cmd.Parameters.AddWithValue("$billId", billId);
                cmd.Parameters.AddWithValue("$name", it.ItemName);
                cmd.Parameters.AddWithValue("$qty", it.Quantity);
                cmd.Parameters.AddWithValue("$unit", it.UnitPrice);
                cmd.Parameters.AddWithValue("$total", it.TotalPrice);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
            return billId;
        }

        public static List<BillSummary> LoadBills(int take = 200)
        {
            var list = new List<BillSummary>();
            using var conn = OpenConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT b.BillID,
       b.BillDate,
       r.RoomNumber,
       b.TotalAmount
FROM Bills b
LEFT JOIN Rooms r ON r.RoomID = b.RoomID
ORDER BY b.BillID DESC
LIMIT $take;";
            cmd.Parameters.AddWithValue("$take", take);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new BillSummary
                {
                    BillID = r.GetInt64(0),
                    BillDate = r.IsDBNull(1) ? "" : r.GetString(1),
                    RoomNumber = r.IsDBNull(2) ? null : r.GetString(2),
                    TotalAmount = r.GetDecimal(3)
                });
            }

            return list;
        }

        public static List<BillItem> LoadBillItems(long billId)
        {
            var list = new List<BillItem>();
            using var conn = OpenConnection();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT ItemName, Quantity, UnitPrice, TotalPrice
FROM BillItems
WHERE BillID = $billId
ORDER BY BillItemID ASC;";
            cmd.Parameters.AddWithValue("$billId", billId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new BillItem
                {
                    ItemName = r.GetString(0),
                    Quantity = r.GetInt32(1),
                    UnitPrice = r.GetDecimal(2),
                    TotalPrice = r.GetDecimal(3)
                });
            }

            return list;
        }

        public static void DeleteBill(long billId)
        {
            using var conn = OpenConnection();
            using var tx = conn.BeginTransaction();

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM Bills WHERE BillID = $billId;";
            cmd.Parameters.AddWithValue("$billId", billId);
            cmd.ExecuteNonQuery();

            tx.Commit();
        }
    }
}
