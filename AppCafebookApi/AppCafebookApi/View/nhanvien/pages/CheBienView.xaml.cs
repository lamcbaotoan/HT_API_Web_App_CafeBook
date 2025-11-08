using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Thêm
using System.Windows.Threading;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class CheBienView : Page
    {
        private static readonly HttpClient _httpClient;
        private DispatcherTimer _refreshTimer;

        static CheBienView()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5166") };
        }

        public CheBienView()
        {
            InitializeComponent();

            // Cài đặt Timer tự động làm mới
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _refreshTimer.Tick += async (s, e) => await LoadDataAsync();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            _refreshTimer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop(); // Dừng timer khi rời trang
        }

        private async Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var allItems = await _httpClient.GetFromJsonAsync<List<CheBienItemDto>>("api/app/nhanvien/chebien/load");

                if (allItems != null)
                {
                    // Lọc theo NhomIn (Bếp hoặc PhaChế)
                    icBep.ItemsSource = allItems
                        .Where(i => string.Equals(i.NhomIn, "Bếp", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    icPhaChe.ItemsSource = allItems
                        .Where(i => !string.Equals(i.NhomIn, "Bếp", StringComparison.OrdinalIgnoreCase)) // Mặc định còn lại là Pha Chế
                        .ToList();
                }

                lblLastUpdated.Text = $"(Cập nhật lúc: {DateTime.Now:HH:mm:ss})";
            }
            catch (Exception ex)
            {
                lblLastUpdated.Text = $"(Lỗi: {ex.Message})";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnStartItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as CheBienItemDto;
            if (item == null) return;

            // Ngăn sự kiện click của Border cha
            e.Handled = true;

            _refreshTimer.Stop();
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PostAsync($"api/app/nhanvien/chebien/start/{item.IdTrangThaiCheBien}", null);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi API"); }

            await LoadDataAsync();
            _refreshTimer.Start();
        }

        private async void BtnCompleteItem_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as CheBienItemDto;
            if (item == null) return;

            // Ngăn sự kiện click của Border cha
            e.Handled = true;

            _refreshTimer.Stop();
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await _httpClient.PostAsync($"api/app/nhanvien/chebien/complete/{item.IdTrangThaiCheBien}", null);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi");
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi API"); }

            await LoadDataAsync();
            _refreshTimer.Start();
        }

        // === THÊM MỚI: HÀM HIỂN THỊ CÔNG THỨC ===

        private async void Border_CongThuc_Click(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as Border)?.DataContext as CheBienItemDto;
            if (item == null) return;

            // Dừng timer khi xem công thức
            _refreshTimer.Stop();
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                // Gọi API lấy công thức
                var congThucItems = await _httpClient.GetFromJsonAsync<List<CongThucItemDto>>($"api/app/nhanvien/chebien/congthuc/{item.IdSanPham}");

                // Cập nhật UI
                lblCongThucTenMon.Text = $"Công thức: {item.TenMon}";
                lvCongThuc.ItemsSource = congThucItems;

                // Hiển thị Overlay
                CongThucOverlay.Visibility = Visibility.Visible;
                // Kích hoạt animation (bằng cách cập nhật binding Target)
                CongThucOverlay.SetBinding(Border.OpacityProperty, new System.Windows.Data.Binding());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải công thức: {ex.Message}", "Lỗi API");
                _refreshTimer.Start(); // Bật lại timer nếu có lỗi
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCloseCongThuc_Click(object sender, RoutedEventArgs e)
        {
            // Ẩn Overlay
            CongThucOverlay.Visibility = Visibility.Collapsed;
            lvCongThuc.ItemsSource = null; // Xóa dữ liệu cũ

            // Bật lại timer
            _refreshTimer.Start();
        }
    }
}