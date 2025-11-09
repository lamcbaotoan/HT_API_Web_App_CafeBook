// Tệp: AppCafebookApi/View/nhanvien/pages/GiaoHangView.xaml.cs
// (*** TẠO TỆP MỚI NÀY ***)
using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class GiaoHangView : Page
    {
        // Biến cờ để ngăn sự kiện SelectionChanged chạy khi đang tải dữ liệu
        private bool _isLoading = false;

        public GiaoHangView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            _isLoading = true;
            try
            {
                var response = await ApiClient.Instance.GetFromJsonAsync<GiaoHangViewDto>("api/app/nhanvien/giaohang/load");
                if (response != null)
                {
                    // 1. Tải danh sách đơn hàng vào DataGrid
                    dgGiaoHang.ItemsSource = response.DonGiaoHang;

                    // 2. Tải danh sách người giao hàng vào Resource của Page
                    // (để ComboBox trong DataGrid có thể binding)
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
            await LoadDataAsync();
        }

        /// <summary>
        /// Xảy ra khi thay đổi Trạng Thái Giao Hàng trong DataGrid
        /// </summary>
        private async void CmbTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return; // Không chạy nếu đang tải dữ liệu

            var comboBox = sender as ComboBox;
            // Lấy GiaoHangItemDto của dòng hiện tại
            var selectedItem = (comboBox?.DataContext) as GiaoHangItemDto;
            var newStatus = (comboBox?.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedItem == null || newStatus == null) return;

            // Cập nhật DTO
            var requestDto = new GiaoHangUpdateRequestDto
            {
                TrangThaiGiaoHang = newStatus,
                IdNguoiGiaoHang = selectedItem.IdNguoiGiaoHang // Giữ nguyên người giao hàng
            };

            await UpdateGiaoHangAsync(selectedItem.IdHoaDon, requestDto);
        }

        /// <summary>
        /// Xảy ra khi thay đổi Người Giao Hàng trong DataGrid
        /// </summary>
        private async void CmbNguoiGiaoHang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;

            var comboBox = sender as ComboBox;
            var selectedItem = (comboBox?.DataContext) as GiaoHangItemDto;
            var newShipperId = (int?)comboBox?.SelectedValue;

            if (selectedItem == null) return;

            // Cập nhật DTO
            var requestDto = new GiaoHangUpdateRequestDto
            {
                TrangThaiGiaoHang = selectedItem.TrangThaiGiaoHang, // Giữ nguyên trạng thái
                IdNguoiGiaoHang = newShipperId
            };

            await UpdateGiaoHangAsync(selectedItem.IdHoaDon, requestDto);

            // Tải lại nếu gán shipper mà trạng thái đang trống
            if (newShipperId.HasValue && string.IsNullOrEmpty(selectedItem.TrangThaiGiaoHang))
            {
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// Hàm chung để gọi API cập nhật
        /// </summary>
        private async Task UpdateGiaoHangAsync(int idHoaDon, GiaoHangUpdateRequestDto dto)
        {
            try
            {
                var response = await ApiClient.Instance.PostAsJsonAsync($"api/app/nhanvien/giaohang/update/{idHoaDon}", dto);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật: {error}", "Lỗi API");
                    // Tải lại để reset thay đổi
                    await LoadDataAsync();
                }

                // Nếu thành công (ví dụ: giao hàng hoàn thành -> tự động thanh toán)
                // Cần tải lại để cập nhật trạng thái thanh toán
                if (dto.TrangThaiGiaoHang == "Hoàn thành")
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
    }
}