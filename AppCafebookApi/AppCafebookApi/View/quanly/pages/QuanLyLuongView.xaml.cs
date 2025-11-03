using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Threading.Tasks;
using System.Net;
using AppCafebookApi.Services; // Cần cho AuthService
using System.Globalization;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyLuongView : Page
    {
        private static readonly HttpClient httpClient;
        private List<NhanVienLookupDto> _allNhanVienList = new List<NhanVienLookupDto>();
        private List<PhieuThuongPhatDto> _currentThuongPhatList = new List<PhieuThuongPhatDto>();
        private List<LuongBangKeDto> _currentBangKeList = new List<LuongBangKeDto>();
        private ChamCongDto? _selectedChamCong = null;

        static QuanLyLuongView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyLuongView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadNhanVienAsync();

            // Tab 1
            dpNgayChamCong.SelectedDate = DateTime.Today;
            await LoadChamCongAsync(DateTime.Today);

            // Tab 3
            var now = DateTime.Now;
            dpNgayBatDauLuong.SelectedDate = new DateTime(now.Year, now.Month, 1);
            dpNgayKetThucLuong.SelectedDate = dpNgayBatDauLuong.SelectedDate.Value.AddMonths(1).AddDays(-1);

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private async Task LoadNhanVienAsync()
        {
            try
            {
                // Tận dụng API của Module 4
                _allNhanVienList = (await httpClient.GetFromJsonAsync<List<NhanVienLookupDto>>("api/app/lichlamviec/all-nhanvien")) ?? new List<NhanVienLookupDto>();
                cmbNhanVienThuongPhat.ItemsSource = _allNhanVienList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nhân viên: {ex.Message}", "Lỗi API");
            }
        }

        #region === TAB 1: BẢNG CHẤM CÔNG ===

        private async Task LoadChamCongAsync(DateTime date)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            formChamCong.IsEnabled = false;
            _selectedChamCong = null;
            try
            {
                var data = (await httpClient.GetFromJsonAsync<List<ChamCongDto>>($"api/app/luong/chamcong?date={date:yyyy-MM-dd}")) ?? new List<ChamCongDto>();
                dgChamCong.ItemsSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bảng chấm công: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void DpNgayChamCong_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpNgayChamCong.SelectedDate.HasValue)
            {
                await LoadChamCongAsync(dpNgayChamCong.SelectedDate.Value);
            }
        }

        private void DgChamCong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgChamCong.SelectedItem is ChamCongDto selected)
            {
                _selectedChamCong = selected;
                formChamCong.IsEnabled = true;
                lblChamCongNhanVien.Text = selected.HoTenNhanVien;
                // Hiển thị HH:mm
                txtGioVaoMoi.Text = selected.GioVao?.ToString("HH:mm") ?? "";
                txtGioRaMoi.Text = selected.GioRa?.ToString("HH:mm") ?? "";
            }
            else
            {
                _selectedChamCong = null;
                formChamCong.IsEnabled = false;
                lblChamCongNhanVien.Text = "[Chọn 1 mục]";
                txtGioVaoMoi.Text = "";
                txtGioRaMoi.Text = "";
            }
        }

        private async void BtnLuuChamCong_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedChamCong == null) return;

            // Lấy ngày đã chọn
            DateTime ngayLam = _selectedChamCong.NgayLam;
            DateTime? gioVaoMoi = null;
            DateTime? gioRaMoi = null;

            try
            {
                // Parse Giờ Vào
                if (!string.IsNullOrWhiteSpace(txtGioVaoMoi.Text))
                {
                    var tsVao = TimeSpan.ParseExact(txtGioVaoMoi.Text, @"hh\:mm", CultureInfo.InvariantCulture);
                    gioVaoMoi = ngayLam.Add(tsVao);
                }
                // Parse Giờ Ra
                if (!string.IsNullOrWhiteSpace(txtGioRaMoi.Text))
                {
                    var tsRa = TimeSpan.ParseExact(txtGioRaMoi.Text, @"hh\:mm", CultureInfo.InvariantCulture);
                    gioRaMoi = ngayLam.Add(tsRa);
                    // Kiểm tra nếu qua ngày
                    if (gioRaMoi < gioVaoMoi) gioRaMoi = gioRaMoi.Value.AddDays(1);
                }
            }
            catch
            {
                MessageBox.Show("Định dạng giờ không hợp lệ. Vui lòng dùng HH:mm (ví dụ: 08:00 hoặc 17:30).", "Lỗi định dạng");
                return;
            }

            var dto = new ChamCongUpdateDto
            {
                IdChamCong = _selectedChamCong.IdChamCong,
                GioVaoMoi = gioVaoMoi,
                GioRaMoi = gioRaMoi
            };

            // Lưu ý: Logic này đang giả định IdChamCong > 0. Cần nâng cấp API để xử lý IdChamCong = 0
            if (dto.IdChamCong == 0)
            {
                MessageBox.Show("Lỗi: Không thể cập nhật thủ công cho nhân viên chưa chấm công lần nào. (Tính năng này cần được nâng cấp API)", "Lỗi");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/app/luong/chamcong", dto);
                if (response.IsSuccessStatusCode)
                {
                    await LoadChamCongAsync(dpNgayChamCong.SelectedDate.Value);
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

        #region === TAB 2: THƯỞNG/PHẠT ===

        private async Task LoadThuongPhatAsync(int idNhanVien)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            formThuongPhat.IsEnabled = true;
            try
            {
                _currentThuongPhatList = (await httpClient.GetFromJsonAsync<List<PhieuThuongPhatDto>>($"api/app/thuongphat/pending/{idNhanVien}")) ?? new List<PhieuThuongPhatDto>();
                dgThuongPhat.ItemsSource = _currentThuongPhatList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách thưởng/phạt: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void CmbNhanVienThuongPhat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbNhanVienThuongPhat.SelectedValue is int idNhanVien)
            {
                await LoadThuongPhatAsync(idNhanVien);
            }
            else
            {
                dgThuongPhat.ItemsSource = null;
                formThuongPhat.IsEnabled = false;
            }
        }

        private async void BtnThemThuongPhat_Click(object sender, RoutedEventArgs e)
        {
            if (cmbNhanVienThuongPhat.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn nhân viên.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtSoTien.Text, out var soTien) || soTien == 0)
            {
                MessageBox.Show("Số tiền không hợp lệ. Dùng số âm (-) cho khoản phạt.", "Lỗi"); return;
            }
            if (string.IsNullOrWhiteSpace(txtLyDoThuongPhat.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do.", "Lỗi"); return;
            }

            var dto = new PhieuThuongPhatCreateDto
            {
                IdNhanVien = (int)cmbNhanVienThuongPhat.SelectedValue,
                IdNguoiTao = AuthService.CurrentUser?.IdNhanVien ?? 0,
                SoTien = soTien,
                LyDo = txtLyDoThuongPhat.Text
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/thuongphat", dto);
                if (response.IsSuccessStatusCode)
                {
                    txtSoTien.Text = "";
                    txtLyDoThuongPhat.Text = "";
                    await LoadThuongPhatAsync(dto.IdNhanVien);
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

        private async void BtnXoaThuongPhat_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not PhieuThuongPhatDto item) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa khoản [ {item.SoTien:N0} - {item.LyDo} ] ?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/thuongphat/{item.IdPhieuThuongPhat}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadThuongPhatAsync(item.IdNhanVien);
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Không thể xóa");
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

        #region === TAB 3: TÍNH & CHỐT LƯƠNG ===

        private async void BtnTinhLuong_Click(object sender, RoutedEventArgs e)
        {
            if (dpNgayBatDauLuong.SelectedDate == null || dpNgayKetThucLuong.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn Ngày Bắt Đầu và Kết Thúc.", "Lỗi"); return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            btnChotLuong.IsEnabled = false;
            try
            {
                var sDate = dpNgayBatDauLuong.SelectedDate.Value;
                var eDate = dpNgayKetThucLuong.SelectedDate.Value;

                var url = $"api/app/luong/calculate?startDate={sDate:yyyy-MM-dd}&endDate={eDate:yyyy-MM-dd}";
                _currentBangKeList = (await httpClient.GetFromJsonAsync<List<LuongBangKeDto>>(url)) ?? new List<LuongBangKeDto>();

                dgBangKeLuong.ItemsSource = _currentBangKeList;
                if (_currentBangKeList.Any())
                {
                    btnChotLuong.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tính lương: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnChotLuong_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBangKeList == null || !_currentBangKeList.Any())
            {
                MessageBox.Show("Không có dữ liệu lương để chốt. Vui lòng nhấn 'Tính Lương' trước.", "Lỗi"); return;
            }
            if (dpNgayBatDauLuong.SelectedDate == null)
            {
                MessageBox.Show("Ngày bắt đầu không hợp lệ.", "Lỗi"); return;
            }

            int thang = dpNgayBatDauLuong.SelectedDate.Value.Month;
            int nam = dpNgayBatDauLuong.SelectedDate.Value.Year;

            var result = MessageBox.Show($"Bạn có chắc muốn CHỐT LƯƠNG cho Tháng {thang}/{nam}?\n\nSau khi chốt sẽ không thể hoàn tác.", "Xác nhận Chốt Lương", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            var dto = new LuongFinalizeDto
            {
                Thang = thang,
                Nam = nam,
                IdNguoiChot = AuthService.CurrentUser?.IdNhanVien ?? 0,
                DanhSachBangKe = _currentBangKeList
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/luong/finalize", dto);
                var message = await response.Content.ReadAsStringAsync(); // Đọc message dù thành công hay thất bại

                if (response.IsSuccessStatusCode)
                {
                    // Đọc message thành công từ JSON
                    var successObj = System.Text.Json.JsonSerializer.Deserialize<dynamic>(message);
                    MessageBox.Show(successObj.GetProperty("message").GetString(), "Chốt Lương Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reset
                    _currentBangKeList = new List<LuongBangKeDto>();
                    dgBangKeLuong.ItemsSource = null;
                    btnChotLuong.IsEnabled = false;

                    // Tải lại Tab 2 (vì các phiếu đã được gán)
                    if (cmbNhanVienThuongPhat.SelectedValue is int idNhanVien)
                    {
                        await LoadThuongPhatAsync(idNhanVien);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi: {message}", "Lỗi Chốt Lương");
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

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            // Quay lại trang QL Nhân Viên
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new QuanLyNhanVienView());
            }
        }
        private void BtnGoToBaoCao_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}