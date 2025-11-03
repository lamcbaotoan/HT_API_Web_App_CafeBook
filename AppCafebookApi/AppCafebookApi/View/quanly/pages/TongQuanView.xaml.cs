using AppCafebookApi.View.common;
using CafebookModel.Model.ModelApp;
// --- 1. SỬ DỤNG CÁC USING CŨ CỦA LIVECHARTS ---
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation; // <-- Đảm bảo bạn có using này

namespace AppCafebookApi.View.quanly.pages
{
    public partial class TongQuanView : Page
    {
        // --- 2. KHAI BÁO HTTPCLIENT ---
        private static readonly HttpClient httpClient;

        static TongQuanView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166") // URL của API
            };
        }

        // --- 3. KHAI BÁO CÁC THUỘC TÍNH BINDING CŨ ---
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        // --- 4. CONSTRUCTOR ---
        public TongQuanView()
        {
            InitializeComponent();

            // Khởi tạo các giá trị cho biểu đồ
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<decimal>(), // Khởi tạo rỗng
                    LineSmoothness = 0 // Tắt làm mịn
                }
            };

            Labels = Array.Empty<string>(); // Khởi tạo rỗng
            YFormatter = value => value.ToString("N0") + " VND"; // Định dạng tiền

            // Set DataContext để XAML có thể binding
            DataContext = this;
        }

        // --- 5. HÀM LOAD DỮ LIỆU TỪ API ---
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Gọi API (API Controller giữ nguyên, không cần sửa)
                var data = await httpClient.GetFromJsonAsync<DashboardSummaryDto>("api/app/dashboard/summary");

                if (data != null)
                {
                    // 1. Cập nhật các Thẻ KPI
                    lblDoanhThu.Text = data.TongDoanhThuHomNay.ToString("N0") + " VND";
                    lblDonHang.Text = data.TongDonHangHomNay.ToString();
                    lblSanPhamBanChay.Text = data.SanPhamBanChayHomNay;

                    // 2. Cập nhật Biểu đồ (theo cách của LVC 0.9.7)

                    // Xóa dữ liệu cũ
                    SeriesCollection[0].Values.Clear();

                    // Thêm dữ liệu mới
                    foreach (var item in data.DoanhThu30Ngay)
                    {
                        SeriesCollection[0].Values.Add(item.TongTien);
                    }

                    // Cập nhật nhãn trục X
                    Labels = data.DoanhThu30Ngay.Select(d => d.Ngay.ToString("dd/MM")).ToArray();

                    // Cập nhật lại DataContext để binding nhận thay đổi (cho Labels)
                    DataContext = null;
                    DataContext = this;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải dữ liệu Dashboard: {ex.Message}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
                lblDoanhThu.Text = "Lỗi";
                lblDonHang.Text = "Lỗi";
                lblSanPhamBanChay.Text = "Lỗi";
            }
        }


        // --- CÁC HÀM XỬ LÝ NÚT BẤM (BÁO CÁO) ---
        // (Giữ nguyên không thay đổi)

        // THAY THẾ HÀM NÀY
        private void BtnExportRevenue_Click(object sender, RoutedEventArgs e)
        {
            // Lỗi của bạn là do dùng code cũ (ShowDialog)
            // Code mới phải là điều hướng (Navigate)
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new BaocaodoanhthuPreviewWindow());
            }
        }

        // --- THAY THẾ HÀM NÀY ---
        private void BtnExportTonKhoSach_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng Frame (MainFrame) đến trang Báo cáo Sách
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new BapCaoTonKhoSachPreviewWindow());
            }
        }

        // --- THAY THẾ HÀM NÀY ---
        private void BtnExportNguyenLieu_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng Frame (MainFrame) đến trang Báo cáo Kho
            this.NavigationService?.Navigate(new BaoCaoTonKhoNguyenLieuPreviewWindow());
        }

        // --- THAY THẾ HÀM NÀY ---
        private void BtnExportPerformance_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng Frame (MainFrame) đến trang Báo cáo Hiệu suất
            this.NavigationService?.Navigate(new BaoCaoHieuSuatNhanVientPreviewWindow());
        }

        // THAY THẾ HÀM NÀY
        private void BtnCaiDat_Click(object sender, RoutedEventArgs e)
        {
            // SỬA LỖI: Dùng trực tiếp NavigationService của Page
            if (this.NavigationService != null)
            {
                // Điều hướng Frame (MainFrame) đến trang CaiDatWindow
                this.NavigationService.Navigate(new CaiDatWindow());
            }
            else
            {
                MessageBox.Show("Không tìm thấy dịch vụ điều hướng.");
            }
        }
    }
}