using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

// SỬA: Đổi "common" thành "Common" để khớp với x:Class trong file .xaml
namespace AppCafebookApi.View.Common
{
    public partial class PhieuGoiMonPreviewWindow : Window
    {
        private readonly int _idHoaDon;
        private static readonly HttpClient _httpClient;

        static PhieuGoiMonPreviewWindow()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5166") };
        }

        public PhieuGoiMonPreviewWindow(int idHoaDon)
        {
            InitializeComponent(); // Lỗi CS0103 sẽ biến mất
            _idHoaDon = idHoaDon;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<PhieuGoiMonPrintDto>($"api/app/nhanvien/goimon/print-data/{_idHoaDon}");
                if (data != null)
                {
                    // Các lỗi CS0103 ở đây sẽ biến mất
                    lblTenQuan.Text = data.TenQuan;
                    lblDiaChiQuan.Text = data.DiaChiQuan;
                    lblSdtQuan.Text = $"SĐT: {data.SdtQuan}";
                    lblIdPhieu.Text = $"Mã: {data.IdPhieu}";
                    lblNgayTao.Text = data.NgayTao.ToString("dd/MM/yyyy HH:mm");
                    lblSoBan.Text = data.SoBan;
                    lblTenNhanVien.Text = data.TenNhanVien;

                    dgChiTiet.ItemsSource = data.ChiTiet;

                    lblTongTienGoc.Text = data.TongTienGoc.ToString("N0") + " đ";
                    lblGiamGia.Text = data.GiamGia.ToString("N0") + " đ";
                    lblThanhTien.Text = data.ThanhTien.ToString("N0") + " đ";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu in: {ex.Message}", "Lỗi API");
                this.Close();
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Tạm ẩn nút In trước khi in
                    btnPrint.Visibility = Visibility.Collapsed;

                    // In khu vực PrintArea
                    printDialog.PrintVisual(PrintArea, "Phiếu Gọi Món Cafebook");

                    // Hiện lại nút In
                    btnPrint.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi in: {ex.Message}", "Lỗi In");
            }
            finally
            {
                // Hiện lại nút In nếu có lỗi
                btnPrint.Visibility = Visibility.Visible;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}