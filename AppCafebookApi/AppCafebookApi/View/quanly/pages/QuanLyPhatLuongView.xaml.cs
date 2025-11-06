using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using AppCafebookApi.Services; // Cần cho AuthService
using AppCafebookApi.View.Common; // Cần cho PhieuLuongPreviewWindow
using CafebookModel.Model.ModelApp; // Cần cho DTOs

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyPhatLuongView : Page
    {
        private static readonly HttpClient httpClient;

        static QuanLyPhatLuongView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyPhatLuongView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await SetupFiltersAsync();
            await LoadDanhSachPhieuLuongAsync();
        }

        private Task SetupFiltersAsync()
        {
            // Setup Tháng
            cmbThang.ItemsSource = Enumerable.Range(1, 12).Select(m => new { Value = m, Display = $"Tháng {m}" });
            cmbThang.DisplayMemberPath = "Display";
            cmbThang.SelectedValuePath = "Value";
            cmbThang.SelectedValue = DateTime.Now.Month;

            // Setup Năm
            int currentYear = DateTime.Now.Year;
            cmbNam.ItemsSource = Enumerable.Range(currentYear - 2, 5).Select(y => new { Value = y, Display = $"Năm {y}" });
            cmbNam.DisplayMemberPath = "Display";
            cmbNam.SelectedValuePath = "Value";
            cmbNam.SelectedValue = currentYear;

            return Task.CompletedTask;
        }

        private async Task LoadDanhSachPhieuLuongAsync()
        {
            if (cmbThang.SelectedValue == null || cmbNam.SelectedValue == null)
                return;

            int thang = (int)cmbThang.SelectedValue;
            int nam = (int)cmbNam.SelectedValue;

            LoadingOverlay.Visibility = Visibility.Visible;
            dgPhieuLuong.ItemsSource = null;

            try
            {
                var response = await httpClient.GetAsync($"api/app/phatluong/danhsach?thang={thang}&nam={nam}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<List<PhieuLuongDto>>();
                    dgPhieuLuong.ItemsSource = data;
                }
                else
                {
                    MessageBox.Show($"Lỗi tải danh sách: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnLoc_Click(object sender, RoutedEventArgs e)
        {
            await LoadDanhSachPhieuLuongAsync();
        }

        private async void BtnXemChiTiet_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not PhieuLuongDto selectedPhieu)
                return;

            // Mở Window popup và truyền ID
            var popup = new PhieuLuongPreviewWindow(selectedPhieu.IdPhieuLuong);

            // ShowDialog để chặn tương tác với cửa sổ chính
            // DialogResult sẽ là 'true' nếu người dùng nhấn "Xác nhận Phát Lương" thành công
            bool? result = popup.ShowDialog();

            if (result == true)
            {
                // Nếu phát lương thành công, tải lại danh sách để cập nhật trạng thái
                await LoadDanhSachPhieuLuongAsync();
            }
        }

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}