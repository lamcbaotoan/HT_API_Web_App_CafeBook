using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Net;
using System.Threading.Tasks;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyVaiTroView : Page
    {
        private static readonly HttpClient httpClient;
        private List<VaiTroDto> _allVaiTroList = new List<VaiTroDto>();
        private VaiTroDto? _selectedVaiTro = null;

        static QuanLyVaiTroView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyVaiTroView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataGridAsync();
            ResetForm();
        }

        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _allVaiTroList = (await httpClient.GetFromJsonAsync<List<VaiTroDto>>("api/app/vaitro/all")) ?? new List<VaiTroDto>();
                dgVaiTro.ItemsSource = _allVaiTroList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách vai trò: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetForm()
        {
            _selectedVaiTro = null;
            dgVaiTro.SelectedItem = null;
            txtTenVaiTro.Text = "";
            txtMoTa.Text = "";
            btnThem.IsEnabled = true;
            btnLuu.IsEnabled = false;
            btnXoa.IsEnabled = false;
        }

        private void DgVaiTro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVaiTro.SelectedItem is VaiTroDto selected)
            {
                _selectedVaiTro = selected;
                txtTenVaiTro.Text = selected.TenVaiTro;
                txtMoTa.Text = selected.MoTa;
                btnThem.IsEnabled = false;
                btnLuu.IsEnabled = true;
                btnXoa.IsEnabled = true;
            }
            else
            {
                ResetForm();
            }
        }

        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync(isCreating: true);
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync(isCreating: false);
        }

        private async Task SaveAsync(bool isCreating)
        {
            if (string.IsNullOrWhiteSpace(txtTenVaiTro.Text))
            {
                MessageBox.Show("Tên vai trò là bắt buộc.", "Lỗi"); return;
            }

            var dto = new VaiTroDto
            {
                TenVaiTro = txtTenVaiTro.Text,
                MoTa = txtMoTa.Text
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/vaitro", dto);
                }
                else
                {
                    dto.IdVaiTro = _selectedVaiTro.IdVaiTro;
                    response = await httpClient.PutAsJsonAsync($"api/app/vaitro/{dto.IdVaiTro}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
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

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVaiTro == null) return;

            var result = MessageBox.Show($"Bạn có chắc muốn xóa vai trò '{_selectedVaiTro.TenVaiTro}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/vaitro/{_selectedVaiTro.IdVaiTro}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Không thể xóa");
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

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void BtnGoToPhanQuyen_Click(object sender, RoutedEventArgs e)
        {
            // Chuyển sang trang Phân Quyền
            this.NavigationService?.Navigate(new PhanQuyenView());
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