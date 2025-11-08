using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CafebookModel.Model.ModelApp.NhanVien;
using AppCafebookApi.View.common;
using System.Windows.Threading;
using System.Text.Json;
using System.Diagnostics;
using AppCafebookApi.Services;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class ThueSachView : Page
    {
        private static readonly HttpClient httpClient;
        private CaiDatThueSachDto _settings = new();

        private PhieuThueChiTietDto? _selectedPhieuChiTiet;

        private DispatcherTimer _searchKhachTimer;
        private DispatcherTimer _searchSachTimer;
        private DispatcherTimer _searchPhieuTraTimer;

        private bool _isUpdatingKhachText = false;

        static ThueSachView()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5166") };
        }

        public ThueSachView()
        {
            InitializeComponent();

            _searchKhachTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchKhachTimer.Tick += async (s, e) => { _searchKhachTimer.Stop(); await SearchKhachHangAsync(); };

            _searchSachTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchSachTimer.Tick += async (s, e) => { _searchSachTimer.Stop(); await SearchSachAsync(); };

            _searchPhieuTraTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchPhieuTraTimer.Tick += async (s, e) => { _searchPhieuTraTimer.Stop(); await LoadPhieuTraAsync(); };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadSettingsAsync();
            await LoadPhieuThueAsync();
            await LoadPhieuTraAsync();
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        // (LoadSettingsAsync, LoadPhieuThueAsync, LoadPhieuTraAsync giữ nguyên)
        // ... (Giữ nguyên các hàm này) ...
        private async Task LoadSettingsAsync()
        {
            try
            {
                _settings = await httpClient.GetFromJsonAsync<CaiDatThueSachDto>("api/app/nhanvien/thuesach/settings") ?? new();
                dpNgayHenTra.SelectedDate = DateTime.Today.AddDays(_settings.SoNgayMuonToiDa);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải cài đặt: {ex.Message}", "Lỗi API");
            }
        }
        private async Task LoadPhieuThueAsync()
        {
            try
            {
                string search = txtSearchPhieuThue.Text;
                string status = (cmbTrangThaiFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đang Thuê";

                var phieuList = await httpClient.GetFromJsonAsync<List<PhieuThueGridDto>>($"api/app/nhanvien/thuesach/phieuthue?search={Uri.EscapeDataString(search)}&status={Uri.EscapeDataString(status)}");
                dgPhieuThue.ItemsSource = phieuList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách phiếu thuê: {ex.Message}", "Lỗi API");
            }
        }
        private async Task LoadPhieuTraAsync()
        {
            try
            {
                string search = txtSearchPhieuTra.Text;
                var phieuTraList = await httpClient.GetFromJsonAsync<List<PhieuTraGridDto>>($"api/app/nhanvien/thuesach/phieutra?search={Uri.EscapeDataString(search)}");
                dgPhieuTra.ItemsSource = phieuTraList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử phiếu trả: {ex.Message}", "Lỗi API");
            }
        }

        #region CỘT 1: TẠO PHIẾU

        private void TxtKhachInfo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingKhachText) return;
            _searchKhachTimer.Stop();
            _searchKhachTimer.Start();
        }

        private async Task SearchKhachHangAsync()
        {
            string query = !string.IsNullOrWhiteSpace(txtSdtKH.Text) ? txtSdtKH.Text : txtHoTenKH.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                lbKhachHangResults.ItemsSource = null;
                lbKhachHangResults.Visibility = Visibility.Collapsed;
                return;
            }
            try
            {
                var results = await httpClient.GetFromJsonAsync<List<KhachHangSearchDto>>($"api/app/nhanvien/thuesach/search-khachhang?query={query}");
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
            if (lbKhachHangResults.SelectedItem is KhachHangSearchDto selected)
            {
                _isUpdatingKhachText = true;

                txtHoTenKH.Text = selected.HoTen;
                txtSdtKH.Text = selected.SoDienThoai;
                txtEmailKH.Text = selected.Email;

                _isUpdatingKhachText = false;

                lbKhachHangResults.Visibility = Visibility.Collapsed;
                lbKhachHangResults.ItemsSource = null;
            }
        }

        // (Các hàm Tìm Sách: TxtSearchSach_TextChanged, SearchSachAsync, LbSachResults_SelectionChanged, BtnXoaSachChon_Click, UpdateTongCoc giữ nguyên)
        // ...
        private void TxtSearchSach_TextChanged(object sender, TextChangedEventArgs e) { _searchSachTimer.Stop(); _searchSachTimer.Start(); }
        private async Task SearchSachAsync()
        {
            if (string.IsNullOrEmpty(txtSearchSach.Text))
            {
                lbSachResults.ItemsSource = null;
                return;
            }
            try
            {
                var results = await httpClient.GetFromJsonAsync<List<SachTimKiemDto>>($"api/app/nhanvien/thuesach/search-sach?query={txtSearchSach.Text}");
                lbSachResults.ItemsSource = results;
            }
            catch { /* Bỏ qua lỗi tìm kiếm */ }
        }
        private void LbSachResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbSachResults.SelectedItem is SachTimKiemDto selectedSach)
            {
                var currentList = (dgSachChon.ItemsSource as List<SachTimKiemDto>) ?? new List<SachTimKiemDto>();
                if (!currentList.Any(s => s.IdSach == selectedSach.IdSach))
                {
                    currentList.Add(selectedSach);
                    dgSachChon.ItemsSource = null;
                    dgSachChon.ItemsSource = currentList;
                    UpdateTongCoc();
                }
                txtSearchSach.Text = "";
                lbSachResults.ItemsSource = null;
            }
        }
        private void BtnXoaSachChon_Click(object sender, RoutedEventArgs e)
        {
            if (dgSachChon.SelectedItem is SachTimKiemDto selected && dgSachChon.ItemsSource is List<SachTimKiemDto> currentList)
            {
                currentList.Remove(selected);
                dgSachChon.ItemsSource = null;
                dgSachChon.ItemsSource = currentList;
                UpdateTongCoc();
            }
        }
        private void UpdateTongCoc()
        {
            var currentList = (dgSachChon.ItemsSource as List<SachTimKiemDto>) ?? new List<SachTimKiemDto>();
            decimal tongCoc = currentList.Sum(s => s.GiaBia);
            decimal phiThue = currentList.Count * _settings.PhiThue;
            lblTongCoc.Text = $"{tongCoc:N0} đ";
            lblPhiThue.Text = $"{phiThue:N0} đ";
        }


        /// <summary>
        /// SỬA LỖI (BUG 1) & CẢI TIẾN (MessageBox)
        /// </summary>
        private async void BtnTaoPhieuThue_Click(object sender, RoutedEventArgs e)
        {
            // CẢI TIẾN: Thêm hộp thoại xác nhận
            var confirm = MessageBox.Show("Xác nhận thuê sách này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No)
            {
                return;
            }

            var request = new PhieuThueRequestDto();

            // SỬA LỖI (BUG 1): Dùng AuthService.CurrentUser.IdNhanVien
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Không xác định được nhân viên. Vui lòng đăng nhập lại.", "Lỗi Phiên");
                return;
            }
            request.IdNhanVien = AuthService.CurrentUser.IdNhanVien;

            if (string.IsNullOrWhiteSpace(txtHoTenKH.Text))
            {
                MessageBox.Show("Tên khách hàng là bắt buộc.", "Lỗi");
                return;
            }

            request.KhachHangInfo = new KhachHangInfoDto
            {
                HoTen = txtHoTenKH.Text,
                SoDienThoai = string.IsNullOrWhiteSpace(txtSdtKH.Text) ? null : txtSdtKH.Text,
                Email = string.IsNullOrWhiteSpace(txtEmailKH.Text) ? null : txtEmailKH.Text
            };

            var sachList = (dgSachChon.ItemsSource as List<SachTimKiemDto>);
            if (sachList == null || !sachList.Any())
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 cuốn sách để thuê.", "Lỗi"); return;
            }
            request.SachCanThue = sachList.Select(s => new SachThueRequestDto { IdSach = s.IdSach, TienCoc = s.GiaBia }).ToList();
            request.NgayHenTra = dpNgayHenTra.SelectedDate ?? DateTime.Today.AddDays(_settings.SoNgayMuonToiDa);

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/nhanvien/thuesach", request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var root = jsonDoc.RootElement;
                    int idPhieu = root.GetProperty("idPhieuThueSach").GetInt32();

                    // Xóa MessageBox

                    ResetFormTaoPhieu();
                    await LoadPhieuThueAsync();

                    var printWindow = new PhieuThuePreviewWindow(idPhieu);
                    printWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}\n\nChi tiết: {ex.InnerException?.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetFormTaoPhieu()
        {
            _isUpdatingKhachText = true;
            txtHoTenKH.Text = "";
            txtSdtKH.Text = "";
            txtEmailKH.Text = "";
            _isUpdatingKhachText = false;

            lbKhachHangResults.ItemsSource = null;
            lbKhachHangResults.Visibility = Visibility.Collapsed;

            txtSearchSach.Text = "";
            lbSachResults.ItemsSource = null;
            dgSachChon.ItemsSource = null;
            dpNgayHenTra.SelectedDate = DateTime.Today.AddDays(_settings.SoNgayMuonToiDa);
            UpdateTongCoc();
        }

        #endregion

        #region CỘT 2: DANH SÁCH

        private async void TxtSearchPhieuThue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await LoadPhieuThueAsync();
        }

        private async void CmbTrangThaiFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await LoadPhieuThueAsync();
        }

        private void TxtSearchPhieuTra_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchPhieuTraTimer.Stop();
            _searchPhieuTraTimer.Start();
        }

        private async void DgPhieuThue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPhieuThue.SelectedItem is PhieuThueGridDto selectedPhieu)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                try
                {
                    _selectedPhieuChiTiet = await httpClient.GetFromJsonAsync<PhieuThueChiTietDto>($"api/app/nhanvien/thuesach/chitiet/{selectedPhieu.IdPhieuThueSach}");
                    if (_selectedPhieuChiTiet != null)
                    {
                        panelChiTietPhieu.Visibility = Visibility.Visible;
                        lblTenKH_ChiTiet.Text = _selectedPhieuChiTiet.HoTenKH;
                        lblSdtKH_ChiTiet.Text = _selectedPhieuChiTiet.SoDienThoaiKH;

                        var sachChuaTra = _selectedPhieuChiTiet.SachDaThue.Where(s => !s.TinhTrang.Contains("Đã Trả")).ToList();
                        dgSachTra.ItemsSource = sachChuaTra;

                        if (selectedPhieu.TrangThai == "Đã Trả")
                        {
                            panelTraSach.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            panelTraSach.Visibility = Visibility.Visible;
                            dgSachTra.SelectAll();
                            UpdateTraSachSummary();
                        }

                        bool sapTre = sachChuaTra.Any(s => (s.NgayHenTra.Date - DateTime.Today).TotalDays == 1);
                        btnGuiNhacHen.Visibility = (sapTre && selectedPhieu.TrangThai == "Đang Thuê") ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải chi tiết: {ex.Message}", "Lỗi API");
                    panelChiTietPhieu.Visibility = Visibility.Collapsed;
                }
                finally
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                panelChiTietPhieu.Visibility = Visibility.Collapsed;
                _selectedPhieuChiTiet = null;
            }
        }

        private void DgPhieuThue_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.DataContext is PhieuThueGridDto item)
            {
                if (item.TinhTrang == "Trễ Hạn")
                {
                    e.Row.Background = new SolidColorBrush(Color.FromArgb(50, 239, 83, 80));
                    e.Row.ToolTip = "Phiếu này đã trễ hạn trả sách.";
                }
                else
                {
                    e.Row.Background = Brushes.Transparent;
                    e.Row.ToolTip = null;
                }
            }
        }

        private async void BtnGuiNhacHangLoat_Click(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsync("api/app/nhanvien/thuesach/send-all-reminders", null);
                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    MessageBox.Show(jsonDoc.RootElement.GetProperty("message").GetString(), "Thành công");
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region CỘT 3: TRẢ SÁCH

        private void UpdateTraSachSummary()
        {
            var selectedSach = dgSachTra.SelectedItems.OfType<ChiTietSachThueDto>().ToList();
            decimal tongPhat = selectedSach.Sum(s => s.TienPhat);
            decimal tongCoc = selectedSach.Sum(s => s.TienCoc);
            decimal tongPhi = selectedSach.Count * _settings.PhiThue;

            lblTongPhat.Text = $"{tongPhat:N0} đ";
            lblTongPhiThue_Tra.Text = $"{tongPhi:N0} đ";
            lblTongCoc_Tra.Text = $"{tongCoc:N0} đ";
        }

        /// <summary>
        /// SỬA LỖI (BUG 1) & CẢI TIẾN (MessageBox, F3)
        /// </summary>
        private async void BtnXacNhanTra_Click(object sender, RoutedEventArgs e)
        {
            // CẢI TIẾN: Thêm hộp thoại xác nhận
            var confirm = MessageBox.Show("Xác nhận trả sách này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No)
            {
                return;
            }

            if (_selectedPhieuChiTiet == null || dgSachTra.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 cuốn sách để trả.", "Lỗi");
                return;
            }

            var selectedSach = dgSachTra.SelectedItems.OfType<ChiTietSachThueDto>().ToList();

            // SỬA LỖI (BUG 1): Dùng AuthService.CurrentUser.IdNhanVien
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Không xác định được nhân viên. Vui lòng đăng nhập lại.", "Lỗi Phiên");
                return;
            }

            var request = new TraSachRequestDto
            {
                IdPhieuThueSach = _selectedPhieuChiTiet.IdPhieuThueSach,
                IdSachs = selectedSach.Select(s => s.IdSach).ToList(),
                IdNhanVien = AuthService.CurrentUser.IdNhanVien
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/nhanvien/thuesach/return", request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TraSachResponseDto>();

                    // Xóa MessageBox

                    await LoadPhieuThueAsync();
                    await LoadPhieuTraAsync();
                    panelChiTietPhieu.Visibility = Visibility.Collapsed;

                    var printWindow = new PhieuTraPreviewWindow(result.IdPhieuTra);
                    printWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnGuiNhacHen_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieuChiTiet == null) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsync($"api/app/nhanvien/thuesach/send-reminder/{_selectedPhieuChiTiet.IdPhieuThueSach}", null);
                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    MessageBox.Show(jsonDoc.RootElement.GetProperty("message").GetString(), "Thành công");
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnInPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPhieuChiTiet == null) return;

            var printWindow = new PhieuThuePreviewWindow(_selectedPhieuChiTiet.IdPhieuThueSach);
            printWindow.ShowDialog();
        }

        #endregion
    }
}