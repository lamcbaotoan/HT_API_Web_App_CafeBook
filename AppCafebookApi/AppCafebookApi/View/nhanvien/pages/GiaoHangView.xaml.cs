// Tệp: AppCafebookApi/View/nhanvien/pages/GiaoHangView.xaml.cs
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
using System.Collections.Generic; // THÊM
using System.Net.Http; // THÊM
using System.Windows.Threading; // THÊM cho Timer

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class GiaoHangView : Page
    {
        private bool _isLoading = false;

        // =======================================
        // === THÊM TIMER CHO LIVE SEARCH (YÊU CẦU 2) ===
        // =======================================
        private DispatcherTimer _searchTimer;

        public GiaoHangView()
        {
            InitializeComponent();

            // Khởi tạo timer
            _searchTimer = new DispatcherTimer();
            _searchTimer.Interval = TimeSpan.FromMilliseconds(500); // Chờ 500ms sau khi gõ
            _searchTimer.Tick += async (s, e) =>
            {
                _searchTimer.Stop(); // Dừng timer
                await LoadDataAsync(); // Gọi API
            };
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_isLoading) return; // Chặn gọi API chồng chéo
            _isLoading = true;

            try
            {
                // LẤY DỮ LIỆU TỪ BỘ LỌC
                string searchQuery = txtSearch.Text;
                string statusQuery = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chờ xác nhận";

                // Xử lý nếu lỡ tay gõ vào ô tìm kiếm
                if (searchQuery == "Tìm theo Tên, SĐT khách hoặc Mã HĐ...")
                {
                    searchQuery = "";
                }

                // Xây dựng query string
                var queryParams = new List<string>();
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    queryParams.Add($"search={Uri.EscapeDataString(searchQuery)}");
                }
                if (statusQuery != "Tất cả")
                {
                    queryParams.Add($"status={Uri.EscapeDataString(statusQuery)}");
                }
                string queryString = string.Join("&", queryParams);

                var response = await ApiClient.Instance.GetFromJsonAsync<GiaoHangViewDto>($"api/app/nhanvien/giaohang/load?{queryString}");

                if (response != null)
                {
                    dgGiaoHang.ItemsSource = response.DonGiaoHang;
                    var shippersSource = (CollectionViewSource)this.FindResource("ShippersSource");
                    shippersSource.Source = response.NguoiGiaoHangSanSang;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu giao hàng: {ex.Message}", "Lỗi API");
            }
            _isLoading = false;
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // SỬA: Reset về "Chờ xác nhận"
            txtSearch.Text = "";
            cmbStatusFilter.SelectedIndex = 1; // 0 = Tất cả, 1 = Chờ xác nhận
            await LoadDataAsync();
        }

        // =======================================
        // === SỬA/THÊM CÁC HÀM SỰ KIỆN (YÊU CẦU 2) ===
        // =======================================

        // XÓA: BtnFilter_Click(object sender, RoutedEventArgs e)

        // THÊM: TxtSearch_TextChanged
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Không gọi API ngay, chỉ reset timer
            if (_isLoading) return;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        // THÊM: CmbStatusFilter_SelectionChanged
        private async void CmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Gọi API ngay khi đổi filter
            if (_isLoading || !this.IsLoaded) return;
            _searchTimer.Stop(); // Hủy tìm kiếm nếu đang gõ
            await LoadDataAsync();
        }

        // =======================================
        // === CÁC HÀM CÒN LẠI (GIỮ NGUYÊN) ===
        // =======================================

        private async void BtnConfirmAll_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("Bạn có chắc chắn muốn chuyển TẤT CẢ đơn 'Chờ xác nhận' sang Bếp không?",
                                          "Xác nhận hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                var response = await ApiClient.Instance.PostAsync("api/app/nhanvien/giaohang/confirm-all-pending", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đã chuyển tất cả đơn sang Bếp thành công.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnChuyenCheBien_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idHoaDon)
            {
                var confirm = MessageBox.Show($"Bạn có muốn đẩy đơn hàng #{idHoaDon} sang Bếp/Bar để chuẩn bị không?",
                                             "Xác nhận gửi bếp", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                var requestDto = new GiaoHangUpdateRequestDto
                {
                    TrangThaiGiaoHang = "Đang chuẩn bị",
                    IdNguoiGiaoHang = (btn.DataContext as GiaoHangItemDto)?.IdNguoiGiaoHang
                };
                await UpdateGiaoHangAsync(idHoaDon, requestDto);
            }
        }

        private async void BtnHuyDon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idHoaDon)
            {
                var confirm = MessageBox.Show($"Bạn có chắc chắn muốn HỦY đơn hàng #{idHoaDon} không? Hành động này không thể hoàn tác.",
                                             "Xác nhận Hủy đơn", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.No) return;

                var requestDto = new GiaoHangUpdateRequestDto
                {
                    TrangThaiGiaoHang = "Hủy",
                    IdNguoiGiaoHang = (btn.DataContext as GiaoHangItemDto)?.IdNguoiGiaoHang
                };
                await UpdateGiaoHangAsync(idHoaDon, requestDto);
            }
        }

        private async void CmbNguoiGiaoHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            var comboBox = sender as ComboBox;
            var selectedItem = (comboBox?.DataContext) as GiaoHangItemDto;
            var newShipperId = (int?)comboBox?.SelectedValue;

            if (selectedItem == null) return;

            var requestDto = new GiaoHangUpdateRequestDto
            {
                TrangThaiGiaoHang = selectedItem.TrangThaiGiaoHang,
                IdNguoiGiaoHang = newShipperId
            };

            await UpdateGiaoHangAsync(selectedItem.IdHoaDon, requestDto);

            if (newShipperId.HasValue && string.IsNullOrEmpty(selectedItem.TrangThaiGiaoHang))
            {
                await LoadDataAsync();
            }
        }

        private async Task UpdateGiaoHangAsync(int idHoaDon, GiaoHangUpdateRequestDto dto)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync($"api/app/nhanvien/giaohang/update/{idHoaDon}", dto);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật: {error}", "Lỗi API");
                    await LoadDataAsync();
                }

                if (dto.TrangThaiGiaoHang == "Hoàn thành" ||
                    dto.TrangThaiGiaoHang == "Đang chuẩn bị" ||
                    dto.TrangThaiGiaoHang == "Hủy")
                {
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
                await LoadDataAsync();
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
                    MessageBox.Show($"Lỗi lấy dữ liệu in: {ex.Message}");
                }
            }
        }
    }
}