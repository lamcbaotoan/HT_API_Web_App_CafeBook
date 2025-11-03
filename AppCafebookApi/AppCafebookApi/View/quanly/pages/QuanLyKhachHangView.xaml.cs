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
using AppCafebookApi.View.common; // Thêm

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyKhachHangView : Page
    {
        private static readonly HttpClient httpClient;

        // Tab 1: Khách hàng
        private List<KhachHangDto> _allKhachHangList = new List<KhachHangDto>();
        private KhachHangDetailDto? _selectedKhachHang = null;
        private string? _currentAvatarBase64 = null;

        // (Xóa logic Tab 2)

        static QuanLyKhachHangView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
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
            _currentAvatarBase64 = null;
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
            AvatarPreview.ImageSource = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultAvatar);
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
                _currentAvatarBase64 = _selectedKhachHang.AnhDaiDienBase64;
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
                AvatarPreview.ImageSource = HinhAnhHelper.LoadImageFromBase64(_currentAvatarBase64, HinhAnhPaths.DefaultAvatar);
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

        private bool ValidateKhachHangInput(out KhachHangUpdateRequestDto dto)
        {
            dto = new KhachHangUpdateRequestDto();
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
            dto.HoTen = txtHoTenKH.Text;
            dto.SoDienThoai = txtSdtKH.Text;
            dto.Email = string.IsNullOrEmpty(txtEmailKH.Text) ? null : txtEmailKH.Text;
            dto.TenDangNhap = string.IsNullOrEmpty(txtTenDangNhapKH.Text) ? null : txtTenDangNhapKH.Text;
            dto.DiaChi = string.IsNullOrEmpty(txtDiaChiKH.Text) ? null : txtDiaChiKH.Text;
            dto.DiemTichLuy = int.TryParse(txtDiemTichLuy.Text, out int diem) ? diem : 0;
            dto.AnhDaiDienBase64 = _currentAvatarBase64;
            return true;
        }

        private async void BtnThemKH_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateKhachHangInput(out var dto)) return;
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/khachhang", dto);
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
            if (_selectedKhachHang == null || !ValidateKhachHangInput(out var dto)) return;
            dto.IdKhachHang = _selectedKhachHang.IdKhachHang;
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/app/khachhang/{dto.IdKhachHang}", dto);
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
                var response = await httpClient.PutAsJsonAsync($"api/app/khachhang/update-status/{id}", biKhoa);
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
            var sfd = new SaveFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx", FileName = $"DSKhachHang_{DateTime.Now:yyyyMMdd_HHmm}.xlsx" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
                    using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                    {
                        var ws = package.Workbook.Worksheets.Add("DanhSachKhachHang");
                        ws.Cells["A1"].Value = "Danh sách Khách hàng";
                        ws.Cells["A1:D1"].Merge = true;
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Size = 16;
                        ws.Cells["A3"].LoadFromCollection(_allKhachHangList, true, OfficeOpenXml.Table.TableStyles.Medium9);
                        ws.Column(4).Style.Numberformat.Format = "dd/MM/yyyy";
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        package.Save();
                    }
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                }
            }
        }

        #endregion

        #region KHUYẾN MÃI (Điều hướng)

        private void BtnGoToKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng đến Page mới
            this.NavigationService?.Navigate(new QuanLyKhuyenMaiView());
        }

        // THÊM MỚI
        private void BtnCaiDatDiem_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng đến Page Cài đặt
            this.NavigationService?.Navigate(new CaiDatWindow());
        }

        #endregion
    }
}