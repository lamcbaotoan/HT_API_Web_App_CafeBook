using AppCafebookApi.Services;
using AppCafebookApi.View.common;
using AppCafebookApi.View.Common;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows.Threading; // Quan trọng cho Timer

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class GiaoHangView : Page
    {
        private bool _isLoading = false;
        private DispatcherTimer _searchTimer;

        public GiaoHangView()
        {
            InitializeComponent();

            // Cấu hình Timer cho Live Search
            _searchTimer = new DispatcherTimer();
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500); // Chờ 0.5s
            _searchTimer.Tick += async (s, e) =>
            {
                _searchTimer.Stop();
                await LoadDataAsync();
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                string searchQuery = txtSearch.Text;
                string statusQuery = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chờ xác nhận";

                // Xử lý placeholder của TextBox (nếu có)
                if (searchQuery.Contains("Tìm theo")) searchQuery = "";

                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    queryParams.Add($"search={Uri.EscapeDataString(searchQuery)}");
                }

                // Nếu chọn "Tất cả", gửi tham số status=Tất cả lên API
                // API backend đã được cập nhật để xử lý "Tất cả" và "Đã Hủy"
                if (statusQuery != "Tất cả")
                {
                    queryParams.Add($"status={Uri.EscapeDataString(statusQuery)}");
                }
                else
                {
                    queryParams.Add("status=Tất cả");
                }

                string queryString = string.Join("&", queryParams);

                // Gọi API đã được nâng cấp (LoadGiaoHangData)
                var response = await ApiClient.Instance.GetFromJsonAsync<GiaoHangViewDto>($"api/app/nhanvien/giaohang/load?{queryString}");

                if (response != null)
                {
                    dgGiaoHang.ItemsSource = response.DonGiaoHang;

                    // Cập nhật nguồn dữ liệu cho ComboBox Shipper trong DataGrid
                    var shippersSource = (CollectionViewSource)this.FindResource("ShippersSource");
                    shippersSource.Source = response.NguoiGiaoHangSanSang;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            _searchTimer.Stop();
            _searchTimer.Start(); // Reset timer mỗi khi gõ
        }

        private async void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded) return;
            _searchTimer.Stop(); // Dừng tìm kiếm đang chờ
            await LoadDataAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbStatusFilter.SelectedIndex = 1; // Reset về "Chờ xác nhận"
            await LoadDataAsync();
        }

        // --- CÁC HÀM XỬ LÝ HÀNH ĐỘNG ---

        private async void BtnConfirmAll_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Xác nhận chuyển TẤT CẢ đơn 'Chờ xác nhận' sang Bếp?",
                "Xác nhận hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                var response = await ApiClient.Instance.PostAsync("api/app/nhanvien/giaohang/confirm-all-pending", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    // Chuyển sang tab "Đang chuẩn bị" để xem kết quả
                    cmbStatusFilter.SelectedIndex = 2;
                    await LoadDataAsync();
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi: {err}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnChuyenCheBien_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idHoaDon)
            {
                var confirm = MessageBox.Show($"Chuyển đơn #{idHoaDon} sang Bếp?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                var dto = new GiaoHangUpdateRequestDto { TrangThaiGiaoHang = "Đang chuẩn bị" };
                await UpdateOrderAsync(idHoaDon, dto);
            }
        }

        private async void BtnHuyDon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idHoaDon)
            {
                var confirm = MessageBox.Show($"HỦY đơn #{idHoaDon}? Hành động này không thể hoàn tác.", "Cảnh báo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.No) return;

                var dto = new GiaoHangUpdateRequestDto { TrangThaiGiaoHang = "Hủy" };
                await UpdateOrderAsync(idHoaDon, dto);
            }
        }

        private async void CmbNguoiGiaoHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            var comboBox = sender as ComboBox;
            var item = comboBox?.DataContext as GiaoHangItemDto;

            // Chỉ xử lý khi người dùng chọn thủ công (không phải do binding ban đầu)
            if (item == null || !comboBox.IsDropDownOpen) return;

            var newShipperId = (int?)comboBox.SelectedValue;

            // Tự động chuyển trạng thái sang "Đang giao" nếu đang là "Chờ lấy hàng"
            string newStatus = item.TrangThaiGiaoHang;
            if (item.TrangThaiGiaoHang == "Chờ lấy hàng" && newShipperId.HasValue)
            {
                newStatus = "Đang giao";
            }

            var dto = new GiaoHangUpdateRequestDto
            {
                TrangThaiGiaoHang = newStatus,
                IdNguoiGiaoHang = newShipperId
            };

            await UpdateOrderAsync(item.IdHoaDon, dto);
        }

        // Nút "Hoàn thành" (Dành cho trường hợp Shipper không dùng Web App)
        // Bạn có thể thêm nút này vào XAML nếu muốn App WPF cũng hoàn thành được đơn
        private async void BtnHoanThanh_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idHoaDon)
            {
                var confirm = MessageBox.Show($"Xác nhận đơn #{idHoaDon} đã giao thành công?", "Hoàn tất", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                var dto = new GiaoHangUpdateRequestDto { TrangThaiGiaoHang = "Hoàn thành" };
                await UpdateOrderAsync(idHoaDon, dto);
            }
        }

        private async Task UpdateOrderAsync(int idHoaDon, GiaoHangUpdateRequestDto dto)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync($"api/app/nhanvien/giaohang/update/{idHoaDon}", dto);
                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Cập nhật thất bại: {err}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Tải lại dữ liệu để cập nhật UI
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnInPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idHoaDon)
            {
                try
                {
                    var printData = await ApiClient.Instance.GetFromJsonAsync<PhieuGoiMonPrintDto>($"api/app/nhanvien/giaohang/print-data/{idHoaDon}");
                    if (printData != null)
                    {
                        var printWindow = new AppCafebookApi.View.Common.PhieuGiaoHangPreviewWindow(printData);
                        printWindow.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi in phiếu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}