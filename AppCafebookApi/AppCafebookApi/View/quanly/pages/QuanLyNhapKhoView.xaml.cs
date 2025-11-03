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
    public partial class QuanLyNhapKhoView : Page
    {
        private static readonly HttpClient httpClient;

        private List<PhieuNhapDto> _phieuNhapList = new List<PhieuNhapDto>();
        // SỬA ĐỔI: Dùng ChiTietPhieuNhapCreateDto vì nó có IdNguyenLieu
        private ObservableCollection<ChiTietPhieuNhapCreateDto> _chiTietPhieuNhapList = new ObservableCollection<ChiTietPhieuNhapCreateDto>();
        private PhieuNhapDto? _selectedPhieuNhap = null;
        private bool _isCreatingPhieuNhap = false;

        private List<NhaCungCapDto> _nhaCungCapList = new List<NhaCungCapDto>();
        private List<NguyenLieuCrudDto> _nguyenLieuList = new List<NguyenLieuCrudDto>();

        static QuanLyNhapKhoView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyNhapKhoView()
        {
            InitializeComponent();
            dpTuNgay_Phieu.SelectedDate = DateTime.Today.AddDays(-30);
            dpDenNgay_Phieu.SelectedDate = DateTime.Today;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            await LoadNguyenLieuAsync();
            await LoadNhaCungCapAsync();
            await LoadPhieuNhapAsync();
            ResetPhieuNhapForm();
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        #region Tải Dữ Liệu

        private async Task LoadNguyenLieuAsync()
        {
            try
            {
                _nguyenLieuList = (await httpClient.GetFromJsonAsync<List<NguyenLieuCrudDto>>("api/app/kho/nguyenlieu")) ?? new List<NguyenLieuCrudDto>();

                var nlList = _nguyenLieuList.Select(nl => new { nl.IdNguyenLieu, TenNguyenLieu = $"{nl.TenNguyenLieu} ({nl.DonViTinh})" }).ToList();
                nlList.Insert(0, new { IdNguyenLieu = 0, TenNguyenLieu = "-- Chọn Nguyên Liệu --" });

                // Gán cho ComboBox trong Form nhập
                cmbNguyenLieu_PhieuNhap.ItemsSource = nlList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nguyên liệu: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadNhaCungCapAsync()
        {
            try
            {
                _nhaCungCapList = (await httpClient.GetFromJsonAsync<List<NhaCungCapDto>>("api/app/kho/nhacungcap")) ?? new List<NhaCungCapDto>();

                var nccList = _nhaCungCapList.Select(ncc => new { ncc.IdNhaCungCap, ncc.TenNhaCungCap }).ToList();
                nccList.Insert(0, new { IdNhaCungCap = 0, TenNhaCungCap = "Nhập lẻ (Không NCC)" });
                cmbNhaCungCap_PhieuNhap.ItemsSource = nccList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nhà cung cấp: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadPhieuNhapAsync()
        {
            DateTime? start = dpTuNgay_Phieu.SelectedDate;
            DateTime? end = dpDenNgay_Phieu.SelectedDate;

            try
            {
                string url = "api/app/kho/phieunhap";
                if (start.HasValue && end.HasValue)
                {
                    url += $"?startDate={start.Value:yyyy-MM-dd}&endDate={end.Value:yyyy-MM-dd}";
                }

                _phieuNhapList = (await httpClient.GetFromJsonAsync<List<PhieuNhapDto>>(url)) ?? new List<PhieuNhapDto>();
                dgPhieuNhap.ItemsSource = _phieuNhapList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải phiếu nhập: {ex.Message}", "Lỗi API");
            }
        }

        #endregion

        #region TAB 3: NHẬP KHO

        private void ResetPhieuNhapForm()
        {
            _selectedPhieuNhap = null;
            _isCreatingPhieuNhap = false;
            dgPhieuNhap.SelectedItem = null;

            dpNgayNhap.SelectedDate = DateTime.Today;
            cmbNhaCungCap_PhieuNhap.SelectedValue = 0;
            lblTongTien.Text = "0 VND";

            _chiTietPhieuNhapList.Clear();
            dgChiTietPhieuNhap.ItemsSource = null; // Xóa ItemsSource

            btnLuuPhieuNhap.IsEnabled = false;
            panelNhapChiTiet.IsEnabled = false; // Tắt panel nhập
        }

        private void BtnTaoPhieuMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetPhieuNhapForm();
            _isCreatingPhieuNhap = true;
            btnLuuPhieuNhap.IsEnabled = true;
            panelNhapChiTiet.IsEnabled = true; // Bật panel nhập

            // Gán ItemsSource là list rỗng
            dgChiTietPhieuNhap.ItemsSource = _chiTietPhieuNhapList;
        }

        private async void BtnLocPhieu_Click(object sender, RoutedEventArgs e)
        {
            await LoadPhieuNhapAsync();
        }

        private async void DgPhieuNhap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPhieuNhap.SelectedItem is not PhieuNhapDto selected)
            {
                ResetPhieuNhapForm();
                return;
            }

            _selectedPhieuNhap = selected;
            _isCreatingPhieuNhap = false;
            btnLuuPhieuNhap.IsEnabled = false;
            panelNhapChiTiet.IsEnabled = false; // Tắt panel nhập

            try
            {
                var details = await httpClient.GetFromJsonAsync<List<ChiTietPhieuNhapDto>>($"api/app/kho/phieunhap/{selected.IdPhieuNhapKho}");

                // Chuyển đổi DTO chi tiết sang DTO tạo (để DataGrid hiển thị)
                _chiTietPhieuNhapList = new ObservableCollection<ChiTietPhieuNhapCreateDto>(
                    details?.Select(d => new ChiTietPhieuNhapCreateDto
                    {
                        IdNguyenLieu = d.IdNguyenLieu,
                        SoLuongNhap = d.SoLuongNhap,
                        DonGiaNhap = d.DonGiaNhap
                    }) ?? new List<ChiTietPhieuNhapCreateDto>()
                );

                dgChiTietPhieuNhap.ItemsSource = _chiTietPhieuNhapList;
                CalculateTotal(); // Tính tổng

                dpNgayNhap.SelectedDate = selected.NgayNhap;
                var ncc = _nhaCungCapList.FirstOrDefault(n => n.TenNhaCungCap == selected.TenNhaCungCap);
                cmbNhaCungCap_PhieuNhap.SelectedValue = ncc?.IdNhaCungCap ?? 0;

                lblTongTien.Text = selected.TongTien.ToString("N0") + " VND";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết phiếu: {ex.Message}", "Lỗi API");
            }
        }

        // --- SỬA LỖI: Thay thế BtnThemDong và CellEditEnding ---

        private void BtnThemVaoPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCreatingPhieuNhap) return;

            if (cmbNguyenLieu_PhieuNhap.SelectedValue == null || (int)cmbNguyenLieu_PhieuNhap.SelectedValue == 0)
            {
                MessageBox.Show("Vui lòng chọn một nguyên liệu.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtSoLuongNL.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soLuong) || soLuong <= 0)
            {
                MessageBox.Show("Số lượng phải là số dương.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtDonGiaNL.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal donGia) || donGia < 0)
            {
                MessageBox.Show("Đơn giá không hợp lệ.", "Lỗi"); return;
            }

            int idNguyenLieu = (int)cmbNguyenLieu_PhieuNhap.SelectedValue;

            // Kiểm tra xem đã tồn tại chưa
            var existingItem = _chiTietPhieuNhapList.FirstOrDefault(i => i.IdNguyenLieu == idNguyenLieu);
            if (existingItem != null)
            {
                // Cập nhật
                existingItem.SoLuongNhap += soLuong;
                existingItem.DonGiaNhap = donGia; // Cập nhật đơn giá mới
            }
            else
            {
                // Thêm mới
                _chiTietPhieuNhapList.Add(new ChiTietPhieuNhapCreateDto
                {
                    IdNguyenLieu = idNguyenLieu,
                    SoLuongNhap = soLuong,
                    DonGiaNhap = donGia
                });
            }

            RefreshDataGrid();
            ResetInputFields();
        }

        private void DgChiTietPhieuNhap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Cho phép xóa khi đang tạo phiếu
            if (_isCreatingPhieuNhap && dgChiTietPhieuNhap.SelectedItem is ChiTietPhieuNhapCreateDto selectedItem)
            {
                if (MessageBox.Show($"Bạn có muốn xóa '{selectedItem.IdNguyenLieu}' khỏi phiếu nhập?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _chiTietPhieuNhapList.Remove(selectedItem);
                    RefreshDataGrid();
                }
                dgChiTietPhieuNhap.SelectedItem = null;
            }
        }

        private void DgChiTietPhieuNhap_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Dùng Dispatcher.BeginInvoke để Refresh SAU KHI edit kết thúc
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Tính toán lại tổng tiền từ danh sách nguồn
                decimal tongTien = 0;
                foreach (var item in _chiTietPhieuNhapList)
                {
                    tongTien += (item.SoLuongNhap * item.DonGiaNhap);
                }
                lblTongTien.Text = tongTien.ToString("N0") + " VND";

                // Nạp lại DataGrid (cách an toàn nhất)
                RefreshDataGrid();

            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void ResetInputFields()
        {
            cmbNguyenLieu_PhieuNhap.SelectedValue = 0;
            txtSoLuongNL.Text = "";
            txtDonGiaNL.Text = "";
        }

        // Hàm này phải tồn tại trong QuanLyNhapKhoView.xaml.cs
        private void RefreshDataGrid()
        {
            dgChiTietPhieuNhap.ItemsSource = null;

            // Gán lại tên (vì DTO create không có tên)
            var displayList = _chiTietPhieuNhapList.Select(ct => new
            {
                ct.IdNguyenLieu,
                // Thêm kiểm tra null cho _nguyenLieuList
                TenNguyenLieu = _nguyenLieuList?.FirstOrDefault(nl => nl.IdNguyenLieu == ct.IdNguyenLieu)?.TenNguyenLieu ?? "...",
                ct.SoLuongNhap,
                ct.DonGiaNhap,
                ThanhTien = ct.SoLuongNhap * ct.DonGiaNhap
            }).ToList();

            dgChiTietPhieuNhap.ItemsSource = displayList;
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            decimal tongTien = _chiTietPhieuNhapList.Sum(item => item.SoLuongNhap * item.DonGiaNhap);
            lblTongTien.Text = tongTien.ToString("N0") + " VND";
        }

        // --- HẾT PHẦN SỬA LỖI ---


        private async void BtnLuuPhieuNhap_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCreatingPhieuNhap || _chiTietPhieuNhapList.Count == 0)
            {
                MessageBox.Show("Vui lòng 'Tạo Phiếu Mới' và 'Thêm Dòng' nguyên liệu trước khi lưu.", "Lỗi");
                return;
            }
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi phiên đăng nhập. Vui lòng đăng nhập lại.", "Lỗi");
                return;
            }

            var dto = new PhieuNhapCreateDto
            {
                IdNhanVien = AuthService.CurrentUser.IdNhanVien,
                IdNhaCungCap = (int)cmbNhaCungCap_PhieuNhap.SelectedValue > 0 ? (int)cmbNhaCungCap_PhieuNhap.SelectedValue : null,
                NgayNhap = dpNgayNhap.SelectedDate ?? DateTime.Today,
                GhiChu = null,
                ChiTiet = _chiTietPhieuNhapList.Where(ct => ct.IdNguyenLieu > 0 && ct.SoLuongNhap > 0)
                                               .Select(ct => new ChiTietPhieuNhapCreateDto
                                               {
                                                   IdNguyenLieu = ct.IdNguyenLieu,
                                                   SoLuongNhap = ct.SoLuongNhap,
                                                   DonGiaNhap = ct.DonGiaNhap
                                               }).ToList()
            };

            if (!dto.ChiTiet.Any())
            {
                MessageBox.Show("Phiếu nhập phải có ít nhất 1 nguyên liệu hợp lệ.", "Lỗi");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/kho/phieunhap", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu phiếu nhập kho thành công!", "Thông báo");
                    await LoadPhieuNhapAsync(); // Tải lại lưới phiếu nhập
                    ResetPhieuNhapForm();
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

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}