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
using System.Collections.ObjectModel;
using System.Globalization;
using AppCafebookApi.Services;

namespace AppCafebookApi.View.quanly.pages
{
    // (DTOs placeholder giữ nguyên - chúng nên ở trong /Model/ModelApp/KhoDto.cs)
    #region DTOs (Tạm thời định nghĩa ở đây)
    public class PhieuKiemKhoDto
    {
        public int IdPhieuKiemKho { get; set; }
        public DateTime NgayKiem { get; set; }
        public string TenNhanVienKiem { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
    }

    public class ChiTietKiemKhoDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal TonKhoHeThong { get; set; }
        public decimal TonKhoThucTe { get; set; }
        public decimal ChenhLech => TonKhoThucTe - TonKhoHeThong;
        public string? LyDoChenhLech { get; set; }
        public decimal GiaTriChenhLech { get; set; }
    }

    public class PhieuKiemKhoCreateDto
    {
        public int IdNhanVien { get; set; }
        public DateTime NgayKiem { get; set; }
        public string? GhiChu { get; set; }
        public List<ChiTietKiemKhoDto> ChiTiet { get; set; } = new();
    }
    #endregion

    public partial class QuanLyKiemKhoView : Page
    {
        private static readonly HttpClient httpClient;

        private List<PhieuKiemKhoDto> _phieuKiemKhoList = new List<PhieuKiemKhoDto>();
        private ObservableCollection<ChiTietKiemKhoDto> _fullKiemKhoList = new ObservableCollection<ChiTietKiemKhoDto>();
        private ObservableCollection<ChiTietKiemKhoDto> _chiTietKiemKhoList = new ObservableCollection<ChiTietKiemKhoDto>();

        private PhieuKiemKhoDto? _selectedPhieu;
        private bool _isCreatingPhieu = false;

        static QuanLyKiemKhoView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyKiemKhoView()
        {
            InitializeComponent();
            dpTuNgay_Phieu.SelectedDate = DateTime.Today.AddDays(-30);
            dpDenNgay_Phieu.SelectedDate = DateTime.Today;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPhieuKiemKhoAsync();
            ResetForm();
        }

        private async Task LoadPhieuKiemKhoAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            DateTime? start = dpTuNgay_Phieu.SelectedDate;
            DateTime? end = dpDenNgay_Phieu.SelectedDate;

            try
            {
                string url = "api/app/kho/phieukiemkho";
                if (start.HasValue && end.HasValue)
                {
                    url += $"?startDate={start.Value:yyyy-MM-dd}&endDate={end.Value:yyyy-MM-dd}";
                }

                _phieuKiemKhoList = (await httpClient.GetFromJsonAsync<List<PhieuKiemKhoDto>>(url)) ?? new List<PhieuKiemKhoDto>();
                dgPhieuKiemKho.ItemsSource = _phieuKiemKhoList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải phiếu kiểm kho: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetForm()
        {
            _selectedPhieu = null;
            _isCreatingPhieu = false;
            dgPhieuKiemKho.SelectedItem = null;

            lblChiTietTitle.Text = "Chi tiết Phiếu Kiểm Kho";
            lblTongChenhLech.Text = "0 VND";
            txtSearchKiemKho.Text = "";

            _chiTietKiemKhoList.Clear();
            _fullKiemKhoList.Clear();
            dgChiTietKiemKho.ItemsSource = null;
            dgChiTietKiemKho.IsReadOnly = true;
            btnLuuPhieuKiem.IsEnabled = false;

            panelNhapChiTiet.IsEnabled = false; // Tắt form nhập
            ResetChiTietForm();
        }

        // --- THÊM MỚI: Hàm reset form nhập liệu ---
        private void ResetChiTietForm()
        {
            lblTenNL_ChiTiet.Text = "Chọn một nguyên liệu từ lưới...";
            lblTonKhoHT_ChiTiet.Text = "0";
            txtTonKhoThucTe.Text = "0";
            txtLyDo.Text = "";
            panelNhapChiTiet.IsEnabled = false;
        }

        private async void BtnTaoPhieuMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
            _isCreatingPhieu = true;
            lblChiTietTitle.Text = "Tạo Phiếu Kiểm Kho Mới";
            btnLuuPhieuKiem.IsEnabled = true;
            dgChiTietKiemKho.IsReadOnly = true; // Lưới luôn ReadOnly

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var currentStock = (await httpClient.GetFromJsonAsync<List<ChiTietKiemKhoDto>>("api/app/kho/phieukiemkho/taomoi")) ?? new List<ChiTietKiemKhoDto>();

                foreach (var item in currentStock)
                {
                    item.TonKhoThucTe = item.TonKhoHeThong;
                }

                _fullKiemKhoList = new ObservableCollection<ChiTietKiemKhoDto>(currentStock);
                dgChiTietKiemKho.ItemsSource = _fullKiemKhoList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu kho: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnLocPhieu_Click(object sender, RoutedEventArgs e)
        {
            await LoadPhieuKiemKhoAsync();
        }

        private async void DgPhieuKiemKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPhieuKiemKho.SelectedItem is not PhieuKiemKhoDto selected)
            {
                ResetForm();
                return;
            }

