// Tệp: AppCafebookApi/View/nhanvien/pages/LichLamViecView.xaml.cs
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class LichLamViecView : Page
    {
        // Enum để quản lý trạng thái xem
        private enum ViewMode { Day, Week, Month }
        private ViewMode _currentView = ViewMode.Week;

        // Giờ bắt đầu cho chế độ xem Ngày/Tuần
        private const int START_HOUR = 6;
        private const int TOTAL_GRID_ROWS = 18; // 1 Header + 1 AllDay + 16 (6h-21h)

        private DateTime _currentDate; // Ngày neo (anchor date)
        private TextBlock[] _weekDayHeaders;

        // Màu cho các nút view
        private readonly SolidColorBrush _activeViewBrush = new SolidColorBrush(Color.FromRgb(222, 222, 222)); // #EEE
        private readonly SolidColorBrush _inactiveViewBrush = new SolidColorBrush(Colors.Transparent);

        public LichLamViecView()
        {
            InitializeComponent();
            _weekDayHeaders = new TextBlock[] { lblHeaderMon, lblHeaderTue, lblHeaderWed, lblHeaderThu, lblHeaderFri, lblHeaderSat, lblHeaderSun };
            _currentDate = DateTime.Today;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateViewControls();
            await LoadScheduleAsync();
        }

        private async Task LoadScheduleAsync()
        {
            try
            {
                if (_currentView == ViewMode.Month)
                {
                    // Tải dữ liệu tháng
                    var data = await ApiClient.Instance.GetFromJsonAsync<LichLamViecThangDto>($"api/app/nhanvien/lichlamviec/thang?ngayTrongThang={_currentDate:yyyy-MM-dd}");
                    if (data == null) return;
                    lblDateRange.Text = _currentDate.ToString("MMMM yyyy", new CultureInfo("vi-VN"));
                    RenderMonthView(data.NgayCoSuKien);
                }
                else
                {
                    // Tải dữ liệu tuần (dùng cho cả Ngày và Tuần)
                    var data = await ApiClient.Instance.GetFromJsonAsync<LichLamViecViewDto>($"api/app/nhanvien/lichlamviec/tuan?ngayTrongTuan={_currentDate:yyyy-MM-dd}");
                    if (data == null) return;

                    if (_currentView == ViewMode.Week)
                    {
                        lblDateRange.Text = $"{data.NgayBatDauTuan:dd/MM} - {data.NgayKetThucTuan:dd/MM/yyyy}";
                        RenderWeekView(data);
                    }
                    else // _currentView == ViewMode.Day
                    {
                        lblDateRange.Text = _currentDate.ToString("dddd, dd MMMM, yyyy", new CultureInfo("vi-VN"));
                        RenderDayView(data);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch: {ex.Message}", "Lỗi API");
            }
        }

        #region Render Chế Độ Xem Tuần (Week View)

        private void RenderWeekView(LichLamViecViewDto data)
        {
            ClearScheduleGrid(WeekViewGrid);

            // Cập nhật header các cột ngày
            for (int i = 0; i < 7; i++)
            {
                _weekDayHeaders[i].Text = $"{_weekDayHeaders[i].Name.Substring(9)} ({data.NgayBatDauTuan.AddDays(i):dd/MM})";
            }

            // Vẽ Ca Làm
            foreach (var shift in data.LichLamViecTrongTuan)
            {
                int dayOfWeek = (int)shift.NgayLam.DayOfWeek;
                int gridColumn = (dayOfWeek == 0) ? 7 : dayOfWeek; // Col 7 = CN

                var (gridRow, rowSpan) = CalculateTimeSlot(shift.GioBatDau, shift.GioKetThuc);
                var shiftBorder = CreateEventBorder(
                    $"{shift.TenCa}\n{shift.GioBatDau:hh\\:mm} - {shift.GioKetThuc:hh\\:mm}",
                    "Shift");

                Grid.SetRow(shiftBorder, gridRow);
                Grid.SetColumn(shiftBorder, gridColumn);
                Grid.SetRowSpan(shiftBorder, rowSpan);
                WeekViewGrid.Children.Add(shiftBorder);
            }

            // Vẽ Đơn Nghỉ (Cả ngày)
            foreach (var leave in data.DonNghiTrongTuan)
            {
                for (DateTime day = leave.NgayBatDau.Date; day <= leave.NgayKetThuc.Date; day = day.AddDays(1))
                {
                    if (day >= data.NgayBatDauTuan && day <= data.NgayKetThucTuan)
                    {
                        int dayOfWeek = (int)day.DayOfWeek;
                        int gridColumn = (dayOfWeek == 0) ? 7 : dayOfWeek;

                        var leaveBorder = CreateAllDayEventBorder(leave.LoaiDon, "Leave");
                        Grid.SetRow(leaveBorder, 1); // Hàng "Cả ngày"
                        Grid.SetColumn(leaveBorder, gridColumn);
                        WeekViewGrid.Children.Add(leaveBorder);
                    }
                }
            }
        }

        #endregion

        #region Render Chế Độ Xem Ngày (Day View)

        private void RenderDayView(LichLamViecViewDto data)
        {
            ClearScheduleGrid(DayViewGrid);

            // Lọc sự kiện chỉ cho ngày hiện tại
            var shiftsToday = data.LichLamViecTrongTuan.Where(s => s.NgayLam.Date == _currentDate.Date).ToList();
            var leaveToday = data.DonNghiTrongTuan.Where(l => _currentDate.Date >= l.NgayBatDau.Date && _currentDate.Date <= l.NgayKetThuc.Date).ToList();

            // Cập nhật header
            lblHeaderDay.Text = _currentDate.ToString("dddd (dd/MM)", new CultureInfo("vi-VN"));

            // Vẽ Ca Làm
            foreach (var shift in shiftsToday)
            {
                var (gridRow, rowSpan) = CalculateTimeSlot(shift.GioBatDau, shift.GioKetThuc);
                var shiftBorder = CreateEventBorder(
                    $"{shift.TenCa}\n{shift.GioBatDau:hh\\:mm} - {shift.GioKetThuc:hh\\:mm}",
                    "Shift");

                Grid.SetRow(shiftBorder, gridRow);
                Grid.SetColumn(shiftBorder, 1); // Luôn là cột 1
                Grid.SetRowSpan(shiftBorder, rowSpan);
                DayViewGrid.Children.Add(shiftBorder);
            }

            // Vẽ Đơn Nghỉ (Cả ngày)
            foreach (var leave in leaveToday)
            {
                var leaveBorder = CreateAllDayEventBorder(leave.LoaiDon, "Leave");
                Grid.SetRow(leaveBorder, 1); // Hàng "Cả ngày"
                Grid.SetColumn(leaveBorder, 1);
                DayViewGrid.Children.Add(leaveBorder);
            }
        }

        #endregion

        #region Render Chế Độ Xem Tháng (Month View)

        private void RenderMonthView(List<LichLamViecNgayDto> events)
        {
            ClearScheduleGrid(MonthViewGrid);

            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int diff = (7 + (int)firstDayOfMonth.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            DateTime startOfGrid = firstDayOfMonth.AddDays(-1 * diff);

            var eventLookup = events.ToDictionary(e => e.Ngay.Date, e => e.SuKien);

            for (int i = 0; i < 42; i++) // 6 tuần * 7 ngày
            {
                int row = i / 7 + 1; // +1 vì hàng 0 là header
                int col = i % 7;
                DateTime day = startOfGrid.AddDays(i);

                Border dayCell = new Border
                {
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = (day.Month == _currentDate.Month) ? Brushes.White : (SolidColorBrush)FindResource("HeaderBackgroundBrush")
                };
                if (day == DateTime.Today)
                    dayCell.Background = (SolidColorBrush)FindResource("TodayBackgroundBrush");

                StackPanel dayPanel = new StackPanel { Orientation = Orientation.Vertical };

                TextBlock dayNumber = new TextBlock
                {
                    Text = day.Day.ToString(),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(5, 2, 0, 0),
                    Foreground = (day.Month == _currentDate.Month) ? Brushes.Black : (SolidColorBrush)FindResource("OtherMonthTextBrush")
                };

                dayPanel.Children.Add(dayNumber);

                if (eventLookup.TryGetValue(day, out var suKienTrongNgay))
                {
                    foreach (var tenSuKien in suKienTrongNgay.Take(3)) // Hiển thị tối đa 3 sự kiện
                    {
                        TextBlock eventText = new TextBlock
                        {
                            Text = tenSuKien,
                            FontSize = 9,
                            Background = tenSuKien.Contains("Nghỉ") ? (SolidColorBrush)FindResource("LeaveColor") : (SolidColorBrush)FindResource("ShiftColor1"),
                            Foreground = tenSuKien.Contains("Nghỉ") ? (SolidColorBrush)FindResource("LeaveBorderColor") : (SolidColorBrush)FindResource("ShiftBorderColor1"),
                            Margin = new Thickness(2, 1, 2, 0),
                            Padding = new Thickness(3, 1, 3, 1),
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };
                        dayPanel.Children.Add(eventText);
                    }
                }

                dayCell.Child = dayPanel;
                Grid.SetRow(dayCell, row);
                Grid.SetColumn(dayCell, col);
                MonthViewGrid.Children.Add(dayCell);
            }
        }

        #endregion

        #region Hàm Helper (Chung)

        private void ClearScheduleGrid(Grid grid)
        {
            var elementsToRemove = grid.Children.OfType<UIElement>()
                .Where(e => Grid.GetRow(e) > 0 && Grid.GetColumn(e) >= 0)
                .ToList();

            if (grid == MonthViewGrid)
            {
                elementsToRemove = grid.Children.OfType<UIElement>()
                   .Where(e => Grid.GetRow(e) > 0)
                   .ToList();
            }

            foreach (var el in elementsToRemove)
            {
                grid.Children.Remove(el);
            }
        }

        private (int gridRow, int rowSpan) CalculateTimeSlot(TimeSpan startTime, TimeSpan endTime)
        {
            int gridRow = startTime.Hours - START_HOUR + 2; // +2 vì (Header + AllDay)
            double durationHours = (endTime - startTime).TotalHours;
            int rowSpan = (int)Math.Ceiling(durationHours);

            if (gridRow < 2) gridRow = 2;
            if (gridRow + rowSpan > TOTAL_GRID_ROWS) rowSpan = TOTAL_GRID_ROWS - gridRow;
            if (rowSpan < 1) rowSpan = 1;

            return (gridRow, rowSpan);
        }

        private Border CreateEventBorder(string text, string tag)
        {
            Border border = new Border
            {
                Background = (SolidColorBrush)FindResource("ShiftColor1"),
                BorderBrush = (SolidColorBrush)FindResource("ShiftBorderColor1"),
                BorderThickness = new Thickness(2, 2, 2, 2),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2, 2, 2, 2),
                Tag = tag
            };

            border.Child = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            return border;
        }

        private Border CreateAllDayEventBorder(string text, string tag)
        {
            Border border = new Border
            {
                Background = (SolidColorBrush)FindResource("LeaveColor"),
                BorderBrush = (SolidColorBrush)FindResource("LeaveBorderColor"),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Margin = new Thickness(1, 1, 1, 1),
                Tag = tag
            };

            border.Child = new TextBlock
            {
                Text = text,
                Padding = new Thickness(5, 2, 5, 2),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = (SolidColorBrush)FindResource("LeaveBorderColor"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            return border;
        }

        #endregion

        #region Điều Hướng (Navigation)

        private async void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == ViewMode.Day) _currentDate = _currentDate.AddDays(-1);
            else if (_currentView == ViewMode.Week) _currentDate = _currentDate.AddDays(-7);
            else _currentDate = _currentDate.AddMonths(-1);
            await LoadScheduleAsync();
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == ViewMode.Day) _currentDate = _currentDate.AddDays(1);
            else if (_currentView == ViewMode.Week) _currentDate = _currentDate.AddDays(7);
            else _currentDate = _currentDate.AddMonths(1);
            await LoadScheduleAsync();
        }

        private async void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = DateTime.Today;
            await LoadScheduleAsync();
        }

        #endregion

        #region Chuyển Đổi Chế Độ Xem (View Switching)

        private void UpdateViewControls()
        {
            DayViewScroll.Visibility = (_currentView == ViewMode.Day) ? Visibility.Visible : Visibility.Collapsed;
            WeekViewScroll.Visibility = (_currentView == ViewMode.Week) ? Visibility.Visible : Visibility.Collapsed;
            MonthViewScroll.Visibility = (_currentView == ViewMode.Month) ? Visibility.Visible : Visibility.Collapsed;

            btnViewDay.Background = (_currentView == ViewMode.Day) ? _activeViewBrush : _inactiveViewBrush;
            btnViewWeek.Background = (_currentView == ViewMode.Week) ? _activeViewBrush : _inactiveViewBrush;
            btnViewMonth.Background = (_currentView == ViewMode.Month) ? _activeViewBrush : _inactiveViewBrush;
        }

        private async void BtnViewDay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == ViewMode.Day) return;
            _currentView = ViewMode.Day;
            UpdateViewControls();
            await LoadScheduleAsync();
        }

        private async void BtnViewWeek_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == ViewMode.Week) return;
            _currentView = ViewMode.Week;
            UpdateViewControls();
            await LoadScheduleAsync();
        }

        private async void BtnViewMonth_Click(object sender, RoutedEventArgs e)
        {
            if (_currentView == ViewMode.Month) return;
            _currentView = ViewMode.Month;
            UpdateViewControls();
            await LoadScheduleAsync();
        }

        #endregion
    }
}