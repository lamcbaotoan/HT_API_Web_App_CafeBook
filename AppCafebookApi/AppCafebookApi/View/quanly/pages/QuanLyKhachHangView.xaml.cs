// Tập tin: AppCafebookApi/View/quanly/pages/QuanLyKhachHangView.xaml.cs
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
using OfficeOpenXml;
using System.Text.RegularExpressions;
using AppCafebookApi.View.common;
using System.Net.Http.Headers; // <-- THÊM

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyKhachHangView : Page
    {
        private static readonly HttpClient httpClient;

        private List<KhachHangDto> _allKhachHangList = new List<KhachHangDto>();

        // SỬA: Dùng DTO chi tiết (Detail)
        private KhachHangDetailDto? _selectedKhachHang = null;

        // SỬA: Thay thế Base64
        private string? _currentAvatarFilePath = null;
        private bool _deleteAvatarRequest = false;

        static QuanLyKhachHangView()
        {
            httpClient = new HttpClient
            {
                // SỬA: Dùng 127.0.0.1
                BaseAddress = new Uri("http://127.0.0.1:5166")
            };
        }

        public QuanLyKhachHangView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cmbFilterTrangThai.SelectedIndex = 0; // "Tất cả"
            await LoadKhachHangGridAsync();
            ResetKhachHangForm();
        }

        #region TAB 1: KHÁCH HÀNG

        // --- Tải dữ liệu & Lọc ---
        private async Task LoadKhachHangGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            string searchText = txtSearchKhachHang.Text;
            bool? biKhoa = (cmbFilterTrangThai.SelectedItem as ComboBoxItem)?.Tag as bool?;
            try
            {
                var url = $"api/app/khachhang/search?searchText={Uri.EscapeDataString(searchText)}";
                if (biKhoa.HasValue)
                {
                    url += $"&biKhoa={biKhoa.Value}";
                }
                _allKhachHangList = (await httpClient.GetFromJsonAsync<List<KhachHangDto>>(url)) ?? new List<KhachHangDto>();
                dgKhachHang.ItemsSource = _allKhachHangList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách khách hàng: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void TxtSearchKhachHang_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.IsLoaded)
                await LoadKhachHangGridAsync();
        }

        private async void CmbFilterTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterTrangThai.IsLoaded)
            {
                await LoadKhachHangGridAsync();
            }
        }

        // --- Form & CRUD ---

        private void ResetKhachHangForm()
        {
            _selectedKhachHang = null;
            _currentAvatarFilePath = null;
            _deleteAvatarRequest = false;

            dgKhachHang.SelectedItem = null;
            lblFormTitle.Text = "Thêm Khách hàng Mới";
            btnThemKH.Visibility = Visibility.Visible;
            btnLuuKH.Visibility = Visibility.Collapsed;
            btnXoaKH.Visibility = Visibility.Collapsed;
            btnKhoaKH.Visibility = Visibility.Collapsed;
            btnMoKhoaKH.Visibility = Visibility.Collapsed;
            txtHoTenKH.Text = "";
            txtSdtKH.Text = "";
            txtEmailKH.Text = "";
            txtTenDangNhapKH.Text = "";
            txtDiaChiKH.Text = "";
            txtDiemTichLuy.Text = "0";
            dgLichSuDonHang.ItemsSource = null;
            dgLichSuThueSach.ItemsSource = null;
            AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultAvatar);
        }

        private async void DgKhachHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgKhachHang.SelectedItem is not KhachHangDto selected)
            {
                ResetKhachHangForm();
                return;
            }
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _selectedKhachHang = await httpClient.GetFromJsonAsync<KhachHangDetailDto>($"api/app/khachhang/details/{selected.IdKhachHang}");
                if (_selectedKhachHang == null)
                {
                    ResetKhachHangForm();
                    return;
                }

                _currentAvatarFilePath = null;
                _deleteAvatarRequest = false;

                lblFormTitle.Text = "Cập nhật Khách hàng";
                btnThemKH.Visibility = Visibility.Collapsed;
                btnLuuKH.Visibility = Visibility.Visible;
                btnXoaKH.Visibility = Visibility.Visible;
                btnKhoaKH.Visibility = _selectedKhachHang.BiKhoa ? Visibility.Collapsed : Visibility.Visible;
                btnMoKhoaKH.Visibility = _selectedKhachHang.BiKhoa ? Visibility.Visible : Visibility.Collapsed;
                txtHoTenKH.Text = _selectedKhachHang.HoTen;
                txtSdtKH.Text = _selectedKhachHang.SoDienThoai;
                txtEmailKH.Text = _selectedKhachHang.Email;
                txtTenDangNhapKH.Text = _selectedKhachHang.TenDangNhap;
                txtDiaChiKH.Text = _selectedKhachHang.DiaChi;
                txtDiemTichLuy.Text = _selectedKhachHang.DiemTichLuy.ToString();
                dgLichSuDonHang.ItemsSource = _selectedKhachHang.LichSuDonHang;
                dgLichSuThueSach.ItemsSource = _selectedKhachHang.LichSuThueSach;

                // SỬA: Load ảnh từ URL
                AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(_selectedKhachHang.AnhDaiDienUrl, HinhAnhPaths.DefaultAvatar);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết khách hàng: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLamMoiKH_Click(object sender, RoutedEventArgs e)
        {
            ResetKhachHangForm();
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

        // THÊM: Nút Xóa Ảnh
        private void BtnXoaAnh_Click(object sender, RoutedEventArgs e)
        {
            _currentAvatarFilePath = null;
            _deleteAvatarRequest = true;
            AvatarPreview.ImageSource = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultAvatar);
        }

        // SỬA: Tách hàm Validate riêng
        private bool ValidateKhachHangInput(out MultipartFormDataContent form)
        {
            form = new MultipartFormDataContent();
            if (string.IsNullOrWhiteSpace(txtHoTenKH.Text) || string.IsNullOrWhiteSpace(txtSdtKH.Text))
            {
                MessageBox.Show("Họ tên và Số điện thoại là bắt buộc.", "Thiếu thông tin");
                return false;
            }
            if (!string.IsNullOrEmpty(txtEmailKH.Text) && !Regex.IsMatch(txtEmailKH.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Định dạng Email không hợp lệ.", "Lỗi");
                return false;
            }

            // 1. Thêm các trường dữ liệu
            form.Add(new StringContent(txtHoTenKH.Text), "HoTen");
            form.Add(new StringContent(txtSdtKH.Text), "SoDienThoai");
            form.Add(new StringContent(txtEmailKH.Text ?? ""), "Email");
            form.Add(new StringContent(txtTenDangNhapKH.Text ?? ""), "TenDangNhap");
            form.Add(new StringContent(txtDiaChiKH.Text ?? ""), "DiaChi");
            form.Add(new StringContent(int.TryParse(txtDiemTichLuy.Text, out int diem) ? diem.ToString() : "0"), "DiemTichLuy");

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

            return true;
        }

        private async void BtnThemKH_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateKhachHangInput(out var form)) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsync("api/app/khachhang", form);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thêm khách hàng thành công!", "Thông báo");
                    await LoadKhachHangGridAsync();
                    ResetKhachHangForm();
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

        private async void BtnLuuKH_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhachHang == null || !ValidateKhachHangInput(out var form)) return;

            // Thêm Id vào form
            form.Add(new StringContent(_selectedKhachHang.IdKhachHang.ToString()), "IdKhachHang");

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PutAsync($"api/app/khachhang/{_selectedKhachHang.IdKhachHang}", form);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật thành công!", "Thông báo");
                    await LoadKhachHangGridAsync();
                    ResetKhachHangForm();
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

        private async void BtnKhoaKH_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhachHang == null) return;
            await UpdateKhachHangStatus(_selectedKhachHang.IdKhachHang, true);
        }

        private async void BtnMoKhoaKH_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhachHang == null) return;
            await UpdateKhachHangStatus(_selectedKhachHang.IdKhachHang, false);
        }

        private async Task UpdateKhachHangStatus(int id, bool biKhoa)
        {
            string action = biKhoa ? "KHÓA" : "MỞ KHÓA";
            if (MessageBox.Show($"Bạn có chắc muốn {action} tài khoản này?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                // SỬA: Gửi JSON object
                var response = await httpClient.PutAsJsonAsync($"api/app/khachhang/update-status/{id}", new { BiKhoa = biKhoa });
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"{action} thành công.", "Thông báo");
                    await LoadKhachHangGridAsync();
                    ResetKhachHangForm();
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

        private async void BtnXoaKH_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhachHang == null) return;
            var result = MessageBox.Show($"Bạn có chắc chắn muốn XÓA vĩnh viễn khách hàng '{_selectedKhachHang.HoTen}'?\n(Hành động này không thể hoàn tác)", "Xác nhận Xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/khachhang/{_selectedKhachHang.IdKhachHang}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadKhachHangGridAsync();
                    ResetKhachHangForm();
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

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            // (Logic Export giữ nguyên)
        }

        #endregion

        #region KHUYẾN MÃI (Điều hướng)
        // (Logic Navigation giữ nguyên)
        private void BtnGoToKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyKhuyenMaiView());
        }
        private void BtnCaiDatDiem_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new CaiDatWindow());
        }

        #endregion
    }
}