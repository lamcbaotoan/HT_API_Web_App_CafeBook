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
using Microsoft.Win32; // Cho Excel
using System.IO; // Cho Excel
using OfficeOpenXml; // Cho Excel
using System.Globalization; // Cho Excel

namespace AppCafebookApi.View.quanly.pages
{
    // Đổi tên class cho khớp (nếu file XAML là QuanLyDonHangView.xaml)
    public partial class QuanLyDonHangView : Page
    {
        private static readonly HttpClient httpClient;
        private List<DonHangDto> _currentOrderList = new List<DonHangDto>();

        static QuanLyDonHangView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyDonHangView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Thiết lập ngày mặc định (hôm nay)
            dpTuNgay.SelectedDate = DateTime.Today;
            dpDenNgay.SelectedDate = DateTime.Today;
            cmbTrangThai.SelectedIndex = 0; // "Tất cả"

            await LoadFiltersAsync();
            await LoadDataGridAsync();
        }

        /// <summary>
        /// Tải dữ liệu cho ComboBox Nhân viên
        /// </summary>
        private async Task LoadFiltersAsync()
        {
            try
            {
                var filters = await httpClient.GetFromJsonAsync<DonHangFiltersDto>("api/app/donhang/filters");
                if (filters != null)
                {
                    filters.NhanViens.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Nhân viên" });
                    cmbNhanVien.ItemsSource = filters.NhanViens;
                    cmbNhanVien.SelectedValue = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        /// <summary>
        /// Tải DataGrid chính dựa trên bộ lọc
        /// </summary>
        private async Task LoadDataGridAsync()
        {
            if (dpTuNgay.SelectedDate == null || dpDenNgay.SelectedDate == null) return;

            DateTime startDate = dpTuNgay.SelectedDate.Value;
            DateTime endDate = dpDenNgay.SelectedDate.Value;
            string trangThai = (cmbTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tất cả";
            int nhanVienId = (int)(cmbNhanVien.SelectedValue ?? 0);
            string searchText = txtSearch.Text;

            try
            {
                var url = $"api/app/donhang/search?" +
                          $"startDate={startDate:yyyy-MM-dd}" +
                          $"&endDate={endDate:yyyy-MM-dd}" +
                          $"&trangThai={Uri.EscapeDataString(trangThai)}" +
                          $"&nhanVienId={nhanVienId}" +
                          $"&searchText={Uri.EscapeDataString(searchText)}";

                _currentOrderList = (await httpClient.GetFromJsonAsync<List<DonHangDto>>(url)) ?? new List<DonHangDto>();
                dgDonHang.ItemsSource = _currentOrderList;

                // Xóa chi tiết
                dgChiTietDonHang.ItemsSource = null;
                btnInLaiHoaDon.IsEnabled = false;
                btnHuyDonHang.IsEnabled = false;
                btnGiaoHang.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách đơn hàng: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnLoc_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataGridAsync();
        }

        private async void BtnXoaLoc_Click(object sender, RoutedEventArgs e)
        {
            dpTuNgay.SelectedDate = DateTime.Today;
            dpDenNgay.SelectedDate = DateTime.Today;
            cmbTrangThai.SelectedIndex = 0;
            cmbNhanVien.SelectedValue = 0;
            txtSearch.Text = "";
            await LoadDataGridAsync();
        }

        /// <summary>
        /// Khi chọn một đơn hàng, tải chi tiết
        /// </summary>
        private async void DgDonHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDonHang.SelectedItem is not DonHangDto selectedOrder)
            {
                dgChiTietDonHang.ItemsSource = null;
                btnInLaiHoaDon.IsEnabled = false;
                btnHuyDonHang.IsEnabled = false;
                btnGiaoHang.IsEnabled = false;
                return;
            }

            try
            {
                var details = await httpClient.GetFromJsonAsync<List<DonHangChiTietDto>>($"api/app/donhang/details/{selectedOrder.IdHoaDon}");
                dgChiTietDonHang.ItemsSource = details;

                // Logic kích hoạt nút
                btnInLaiHoaDon.IsEnabled = true; // Luôn cho phép in

                if (selectedOrder.TrangThai == "Đã thanh toán" || selectedOrder.TrangThai == "Đã hủy")
                {
                    btnHuyDonHang.IsEnabled = false;
                    btnGiaoHang.IsEnabled = false;
                }
                else
                {
                    // "Chưa thanh toán"
                    btnHuyDonHang.IsEnabled = true;
                    if (selectedOrder.LoaiHoaDon == "Giao hàng")
                    {
                        btnGiaoHang.IsEnabled = true;
                    }
                    else
                    {
                        btnGiaoHang.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết đơn hàng: {ex.Message}", "Lỗi API");
            }
        }

        private void BtnInLaiHoaDon_Click(object sender, RoutedEventArgs e)
        {
            if (dgDonHang.SelectedItem is DonHangDto selectedOrder)
            {
                MessageBox.Show($"Đang chuẩn bị in HĐ {selectedOrder.IdHoaDon}...\n(Chức năng này cần logic in ấn riêng).", "Thông báo");
                // TODO: Gọi service in ấn
            }
        }

        private async void BtnHuyDonHang_Click(object sender, RoutedEventArgs e)
        {
            if (dgDonHang.SelectedItem is DonHangDto selectedOrder)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn HỦY Hóa đơn {selectedOrder.IdHoaDon} không?", "Xác nhận Hủy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) return;

                await UpdateOrderStatus(selectedOrder.IdHoaDon, "Hủy");
            }
        }

        private async void BtnGiaoHang_Click(object sender, RoutedEventArgs e)
        {
            if (dgDonHang.SelectedItem is DonHangDto selectedOrder)
            {
                var result = MessageBox.Show($"Xác nhận bắt đầu GIAO HÀNG cho HĐ {selectedOrder.IdHoaDon}?", "Xác nhận Giao hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;

                await UpdateOrderStatus(selectedOrder.IdHoaDon, "Đang giao");
            }
        }

        private async Task UpdateOrderStatus(int hoaDonId, string newStatus)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/app/donhang/update-status/{hoaDonId}", newStatus);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Cập nhật trạng thái thành '{newStatus}' thành công.", "Thành công");
                    await LoadDataGridAsync(); // Tải lại danh sách
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
        }

        private void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DSDonHang_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
                    using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                    {
                        var ws = package.Workbook.Worksheets.Add("DanhSachDonHang");
                        ws.Cells["A1"].Value = "Danh sách Đơn hàng";
                        ws.Cells["A1:D1"].Merge = true;
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Size = 16;

                        ws.Cells["A3"].LoadFromCollection(_currentOrderList, true, OfficeOpenXml.Table.TableStyles.Medium9);

                        // Định dạng cột
                        ws.Column(2).Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
                        ws.Column(6).Style.Numberformat.Format = "#,##0 \"đ\"";
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
    }
}