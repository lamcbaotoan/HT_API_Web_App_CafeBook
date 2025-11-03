using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CafebookModel.Model.ModelApp;
using AppCafebookApi.Services;
using AppCafebookApi.View.common; // <-- SỬA LỖI CS0246 (InputDialogWindow)

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class SoDoBanView : Page
    {
        private static readonly HttpClient httpClient;
        private BanSoDoDto? _selectedBan = null;
        private List<BanSoDoDto> _allTables = new List<BanSoDoDto>();

        static SoDoBanView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public SoDoBanView()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTablesAsync();
        }

        private async Task LoadTablesAsync()
        {
            panelChuaChon.Visibility = Visibility.Visible;
            panelDaChon.Visibility = Visibility.Collapsed;
            _selectedBan = null;
            MainPanel.Opacity = 0.5;

            try
            {
                // SỬA LỖI CS8601: Thêm '?? new ...' để xử lý null
                _allTables = (await httpClient.GetFromJsonAsync<List<BanSoDoDto>>("api/app/sodoban/tables"))
                             ?? new List<BanSoDoDto>();

                icBan.ItemsSource = _allTables;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải sơ đồ bàn: {ex.Message}", "Lỗi API", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainPanel.Opacity = 1.0;
            }
        }

        private void BtnBan_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            _selectedBan = button?.DataContext as BanSoDoDto;

            if (_selectedBan == null) return;

            panelChuaChon.Visibility = Visibility.Collapsed;
            panelDaChon.Visibility = Visibility.Visible;

            runSoBan.Text = _selectedBan.SoBan;
            runTrangThai.Text = _selectedBan.TrangThai;

            if (!string.IsNullOrEmpty(_selectedBan.GhiChu))
            {
                tbGhiChu.Text = $"Ghi chú: {_selectedBan.GhiChu}";
                tbGhiChu.Visibility = Visibility.Visible;
            }
            else
            {
                tbGhiChu.Visibility = Visibility.Collapsed;
            }

            switch (_selectedBan.TrangThai)
            {
                case "Trống":
                    btnGoiMon.Content = "Tạo Hóa Đơn Mới";
                    btnGoiMon.IsEnabled = true;
                    btnChuyenBan.IsEnabled = false;
                    btnGopBan.IsEnabled = false;
                    btnBaoCaoSuCo.IsEnabled = true;
                    tbTongTienWrapper.Visibility = Visibility.Collapsed;
                    break;

                case "Có khách":
                    btnGoiMon.Content = "Gọi Món / Thanh Toán";
                    btnGoiMon.IsEnabled = true;
                    btnChuyenBan.IsEnabled = true;
                    btnGopBan.IsEnabled = true;
                    btnBaoCaoSuCo.IsEnabled = false;
                    tbTongTienWrapper.Visibility = Visibility.Visible;
                    runTongTien.Text = _selectedBan.TongTienHienTai.ToString("N0") + " đ";
                    break;

                case "Đã đặt":
                    btnGoiMon.Content = "Khách đặt (Mở Hóa Đơn)";
                    btnGoiMon.IsEnabled = true;
                    btnChuyenBan.IsEnabled = false;
                    btnGopBan.IsEnabled = false;
                    btnBaoCaoSuCo.IsEnabled = true;
                    tbTongTienWrapper.Visibility = Visibility.Collapsed;
                    break;

                case "Bảo trì":
                case "Tạm ngưng": // Thêm
                    btnGoiMon.Content = "BÀN ĐANG BẢO TRÌ";
                    btnGoiMon.IsEnabled = false;
                    btnChuyenBan.IsEnabled = false;
                    btnGopBan.IsEnabled = false;
                    btnBaoCaoSuCo.IsEnabled = false;
                    tbTongTienWrapper.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private async void BtnGoiMon_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBan == null) return;

            int? idHoaDon = _selectedBan.IdHoaDonHienTai;

            if (_selectedBan.TrangThai == "Trống" || _selectedBan.TrangThai == "Đã đặt")
            {
                if (AuthService.CurrentUser == null)
                {
                    MessageBox.Show("Lỗi: Không tìm thấy thông tin nhân viên đăng nhập.", "Lỗi Phiên");
                    return;
                }
                int idNhanVien = AuthService.CurrentUser.IdNhanVien;

                try
                {
                    var response = await httpClient.PostAsJsonAsync($"api/app/sodoban/createorder/{_selectedBan.IdBan}/{idNhanVien}", new { });

                    if (response.IsSuccessStatusCode)
                    {
                        // SỬA LỖI CS1061: Dùng ReadFromJsonAsync (hiện đại)
                        dynamic? result = await response.Content.ReadFromJsonAsync<dynamic>();
                        if (result != null)
                        {
                            // Cần deserialize JObject
                            var resultObj = System.Text.Json.JsonSerializer.Deserialize<dynamic>(result.ToString());
                            idHoaDon = (int)resultObj.idHoaDon;
                        }

                        await LoadTablesAsync();
                    }
                    else
                    {
                        MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi tạo hóa đơn");
                        return;
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi API: {ex.Message}", "Lỗi"); return; }
            }

            if (idHoaDon.HasValue)
            {
                // ** YÊU CẦU: Bạn cần tạo trang 'ChiTietHoaDonView.xaml' **
                // this.NavigationService.Navigate(new ChiTietHoaDonView(idHoaDon.Value));
                MessageBox.Show($"Sẵn sàng điều hướng đến trang gọi món cho Hóa đơn ID: {idHoaDon.Value}");
            }
        }

        private async void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            await LoadTablesAsync();
        }

        private async void BtnBaoCaoSuCo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBan == null) return;

            // Lấy ID nhân viên đang đăng nhập
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi: Không tìm thấy thông tin nhân viên đăng nhập.", "Lỗi Phiên");
                return;
            }
            int idNhanVien = AuthService.CurrentUser.IdNhanVien;

            var inputDialog = new InputDialogWindow("Báo cáo sự cố", $"Vui lòng mô tả sự cố cho bàn {_selectedBan.SoBan}:");
            if (inputDialog.ShowDialog() == true)
            {
                string ghiChu = inputDialog.InputText;
                if (string.IsNullOrWhiteSpace(ghiChu)) return;

                try
                {
                    var request = new BaoCaoSuCoRequestDto { GhiChuSuCo = ghiChu };

                    // SỬA LỖI: Gửi cả idNhanVien
                    var response = await httpClient.PostAsJsonAsync($"api/app/sodoban/reportproblem/{_selectedBan.IdBan}/{idNhanVien}", request);

                    if (response.IsSuccessStatusCode)
                    {
                        // Không cần MessageBox, quản lý sẽ thấy
                        await LoadTablesAsync(); // Làm mới
                    }
                    else
                    {
                        MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi báo cáo");
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi API: {ex.Message}", "Lỗi"); }
            }
        }

        private void BtnChuyenBan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng 'Chuyển Bàn' đang được phát triển. Yêu cầu tạo Popup chọn bàn đích.", "Thông báo");
        }

        private void BtnGopBan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng 'Gộp Bàn' đang được phát triển. Yêu cầu tạo Popup chọn bàn đích.", "Thông báo");
        }
    }
}