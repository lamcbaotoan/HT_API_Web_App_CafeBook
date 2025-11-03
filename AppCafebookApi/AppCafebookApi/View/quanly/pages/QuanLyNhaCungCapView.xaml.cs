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
// THÊM MỚI: Using cho Excel
using Microsoft.Win32;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyNhaCungCapView : Page
    {
        private static readonly HttpClient httpClient;
        private List<NhaCungCapDto> _nhaCungCapList = new List<NhaCungCapDto>();

        static QuanLyNhaCungCapView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyNhaCungCapView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadNhaCungCapAsync();
            ResetNhaCungCapForm();
        }

        private async Task LoadNhaCungCapAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _nhaCungCapList = (await httpClient.GetFromJsonAsync<List<NhaCungCapDto>>("api/app/kho/nhacungcap")) ?? new List<NhaCungCapDto>();
                dgNhaCungCap.ItemsSource = _nhaCungCapList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải nhà cung cấp: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetNhaCungCapForm()
        {
            dgNhaCungCap.SelectedItem = null;
            txtTenNCC.Text = "";
            txtSdtNCC.Text = "";
            txtDiaChiNCC.Text = "";
            txtEmailNCC.Text = "";
            btnThemNCC.IsEnabled = true;
            btnLuuNCC.IsEnabled = false;
            btnXoaNCC.IsEnabled = false;
        }

        private void DgNhaCungCap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgNhaCungCap.SelectedItem is NhaCungCapDto selected)
            {
                txtTenNCC.Text = selected.TenNhaCungCap;
                txtSdtNCC.Text = selected.SoDienThoai;
                txtDiaChiNCC.Text = selected.DiaChi;
                txtEmailNCC.Text = selected.Email;
                btnThemNCC.IsEnabled = false;
                btnLuuNCC.IsEnabled = true;
                btnXoaNCC.IsEnabled = true;
            }
            else
            {
                ResetNhaCungCapForm();
            }
        }

        private async void BtnThemNCC_Click(object sender, RoutedEventArgs e)
        {
            await SaveNhaCungCapAsync(isCreating: true);
        }

        private async void BtnLuuNCC_Click(object sender, RoutedEventArgs e)
        {
            await SaveNhaCungCapAsync(isCreating: false);
        }

        private async Task SaveNhaCungCapAsync(bool isCreating)
        {
            if (string.IsNullOrWhiteSpace(txtTenNCC.Text))
            {
                MessageBox.Show("Tên nhà cung cấp là bắt buộc.", "Lỗi"); return;
            }

            var dto = new NhaCungCapDto
            {
                TenNhaCungCap = txtTenNCC.Text,
                SoDienThoai = txtSdtNCC.Text,
                DiaChi = txtDiaChiNCC.Text,
                Email = txtEmailNCC.Text
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/kho/nhacungcap", dto);
                }
                else
                {
                    int id = (dgNhaCungCap.SelectedItem as NhaCungCapDto)?.IdNhaCungCap ?? 0;
                    response = await httpClient.PutAsJsonAsync($"api/app/kho/nhacungcap/{id}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadNhaCungCapAsync();
                    ResetNhaCungCapForm();
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

        private async void BtnXoaNCC_Click(object sender, RoutedEventArgs e)
        {
            if (dgNhaCungCap.SelectedItem is not NhaCungCapDto selected) return;

            if (MessageBox.Show($"Bạn có chắc muốn xóa '{selected.TenNhaCungCap}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/kho/nhacungcap/{selected.IdNhaCungCap}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadNhaCungCapAsync();
                    ResetNhaCungCapForm();
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

        private void BtnLamMoiNCC_Click(object sender, RoutedEventArgs e)
        {
            ResetNhaCungCapForm();
        }

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService?.CanGoBack == true)
            {
                this.NavigationService.GoBack();
            }
        }

        // --- THÊM MỚI: HÀM XUẤT EXCEL ---
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_nhaCungCapList == null || !_nhaCungCapList.Any())
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DSNhaCungCap_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
                    using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                    {
                        var ws = package.Workbook.Worksheets.Add("DanhSachNCC");
                        ws.Cells["A1"].Value = "Danh sách Nhà Cung Cấp";
                        ws.Cells["A1:D1"].Merge = true;
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Size = 16;

                        // Tải dữ liệu vào, tự động nhận Header
                        ws.Cells["A3"].LoadFromCollection(_nhaCungCapList, true, TableStyles.Medium9);
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();

                        package.Save();
                    }
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo file không đang được mở.", "Lỗi");
                }
            }
        }
    }
}