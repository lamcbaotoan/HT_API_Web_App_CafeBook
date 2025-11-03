using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class CaiDatNhanSuView : Page
    {
        private static readonly HttpClient httpClient;
        private CaiDatNhanSuDto _currentSettings = new CaiDatNhanSuDto();

        static CaiDatNhanSuView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public CaiDatNhanSuView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _currentSettings = await httpClient.GetFromJsonAsync<CaiDatNhanSuDto>("api/app/caidatnhansu/all");
                if (_currentSettings != null)
                {
                    // Dùng CultureInfo.InvariantCulture để xử lý dấu chấm (.)
                    txtGioLamChuan.Text = _currentSettings.GioLamChuan.ToString(CultureInfo.InvariantCulture);
                    txtHeSoOT.Text = _currentSettings.HeSoOT.ToString(CultureInfo.InvariantCulture);
                    txtPhatDiTre_Phut.Text = _currentSettings.PhatDiTre_Phut.ToString();
                    txtPhatDiTre_HeSo.Text = _currentSettings.PhatDiTre_HeSo.ToString(CultureInfo.InvariantCulture);
                    txtChuyenCan_SoNgay.Text = _currentSettings.ChuyenCan_SoNgay.ToString();
                    txtChuyenCan_TienThuong.Text = _currentSettings.ChuyenCan_TienThuong.ToString("F0", CultureInfo.InvariantCulture);
                    txtPhepNam_MacDinh.Text = _currentSettings.PhepNam_MacDinh.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải cài đặt: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                // Validate và Parse
                var dto = new CaiDatNhanSuDto
                {
                    GioLamChuan = double.Parse(txtGioLamChuan.Text, CultureInfo.InvariantCulture),
                    HeSoOT = double.Parse(txtHeSoOT.Text, CultureInfo.InvariantCulture),
                    PhatDiTre_Phut = int.Parse(txtPhatDiTre_Phut.Text),
                    PhatDiTre_HeSo = double.Parse(txtPhatDiTre_HeSo.Text, CultureInfo.InvariantCulture),
                    ChuyenCan_SoNgay = int.Parse(txtChuyenCan_SoNgay.Text),
                    ChuyenCan_TienThuong = decimal.Parse(txtChuyenCan_TienThuong.Text, CultureInfo.InvariantCulture),
                    PhepNam_MacDinh = int.Parse(txtPhepNam_MacDinh.Text)
                };

                // Gọi API
                var response = await httpClient.PutAsJsonAsync("api/app/caidatnhansu/update", dto);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu cài đặt thành công!", "Thành công");
                    await LoadSettingsAsync(); // Tải lại dữ liệu
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra các con số (dùng dấu . cho số thập phân).", "Lỗi Định Dạng");
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

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            // Quay lại trang QL Lương (Module 6)
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}