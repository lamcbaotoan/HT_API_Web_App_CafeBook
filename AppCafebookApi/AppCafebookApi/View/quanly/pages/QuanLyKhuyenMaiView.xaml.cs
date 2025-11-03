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
// THÊM MỚI
using Microsoft.Win32;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyKhuyenMaiView : Page
    {
        private static readonly HttpClient httpClient;
        private List<KhuyenMaiDto> _allKhuyenMaiList = new List<KhuyenMaiDto>();
        private KhuyenMaiUpdateRequestDto? _selectedKhuyenMai = null;
        private List<FilterLookupDto> _sanPhamList = new List<FilterLookupDto>();

        static QuanLyKhuyenMaiView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyKhuyenMaiView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cmbFilterTrangThai.SelectedIndex = 0; // "Tất cả"
            await LoadKhuyenMaiFiltersAsync();
            await LoadKhuyenMaiGridAsync();
            ResetKhuyenMaiForm();
        }

        private async Task LoadKhuyenMaiFiltersAsync()
        {
            try
            {
                var filters = await httpClient.GetFromJsonAsync<KhuyenMaiFiltersDto>("api/app/khuyenmai/filters");
                if (filters != null)
                {
                    _sanPhamList = filters.SanPhams;
                    _sanPhamList.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả sản phẩm" });
                    cmbSanPhamApDung.ItemsSource = _sanPhamList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bộ lọc khuyến mãi: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadKhuyenMaiGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                string maKhuyenMai = txtSearchMaKM.Text;
                string trangThai = (cmbFilterTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tất cả";

                var url = $"api/app/khuyenmai/search?maKhuyenMai={Uri.EscapeDataString(maKhuyenMai)}&trangThai={Uri.EscapeDataString(trangThai)}";

                _allKhuyenMaiList = (await httpClient.GetFromJsonAsync<List<KhuyenMaiDto>>(url)) ?? new List<KhuyenMaiDto>();
                dgKhuyenMai.ItemsSource = _allKhuyenMaiList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách khuyến mãi: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // SỰ KIỆN MỚI: Xử lý khi thay đổi filter
        private async void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded) // Chỉ chạy khi Page đã load xong
            {
                await LoadKhuyenMaiGridAsync();
            }
        }

        // (Hàm Filter_Changed(object sender, SelectionChangedEventArgs e) đã được gộp chung)

        private void ResetKhuyenMaiForm()
        {
            _selectedKhuyenMai = null;
            dgKhuyenMai.SelectedItem = null;

            btnThem.Visibility = Visibility.Visible;
            btnLuu.Visibility = Visibility.Collapsed;
            btnXoa.Visibility = Visibility.Collapsed;
            btnTamDung.Visibility = Visibility.Collapsed;

            txtMaKhuyenMai.IsEnabled = true;
            txtMaKhuyenMai.Text = "";
            txtTenKM.Text = "";
            txtMoTa.Text = "";
            cmbLoaiGiamGia.SelectedIndex = 0; // PhanTram
            txtGiaTriGiam.Text = "10";
            txtGiamToiDa.Text = "0";
            txtDonHangToiThieu.Text = "0";
            cmbSanPhamApDung.SelectedValue = 0;
            dpNgayBatDau.SelectedDate = DateTime.Today;
            dpNgayKetThuc.SelectedDate = DateTime.Today.AddDays(7);
            txtDayOfWeek.Text = "";
            txtTimeStart.Text = "00:00";
            txtTimeEnd.Text = "23:59";
            txtDieuKienApDung.Text = "";
        }

        private async void DgKhuyenMai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgKhuyenMai.SelectedItem is not KhuyenMaiDto selected)
            {
                ResetKhuyenMaiForm();
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _selectedKhuyenMai = await httpClient.GetFromJsonAsync<KhuyenMaiUpdateRequestDto>($"api/app/khuyenmai/details/{selected.IdKhuyenMai}");
                if (_selectedKhuyenMai == null)
                {
                    ResetKhuyenMaiForm();
                    return;
                }

                btnThem.Visibility = Visibility.Collapsed;
                btnLuu.Visibility = Visibility.Visible;
                btnXoa.Visibility = Visibility.Visible;

                btnTamDung.Visibility = (_selectedKhuyenMai.TrangThai != "Hết hạn") ? Visibility.Visible : Visibility.Collapsed;
                btnTamDung.Content = (_selectedKhuyenMai.TrangThai == "Tạm dừng") ? "Kích Hoạt Lại" : "Tạm Dừng";

                txtMaKhuyenMai.Text = _selectedKhuyenMai.MaKhuyenMai;
                txtMaKhuyenMai.IsEnabled = false;

                txtTenKM.Text = _selectedKhuyenMai.TenChuongTrinh;
                txtMoTa.Text = _selectedKhuyenMai.MoTa;
                cmbLoaiGiamGia.Text = _selectedKhuyenMai.LoaiGiamGia;
                txtGiaTriGiam.Text = _selectedKhuyenMai.GiaTriGiam.ToString("F0");
                txtGiamToiDa.Text = _selectedKhuyenMai.GiamToiDa?.ToString("F0") ?? "0";
                txtDonHangToiThieu.Text = _selectedKhuyenMai.HoaDonToiThieu?.ToString("F0") ?? "0";
                cmbSanPhamApDung.SelectedValue = _selectedKhuyenMai.IdSanPhamApDung ?? 0;
                dpNgayBatDau.SelectedDate = _selectedKhuyenMai.NgayBatDau;
                dpNgayKetThuc.SelectedDate = _selectedKhuyenMai.NgayKetThuc;
                txtDayOfWeek.Text = _selectedKhuyenMai.NgayTrongTuan;
                txtTimeStart.Text = _selectedKhuyenMai.GioBatDau ?? "00:00";
                txtTimeEnd.Text = _selectedKhuyenMai.GioKetThuc ?? "23:59";
                txtDieuKienApDung.Text = _selectedKhuyenMai.DieuKienApDung;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết khuyến mãi: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetKhuyenMaiForm();
        }

        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            await SaveKhuyenMaiAsync(isCreating: true);
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            await SaveKhuyenMaiAsync(isCreating: false);
        }

        private async Task SaveKhuyenMaiAsync(bool isCreating)
        {
            if (string.IsNullOrWhiteSpace(txtMaKhuyenMai.Text))
            {
                MessageBox.Show("Mã khuyến mãi là bắt buộc.", "Lỗi"); return;
            }
            if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null)
            {
                MessageBox.Show("Ngày bắt đầu và kết thúc là bắt buộc.", "Lỗi"); return;
            }
            if (dpNgayKetThuc.SelectedDate < dpNgayBatDau.SelectedDate)
            {
                MessageBox.Show("Ngày kết thúc không thể trước ngày bắt đầu.", "Lỗi"); return;
            }

            var dto = new KhuyenMaiUpdateRequestDto
            {
                MaKhuyenMai = txtMaKhuyenMai.Text,
                TenChuongTrinh = txtTenKM.Text,
                MoTa = txtMoTa.Text,
                LoaiGiamGia = (cmbLoaiGiamGia.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "SoTien",
                GiaTriGiam = decimal.TryParse(txtGiaTriGiam.Text, out var g) ? g : 0,
                GiamToiDa = decimal.TryParse(txtGiamToiDa.Text, out var gt) ? (decimal?)gt : null,
                HoaDonToiThieu = decimal.TryParse(txtDonHangToiThieu.Text, out var hd) ? (decimal?)hd : null,
                IdSanPhamApDung = (int)cmbSanPhamApDung.SelectedValue > 0 ? (int)cmbSanPhamApDung.SelectedValue : null,
                NgayBatDau = dpNgayBatDau.SelectedDate.Value,
                NgayKetThuc = dpNgayKetThuc.SelectedDate.Value,
                NgayTrongTuan = txtDayOfWeek.Text,
                GioBatDau = txtTimeStart.Text,
                GioKetThuc = txtTimeEnd.Text,
                DieuKienApDung = txtDieuKienApDung.Text,
                TrangThai = _selectedKhuyenMai?.TrangThai ?? "Hoạt động"
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/khuyenmai", dto);
                }
                else
                {
                    dto.IdKhuyenMai = _selectedKhuyenMai.IdKhuyenMai;
                    response = await httpClient.PutAsJsonAsync($"api/app/khuyenmai/{dto.IdKhuyenMai}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu khuyến mãi thành công!", "Thông báo");
                    await LoadKhuyenMaiGridAsync();
                    ResetKhuyenMaiForm();
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
            if (_selectedKhuyenMai == null) return;
            var result = MessageBox.Show($"Bạn có chắc muốn xóa '{_selectedKhuyenMai.TenChuongTrinh}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/khuyenmai/{_selectedKhuyenMai.IdKhuyenMai}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadKhuyenMaiGridAsync();
                    ResetKhuyenMaiForm();
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

        private async void BtnTamDung_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhuyenMai == null) return;

            string newStatus = (_selectedKhuyenMai.TrangThai == "Hoạt động") ? "Tạm dừng" : "Hoạt động";
            _selectedKhuyenMai.TrangThai = newStatus;

            await SaveKhuyenMaiAsync(isCreating: false);
        }

        // --- THÊM MỚI: NÚT QUAY LẠI ---
        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        // --- THÊM MỚI: NÚT XUẤT EXCEL ---
        private void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_allKhuyenMaiList == null || !_allKhuyenMaiList.Any())
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DSKhuyenMai_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
                    using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                    {
                        var ws = package.Workbook.Worksheets.Add("DanhSachKhuyenMai");
                        ws.Cells["A1"].Value = "Danh sách Khuyến mãi";
                        ws.Cells["A1:D1"].Merge = true;
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Size = 16;

                        ws.Cells["A3"].LoadFromCollection(_allKhuyenMaiList, true, TableStyles.Medium9);

                        // Định dạng cột ngày
                        var table = ws.Tables[0];
                        int ngayBatDauCol = table.Columns["NgayBatDau"].Position + table.Address.Start.Column;
                        int ngayKetThucCol = table.Columns["NgayKetThuc"].Position + table.Address.Start.Column;

                        ws.Column(ngayBatDauCol).Style.Numberformat.Format = "dd/MM/yyyy";
                        ws.Column(ngayKetThucCol).Style.Numberformat.Format = "dd/MM/yyyy";

                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        package.Save();
                    }
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo file không đang được mở.", "Lỗi");
                }
            }
        }
    }
}