using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Threading.Tasks;
using LiveCharts;
using LiveCharts.Wpf;
using System.Globalization;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class BaoCaoNhanSuView : Page
    {
        private static readonly HttpClient httpClient;

        // Dành cho Biểu đồ
        public SeriesCollection LuongChartSeries { get; set; }
        public string[] ChartLabels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        static BaoCaoNhanSuView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public BaoCaoNhanSuView()
        {
            InitializeComponent();

            // Khởi tạo cho Biểu đồ
            LuongChartSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Tổng Lương",
                    Values = new ChartValues<decimal>(),
                    LineSmoothness = 0
                }
            };
            ChartLabels = Array.Empty<string>();
            YFormatter = value => value.ToString("N0") + " đ";

            DataContext = this;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFiltersAsync();

            var now = DateTime.Now;
            dpStartDate.SelectedDate = new DateTime(now.Year, now.Month, 1);
            dpEndDate.SelectedDate = now;
            cmbTrangThaiFilter.SelectedIndex = 0; // Đang làm việc
        }

        private async Task LoadFiltersAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var filters = await httpClient.GetFromJsonAsync<BaoCaoNhanSu_FiltersDto>("api/app/baocaonhansu/filters");
                if (filters != null)
                {
                    // Lọc Nhân viên
                    var nvList = filters.NhanViens;
                    nvList.Insert(0, new NhanVienLookupDto { IdNhanVien = 0, HoTen = "--- Tất cả Nhân viên ---" });
                    cmbNhanVienFilter.ItemsSource = nvList;
                    cmbNhanVienFilter.SelectedValue = 0;

                    // Lọc Vai trò
                    var vtList = filters.VaiTros;
                    vtList.Insert(0, new FilterLookupDto { Id = 0, Ten = "--- Tất cả Vai trò ---" });
                    cmbVaiTroFilter.ItemsSource = vtList;
                    cmbVaiTroFilter.SelectedValue = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bộ lọc: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn Từ Ngày và Đến Ngày.", "Thiếu thông tin");
                return;
            }

            var request = new BaoCaoNhanSuRequestDto
            {
                StartDate = dpStartDate.SelectedDate.Value,
                EndDate = dpEndDate.SelectedDate.Value,
                IdNhanVien = (int)(cmbNhanVienFilter.SelectedValue ?? 0),
                IdVaiTro = (int)(cmbVaiTroFilter.SelectedValue ?? 0),
                TrangThaiNhanVien = (cmbTrangThaiFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Tất cả"
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/baocaonhansu/report", request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<BaoCaoNhanSuDto>();
                    if (data != null)
                    {
                        PopulateReport(data);
                    }
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

        private void PopulateReport(BaoCaoNhanSuDto data)
        {
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            // 1. Cập nhật KPIs
            lblSoLuongNhanVien.Text = data.Kpi.SoLuongNhanVien.ToString();
            lblTongLuongDaTra.Text = data.Kpi.TongLuongDaTra.ToString("C0", culture);
            lblTongGioLam.Text = data.Kpi.TongGioLam.ToString("F2");
            lblTongSoNgayNghi.Text = data.Kpi.TongSoNgayNghi.ToString();

            // 2. Cập nhật DataGrids
            dgLuong.ItemsSource = data.BangLuongChiTiet;
            dgNghiPhep.ItemsSource = data.ThongKeNghiPhep;

            // 3. Cập nhật Biểu đồ
            LuongChartSeries[0].Values.Clear();
            LuongChartSeries[0].Values.AddRange(data.LuongChartData.Select(d => d.TongTien).Cast<object>());
            ChartLabels = data.LuongChartData.Select(d => d.Ngay.ToString("dd/MM")).ToArray();

            // Cần reset DataContext để Chart cập nhật Labels
            DataContext = null;
            DataContext = this;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng Xuất Excel đang được phát triển. Dữ liệu sẽ được lấy từ các bảng và biểu đồ hiện tại.", "Thông báo");
        }

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                // Fallback nếu không CanGoBack
                this.NavigationService?.Navigate(new QuanLyNhanVienView());
            }
        }
    }
}