using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using HotelPOS.Wpf.Data;
using HotelPOS.Wpf.Helpers;
using HotelPOS.Wpf.Models;

namespace HotelPOS.Wpf.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _clockTimer;

        private string _systemTimeText = "";
        public string SystemTimeText
        {
            get => _systemTimeText;
            private set { _systemTimeText = value; OnPropertyChanged(); }
        }

        private bool _isAdminUnlocked;
        public bool IsAdminUnlocked
        {
            get => _isAdminUnlocked;
            private set { _isAdminUnlocked = value; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        private string _adminPinInput = "";
        public string AdminPinInput
        {
            get => _adminPinInput;
            set { _adminPinInput = value ?? ""; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        public ICommand UnlockAdminCommand { get; }
        public ICommand LockAdminCommand { get; }

        public ObservableCollection<Room> Rooms { get; } = new();
        public ObservableCollection<FoodItem> FoodItems { get; } = new();
        public ObservableCollection<BillItem> BillItems { get; } = new();

        public ObservableCollection<BillSummary> PastBills { get; } = new();
        public ObservableCollection<BillItem> PastBillItems { get; } = new();

        private Room? _selectedRoom;
        public Room? SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                _selectedRoom = value;
                OnPropertyChanged();
                SyncRoomFormFromSelection();
                RaiseCanExecutes();
            }
        }

        private string _roomCode = "";
        public string RoomCode
        {
            get => _roomCode;
            set { _roomCode = value ?? ""; OnPropertyChanged(); }
        }

        private string _roomType = "";
        public string RoomType
        {
            get => _roomType;
            set { _roomType = value ?? ""; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        private string _roomPricePerDay = "";
        public string RoomPricePerDay
        {
            get => _roomPricePerDay;
            set { _roomPricePerDay = value ?? ""; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        private string _roomStatus = "Available";
        public string RoomStatus
        {
            get => _roomStatus;
            set { _roomStatus = string.IsNullOrWhiteSpace(value) ? "Available" : value; OnPropertyChanged(); }
        }

        private FoodItem? _selectedFoodItem;
        public FoodItem? SelectedFoodItem
        {
            get => _selectedFoodItem;
            set
            {
                _selectedFoodItem = value;
                OnPropertyChanged();
                SyncFoodFormFromSelection();
                RaiseCanExecutes();
            }
        }

        private string _manageFoodName = "";
        public string ManageFoodName
        {
            get => _manageFoodName;
            set { _manageFoodName = value ?? ""; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        private string _manageFoodPrice = "";
        public string ManageFoodPrice
        {
            get => _manageFoodPrice;
            set { _manageFoodPrice = value ?? ""; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        private string _manageFoodCategory = "";
        public string ManageFoodCategory
        {
            get => _manageFoodCategory;
            set { _manageFoodCategory = value ?? ""; OnPropertyChanged(); RaiseCanExecutes(); }
        }

        private BillItem? _selectedBillItem;
        public BillItem? SelectedBillItem
        {
            get => _selectedBillItem;
            set { _selectedBillItem = value; OnPropertyChanged(); }
        }

        private string _foodSearch = "";
        public string FoodSearch
        {
            get => _foodSearch;
            set
            {
                _foodSearch = value ?? "";
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredFoodItems));
            }
        }

        public ObservableCollection<FoodItem> FilteredFoodItems
        {
            get
            {
                var s = (FoodSearch ?? "").Trim().ToLowerInvariant();
                var filtered = string.IsNullOrEmpty(s)
                    ? FoodItems
                    : new ObservableCollection<FoodItem>(
                        FoodItems.Where(f =>
                            (f.FoodName ?? "").ToLowerInvariant().Contains(s) ||
                            (f.Category ?? "").ToLowerInvariant().Contains(s)));

                return filtered;
            }
        }

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            private set { _totalAmount = value; OnPropertyChanged(); }
        }

        private BillSummary? _selectedPastBill;
        public BillSummary? SelectedPastBill
        {
            get => _selectedPastBill;
            set
            {
                _selectedPastBill = value;
                OnPropertyChanged();
                LoadSelectedPastBillItems();
                RaiseCanExecutes();
            }
        }

        public ICommand AddFoodCommand { get; }
        public ICommand RemoveSelectedBillItemCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand NewBillCommand { get; }
        public ICommand SaveBillCommand { get; }
        public ICommand PrintCurrentBillCommand { get; }
        public ICommand RefreshBillsCommand { get; }
        public ICommand PrintSelectedPastBillCommand { get; }
        public ICommand DeleteSelectedPastBillCommand { get; }

        public ICommand AddRoomCommand { get; }
        public ICommand UpdateRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand ClearRoomFormCommand { get; }
        public ICommand AddSelectedRoomToBillCommand { get; }

        public ICommand AddFoodItemCommand { get; }
        public ICommand UpdateFoodItemCommand { get; }
        public ICommand DeleteFoodItemCommand { get; }
        public ICommand ClearFoodFormCommand { get; }

        public MainViewModel()
        {
            SystemTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (_, __) => SystemTimeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _clockTimer.Start();

            UnlockAdminCommand = new RelayCommand(_ => UnlockAdmin(), _ => !IsAdminUnlocked && !string.IsNullOrWhiteSpace(AdminPinInput));
            LockAdminCommand = new RelayCommand(_ => LockAdmin(), _ => IsAdminUnlocked);

            AddFoodCommand = new RelayCommand(p => AddFood((FoodItem)p!));
            RemoveSelectedBillItemCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedBillItem != null);
            IncreaseQuantityCommand = new RelayCommand(p => ChangeQuantity((BillItem)p!, +1));
            DecreaseQuantityCommand = new RelayCommand(p => ChangeQuantity((BillItem)p!, -1), p => CanDecrease((BillItem?)p));
            NewBillCommand = new RelayCommand(_ => NewBill());
            SaveBillCommand = new RelayCommand(_ => SaveBill(), _ => BillItems.Count > 0);
            PrintCurrentBillCommand = new RelayCommand(_ => PrintCurrentBill(), _ => BillItems.Count > 0);
            RefreshBillsCommand = new RelayCommand(_ => LoadBills());
            PrintSelectedPastBillCommand = new RelayCommand(_ => PrintPastBill(), _ => SelectedPastBill != null);
            DeleteSelectedPastBillCommand = new RelayCommand(_ => DeletePastBill(), _ => SelectedPastBill != null);

            AddRoomCommand = new RelayCommand(_ => AddRoom(), _ => IsAdminUnlocked && CanSaveRoom(isUpdate: false));
            UpdateRoomCommand = new RelayCommand(_ => UpdateRoom(), _ => IsAdminUnlocked && CanSaveRoom(isUpdate: true));
            DeleteRoomCommand = new RelayCommand(_ => DeleteRoom(), _ => IsAdminUnlocked && SelectedRoom != null);
            ClearRoomFormCommand = new RelayCommand(_ => ClearRoomForm());
            AddSelectedRoomToBillCommand = new RelayCommand(_ => AddSelectedRoomToBill(), _ => SelectedRoom != null);

            AddFoodItemCommand = new RelayCommand(_ => AddFoodItem(), _ => IsAdminUnlocked && CanSaveFood(isUpdate: false));
            UpdateFoodItemCommand = new RelayCommand(_ => UpdateFoodItem(), _ => IsAdminUnlocked && CanSaveFood(isUpdate: true));
            DeleteFoodItemCommand = new RelayCommand(_ => DeleteFoodItem(), _ => IsAdminUnlocked && SelectedFoodItem != null);
            ClearFoodFormCommand = new RelayCommand(_ => ClearFoodForm());

            Load();
        }

        private void UnlockAdmin()
        {
            if (AdminAuth.VerifyPin(AdminPinInput))
            {
                IsAdminUnlocked = true;
                AdminPinInput = "";
                return;
            }

            MessageBox.Show("Wrong PIN.", "Admin");
        }

        private void LockAdmin()
        {
            IsAdminUnlocked = false;
            AdminPinInput = "";
        }

        private void Load()
        {
            Rooms.Clear();
            foreach (var r in SqliteDb.LoadRooms()) Rooms.Add(r);

            if (SelectedRoom != null)
            {
                SelectedRoom = Rooms.FirstOrDefault(x => x.RoomID == SelectedRoom.RoomID);
            }

            FoodItems.Clear();
            foreach (var f in SqliteDb.LoadFoodItems()) FoodItems.Add(f);

            if (SelectedFoodItem != null)
            {
                SelectedFoodItem = FoodItems.FirstOrDefault(x => x.FoodID == SelectedFoodItem.FoodID);
            }

            OnPropertyChanged(nameof(FilteredFoodItems));

            LoadBills();
        }

        private void ReloadFoodItems()
        {
            FoodItems.Clear();
            foreach (var f in SqliteDb.LoadFoodItems()) FoodItems.Add(f);
            OnPropertyChanged(nameof(FilteredFoodItems));
            RaiseCanExecutes();
        }

        private void SyncFoodFormFromSelection()
        {
            if (SelectedFoodItem == null) return;
            ManageFoodName = SelectedFoodItem.FoodName;
            ManageFoodPrice = SelectedFoodItem.Price.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ManageFoodCategory = SelectedFoodItem.Category;
        }

        private void ClearFoodForm()
        {
            SelectedFoodItem = null;
            ManageFoodName = "";
            ManageFoodPrice = "";
            ManageFoodCategory = "";
        }

        private bool CanSaveFood(bool isUpdate)
        {
            if (isUpdate && SelectedFoodItem == null) return false;
            if (string.IsNullOrWhiteSpace(ManageFoodName)) return false;
            if (string.IsNullOrWhiteSpace(ManageFoodCategory)) return false;
            return TryParsePrice(ManageFoodPrice, out var p) && p >= 0;
        }

        private void AddFoodItem()
        {
            if (!TryParsePrice(ManageFoodPrice, out var price))
            {
                MessageBox.Show("Invalid price.", "Foods");
                return;
            }

            try
            {
                SqliteDb.AddFoodItem(ManageFoodName, price, ManageFoodCategory);
                ReloadFoodItems();
                ClearFoodForm();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Foods");
            }
        }

        private void UpdateFoodItem()
        {
            if (SelectedFoodItem == null) return;
            if (!TryParsePrice(ManageFoodPrice, out var price))
            {
                MessageBox.Show("Invalid price.", "Foods");
                return;
            }

            try
            {
                SqliteDb.UpdateFoodItem(SelectedFoodItem.FoodID, ManageFoodName, price, ManageFoodCategory);
                ReloadFoodItems();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Foods");
            }
        }

        private void DeleteFoodItem()
        {
            if (SelectedFoodItem == null) return;

            var confirm = MessageBox.Show(
                $"Delete food '{SelectedFoodItem.FoodName}'?",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                SqliteDb.DeleteFoodItem(SelectedFoodItem.FoodID);
                ReloadFoodItems();
                ClearFoodForm();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Foods");
            }
        }

        private void ReloadRooms()
        {
            Rooms.Clear();
            foreach (var r in SqliteDb.LoadRooms()) Rooms.Add(r);
            RaiseCanExecutes();
        }

        private void SyncRoomFormFromSelection()
        {
            if (SelectedRoom == null) return;
            RoomCode = SelectedRoom.RoomNumber;
            RoomType = SelectedRoom.RoomType;
            RoomPricePerDay = SelectedRoom.PricePerDay.ToString(System.Globalization.CultureInfo.InvariantCulture);
            RoomStatus = string.IsNullOrWhiteSpace(SelectedRoom.Status) ? "Available" : SelectedRoom.Status;
        }

        private void ClearRoomForm()
        {
            RoomCode = "";
            RoomType = "";
            RoomPricePerDay = "";
            RoomStatus = "Available";
            SelectedRoom = null;
            RaiseCanExecutes();
        }

        private bool CanSaveRoom(bool isUpdate)
        {
            if (isUpdate && SelectedRoom == null) return false;
            if (string.IsNullOrWhiteSpace(RoomType)) return false;
            return TryParsePrice(RoomPricePerDay, out var p) && p >= 0;
        }

        private static bool TryParsePrice(string s, out decimal price)
        {
            s = (s ?? "").Trim();
            if (decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out price))
                return true;
            return decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out price);
        }

        private void AddRoom()
        {
            if (!TryParsePrice(RoomPricePerDay, out var price))
            {
                MessageBox.Show("Invalid price.", "Rooms");
                return;
            }

            try
            {
                SqliteDb.AddRoom(RoomCode, RoomType, price, RoomStatus);
                ReloadRooms();
                ClearRoomForm();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Rooms");
            }
        }

        private void UpdateRoom()
        {
            if (SelectedRoom == null) return;
            if (!TryParsePrice(RoomPricePerDay, out var price))
            {
                MessageBox.Show("Invalid price.", "Rooms");
                return;
            }

            try
            {
                SqliteDb.UpdateRoom(SelectedRoom.RoomID, RoomCode, RoomType, price, RoomStatus);
                ReloadRooms();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Rooms");
            }
        }

        private void DeleteRoom()
        {
            if (SelectedRoom == null) return;
            var confirm = MessageBox.Show(
                $"Delete room '{SelectedRoom.RoomType}'?",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                SqliteDb.DeleteRoom(SelectedRoom.RoomID);
                ReloadRooms();
                ClearRoomForm();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Rooms");
            }
        }

        private void LoadBills()
        {
            PastBills.Clear();
            foreach (var b in SqliteDb.LoadBills()) PastBills.Add(b);

            if (SelectedPastBill != null && PastBills.All(x => x.BillID != SelectedPastBill.BillID))
            {
                SelectedPastBill = null;
            }

            RaiseCanExecutes();
        }

        private void AddFood(FoodItem food)
        {
            var existing = BillItems.FirstOrDefault(b => b.ItemName == food.FoodName);
            if (existing != null)
            {
                ChangeQuantity(existing, +1);
            }
            else
            {
                BillItems.Add(new BillItem
                {
                    ItemName = food.FoodName,
                    Quantity = 1,
                    UnitPrice = food.Price,
                    TotalPrice = food.Price
                });
            }

            Recalc();
            RaiseCanExecutes();
        }

        private void AddSelectedRoomToBill()
        {
            if (SelectedRoom == null) return;

            var itemName = $"Room: {SelectedRoom.RoomType}";
            var existing = BillItems.FirstOrDefault(b => b.ItemName == itemName);
            if (existing != null)
            {
                // Treat Quantity as number of days.
                ChangeQuantity(existing, +1);
                return;
            }

            var unit = SelectedRoom.PricePerDay;
            BillItems.Add(new BillItem
            {
                ItemName = itemName,
                Quantity = 1,
                UnitPrice = unit,
                TotalPrice = unit
            });

            Recalc();
            RaiseCanExecutes();
        }

        private static bool CanDecrease(BillItem? item)
            => item != null && item.Quantity > 1;

        private void ChangeQuantity(BillItem item, int delta)
        {
            var next = item.Quantity + delta;
            if (next < 1) next = 1;
            item.Quantity = next;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            Recalc();
            RaiseCanExecutes();
        }

        private void RemoveSelected()
        {
            if (SelectedBillItem == null) return;
            BillItems.Remove(SelectedBillItem);
            SelectedBillItem = null;
            Recalc();
            RaiseCanExecutes();
        }

        private void NewBill()
        {
            SelectedRoom = null;
            SelectedBillItem = null;
            BillItems.Clear();
            Recalc();
            RaiseCanExecutes();
        }

        private void SaveBill()
        {
            var roomId = SelectedRoom?.RoomID;
            SqliteDb.SaveBill(roomId, TotalAmount, BillItems.ToList());
            NewBill();
            LoadBills();
        }

        private void LoadSelectedPastBillItems()
        {
            PastBillItems.Clear();
            if (SelectedPastBill == null) return;
            foreach (var it in SqliteDb.LoadBillItems(SelectedPastBill.BillID)) PastBillItems.Add(it);
        }

        private void PrintCurrentBill()
        {
            if (BillItems.Count <= 0) return;

            var pd = new PrintDialog();
            if (pd.ShowDialog() != true) return;

            // Save first so we can print with the unique bill number.
            var roomId = SelectedRoom?.RoomID;
            var billId = SqliteDb.SaveBill(roomId, TotalAmount, BillItems.ToList());

            var room = SelectedRoom?.RoomNumber;
            var receiptNo = $"BILL-{billId:D6}";
            var printedAt = DateTime.Now;

            PrintReceipt80mm(pd, receiptNo, printedAt, room, TotalAmount, BillItems);

            NewBill();
            LoadBills();
        }

        private void PrintPastBill()
        {
            if (SelectedPastBill == null) return;
            var pd = new PrintDialog();
            if (pd.ShowDialog() != true) return;

            var room = SelectedPastBill.RoomNumber;
            var receiptNo = SelectedPastBill.BillNo;
            var printedAt = DateTime.Now;
            PrintReceipt80mm(pd, receiptNo, printedAt, room, SelectedPastBill.TotalAmount, PastBillItems);
        }

        private void DeletePastBill()
        {
            if (SelectedPastBill == null) return;

            var billNo = SelectedPastBill.BillNo;
            var confirm = MessageBox.Show(
                $"Delete {billNo}? This cannot be undone.",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            SqliteDb.DeleteBill(SelectedPastBill.BillID);
            SelectedPastBill = null;
            PastBillItems.Clear();
            LoadBills();
        }

        private static void PrintReceipt80mm(PrintDialog pd, string receiptNo, DateTime printedAt, string? roomNumber, decimal total, System.Collections.IEnumerable items)
        {
            try
            {
                // 80mm paper width target.
                var maxWidth = MmToDip(80);
                var pageWidth = pd.PrintableAreaWidth > 0
                    ? Math.Min(pd.PrintableAreaWidth, maxWidth)
                    : maxWidth;

                var doc = BuildReceipt80mmDocument(receiptNo, printedAt, roomNumber, total, items, pageWidth);
                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Receipt {receiptNo}");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Print error");
            }
        }

        private static FlowDocument BuildReceipt80mmDocument(string receiptNo, DateTime printedAt, string? roomNumber, decimal total, System.Collections.IEnumerable items, double pageWidth)
        {
            // Design for 72mm printable area on 80mm paper.
            var targetPrintableWidth = Math.Min(pageWidth, MmToDip(72));
            var sideMargin = Math.Max(0, (pageWidth - targetPrintableWidth) / 2);
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.0,
                PageWidth = pageWidth,
                ColumnWidth = pageWidth,
                PagePadding = new Thickness(sideMargin, MmToDip(5), sideMargin, MmToDip(6))
            };

            // Typical 80mm receipts are 42 columns at 72mm printable width.
            const int lineWidth = 42;

            static string LR(string left, string right, int width)
            {
                left ??= "";
                right ??= "";
                left = left.TrimEnd();
                right = right.TrimStart();
                if (left.Length + 1 + right.Length > width)
                {
                    // Best-effort: truncate left to keep the amount visible.
                    var maxLeft = Math.Max(0, width - right.Length - 1);
                    if (left.Length > maxLeft) left = left[..maxLeft];
                }
                var spaces = Math.Max(1, width - left.Length - right.Length);
                return left + new string(' ', spaces) + right;
            }

            static System.Collections.Generic.IEnumerable<string> Wrap(string text, int width)
            {
                text ??= "";
                text = text.Trim();
                if (text.Length == 0) yield break;

                while (text.Length > width)
                {
                    var cut = text.LastIndexOf(' ', width);
                    if (cut <= 0) cut = width;
                    yield return text[..cut].TrimEnd();
                    text = text[cut..].TrimStart();
                }
                if (text.Length > 0) yield return text;
            }

            static Paragraph ParaLines(System.Collections.Generic.IEnumerable<string> lines, bool bold = false, TextAlignment align = TextAlignment.Left, double? fontSize = null, Thickness? margin = null)
            {
                var p = new Paragraph { Margin = margin ?? new Thickness(0), TextAlignment = align };
                if (bold) p.FontWeight = FontWeights.SemiBold;
                if (fontSize.HasValue) p.FontSize = fontSize.Value;

                var first = true;
                foreach (var line in lines)
                {
                    if (!first) p.Inlines.Add(new LineBreak());
                    p.Inlines.Add(new Run(line));
                    first = false;
                }
                return p;
            }

            static Paragraph Para1(string text, bool bold = false, TextAlignment align = TextAlignment.Left, double? fontSize = null, Thickness? margin = null)
                => ParaLines(new[] { text }, bold, align, fontSize, margin);

            // Big header
            doc.Blocks.Add(ParaLines(new[]
            {
                "MAYKA'S HOLIDAY HOMES",
                "AND CAFE",
                "POS RECEIPT",
                "+94 71 944 7567"
            }, bold: true, align: TextAlignment.Center, fontSize: 12.0));

            var sep = new string('-', lineWidth);
            doc.Blocks.Add(Para1(sep, align: TextAlignment.Center, margin: new Thickness(0, 4, 0, 4)));

            doc.Blocks.Add(Para1(LR("Receipt:", receiptNo, lineWidth), bold: true));
            doc.Blocks.Add(Para1(LR("Date:", printedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), lineWidth)));
            doc.Blocks.Add(Para1(LR("Room:", string.IsNullOrWhiteSpace(roomNumber) ? "-" : roomNumber, lineWidth)));

            doc.Blocks.Add(Para1(sep, align: TextAlignment.Center, margin: new Thickness(0, 4, 0, 4)));

            // Items: compact receipt style (name line + qty x rate line)
            decimal subTotal = 0m;
            foreach (var obj in items)
            {
                if (obj is not BillItem it) continue;
                subTotal += it.TotalPrice;

                foreach (var nameLine in Wrap(it.ItemName ?? "-", lineWidth))
                {
                    doc.Blocks.Add(Para1(nameLine, bold: true));
                }

                var qtyRate = $"{it.Quantity} x {it.UnitPrice:N2}";
                var amount = it.TotalPrice.ToString("N2", CultureInfo.InvariantCulture);
                doc.Blocks.Add(Para1(LR(qtyRate, amount, lineWidth)));
            }

            doc.Blocks.Add(Para1(sep, align: TextAlignment.Center, margin: new Thickness(0, 4, 0, 4)));

            var grandTotal = total != 0 ? total : subTotal;

            doc.Blocks.Add(Para1(LR("Subtotal", $"LKR {subTotal:N2}", lineWidth)));
            doc.Blocks.Add(Para1(LR("TOTAL", $"LKR {grandTotal:N2}", lineWidth), bold: true));

            doc.Blocks.Add(Para1(sep, align: TextAlignment.Center, margin: new Thickness(0, 4, 0, 4)));
            doc.Blocks.Add(ParaLines(Wrap(ToMoneyWords(grandTotal), lineWidth), fontSize: 10.0));

            doc.Blocks.Add(ParaLines(new[]
            {
                "Thank you!"
            }, bold: true, align: TextAlignment.Center, fontSize: 11.0, margin: new Thickness(0, 10, 0, 0)));

            return doc;
        }

        private static double MmToDip(double mm)
        {
            // 1 inch = 25.4 mm, 1 WPF DIP = 1/96 inch
            return mm / 25.4 * 96.0;
        }

        private static string ToMoneyWords(decimal amount)
        {
            // LKR words, simple English conversion for printing.
            var abs = Math.Abs(amount);
            var rupees = (long)Math.Floor(abs);
            var cents = (int)Math.Round((abs - rupees) * 100m, 0, MidpointRounding.AwayFromZero);

            var words = NumberToWords(rupees);
            if (string.IsNullOrWhiteSpace(words)) words = "Zero";

            var result = $"LKR {words} only";
            if (cents > 0)
            {
                result = $"LKR {words} and {NumberToWords(cents)} cents only";
            }

            return result;
        }

        private static string NumberToWords(long number)
        {
            if (number == 0) return "Zero";
            if (number < 0) return "Minus " + NumberToWords(Math.Abs(number));

            static string WordsBelow1000(int n)
            {
                var units = new[]
                {
                    "Zero","One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Ten",
                    "Eleven","Twelve","Thirteen","Fourteen","Fifteen","Sixteen","Seventeen","Eighteen","Nineteen"
                };
                var tens = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                var parts = new System.Collections.Generic.List<string>();
                if (n >= 100)
                {
                    parts.Add(units[n / 100] + " Hundred");
                    n %= 100;
                }
                if (n >= 20)
                {
                    parts.Add(tens[n / 10] + (n % 10 != 0 ? " " + units[n % 10] : ""));
                }
                else if (n > 0)
                {
                    parts.Add(units[n]);
                }

                return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            }

            var scales = new (long value, string name)[]
            {
                (1_000_000_000, "Billion"),
                (1_000_000, "Million"),
                (1_000, "Thousand")
            };

            var remaining = number;
            var resultParts = new System.Collections.Generic.List<string>();

            foreach (var (value, name) in scales)
            {
                if (remaining >= value)
                {
                    var chunk = (int)(remaining / value);
                    remaining %= value;
                    resultParts.Add(WordsBelow1000(chunk) + " " + name);
                }
            }

            if (remaining > 0)
            {
                resultParts.Add(WordsBelow1000((int)remaining));
            }

            return string.Join(" ", resultParts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private void Recalc()
        {
            TotalAmount = BillItems.Sum(i => i.TotalPrice);
        }

        private void RaiseCanExecutes()
        {
            (RemoveSelectedBillItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveBillCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DecreaseQuantityCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PrintCurrentBillCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PrintSelectedPastBillCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteSelectedPastBillCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (AddRoomCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (UpdateRoomCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteRoomCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (AddSelectedRoomToBillCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (AddFoodItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (UpdateFoodItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteFoodItemCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (UnlockAdminCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (LockAdminCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
