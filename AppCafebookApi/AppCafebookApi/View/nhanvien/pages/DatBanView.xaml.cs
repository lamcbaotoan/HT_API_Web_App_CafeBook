// [DatBanView.xaml.cs]
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp.NhanVien.DatBan;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AppCafebookApi.View.nhanvien;
using CafebookModel.Model.ModelApp;
using System.Windows.Threading; // THÊM MỚI
using System.Diagnostics; // THÊM MỚI

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class DatBanView : Page
    {
        private static readonly HttpClient httpClient;

        private ObservableCollection<PhieuDatBanDto> PhieuDatBans { get; set; }
        private ObservableCollection<BanDatBanDto> AvailableBans { get; set; }

        private List<PhieuDatBanDto> _allPhieuDatBansCache = new List<PhieuDatBanDto>();
        private List<BanDatBanDto> _allBansCache = new List<BanDatBanDto>();
        private List<KhuVucDto> _allKhuVucCache = new List<KhuVucDto>();
        // XÓA: private List<KhachHangLookupDto> _globalCustomerCache = new List<KhachHangLookupDto>();

        private PhieuDatBanDto? _selectedPhieu;
        private JsonSerializerOptions _jsonOptions;

        // SỬA: Thay thế _isCustomerSearchLoading
        private DispatcherTimer _searchKhachTimer;
        private bool _isUpdatingKhachText = false;


        private (TimeSpan Open, TimeSpan Close) _openingHours = (new TimeSpan(6, 0, 0), new TimeSpan(23, 0, 0));
        private List<string> _validHours = new List<string>();
        private List<string> _validMinutes = Enumerable.Range(0, 60).Select(m => m.ToString("00")).ToList();

        static DatBanView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166") // Đảm bảo địa chỉ này là đúng
            };
        }

        public DatBanView()
        {
            InitializeComponent();
            PhieuDatBans = new ObservableCollection<PhieuDatBanDto>();
            AvailableBans = new ObservableCollection<BanDatBanDto>();
            this.DataContext = this;

            dgPhieuDatBan.ItemsSource = PhieuDatBans;
            cmbBan.ItemsSource = AvailableBans;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // THÊM MỚI: Khởi tạo Timer
            _searchKhachTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchKhachTimer.Tick += async (s, e) => { _searchKhachTimer.Stop(); await SearchKhachHangAsync(); };

            cmbSearchTrangThai.SelectedIndex = 0; // Mặc định là "Tất cả"
            cmbTrangThai.SelectedIndex = 0;
            dpThoiGianDat.SelectedDate = DateTime.Now;

            InitializeTimePickers(false);
        }

        private void InitializeTimePickers(bool useCache)
        {
            if (useCache) // useCache = true khi đã tải _openingHours từ API
            {
                _validHours.Clear();

                // SỬA: Logic vòng lặp giờ
                // Giờ đóng cửa là 23:00. Dừng trước 1 tiếng nghĩa là giờ cuối cùng là 21:xx (chọn "21").
                // Vì vậy, vòng lặp phải DƯỚI 22 (tức là < GiờĐóngCửa - 1).
                int gioBatDau = _openingHours.Open.Hours; // vd: 6
                int gioCuoiCung = _openingHours.Close.Hours - 1; // vd: 23 - 1 = 22

                for (int h = gioBatDau; h < gioCuoiCung; h++)
                {
                    _validHours.Add(h.ToString("00"));
                }
            }
            else
            {
                _validHours = Enumerable.Range(0, 24).Select(h => h.ToString("00")).ToList();
            }

            cmbHour.ItemsSource = _validHours;
            cmbMinute.ItemsSource = _validMinutes;

            var now = DateTime.Now;
            DateTime roundedNow = now;
            //int minute = (now.Minute / 15 + 1) * 15;
            //DateTime roundedNow = now.Date.AddHours(now.Hour).AddMinutes(minute);

            string currentHour = roundedNow.ToString("HH");
            if (_validHours.Contains(currentHour))
            {
                cmbHour.Text = currentHour;
            }
            else if (roundedNow.TimeOfDay < _openingHours.Open)
            {
                cmbHour.Text = _openingHours.Open.ToString("hh");
            }
            else
            {
                cmbHour.Text = _validHours.LastOrDefault();
            }

            cmbMinute.Text = roundedNow.ToString("mm");
            dpThoiGianDat.SelectedDate = roundedNow.Date;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAllDataAsync();
        }

        #region Tải và Lọc Dữ Liệu

        private async Task LoadAllDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await Task.WhenAll(
                LoadPhieuDatBansAsync(),
                LoadAvailableBansAsync(),
                LoadKhuVucAsync(),
                LoadOpeningHoursAsync()
            );

            InitializeTimePickers(true);

            ApplyFilter();
            ClearForm();
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private async Task LoadPhieuDatBansAsync()
        {
            try
            {
                var data = await httpClient.GetFromJsonAsync<List<PhieuDatBanDto>>("api/app/datban/list", _jsonOptions);
                _allPhieuDatBansCache.Clear();
                if (data != null) _allPhieuDatBansCache = data;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải danh sách phiếu đặt bàn: " + ex.Message); }
        }
        private async Task LoadAvailableBansAsync()
        {
            try
            {
                var data = await httpClient.GetFromJsonAsync<List<BanDatBanDto>>("api/app/datban/available-bans", _jsonOptions);
                _allBansCache.Clear(); AvailableBans.Clear();
                if (data != null)
                {
                    _allBansCache = data;
                    foreach (var item in data.OrderBy(b => b.SoBan)) AvailableBans.Add(item);
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải danh sách bàn: " + ex.Message); }
        }
        private async Task LoadKhuVucAsync()
        {
            try
            {
                // Giả định API này tồn tại
                _allKhuVucCache = (await httpClient.GetFromJsonAsync<List<KhuVucDto>>("api/app/banquanly/tree")) ?? new List<KhuVucDto>();
                var filterList = new List<KhuVucDto>(_allKhuVucCache);
                filterList.Insert(0, new KhuVucDto { IdKhuVuc = 0, TenKhuVuc = "Tất cả Khu vực" });
                cmbFilterKhuVuc_Form.ItemsSource = filterList;
                cmbFilterKhuVuc_Form.SelectedIndex = 0;
            }
            catch (Exception ex) { MessageBox.Show($"Không thể tải danh sách Khu Vực: {ex.Message}"); }
        }

        private async Task LoadOpeningHoursAsync()
        {
            try
            {
                string settingValue = await httpClient.GetStringAsync("api/app/datban/opening-hours");
                ParseOpeningHoursClient(settingValue);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải giờ mở cửa: " + ex.Message);
                ParseOpeningHoursClient("06:00 - 23:00");
            }
        }

        private void ParseOpeningHoursClient(string settingValue)
        {
            try
            {
                var match = Regex.Match(settingValue, @"(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})");
                if (match.Success)
                {
                    TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan open);
                    TimeSpan.TryParse(match.Groups[2].Value, out TimeSpan close);
                    _openingHours = (open, close);
                }
            }
            catch
            {
                _openingHours = (new TimeSpan(6, 0, 0), new TimeSpan(23, 0, 0));
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        private void TxtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFilter();
            }
        }

        private void BtnShowHistory_Click(object sender, RoutedEventArgs e)
        {
            bool showHistory = btnShowHistory.IsChecked == true;
            if (showHistory)
            {
                btnShowHistory.Content = "Hiện Đơn Đang Chờ";
                cmbSearchTrangThai.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnShowHistory.Content = "Hiện Lịch Sử Hủy";
                cmbSearchTrangThai.Visibility = Visibility.Visible;
                cmbSearchTrangThai.SelectedIndex = 0; // Reset về "Tất cả"
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allPhieuDatBansCache == null) return;
            IEnumerable<PhieuDatBanDto> filteredList;
            bool showHistory = btnShowHistory.IsChecked == true;

            if (showHistory)
            {
                filteredList = _allPhieuDatBansCache.Where(p =>
                    p.TrangThai == "Đã hủy" ||
                    p.TrangThai == "Khách đã đến");
            }
            else
            {
                var trangThaiFilter = (cmbSearchTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tất cả";

                if (trangThaiFilter == "Tất cả")
                {
                    filteredList = _allPhieuDatBansCache.Where(p =>
                        p.TrangThai == "Chờ xác nhận" ||
                        p.TrangThai == "Đã xác nhận");
                }
                else
                {
                    filteredList = _allPhieuDatBansCache.Where(p => p.TrangThai == trangThaiFilter);
                }
            }

            var filterText = txtSearch.Text.ToLower().Trim();
            if (!string.IsNullOrEmpty(filterText))
            {
                filteredList = filteredList
                    .Where(p => (p.TenKhachHang != null && p.TenKhachHang.ToLower().Contains(filterText)) ||
                                (p.SoDienThoai != null && p.SoDienThoai.Contains(filterText)));
            }
            var filterDate = dpSearchDate.SelectedDate;
            if (filterDate.HasValue)
            {
                filteredList = filteredList.Where(p => p.ThoiGianDat.Date == filterDate.Value.Date);
            }
            if (showHistory)
            {
                filteredList = filteredList.OrderByDescending(p => p.ThoiGianDat);
            }
            else
            {
                filteredList = filteredList.OrderBy(p => p.ThoiGianDat);
            }
            PhieuDatBans.Clear();
            foreach (var item in filteredList)
            {
                PhieuDatBans.Add(item);
            }
        }


        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            dpSearchDate.SelectedDate = null;
            cmbSearchTrangThai.SelectedIndex = 0;
            btnShowHistory.IsChecked = false;
            await LoadAllDataAsync();
            ApplyFilter();
        }

        #endregion

        #region Xử lý Form (Thêm/Sửa/Xóa)

        private void DgPhieuDatBan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPhieu = dgPhieuDatBan.SelectedItem as PhieuDatBanDto;
            if (_selectedPhieu == null)
            {
                ClearForm();
                return;
            }

            // SỬA: Dùng _isUpdatingKhachText
            _isUpdatingKhachText = true;
            txtTenKhach.Text = _selectedPhieu.TenKhachHang;
            txtSdtKH.Text = _selectedPhieu.SoDienThoai;
            txtEmailKH.Text = _selectedPhieu.Email;
            _isUpdatingKhachText = false;

            lbKhachHangResults.Visibility = Visibility.Collapsed;
            lbKhachHangResults.ItemsSource = null;

            dpThoiGianDat.SelectedDate = _selectedPhieu.ThoiGianDat;
            cmbHour.Text = _selectedPhieu.ThoiGianDat.ToString("HH");
            cmbMinute.Text = _selectedPhieu.ThoiGianDat.ToString("mm");
            txtSoLuongKhach.Text = _selectedPhieu.SoLuongKhach.ToString();
            txtGhiChu.Text = _selectedPhieu.GhiChu;
            foreach (ComboBoxItem item in cmbTrangThai.Items)
            {
                if (item.Content.ToString() == _selectedPhieu.TrangThai)
                {
                    cmbTrangThai.SelectedItem = item;
                    break;
                }
            }
            var ban = _allBansCache.FirstOrDefault(b => b.IdBan == _selectedPhieu.IdBan);
            if (ban == null)
            {
                if (!AvailableBans.Any(b => b.IdBan == _selectedPhieu.IdBan))
                {
                    AvailableBans.Add(new BanDatBanDto { IdBan = _selectedPhieu.IdBan, SoBan = _selectedPhieu.SoBan, TenKhuVuc = _selectedPhieu.TenKhuVuc, IdKhuVuc = 0, SoGhe = 0 });
                }
            }
            cmbBan.SelectedValue = _selectedPhieu.IdBan;
            if (_selectedPhieu.TrangThai == "Đã xác nhận")
            {
                btnXacNhanDen_Form.Visibility = Visibility.Visible;
                menuXacNhanDen.IsEnabled = true;
                menuHuyPhieu.IsEnabled = true;
            }
            else if (_selectedPhieu.TrangThai == "Chờ xác nhận")
            {
                btnXacNhanDen_Form.Visibility = Visibility.Collapsed;
                menuXacNhanDen.IsEnabled = true;
                menuHuyPhieu.IsEnabled = true;
            }
            else
            {
                btnXacNhanDen_Form.Visibility = Visibility.Collapsed;
                menuXacNhanDen.IsEnabled = false;
                menuHuyPhieu.IsEnabled = false;
            }
            btnSua.IsEnabled = true;
            btnXoa.IsEnabled = true;

            // XÓA: Toàn bộ logic chkKhachVangLai
        }

        private void ClearForm()
        {
            _selectedPhieu = null;

            _isUpdatingKhachText = true; // SỬA
            txtTenKhach.Text = string.Empty;
            txtSdtKH.Text = string.Empty;
            txtEmailKH.Text = string.Empty;
            _isUpdatingKhachText = false;

            lbKhachHangResults.ItemsSource = null; // SỬA
            lbKhachHangResults.Visibility = Visibility.Collapsed; // SỬA

            cmbBan.SelectedIndex = -1;

            var now = DateTime.Now;
            DateTime roundedNow = now;
            //int minute = (now.Minute / 15 + 1) * 15;
            //DateTime roundedNow = now.Date.AddHours(now.Hour).AddMinutes(minute);

            dpThoiGianDat.SelectedDate = roundedNow.Date;
            cmbHour.Text = roundedNow.ToString("HH");
            cmbMinute.Text = roundedNow.ToString("mm");

            txtSoLuongKhach.Text = "1";
            cmbTrangThai.SelectedIndex = 0;
            txtGhiChu.Text = string.Empty;
            dgPhieuDatBan.SelectedIndex = -1;
            btnSua.IsEnabled = false;
            btnXoa.IsEnabled = false;
            btnXacNhanDen_Form.Visibility = Visibility.Collapsed;
            menuXacNhanDen.IsEnabled = false;
            menuHuyPhieu.IsEnabled = false;

            // XÓA: Toàn bộ logic chkKhachVangLai
        }

        private void BtnLamMoiForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private bool ValidateForm(out PhieuDatBanCreateUpdateDto? dto)
        {
            dto = null;
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.");
                return false;
            }

            // SỬA: Đọc từ TextBox
            if (string.IsNullOrWhiteSpace(txtTenKhach.Text) ||
                string.IsNullOrWhiteSpace(txtSdtKH.Text) ||
                cmbBan.SelectedValue == null ||
                dpThoiGianDat.SelectedDate == null ||
                string.IsNullOrWhiteSpace(cmbHour.Text) || string.IsNullOrWhiteSpace(cmbMinute.Text) ||
                string.IsNullOrWhiteSpace(txtSoLuongKhach.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ các trường bắt buộc (*).");
                return false;
            }
            if (!int.TryParse(cmbHour.Text, out int hour) || hour < 0 || hour > 23)
            {
                MessageBox.Show("Giờ nhập không hợp lệ (phải là số 0-23).");
                return false;
            }
            if (!int.TryParse(cmbMinute.Text, out int minute) || minute < 0 || minute > 59)
            {
                MessageBox.Show("Phút nhập không hợp lệ (phải là số 0-59).");
                return false;
            }

            DateTime selectedDate = dpThoiGianDat.SelectedDate.Value;
            DateTime thoiGianDat = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, hour, minute, 0);

            if (thoiGianDat < DateTime.Now.AddMinutes(10))
            {
                MessageBox.Show("Thời gian đặt phải cách ít nhất 10 phút so với hiện tại.", "Lỗi Thời Gian");
                return false;
            }

            var timeOfDay = thoiGianDat.TimeOfDay;
            if (timeOfDay < _openingHours.Open || timeOfDay > _openingHours.Close)
            {
                MessageBox.Show($"Giờ đặt ({thoiGianDat:HH:mm}) nằm ngoài giờ mở cửa ({_openingHours.Open:hh\\:mm} - {_openingHours.Close:hh\\:mm}).");
                return false;
            }
            var selectedBan = cmbBan.SelectedItem as BanDatBanDto;
            int soKhach = int.Parse(txtSoLuongKhach.Text);
            if (selectedBan != null && soKhach > selectedBan.SoGhe)
            {
                MessageBox.Show($"Số lượng khách ({soKhach}) vượt quá số ghế của bàn ({selectedBan.SoGhe}).", "Lỗi Sức Chứa");
                return false;
            }

            // SỬA: Lấy thông tin trực tiếp từ TextBox
            dto = new PhieuDatBanCreateUpdateDto
            {
                TenKhachHang = txtTenKhach.Text.Trim(),
                SoDienThoai = txtSdtKH.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(txtEmailKH.Text) ? null : txtEmailKH.Text.Trim(),
                IdBan = (int)cmbBan.SelectedValue,
                ThoiGianDat = thoiGianDat,
                SoLuongKhach = soKhach,
                GhiChu = txtGhiChu.Text.Trim(),
                TrangThai = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đã xác nhận",
                IdNhanVienTao = AuthService.CurrentUser.IdNhanVien,
                // IsKhachVangLai đã được XÓA
            };
            return true;
        }


        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(out var dto)) return;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/datban/create-staff", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thêm phiếu đặt bàn thành công.");
                    await LoadAllDataAsync();
                    ApplyFilter();
                }
                else
                {
                    MessageBox.Show("Lỗi tạo phiếu: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi API: " + ex.Message); }
        }

        private async void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null) return;
            if (!ValidateForm(out var dto)) return;
            if (dto == null) return;
            dto.IdPhieuDatBan = _selectedPhieu.IdPhieuDatBan;
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/app/datban/update/{dto.IdPhieuDatBan}", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật phiếu thành công.");
                    await LoadAllDataAsync();
                    ApplyFilter();
                }
                else
                {
                    MessageBox.Show("Lỗi cập nhật: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi API: " + ex.Message); }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu để xóa."); return;
            }
            if (MessageBox.Show($"Bạn có chắc muốn xóa phiếu của khách '{_selectedPhieu.TenKhachHang}'?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }
            await HuyPhieu(_selectedPhieu.IdPhieuDatBan, true);
        }

        private void MenuSuaPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu để sửa.");
            }
        }

        private void MenuXoaPhieu_Click(object sender, RoutedEventArgs e)
        {
            BtnXoa_Click(sender, e);
        }

        #endregion

        #region Xử lý Context Menu và Nút Form

        // === HÀM ĐÃ SỬA ===
        private async void BtnXacNhanDen_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null) return;
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Phiên đăng nhập hết hạn."); return;
            }
            if (_selectedPhieu.TrangThai != "Đã xác nhận")
            {
                MessageBox.Show("Chỉ có thể xác nhận khách đến cho phiếu 'Đã xác nhận'.");
                return;
            }
            var request = new XacNhanKhachDenRequestDto
            {
                IdPhieuDatBan = _selectedPhieu.IdPhieuDatBan,
                IdNhanVien = AuthService.CurrentUser.IdNhanVien
            };
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/datban/xacnhan-den", request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<XacNhanKhachDenResponseDto>(_jsonOptions);

                    // 1. Thông báo cho người dùng
                    MessageBox.Show("Đã xác nhận khách đến. Chuyển đến Sơ đồ bàn...");

                    // 2. === SỬA LỖI ĐIỀU HƯỚNG ===
                    if (this.NavigationService != null && result != null)
                    {
                        this.NavigationService.Navigate(new SoDoBanView(_selectedPhieu.IdBan));
                    }
                    else
                    {
                        var mainWindow = Application.Current.MainWindow as ManHinhNhanVien;
                        if (mainWindow != null && result != null)
                        {
                            mainWindow.MainFrame.Navigate(new SoDoBanView(_selectedPhieu.IdBan));
                        }
                        else
                        {
                            MessageBox.Show("Lỗi: Không tìm thấy Frame chính để điều hướng.");
                        }
                    }

                    // 3. === SỬA LỖI "ĐỨNG YÊN" ===
                    // Xóa 2 dòng này.
                    // await LoadAllDataAsync();
                    // ApplyFilter();
                }
                else
                {
                    MessageBox.Show("Lỗi xác nhận: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi API: " + ex.Message); }
        }

        private async void BtnHuyPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null) return;
            if (MessageBox.Show($"Bạn có chắc muốn HỦY phiếu của khách '{_selectedPhieu.TenKhachHang}'?",
               "Xác nhận hủy", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }
            await HuyPhieu(_selectedPhieu.IdPhieuDatBan, false);
        }

        private async Task HuyPhieu(int idPhieu, bool isDelete)
        {
            try
            {
                var response = await httpClient.PostAsync($"api/app/datban/huy/{idPhieu}", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(isDelete ? "Xóa (hủy) phiếu thành công." : "Hủy phiếu thành công.");
                    await LoadAllDataAsync();
                    ApplyFilter();
                }
                else
                {
                    MessageBox.Show("Lỗi hủy phiếu: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi API: " + ex.Message); }
        }

        #endregion

        #region Helpers (Tìm kiếm Form, Lọc ComboBox)

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CmbFilterKhuVuc_Form_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterKhuVuc_Form.SelectedItem is KhuVucDto selectedKhuVuc)
            {
                AvailableBans.Clear();
                IEnumerable<BanDatBanDto> filteredBans = _allBansCache;
                if (selectedKhuVuc.IdKhuVuc > 0)
                {
                    filteredBans = _allBansCache.Where(b => b.IdKhuVuc == selectedKhuVuc.IdKhuVuc);
                }
                foreach (var ban in filteredBans.OrderBy(b => b.SoBan))
                {
                    AvailableBans.Add(ban);
                }
            }
        }

        private void CmbBan_KeyUp(object sender, KeyEventArgs e)
        {
            if (cmbBan.ItemsSource == null) cmbBan.ItemsSource = AvailableBans;
            if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Escape)
            {
                return;
            }
            string searchText = cmbBan.Text.ToLower();
            var selectedKhuVucId = (int?)cmbFilterKhuVuc_Form.SelectedValue;
            if (selectedKhuVucId == null) selectedKhuVucId = 0;
            var filteredBans = _allBansCache
                .Where(b => (selectedKhuVucId == 0 || b.IdKhuVuc == selectedKhuVucId))
                .Where(b => b.SoBan != null && b.SoBan.ToLower().Contains(searchText));
            AvailableBans.Clear();
            foreach (var ban in filteredBans.OrderBy(b => b.SoBan))
            {
                AvailableBans.Add(ban);
            }
            cmbBan.IsDropDownOpen = true;
        }

        // XÓA: CmbKhachHang_KeyUp, CmbKhachHang_DropDownOpened, SearchCustomerAsync, CmbKhachHang_SelectionChanged
        // XÓA: ChkKhachVangLai_Changed, ToggleCustomerSearch

        // THÊM MỚI: Logic tìm kiếm khách hàng (giống ThueSachView)
        private void TxtKhachInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingKhachText) return;
            _searchKhachTimer.Stop();
            _searchKhachTimer.Start();
        }

        private async Task SearchKhachHangAsync()
        {
            string query = !string.IsNullOrWhiteSpace(txtSdtKH.Text) ? txtSdtKH.Text : txtTenKhach.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                lbKhachHangResults.ItemsSource = null;
                lbKhachHangResults.Visibility = Visibility.Collapsed;
                return;
            }
            try
            {
                // API endpoint này đã được kiểm tra (từ file DatBanController) là tìm theo SĐT, Email, Tên.
                var results = await httpClient.GetFromJsonAsync<List<KhachHangLookupDto>>($"api/app/datban/search-customer?query={query}", _jsonOptions);
                if (results != null && results.Any())
                {
                    lbKhachHangResults.ItemsSource = results;
                    lbKhachHangResults.Visibility = Visibility.Visible;
                }
                else
                {
                    lbKhachHangResults.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi tìm khách: {ex.Message}");
            }
        }

        private void LbKhachHangResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbKhachHangResults.SelectedItem is KhachHangLookupDto selected)
            {
                _isUpdatingKhachText = true;

                txtTenKhach.Text = selected.HoTen;
                txtSdtKH.Text = selected.SoDienThoai;
                txtEmailKH.Text = selected.Email;

                _isUpdatingKhachText = false;

                lbKhachHangResults.Visibility = Visibility.Collapsed;
                lbKhachHangResults.ItemsSource = null;
            }
        }

        private void CmbTime_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Escape)
            {
                return;
            }
            var cmb = sender as ComboBox;
            if (cmb == null) return;
            var source = (cmb.Name == "cmbHour") ? _validHours : _validMinutes;
            string searchText = cmb.Text;
            var filteredList = source.Where(t => t.StartsWith(searchText)).ToList();
            cmb.ItemsSource = filteredList;
            cmb.IsDropDownOpen = true;
        }

        #endregion
    }

    public class TrangThaiDatBanToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? trangThai = value as string;
            if (trangThai == null) return Brushes.Gray;
            switch (trangThai)
            {
                case "Chờ xác nhận":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA726"));
                case "Đã xác nhận":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#42A5F5"));
                case "Khách đã đến":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#66BB6A"));
                case "Đã hủy":
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350"));
                default:
                    return Brushes.LightGray;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}