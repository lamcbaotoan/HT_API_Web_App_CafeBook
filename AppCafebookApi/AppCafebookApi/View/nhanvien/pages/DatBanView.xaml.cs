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
        private List<KhachHangLookupDto> _customerSearchCache = new List<KhachHangLookupDto>();

        private PhieuDatBanDto? _selectedPhieu;
        private JsonSerializerOptions _jsonOptions;
        private bool _isCustomerSearchLoading = false;

        static DatBanView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
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

            cmbSearchTrangThai.SelectedIndex = 0;
            cmbTrangThai.SelectedIndex = 0;
            dpThoiGianDat.SelectedDate = DateTime.Now;

            InitializeTimePickers();
        }

        private void InitializeTimePickers()
        {
            cmbHour.ItemsSource = Enumerable.Range(0, 24).Select(h => h.ToString("00"));
            cmbMinute.ItemsSource = new List<string> { "00", "15", "30", "45" };

            var now = DateTime.Now;
            int minute = (now.Minute / 15) * 15;
            cmbHour.SelectedItem = now.ToString("HH");
            cmbMinute.SelectedItem = minute.ToString("00");
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
                LoadKhuVucAsync()
            );

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
                if (data != null)
                {
                    _allPhieuDatBansCache = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách phiếu đặt bàn: " + ex.Message);
            }
        }

        private async Task LoadAvailableBansAsync()
        {
            try
            {
                var data = await httpClient.GetFromJsonAsync<List<BanDatBanDto>>("api/app/datban/available-bans", _jsonOptions);
                _allBansCache.Clear();
                AvailableBans.Clear();
                if (data != null)
                {
                    _allBansCache = data;
                    foreach (var item in data.OrderBy(b => b.SoBan))
                    {
                        AvailableBans.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách bàn: " + ex.Message);
            }
        }

        private async Task LoadKhuVucAsync()
        {
            try
            {
                _allKhuVucCache = (await httpClient.GetFromJsonAsync<List<KhuVucDto>>("api/app/banquanly/tree"))
                                 ?? new List<KhuVucDto>();

                var filterList = new List<KhuVucDto>(_allKhuVucCache);
                filterList.Insert(0, new KhuVucDto { IdKhuVuc = 0, TenKhuVuc = "Tất cả Khu vực" });

                cmbFilterKhuVuc_Form.ItemsSource = filterList;
                cmbFilterKhuVuc_Form.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách Khu Vực: {ex.Message}");
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
                cmbSearchTrangThai.SelectedIndex = 0; // Reset về "Chờ xác nhận"
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
                // 1. Chế độ xem Lịch Sử Hủy
                filteredList = _allPhieuDatBansCache.Where(p => p.TrangThai == "Đã hủy");
            }
            else
            {
                // 2. Chế độ xem Hiện Tại (Chờ, Đã xác nhận)
                // SỬA LỖI CS1061: Dùng (SelectedItem as ComboBoxItem).Content
                var trangThaiFilter = (cmbSearchTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Chờ xác nhận";
                filteredList = _allPhieuDatBansCache.Where(p => p.TrangThai == trangThaiFilter);
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

            // Sắp xếp
            if (showHistory)
            {
                filteredList = filteredList.OrderByDescending(p => p.ThoiGianDat); // Hủy mới nhất lên đầu
            }
            else
            {
                filteredList = filteredList.OrderBy(p => p.ThoiGianDat); // Chờ sớm nhất lên đầu
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

            txtTenKhach.Text = _selectedPhieu.TenKhachHang;
            cmbSoDienThoai.Text = _selectedPhieu.SoDienThoai;
            cmbEmail.Text = _selectedPhieu.Email;
            dpThoiGianDat.SelectedDate = _selectedPhieu.ThoiGianDat;
            cmbHour.SelectedItem = _selectedPhieu.ThoiGianDat.ToString("HH");
            cmbMinute.SelectedItem = _selectedPhieu.ThoiGianDat.ToString("mm");

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

            // YÊU CẦU 4: Hiển thị nút Xác Nhận
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
            else // Đã hủy, đã đến
            {
                btnXacNhanDen_Form.Visibility = Visibility.Collapsed;
                menuXacNhanDen.IsEnabled = false;
                menuHuyPhieu.IsEnabled = false;
            }

            btnSua.IsEnabled = true;
            btnXoa.IsEnabled = true;
        }

        private void ClearForm()
        {
            _selectedPhieu = null;
            txtTenKhach.Text = string.Empty;
            cmbSoDienThoai.Text = string.Empty;
            cmbEmail.Text = string.Empty;
            cmbBan.SelectedIndex = -1;
            dpThoiGianDat.SelectedDate = DateTime.Now;

            var now = DateTime.Now;
            int minute = (now.Minute / 15) * 15;
            cmbHour.SelectedItem = now.ToString("HH");
            cmbMinute.SelectedItem = minute.ToString("00");

            txtSoLuongKhach.Text = "1";
            cmbTrangThai.SelectedIndex = 0;
            txtGhiChu.Text = string.Empty;

            dgPhieuDatBan.SelectedIndex = -1;
            btnSua.IsEnabled = false;
            btnXoa.IsEnabled = false;
            btnXacNhanDen_Form.Visibility = Visibility.Collapsed;
            menuXacNhanDen.IsEnabled = false;
            menuHuyPhieu.IsEnabled = false;
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

            if (string.IsNullOrWhiteSpace(txtTenKhach.Text) ||
                string.IsNullOrWhiteSpace(cmbSoDienThoai.Text) ||
                cmbBan.SelectedValue == null ||
                dpThoiGianDat.SelectedDate == null ||
                cmbHour.SelectedValue == null || cmbMinute.SelectedValue == null ||
                string.IsNullOrWhiteSpace(txtSoLuongKhach.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ các trường bắt buộc (*).");
                return false;
            }

            // SỬA LỖI CS8604: Thêm kiểm tra null trước khi Parse
            DateTime selectedDate = dpThoiGianDat.SelectedDate.Value;
            int hour = int.Parse(cmbHour.SelectedItem?.ToString() ?? "0");
            int minute = int.Parse(cmbMinute.SelectedItem?.ToString() ?? "0");
            DateTime thoiGianDat = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, hour, minute, 0);

            var selectedBan = cmbBan.SelectedItem as BanDatBanDto;
            int soKhach = int.Parse(txtSoLuongKhach.Text);
            if (selectedBan != null && soKhach > selectedBan.SoGhe)
            {
                MessageBox.Show($"Số lượng khách ({soKhach}) vượt quá số ghế của bàn ({selectedBan.SoGhe}).", "Lỗi Sức Chứa");
                return false;
            }

            dto = new PhieuDatBanCreateUpdateDto
            {
                TenKhachHang = txtTenKhach.Text.Trim(),
                SoDienThoai = cmbSoDienThoai.Text.Trim(),
                Email = cmbEmail.Text.Trim(),
                IdBan = (int)cmbBan.SelectedValue,
                ThoiGianDat = thoiGianDat,
                SoLuongKhach = soKhach,
                GhiChu = txtGhiChu.Text.Trim(),
                TrangThai = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đã xác nhận",
                IdNhanVienTao = AuthService.CurrentUser.IdNhanVien
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
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi API: " + ex.Message);
            }
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
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi API: " + ex.Message);
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu để xóa.");
                return;
            }
            if (MessageBox.Show($"Bạn có chắc muốn xóa phiếu của khách '{_selectedPhieu.TenKhachHang}'?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            await HuyPhieu(_selectedPhieu.IdPhieuDatBan, true);
        }

        // SỬA LỖI CS1061: Thêm các hàm này
        private void MenuSuaPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu để sửa.");
            }
            // Dữ liệu đã được nạp lên form, không cần làm gì thêm
        }

        private void MenuXoaPhieu_Click(object sender, RoutedEventArgs e)
        {
            BtnXoa_Click(sender, e);
        }

        #endregion

        #region Xử lý Context Menu và Nút Form

        private async void BtnXacNhanDen_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieu == null) return;
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Phiên đăng nhập hết hạn.");
                return;
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
                    MessageBox.Show("Đã xác nhận khách đến. Mở giao diện gọi món...");

                    var mainWindow = Application.Current.MainWindow as ManHinhNhanVien;

                    if (mainWindow != null && result != null)
                    {
                        // SỬA: Dùng tên Frame chính xác từ ManHinhNhanVien.xaml.cs
                        mainWindow.MainFrame.Navigate(new GoiMonView(result.IdHoaDon));
                    }

                    await LoadAllDataAsync();
                    ApplyFilter();
                }
                else
                {
                    MessageBox.Show("Lỗi xác nhận: " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi API: " + ex.Message);
            }
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
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi API: " + ex.Message);
            }
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

            // SỬA LỖI CS1061: Thay IsLetterOrDigit bằng cách kiểm tra phím điều hướng
            if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Escape)
            {
                return; // Không lọc khi nhấn các phím này
            }

            string searchText = cmbBan.Text.ToLower();
            var selectedKhuVucId = (int?)cmbFilterKhuVuc_Form.SelectedValue;

            var filteredBans = _allBansCache
                .Where(b => (selectedKhuVucId == 0 || b.IdKhuVuc == selectedKhuVucId))
                .Where(b => b.SoBan.ToLower().Contains(searchText));

            AvailableBans.Clear();
            foreach (var ban in filteredBans.OrderBy(b => b.SoBan))
            {
                AvailableBans.Add(ban);
            }
            cmbBan.IsDropDownOpen = true;
        }

        private async void CmbKhachHang_KeyUp(object sender, KeyEventArgs e)
        {
            if (_isCustomerSearchLoading) return;

            var cmb = sender as ComboBox;
            if (cmb == null || cmb.Text.Length < 3) return;

            _isCustomerSearchLoading = true;
            try
            {
                var results = await SearchCustomerAsync(cmb.Text);
                if (results != null)
                {
                    _customerSearchCache = results;
                    cmb.ItemsSource = results;
                    cmb.IsDropDownOpen = true;
                }
            }
            finally
            {
                _isCustomerSearchLoading = false;
            }
        }

        private async Task<List<KhachHangLookupDto>> SearchCustomerAsync(string query)
        {
            try
            {
                var data = await httpClient.GetFromJsonAsync<List<KhachHangLookupDto>>($"api/app/datban/search-customer?query={query}", _jsonOptions);
                return data ?? new List<KhachHangLookupDto>();
            }
            catch { return new List<KhachHangLookupDto>(); }
        }

        private void CmbKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isCustomerSearchLoading || e.AddedItems.Count == 0) return;

            var selectedCustomer = e.AddedItems[0] as KhachHangLookupDto;
            if (selectedCustomer != null)
            {
                _isCustomerSearchLoading = true;

                txtTenKhach.Text = selectedCustomer.HoTen;
                cmbSoDienThoai.Text = selectedCustomer.SoDienThoai;
                cmbEmail.Text = selectedCustomer.Email;

                cmbSoDienThoai.ItemsSource = _customerSearchCache;
                cmbEmail.ItemsSource = _customerSearchCache;

                _isCustomerSearchLoading = false;
            }
        }

        private void TxtSoDienThoai_TextChanged(object sender, TextChangedEventArgs e) { }

        #endregion
    }

    // SỬA LỖI XDG0008: Di chuyển Converter ra ngoài class
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