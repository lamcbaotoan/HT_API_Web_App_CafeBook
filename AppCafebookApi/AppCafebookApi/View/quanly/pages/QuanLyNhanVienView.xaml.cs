// Tập tin: AppCafebookApi/View/quanly/pages/QuanLyNhanVienView.xaml.cs
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
using AppCafebookApi.View.common;
using System.Net.Http.Headers; // <-- THÊM

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyNhanVienView : Page
    {
        private static readonly HttpClient httpClient;
        private List<NhanVienGridDto> _allNhanVienList = new List<NhanVienGridDto>();
        private List<FilterLookupDto> _vaiTroList = new List<FilterLookupDto>();

        // SỬA: Dùng DTO chi tiết (Detail) để nhận URL
        private NhanVienDetailDto? _selectedNhanVien = null;

        // SỬA: Thay thế Base64 bằng đường dẫn file
        private string? _currentAvatarFilePath = null;
        private bool _deleteAvatarRequest = false;

        static QuanLyNhanVienView()
        {
            httpClient = new HttpClient
            {
                // SỬA: Đổi port về 5166 (theo launchSettings.json của bạn)
                BaseAddress = new Uri("http://127.0.0.1:5166")
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
                    var filterVaiTro = new List<FilterLookupDto>(_vaiTroList);
                    filterVaiTro.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Vai trò" });
                    cmbFilterVaiTro.ItemsSource = filterVaiTro;
                    cmbFilterVaiTro.SelectedValue = 0;
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

        // SỬA: Thêm sự kiện cho XAML
        private void Filter_Changed(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
                _ = LoadDataGridAsync();
        }

        #endregion

        #region Form & CRUD

        private void ResetForm()
        {
            _selectedNhanVien = null;
            _currentAvatarFilePath = null;
            _deleteAvatarRequest = false;

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
            cmbVaiTro.SelectedIndex = -1;
            txtLuongCoBan.Text = "0";
            cmbTrangThai.SelectedIndex = 0;
            dpNgayVaoLam.SelectedDate = DateTime.Today;
            txtSoDienThoai.Text = "";
            txtEmail.Text = "";
            txtDiaChi.Text = "";

            AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultAvatar);
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
                // SỬA: Gọi NhanVienDetailDto
                _selectedNhanVien = await httpClient.GetFromJsonAsync<NhanVienDetailDto>($"api/app/nhanvien/details/{selected.IdNhanVien}");
                if (_selectedNhanVien == null)
                {
                    ResetForm();
                    return;
                }

                _currentAvatarFilePath = null;
                _deleteAvatarRequest = false;

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

                // SỬA: Load ảnh từ URL
                AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(_selectedNhanVien.AnhDaiDienUrl, HinhAnhPaths.DefaultAvatar);
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
                    // SỬA: Chỉ lưu đường dẫn file
                    _currentAvatarFilePath = ofd.FileName;
                    _deleteAvatarRequest = false;
                    AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(_currentAvatarFilePath, HinhAnhPaths.DefaultAvatar);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file ảnh: {ex.Message}", "Lỗi File");
                    _currentAvatarFilePath = null;
                    AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultAvatar);
                }
            }
        }

        // SỬA: Thêm hàm này (từ XAML của bạn)
        private void BtnXoaAnh_Click(object sender, RoutedEventArgs e)
        {
            _currentAvatarFilePath = null;
            _deleteAvatarRequest = true;
            AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultAvatar);
        }

        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            await SaveNhanVienAsync(isCreating: true);
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            await SaveNhanVienAsync(isCreating: false);
        }

        // SỬA: Dùng MultipartFormDataContent
        private async Task SaveNhanVienAsync(bool isCreating)
        {
            // ... (validation) ...
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

            // SỬA: Dùng MultipartFormDataContent
            using var form = new MultipartFormDataContent();

            // 1. Thêm các trường dữ liệu
            form.Add(new StringContent(txtHoTen.Text), "HoTen");
            form.Add(new StringContent(txtTenDangNhap.Text), "TenDangNhap");
            if (!string.IsNullOrWhiteSpace(txtMatKhau.Password))
                form.Add(new StringContent(txtMatKhau.Password), "MatKhau");

            // === SỬA LỖI CS8604 (Dòng 281) ===
            // Dùng string interpolation ($"") để đảm bảo kết quả luôn là `string` (non-null)
            // Logic (dòng 269) đã đảm bảo SelectedValue không null
            form.Add(new StringContent($"{cmbVaiTro.SelectedValue!}"), "IdVaiTro");

            form.Add(new StringContent(decimal.TryParse(txtLuongCoBan.Text, out var l) ? l.ToString() : "0"), "LuongCoBan");
            form.Add(new StringContent(((cmbTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString()) ?? "Đang làm việc"), "TrangThaiLamViec");
            form.Add(new StringContent((dpNgayVaoLam.SelectedDate ?? DateTime.Today).ToString("o")), "NgayVaoLam"); // Gửi ISO 8601
            form.Add(new StringContent(txtSoDienThoai.Text ?? ""), "SoDienThoai");
            form.Add(new StringContent(txtEmail.Text ?? ""), "Email");
            form.Add(new StringContent(txtDiaChi.Text ?? ""), "DiaChi");

            if (!isCreating)
            {
                // (Fix từ lần trước)
                form.Add(new StringContent(_selectedNhanVien!.IdNhanVien.ToString()), "IdNhanVien");
            }

            // 2. Thêm file
            if (!string.IsNullOrEmpty(_currentAvatarFilePath))
            {
                var fileStream = File.OpenRead(_currentAvatarFilePath);
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                form.Add(streamContent, "AnhDaiDienUpload", Path.GetFileName(_currentAvatarFilePath));
            }

            // 3. Thêm cờ Xóa
            if (_deleteAvatarRequest)
            {
                form.Add(new StringContent("true"), "XoaAnhDaiDien");
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsync("api/app/nhanvien", form);
                }
                else
                {
                    // (Fix từ lần trước)
                    response = await httpClient.PutAsync($"api/app/nhanvien/{_selectedNhanVien!.IdNhanVien}", form);
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
                _currentAvatarFilePath = null;
                _deleteAvatarRequest = false;
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
                        // SỬA: Gửi JSON chuẩn
                        var statusResponse = await httpClient.PutAsJsonAsync($"api/app/nhanvien/update-status/{_selectedNhanVien.IdNhanVien}", new { newStatus = "Nghỉ việc" });
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
        // ... (Toàn bộ region Navigation giữ nguyên) ...
        private void BtnGoToRoles_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyVaiTroView());
        }
        private void BtnGoToBaoCao_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BaoCaoNhanSuView());
        }
        private void BtnGoToLichLamViec_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyLichLamViecView());
        }
        private void BtnGoToDonXinNghi_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyDonXinNghiView());
        }/*
        private void BtnGoToLuong_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyLuongView());
        }*/
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