            _selectedPhieu = selected;
            _isCreatingPhieu = false;
            btnLuuPhieuKiem.IsEnabled = false;
            dgChiTietKiemKho.IsReadOnly = true;
            txtSearchKiemKho.Text = "";
            ResetChiTietForm(); // Tắt form nhập

            try
            {
                var details = await httpClient.GetFromJsonAsync<List<ChiTietKiemKhoDto>>($"api/app/kho/phieukiemkho/{selected.IdPhieuKiemKho}");

                _fullKiemKhoList = new ObservableCollection<ChiTietKiemKhoDto>(details ?? new List<ChiTietKiemKhoDto>());
                dgChiTietKiemKho.ItemsSource = _fullKiemKhoList;

                lblChiTietTitle.Text = $"Chi tiết Phiếu {selected.IdPhieuKiemKho}";
                lblTongChenhLech.Text = _fullKiemKhoList.Sum(ct => ct.GiaTriChenhLech).ToString("N0") + " VND";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết phiếu: {ex.Message}", "Lỗi API");
            }
        }

        private void TxtSearchKiemKho_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearchKiemKho.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                dgChiTietKiemKho.ItemsSource = _fullKiemKhoList;
            }
            else
            {
                var filteredList = _fullKiemKhoList
                    .Where(nl => nl.TenNguyenLieu.ToLower().Contains(searchText))
                    .ToList();

                _chiTietKiemKhoList = new ObservableCollection<ChiTietKiemKhoDto>(filteredList);
                dgChiTietKiemKho.ItemsSource = _chiTietKiemKhoList;
            }
        }

        // =================================================================
        // === SỬA LỖI HÀM NÀY (System.InvalidOperationException) ===
        // =================================================================
        private void DgChiTietKiemKho_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Tự động tính Chênh lệch

            // XÓA: Task.Delay(100).ContinueWith(...

            // THAY BẰNG:
            // Xếp hàng tác vụ Refresh() vào Dispatcher với mức ưu tiên thấp (ContextIdle).
            // Điều này đảm bảo giao dịch (edit transaction) kết thúc TRƯỚC KHI Refresh() được gọi.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                dgChiTietKiemKho.Items.Refresh();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
        // =================================================================
        // === KẾT THÚC SỬA LỖI ===
        // =================================================================
        // --- XÓA HÀM DgChiTietKiemKho_CellEditEnding ---
        // private void DgChiTietKiemKho_CellEditEnding(...) { ... }

        // --- THÊM MỚI: Xử lý chọn dòng chi tiết ---
        private void DgChiTietKiemKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgChiTietKiemKho.SelectedItem is ChiTietKiemKhoDto selected && _isCreatingPhieu)
            {
                panelNhapChiTiet.IsEnabled = true;
                lblTenNL_ChiTiet.Text = selected.TenNguyenLieu;
                lblTonKhoHT_ChiTiet.Text = selected.TonKhoHeThong.ToString("N2");
                txtTonKhoThucTe.Text = selected.TonKhoThucTe.ToString("N2");
                txtLyDo.Text = selected.LyDoChenhLech;
                txtTonKhoThucTe.Focus();
            }
            else
            {
                ResetChiTietForm();
            }
        }

        // --- THÊM MỚI: Xử lý nút "Cập nhật dòng" ---
        private void BtnCapNhatDong_Click(object sender, RoutedEventArgs e)
        {
            if (dgChiTietKiemKho.SelectedItem is not ChiTietKiemKhoDto selectedItem)
            {
                MessageBox.Show("Vui lòng chọn một dòng trong lưới để cập nhật.", "Lỗi");
                return;
            }

            // Tìm item tương ứng trong danh sách GỐC
            var itemInFullList = _fullKiemKhoList.FirstOrDefault(i => i.IdNguyenLieu == selectedItem.IdNguyenLieu);
            if (itemInFullList == null) return;

            // Lấy dữ liệu từ form
            if (!decimal.TryParse(txtTonKhoThucTe.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var thucTe))
            {
                MessageBox.Show("Số lượng thực tế không hợp lệ.", "Lỗi");
                return;
            }

            // Cập nhật DỮ LIỆU GỐC (trong _fullKiemKhoList)
            itemInFullList.TonKhoThucTe = thucTe;
            itemInFullList.LyDoChenhLech = txtLyDo.Text;

            // Refresh lại DataGrid (an toàn, vì không ở trong CellEditEnding)
            dgChiTietKiemKho.Items.Refresh();

            // Reset form nhập
            ResetChiTietForm();
            dgChiTietKiemKho.SelectedItem = null;
        }

        private async void BtnLuuPhieuKiem_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCreatingPhieu || _fullKiemKhoList.Count == 0)
            {
                MessageBox.Show("Vui lòng 'Tạo Phiếu Mới' trước khi lưu.", "Lỗi");
                return;
            }
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi phiên đăng nhập. Vui lòng đăng nhập lại.", "Lỗi");
                return;
            }

            var dto = new PhieuKiemKhoCreateDto
            {
                IdNhanVien = AuthService.CurrentUser.IdNhanVien,
                NgayKiem = DateTime.Now,
                ChiTiet = _fullKiemKhoList.ToList() // Luôn lưu list gốc
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/kho/phieukiemkho", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu phiếu kiểm kho và cân bằng kho thành công!", "Thông báo");
                    await LoadPhieuKiemKhoAsync();
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

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}