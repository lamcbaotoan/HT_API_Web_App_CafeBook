using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using AppCafebookApi.Services;
using CafebookModel.Utils;
using System.Globalization;
using AppCafebookApi.View.Common;
// THÊM MỚI:
using AppCafebookApi.View.common;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyNhanVienView : Page
    {
        private static readonly HttpClient httpClient;
        private List<NhanVienGridDto> _allNhanVienList = new List<NhanVienGridDto>();
        private List<FilterLookupDto> _vaiTroList = new List<FilterLookupDto>();
        private NhanVienUpdateRequestDto? _selectedNhanVien = null;
        private string? _currentAvatarBase64 = null;

        static QuanLyNhanVienView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyNhanVienView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFiltersAsync();
            await LoadDataGridAsync();
            ResetForm();
        }

        #region Tải Dữ Liệu & Lọc

        private async Task LoadFiltersAsync()
        {
            try
            {
                var filters = await httpClient.GetFromJsonAsync<NhanSuFiltersDto>("api/app/nhanvien/filters");
                if (filters != null)
                {
                    _vaiTroList = filters.VaiTros;

                    // Lọc
                    var filterVaiTro = new List<FilterLookupDto>(_vaiTroList);
                    filterVaiTro.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Vai trò" });
                    cmbFilterVaiTro.ItemsSource = filterVaiTro;
                    cmbFilterVaiTro.SelectedValue = 0;

                    // Form
                    cmbVaiTro.ItemsSource = _vaiTroList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            string searchText = txtSearchNhanVien.Text;
            int vaiTroId = (int)(cmbFilterVaiTro.SelectedValue ?? 0);

            try
            {
                var url = $"api/app/nhanvien/search?searchText={Uri.EscapeDataString(searchText)}&vaiTroId={vaiTroId}";
                _allNhanVienList = (await httpClient.GetFromJsonAsync<List<NhanVienGridDto>>(url)) ?? new List<NhanVienGridDto>();
                dgNhanVien.ItemsSource = _allNhanVienList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách nhân viên: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                await LoadDataGridAsync();
        }

        #endregion

        #region Form & CRUD

        private void ResetForm()
        {
            _selectedNhanVien = null;
            _currentAvatarBase64 = null;
            dgNhanVien.SelectedItem = null;
            formChiTiet.IsEnabled = true;
            panelActions.Visibility = Visibility.Visible;

            btnThem.Visibility = Visibility.Visible;
            btnLuu.Visibility = Visibility.Collapsed;
            btnXoa.Visibility = Visibility.Collapsed;

            lblFormTitle.Text = "Thêm Nhân Viên Mới";
            lblMatKhau.Visibility = Visibility.Visible;
            txtMatKhau.Visibility = Visibility.Visible;
            lblMatKhauInfo.Visibility = Visibility.Collapsed;

            txtHoTen.Text = "";
            txtTenDangNhap.Text = "";
            txtMatKhau.Password = "";
            cmbVaiTro.SelectedIndex = -1; // Bỏ chọn
            txtLuongCoBan.Text = "0";
            cmbTrangThai.SelectedIndex = 0; // "Đang làm việc"
            dpNgayVaoLam.SelectedDate = DateTime.Today;
            txtSoDienThoai.Text = "";
            txtEmail.Text = "";
            txtDiaChi.Text = "";

            AvatarPreview.ImageSource = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultAvatar);
        }

        private async void DgNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgNhanVien.SelectedItem is not NhanVienGridDto selected)
            {
                ResetForm();
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _selectedNhanVien = await httpClient.GetFromJsonAsync<NhanVienUpdateRequestDto>($"api/app/nhanvien/details/{selected.IdNhanVien}");
                if (_selectedNhanVien == null)
                {
                    ResetForm();
                    return;
                }

                _currentAvatarBase64 = _selectedNhanVien.AnhDaiDienBase64;
                formChiTiet.IsEnabled = true;
                panelActions.Visibility = Visibility.Visible;
                btnThem.Visibility = Visibility.Collapsed;
                btnLuu.Visibility = Visibility.Visible;
                btnXoa.Visibility = Visibility.Visible;

                lblFormTitle.Text = "Cập nhật Nhân Viên";
                lblMatKhau.Visibility = Visibility.Collapsed;
                txtMatKhau.Visibility = Visibility.Collapsed;
                lblMatKhauInfo.Visibility = Visibility.Visible;
                txtMatKhau.Password = "";

                txtHoTen.Text = _selectedNhanVien.HoTen;
                txtTenDangNhap.Text = _selectedNhanVien.TenDangNhap;
                cmbVaiTro.SelectedValue = _selectedNhanVien.IdVaiTro;
                txtLuongCoBan.Text = _selectedNhanVien.LuongCoBan.ToString("F0");
                cmbTrangThai.Text = _selectedNhanVien.TrangThaiLamViec;
                dpNgayVaoLam.SelectedDate = _selectedNhanVien.NgayVaoLam;
                txtSoDienThoai.Text = _selectedNhanVien.SoDienThoai;
                txtEmail.Text = _selectedNhanVien.Email;
                txtDiaChi.Text = _selectedNhanVien.DiaChi;

                AvatarPreview.ImageSource = HinhAnhHelper.LoadImageFromBase64(_currentAvatarBase64, HinhAnhPaths.DefaultAvatar);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLamMoiForm_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg", Title = "Chọn Avatar" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(ofd.FileName);
                    _currentAvatarBase64 = Convert.ToBase64String(imageBytes);
                    AvatarPreview.ImageSource = HinhAnhHelper.LoadImageFromBase64(_currentAvatarBase64, HinhAnhPaths.DefaultAvatar);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file ảnh: {ex.Message}", "Lỗi File");
                    _currentAvatarBase64 = null;
                    AvatarPreview.ImageSource = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultAvatar);
                }
            }
        }

        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            await SaveNhanVienAsync(isCreating: true);
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            await SaveNhanVienAsync(isCreating: false);
        }

        private async Task SaveNhanVienAsync(bool isCreating)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) || string.IsNullOrWhiteSpace(txtTenDangNhap.Text))
            {
                MessageBox.Show("Họ tên và Tên đăng nhập là bắt buộc.", "Lỗi"); return;
            }
            if (isCreating && string.IsNullOrWhiteSpace(txtMatKhau.Password))
            {
                MessageBox.Show("Mật khẩu là bắt buộc khi tạo mới.", "Lỗi"); return;
            }
            if (cmbVaiTro.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Vai trò.", "Lỗi"); return;
            }

            var dto = new NhanVienUpdateRequestDto
            {
                HoTen = txtHoTen.Text,
                TenDangNhap = txtTenDangNhap.Text,
                MatKhau = string.IsNullOrWhiteSpace(txtMatKhau.Password) ? null : txtMatKhau.Password, // Chỉ gửi nếu có
                IdVaiTro = (int)cmbVaiTro.SelectedValue,
                LuongCoBan = decimal.TryParse(txtLuongCoBan.Text, out var l) ? l : 0,
                TrangThaiLamViec = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đang làm việc",
                NgayVaoLam = dpNgayVaoLam.SelectedDate ?? DateTime.Today,
                SoDienThoai = txtSoDienThoai.Text,
                Email = txtEmail.Text,
                DiaChi = txtDiaChi.Text,
                AnhDaiDienBase64 = _currentAvatarBase64
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/nhanvien", dto);
                }
                else
                {
                    dto.IdNhanVien = _selectedNhanVien.IdNhanVien;
                    response = await httpClient.PutAsJsonAsync($"api/app/nhanvien/{dto.IdNhanVien}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
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

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedNhanVien == null) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa nhân viên '{_selectedNhanVien.HoTen}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/nhanvien/{_selectedNhanVien.IdNhanVien}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    var confirmDeactivate = MessageBox.Show($"{error}\n\nBạn có muốn chuyển trạng thái nhân viên này thành 'Nghỉ việc' không?", "Không thể xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (confirmDeactivate == MessageBoxResult.Yes)
                    {
                        // Gọi API Update Status
                        var statusResponse = await httpClient.PutAsJsonAsync($"api/app/nhanvien/update-status/{_selectedNhanVien.IdNhanVien}", "Nghỉ việc");
                        if (statusResponse.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Đã cập nhật trạng thái thành 'Nghỉ việc'.", "Thành công");
                            await LoadDataGridAsync();
                            ResetForm();
                        }
                        else
                        {
                            MessageBox.Show("Lỗi khi cập nhật trạng thái.", "Lỗi API");
                        }
                    }
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

        #region Navigation

        private void BtnGoToRoles_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyVaiTroView());
        }

        private void BtnGoToBaoCao_Click(object sender, RoutedEventArgs e)
        {
            // Yêu cầu #8
            this.NavigationService?.Navigate(new BaoCaoNhanSuView());
        }

        private void BtnGoToLichLamViec_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyLichLamViecView());
        }

        private void BtnGoToDonXinNghi_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyDonXinNghiView());
        }
        private void BtnGoToLuong_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyLuongView());
        }

        private void BtnGoToCaiDat_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new CaiDatNhanSuView());
        }
        private void BtnGoToHieuSuat_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BaoCaoHieuSuatNhanVientPreviewWindow());
        }
        #endregion
    }
}