using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using AppCafebookApi.View.common; // <-- THÊM MỚI

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyTonKhoView : Page
    {
        private static readonly HttpClient httpClient;
        private List<NguyenLieuTonKhoDto> _tonKhoList = new List<NguyenLieuTonKhoDto>();

        static QuanLyTonKhoView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyTonKhoView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTonKhoAsync();
        }

        private async Task LoadTonKhoAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _tonKhoList = (await httpClient.GetFromJsonAsync<List<NguyenLieuTonKhoDto>>("api/app/kho/tonkho")) ?? new List<NguyenLieuTonKhoDto>();

                dgTonKho.ItemsSource = _tonKhoList.Where(nl => nl.TinhTrang == "Đủ dùng").ToList();
                dgCanhBao.ItemsSource = _tonKhoList.Where(nl => nl.TinhTrang == "Sắp hết" || nl.TinhTrang == "Hết hàng").ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải tồn kho: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #region Navigation

        private void BtnGoToNguyenLieu_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyNguyenLieuView());
        }

        private void BtnGoToNhapKho_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyNhapKhoView());
        }

        private void BtnGoToXuatHuy_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyXuatHuyView());
        }

        private void BtnGoToKiemKho_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyKiemKhoView());
        }

        private void BtnGoToNCC_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new QuanLyNhaCungCapView());
        }

        // --- THÊM MỚI: Xử lý nút Báo Cáo ---
        private void BtnGoToBaoCaoKho_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BaoCaoTonKhoNguyenLieuPreviewWindow());
        }

        #endregion
    }
}