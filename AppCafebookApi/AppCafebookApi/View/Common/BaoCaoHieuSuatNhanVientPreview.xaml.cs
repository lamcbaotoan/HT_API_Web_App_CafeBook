using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // Cần NuGet Newtonsoft.Json
using System.Globalization;
using System.Text.Json; // THÊM

namespace AppCafebookApi.View.common
{
    public partial class BaoCaoHieuSuatNhanVientPreviewWindow : Page
    {
        private static readonly HttpClient httpClient;
        private BaoCaoHieuSuatTongHopDto? currentReportData;

        static BaoCaoHieuSuatNhanVientPreviewWindow()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public BaoCaoHieuSuatNhanVientPreviewWindow()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Thiết lập ngày mặc định (ví dụ: tháng này)
            var now = DateTime.Now;
            dpStartDate.SelectedDate = new DateTime(now.Year, now.Month, 1);
            dpEndDate.SelectedDate = now;

            await LoadFiltersAsync();
            await GenerateReportAsync();
        }

        private async Task LoadFiltersAsync()
        {
            try
            {
                // SỬA: Dùng GetFromJsonAsync<JsonElement?>
                var response = await httpClient.GetFromJsonAsync<JsonElement?>("api/app/baocaohieusuat/filters");

                if (!response.HasValue) // SỬA: Dùng .HasValue
                {
                    MessageBox.Show("Không nhận được dữ liệu lọc.", "Lỗi API");
                    return;
                }

                // SỬA: Dùng .GetProperty().Deserialize<>()
                var vaiTros = response.Value.GetProperty("vaiTros").Deserialize<List<FilterLookupDto>>() ?? new List<FilterLookupDto>();

                vaiTros.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Vai trò" });
                cmbVaiTro.ItemsSource = vaiTros;
                cmbVaiTro.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateReportAsync();
        }

        private async Task GenerateReportAsync()
        {
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn cả Ngày bắt đầu và Ngày kết thúc.", "Thiếu thông tin");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = false;

            var request = new BaoCaoHieuSuatRequestDto
            {
                StartDate = dpStartDate.SelectedDate.Value,
                EndDate = dpEndDate.SelectedDate.Value,
                SearchText = string.IsNullOrEmpty(txtSearchNhanVien.Text) ? null : txtSearchNhanVien.Text,
                VaiTroId = (int)cmbVaiTro.SelectedValue == 0 ? (int?)null : (int)cmbVaiTro.SelectedValue,
            };

            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/baocaohieusuat/report", request);
                if (response.IsSuccessStatusCode)
                {
                    currentReportData = await response.Content.ReadFromJsonAsync<BaoCaoHieuSuatTongHopDto>();
                    if (currentReportData != null)
                    {
                        PopulateUi(currentReportData);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi API: {response.ReasonPhrase}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối API: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                btnGenerate.IsEnabled = true;
            }
        }

        private void PopulateUi(BaoCaoHieuSuatTongHopDto data)
        {
            var culture = CultureInfo.GetCultureInfo("vi-VN");

            // 1. Cập nhật Thẻ KPI
            lblTongDoanhThu.Text = data.Kpi.TongDoanhThu.ToString("C0", culture);
            lblTongGioLam.Text = data.Kpi.TongGioLam.ToString("N1");
            lblTongSoCaLam.Text = data.Kpi.TongSoCaLam.ToString();
            lblTongLanHuyMon.Text = data.Kpi.TongLanHuyMon.ToString();

            // 2. Cập nhật các Tab
            dgSalesPerformance.ItemsSource = data.SalesPerformance;
            dgOperationalPerformance.ItemsSource = data.OperationalPerformance;
            dgAttendance.ItemsSource = data.Attendance;
        }

        // Dán đè lên hàm BtnExportToExcel_Click CŨ
        private async void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null)
            {
                MessageBox.Show("Chưa có dữ liệu báo cáo để xuất. Vui lòng nhấn 'Lọc Báo Cáo' trước.", "Chưa có dữ liệu");
                return;
            }

            // KIỂM TRA QUAN TRỌNG: Đảm bảo ngày đã được chọn
            if (dpStartDate.SelectedDate == null || dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày bắt đầu và kết thúc hợp lệ.", "Lỗi ngày");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoHieuSuatNV_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;

                    // SỬA LỖI: Truyền ngày vào hàm
                    await CreateExcelReportAsync(
                        sfd.FileName,
                        currentReportData,
                        dpStartDate.SelectedDate.Value,
                        dpEndDate.SelectedDate.Value);

                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    MessageBox.Show($"Đã xuất báo cáo thành công!\n\nĐường dẫn: {sfd.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Có lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo bạn đã đóng file Excel (nếu nó đang mở).", "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Dán đè lên hàm CreateExcelReportAsync CŨ của bạn
        private async Task CreateExcelReportAsync(string filePath, BaoCaoHieuSuatTongHopDto data, DateTime startDate, DateTime endDate)
        {
            ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
            {
                fileInfo.Delete(); // Xóa file cũ nếu tồn tại
            }

            using (var package = new ExcelPackage(fileInfo))
            {
                // --- 1. ĐỊNH NGHĨA STYLES & FORMATS ---
                var currencyFormat = "#,##0 \"đ\"";
                var numberFormat = "#,##0";
                var decimalFormat = "#,##0.0";
                var culture = CultureInfo.GetCultureInfo("vi-VN");

                // Style cho Tiêu đề chính (Merge cell, to, đậm, nền xanh)
                Action<ExcelRange> styleMainTitle = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 16;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#333366")); // Xanh đậm
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                };

                // Style cho Tiêu đề phụ (Merge cell, nghiêng)
                Action<ExcelRange> styleSubTitle = (range) =>
                {
                    range.Merge = true;
                    range.Style.Font.Italic = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                };

                // --- 2. SHEET 1: TỔNG QUAN (Sheet MỚI) ---
                var wsTongQuan = package.Workbook.Worksheets.Add("Tổng Quan Hiệu Suất");

                // Tiêu đề
                wsTongQuan.Cells["A1:D1"].Value = "BÁO CÁO TỔNG QUAN HIỆU SUẤT";
                styleMainTitle(wsTongQuan.Cells["A1:D1"]);

                wsTongQuan.Cells["A2:D2"].Value = $"Báo cáo cho kỳ từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}";
                styleSubTitle(wsTongQuan.Cells["A2:D2"]);

                // Hiển thị KPIs
                wsTongQuan.Cells["A4"].Value = "CHỈ SỐ KPI TOÀN QUÁN";
                wsTongQuan.Cells["A4:B4"].Merge = true;
                wsTongQuan.Cells["A4:B4"].Style.Font.Bold = true;
                wsTongQuan.Cells["A4:B4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsTongQuan.Cells["A4:B4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                wsTongQuan.Cells["A5"].Value = "Tổng Doanh Thu";
                wsTongQuan.Cells["B5"].Value = data.Kpi.TongDoanhThu;
                wsTongQuan.Cells["B5"].Style.Numberformat.Format = currencyFormat;

                wsTongQuan.Cells["A6"].Value = "Tổng Giờ Làm";
                wsTongQuan.Cells["B6"].Value = data.Kpi.TongGioLam;
                wsTongQuan.Cells["B6"].Style.Numberformat.Format = decimalFormat;

                wsTongQuan.Cells["A7"].Value = "Tổng Số Ca Làm";
                wsTongQuan.Cells["B7"].Value = data.Kpi.TongSoCaLam;
                wsTongQuan.Cells["B7"].Style.Numberformat.Format = numberFormat;

                wsTongQuan.Cells["A8"].Value = "Tổng Lần Hủy Món";
                wsTongQuan.Cells["B8"].Value = data.Kpi.TongLanHuyMon;
                wsTongQuan.Cells["B8"].Style.Numberformat.Format = numberFormat;
                if (data.Kpi.TongLanHuyMon > 0)
                {
                    wsTongQuan.Cells["A8:B8"].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    wsTongQuan.Cells["A8:B8"].Style.Font.Bold = true;
                }

                // CHỈ SỐ MỚI (QUAN TRỌNG)
                decimal doanhThuPerGioLam = (data.Kpi.TongGioLam > 0) ? (data.Kpi.TongDoanhThu / (decimal)data.Kpi.TongGioLam) : 0;
                wsTongQuan.Cells["A10"].Value = "HIỆU SUẤT (Doanh Thu / Giờ)";
                wsTongQuan.Cells["B10"].Value = doanhThuPerGioLam;
                wsTongQuan.Cells["B10"].Style.Numberformat.Format = currencyFormat;
                wsTongQuan.Cells["A10:B10"].Style.Font.Bold = true;
                wsTongQuan.Cells["A10:B10"].Style.Font.Color.SetColor(System.Drawing.Color.Green);

                // Kẻ viền
                wsTongQuan.Cells["A4:B8"].Style.Border.BorderAround(ExcelBorderStyle.Medium);
                wsTongQuan.Cells["A10:B10"].Style.Border.BorderAround(ExcelBorderStyle.Medium);

                wsTongQuan.Cells[wsTongQuan.Dimension.Address].AutoFitColumns();

                // --- 3. SHEET 2: HIỆU SUẤT BÁN HÀNG ---
                var ws1 = package.Workbook.Worksheets.Add("Hiệu Suất Bán Hàng");
                ws1.Cells["A1:F1"].Value = "Báo cáo Hiệu suất Bán hàng (Thu ngân, Phục vụ)";
                styleMainTitle(ws1.Cells["A1:F1"]);

                ws1.Cells["A3"].LoadFromCollection(data.SalesPerformance, true, OfficeOpenXml.Table.TableStyles.Medium9);
                var tbl1 = ws1.Tables[0];

                // Định dạng cột
                ws1.Cells[tbl1.Address.Start.Row + 1, tbl1.Columns["TongDoanhThu"].Position + 1, tbl1.Address.End.Row, tbl1.Columns["TongDoanhThu"].Position + 1].Style.Numberformat.Format = currencyFormat;
                ws1.Cells[tbl1.Address.Start.Row + 1, tbl1.Columns["SoHoaDon"].Position + 1, tbl1.Address.End.Row, tbl1.Columns["SoHoaDon"].Position + 1].Style.Numberformat.Format = numberFormat;
                ws1.Cells[tbl1.Address.Start.Row + 1, tbl1.Columns["DoanhThuTrungBinh"].Position + 1, tbl1.Address.End.Row, tbl1.Columns["DoanhThuTrungBinh"].Position + 1].Style.Numberformat.Format = currencyFormat;

                // Định dạng có điều kiện: Tô đỏ cột "Hủy Món" nếu > 0
                var huyMonColRange = ws1.Cells[tbl1.Address.Start.Row + 1, tbl1.Columns["SoLanHuyMon"].Position + 1, tbl1.Address.End.Row, tbl1.Columns["SoLanHuyMon"].Position + 1];
                huyMonColRange.Style.Numberformat.Format = numberFormat;
                var cfRuleHuyMon = huyMonColRange.ConditionalFormatting.AddGreaterThan();
                cfRuleHuyMon.Formula = "0";
                cfRuleHuyMon.Style.Font.Color.SetColor(System.Drawing.Color.Red);
                cfRuleHuyMon.Style.Font.Bold = true;

                ws1.Cells[ws1.Dimension.Address].AutoFitColumns();

                // --- 4. SHEET 3: HIỆU SUẤT VẬN HÀNH (Kho, Quản lý) ---
                var ws2 = package.Workbook.Worksheets.Add("Hiệu Suất Vận Hành");
                ws2.Cells["A1:F1"].Value = "Báo cáo Hiệu suất Vận hành (Kho, Quản lý)";
                styleMainTitle(ws2.Cells["A1:F1"]);

                ws2.Cells["A3"].LoadFromCollection(data.OperationalPerformance, true, OfficeOpenXml.Table.TableStyles.Medium10);
                var tbl2 = ws2.Tables[0];

                // Định dạng số
                ws2.Cells[tbl2.Address.Start.Row + 1, 3, tbl2.Address.End.Row, tbl2.Address.End.Column].Style.Numberformat.Format = numberFormat;

                ws2.Cells[ws2.Dimension.Address].AutoFitColumns();

                // --- 5. SHEET 4: CHUYÊN CẦN ---
                var ws3 = package.Workbook.Worksheets.Add("Báo Cáo Chuyên Cần");
                ws3.Cells["A1:F1"].Value = "Báo cáo Chuyên cần & Vi phạm";
                styleMainTitle(ws3.Cells["A1:F1"]);

                ws3.Cells["A3"].LoadFromCollection(data.Attendance, true, OfficeOpenXml.Table.TableStyles.Medium11);
                var tbl3 = ws3.Tables[0];

                // Định dạng cột
                ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["TongGioLam"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["TongGioLam"].Position + 1].Style.Numberformat.Format = decimalFormat;
                ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["TongSoCaLam"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["TongSoCaLam"].Position + 1].Style.Numberformat.Format = numberFormat;
                ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["SoDonXinNghi"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["SoDonXinNghi"].Position + 1].Style.Numberformat.Format = numberFormat;
                ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["SoDonDaDuyet"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["SoDonDaDuyet"].Position + 1].Style.Numberformat.Format = numberFormat;

                // Định dạng có điều kiện: Tô vàng cột "Chờ Duyệt" nếu > 0 (để quản lý chú ý)
                var choDuyetColRange = ws3.Cells[tbl3.Address.Start.Row + 1, tbl3.Columns["SoDonChoDuyet"].Position + 1, tbl3.Address.End.Row, tbl3.Columns["SoDonChoDuyet"].Position + 1];
                choDuyetColRange.Style.Numberformat.Format = numberFormat;
                var cfRuleChoDuyet = choDuyetColRange.ConditionalFormatting.AddGreaterThan();
                cfRuleChoDuyet.Formula = "0";
                cfRuleChoDuyet.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cfRuleChoDuyet.Style.Fill.BackgroundColor.Color = System.Drawing.Color.LightGoldenrodYellow;
                cfRuleChoDuyet.Style.Font.Bold = true;

                ws3.Cells[ws3.Dimension.Address].AutoFitColumns();

                // --- 6. LƯU FILE ---
                await package.SaveAsync();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}