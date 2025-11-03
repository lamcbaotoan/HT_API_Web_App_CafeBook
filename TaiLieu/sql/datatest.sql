/*
================================================================================
 KỊCH BẢN CHÈN DỮ LIỆU MẪU TOÀN DIỆN (v4 - SỬA LỖI)
================================================================================
 - Sửa lỗi Msg 110 (Cột/Giá trị không khớp) ở bảng [TacGia].
 - Sửa lỗi Msg 242 (Lỗi CAST DateTime) bằng định dạng ISO 8601.
 - Sắp xếp lại thứ tự DELETE và thêm DBCC CHECKIDENT để đảm bảo chạy lại được.
*/

USE [CAFEBOOKDB_v2]
GO
SET DATEFORMAT dmy
GO

/*
================================================================================
 PHẦN 0: DỌN DẸP DỮ LIỆU CŨ (Sắp xếp lại thứ tự)
================================================================================
*/
PRINT N'PHẦN 0: Bắt đầu dọn dẹp dữ liệu cũ...';
GO

-- 1. Xóa các bảng Transaction (phụ thuộc nhiều nhất)
IF OBJECT_ID('dbo.GiaoDichThanhToan', 'U') IS NOT NULL DELETE FROM [dbo].[GiaoDichThanhToan];
IF OBJECT_ID('dbo.ChiTietHoaDon', 'U') IS NOT NULL DELETE FROM [dbo].[ChiTietHoaDon];
IF OBJECT_ID('dbo.NhatKyHuyMon', 'U') IS NOT NULL DELETE FROM [dbo].[NhatKyHuyMon];
IF OBJECT_ID('dbo.ChiTietPhuThuHoaDon', 'U') IS NOT NULL DELETE FROM [dbo].[ChiTietPhuThuHoaDon];
IF OBJECT_ID('dbo.HoaDon_KhuyenMai', 'U') IS NOT NULL DELETE FROM [dbo].[HoaDon_KhuyenMai];
IF OBJECT_ID('dbo.BangChamCong', 'U') IS NOT NULL DELETE FROM [dbo].[BangChamCong];
IF OBJECT_ID('dbo.PhieuThuongPhat', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuThuongPhat];
IF OBJECT_ID('dbo.ChiTietNhapKho', 'U') IS NOT NULL DELETE FROM [dbo].[ChiTietNhapKho];
IF OBJECT_ID('dbo.ChiTietKiemKho', 'U') IS NOT NULL DELETE FROM [dbo].[ChiTietKiemKho];
IF OBJECT_ID('dbo.ChiTietXuatHuy', 'U') IS NOT NULL DELETE FROM [dbo].[ChiTietXuatHuy];
IF OBJECT_ID('dbo.ChiTietPhieuThue', 'U') IS NOT NULL DELETE FROM [dbo].[ChiTietPhieuThue];
IF OBJECT_ID('dbo.DinhLuong', 'U') IS NOT NULL DELETE FROM [dbo].[DinhLuong];
IF OBJECT_ID('dbo.DeXuatSanPham', 'U') IS NOT NULL DELETE FROM [dbo].[DeXuatSanPham];

-- 2. Xóa các bảng Master (sau khi transaction đã xóa)
IF OBJECT_ID('dbo.HoaDon', 'U') IS NOT NULL DELETE FROM [dbo].[HoaDon];
IF OBJECT_ID('dbo.DonXinNghi', 'U') IS NOT NULL DELETE FROM [dbo].[DonXinNghi];
IF OBJECT_ID('dbo.LichLamViec', 'U') IS NOT NULL DELETE FROM [dbo].[LichLamViec];
IF OBJECT_ID('dbo.PhieuLuong', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuLuong];
IF OBJECT_ID('dbo.PhieuNhapKho', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuNhapKho];
IF OBJECT_ID('dbo.PhieuKiemKho', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuKiemKho];
IF OBJECT_ID('dbo.PhieuXuatHuy', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuXuatHuy];
IF OBJECT_ID('dbo.PhieuDatBan', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuDatBan];
IF OBJECT_ID('dbo.Ban', 'U') IS NOT NULL DELETE FROM [dbo].[Ban];
IF OBJECT_ID('dbo.KhuVuc', 'U') IS NOT NULL DELETE FROM [dbo].[KhuVuc];
IF OBJECT_ID('dbo.KhuyenMai', 'U') IS NOT NULL DELETE FROM [dbo].[KhuyenMai];
IF OBJECT_ID('dbo.SanPham', 'U') IS NOT NULL DELETE FROM [dbo].[SanPham];
IF OBJECT_ID('dbo.DanhMuc', 'U') IS NOT NULL DELETE FROM [dbo].[DanhMuc];
IF OBJECT_ID('dbo.DonViChuyenDoi', 'U') IS NOT NULL DELETE FROM [dbo].[DonViChuyenDoi];
IF OBJECT_ID('dbo.NguyenLieu', 'U') IS NOT NULL DELETE FROM [dbo].[NguyenLieu];
IF OBJECT_ID('dbo.NhaCungCap', 'U') IS NOT NULL DELETE FROM [dbo].[NhaCungCap];
IF OBJECT_ID('dbo.PhieuThueSach', 'U') IS NOT NULL DELETE FROM [dbo].[PhieuThueSach];
IF OBJECT_ID('dbo.DeXuatSach', 'U') IS NOT NULL DELETE FROM [dbo].[DeXuatSach];
IF OBJECT_ID('dbo.Sach', 'U') IS NOT NULL DELETE FROM [dbo].[Sach];
IF OBJECT_ID('dbo.TheLoai', 'U') IS NOT NULL DELETE FROM [dbo].[TheLoai];
IF OBJECT_ID('dbo.TacGia', 'U') IS NOT NULL DELETE FROM [dbo].[TacGia];
IF OBJECT_ID('dbo.NhaXuatBan', 'U') IS NOT NULL DELETE FROM [dbo].[NhaXuatBan];
IF OBJECT_ID('dbo.PhuThu', 'U') IS NOT NULL DELETE FROM [dbo].[PhuThu];
IF OBJECT_ID('dbo.NguoiGiaoHang', 'U') IS NOT NULL DELETE FROM [dbo].[NguoiGiaoHang];
IF OBJECT_ID('dbo.ChatLichSu', 'U') IS NOT NULL DELETE FROM [dbo].[ChatLichSu];
IF OBJECT_ID('dbo.ThongBao', 'U') IS NOT NULL DELETE FROM [dbo].[ThongBao];
IF OBJECT_ID('dbo.VaiTro_Quyen', 'U') IS NOT NULL DELETE FROM [dbo].[VaiTro_Quyen];
IF OBJECT_ID('dbo.NhanVien', 'U') IS NOT NULL DELETE FROM [dbo].[NhanVien];
IF OBJECT_ID('dbo.VaiTro', 'U') IS NOT NULL DELETE FROM [dbo].[VaiTro];
IF OBJECT_ID('dbo.Quyen', 'U') IS NOT NULL DELETE FROM [dbo].[Quyen];
IF OBJECT_ID('dbo.CaLamViec', 'U') IS NOT NULL DELETE FROM [dbo].[CaLamViec];
IF OBJECT_ID('dbo.KhachHang', 'U') IS NOT NULL DELETE FROM [dbo].[KhachHang];
IF OBJECT_ID('dbo.CaiDat', 'U') IS NOT NULL DELETE FROM [dbo].[CaiDat];

-- 3. Reset Identity (PK) cho các bảng
IF OBJECT_ID('dbo.VaiTro', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[VaiTro]', RESEED, 0);
IF OBJECT_ID('dbo.NhanVien', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[NhanVien]', RESEED, 0);
IF OBJECT_ID('dbo.CaLamViec', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[CaLamViec]', RESEED, 0);
IF OBJECT_ID('dbo.DonXinNghi', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[DonXinNghi]', RESEED, 0);
IF OBJECT_ID('dbo.KhuVuc', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[KhuVuc]', RESEED, 0);
IF OBJECT_ID('dbo.Ban', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[Ban]', RESEED, 0);
IF OBJECT_ID('dbo.KhachHang', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[KhachHang]', RESEED, 0);
IF OBJECT_ID('dbo.NguyenLieu', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[NguyenLieu]', RESEED, 0);
IF OBJECT_ID('dbo.DonViChuyenDoi', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[DonViChuyenDoi]', RESEED, 0);
IF OBJECT_ID('dbo.DanhMuc', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[DanhMuc]', RESEED, 0);
IF OBJECT_ID('dbo.SanPham', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[SanPham]', RESEED, 0);
IF OBJECT_ID('dbo.TheLoai', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[TheLoai]', RESEED, 0);
IF OBJECT_ID('dbo.TacGia', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[TacGia]', RESEED, 0);
IF OBJECT_ID('dbo.NhaXuatBan', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[NhaXuatBan]', RESEED, 0);
IF OBJECT_ID('dbo.Sach', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[Sach]', RESEED, 0);
IF OBJECT_ID('dbo.PhuThu', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhuThu]', RESEED, 0);
IF OBJECT_ID('dbo.NguoiGiaoHang', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[NguoiGiaoHang]', RESEED, 0);
IF OBJECT_ID('dbo.NhaCungCap', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[NhaCungCap]', RESEED, 0);
IF OBJECT_ID('dbo.HoaDon', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[HoaDon]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuLuong', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuLuong]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuThuongPhat', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuThuongPhat]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuThueSach', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuThueSach]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuNhapKho', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuNhapKho]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuKiemKho', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuKiemKho]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuXuatHuy', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuXuatHuy]', RESEED, 0);
IF OBJECT_ID('dbo.PhieuDatBan', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[PhieuDatBan]', RESEED, 0);
IF OBJECT_ID('dbo.ThongBao', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[ThongBao]', RESEED, 0);
IF OBJECT_ID('dbo.KhuyenMai', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[KhuyenMai]', RESEED, 0);
IF OBJECT_ID('dbo.ChatLichSu', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[ChatLichSu]', RESEED, 0);
IF OBJECT_ID('dbo.GiaoDichThanhToan', 'U') IS NOT NULL DBCC CHECKIDENT ('[dbo].[GiaoDichThanhToan]', RESEED, 0);

PRINT N'PHẦN 0: Dọn dẹp dữ liệu cũ và RESET ID thành công.';
GO

/*
================================================================================
 PHẦN 1: DỮ LIỆU CẤU HÌNH & NHÂN SỰ (TIER 0 & 1)
================================================================================
*/
PRINT N'PHẦN 1: Bắt đầu chèn dữ liệu Nhân sự & Cấu hình...';
GO

-- 1.1. Bảng [CaiDat] (Module 7)
-- 2. Chèn 20 dòng cài đặt CHUNG, SÁCH, AI, ĐIỂM TÍCH LŨY (Từ file của bạn)
INSERT INTO [dbo].[CaiDat] ([tenCaiDat], [giaTri], [moTa]) VALUES
(N'AI_Chat_API_Key', N'sk-xxxxxxxxxxxxxxxxxxxx', N'API Key cho dịch vụ (OpenAI, Gemini...)'),
(N'AI_Chat_Endpoint', N'https://api.openai.com/v1/chat/completions', N'Endpoint của dịch vụ AI Chat'),
(N'DiaChi', N'08 Hà Văn Tín, P. Hòa Khánh Nam, Q. Liên Chiểu, TP. Đà Nẵng', N'Địa chỉ in trên hóa đơn'),
(N'DiemTichLuy_DoiVND', N'1000', N'1 điểm tích lũy bằng ... VND trừ vào hóa đơn'),
(N'DiemTichLuy_NhanVND', N'10000', N'Mỗi ... VND trong hóa đơn được 1 điểm'),
(N'GioiThieu', N'Cafebook là không gian lý tưởng, kết hợp giữa niềm đam mê cà phê và tình yêu sách. Chúng tôi mang đến những hạt cà phê chất lượng cùng hàng ngàn đầu sách chọn lọc, tạo nên một ốc đảo bình yên cho tâm hồn bạn. test', N'Giới thiệu được hiển thị ở trên web'),
(N'LienHe_Email', N'cafebook.hotro@gmail.com', N'Gmail của quán'),
(N'LienHe_Facebook', N'https://www.facebook.com/lamtoan24/', N'Link Facebook quán (Đã sửa lỗi typo Facbook)'),
(N'LienHe_GioMoCua', N'Thứ 2 - CN: 06:00 - 23:00', N'Giờ mở cửa quán'),
(N'LienHe_GoogleMapsEmbed', N'https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3919.106598502801!2d106.7010418153489!3d10.80311546168051!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x317528a459b2184f%3A0x805d52140130f4d3!2zVHLGsOG7nW5nIMSQ4bqhaSBo4buNYyBIw7luZyBCw6BuZw!5e0!3m2!1svi!2s!4v1678888888888!5m2!1svi!2s', N'Link GoogleMapsEmbed Quán'),
(N'LienHe_Instagram', N'https://instagram.com/lamtoan24', N'Link Instagram quán'),
(N'LienHe_Website', N'https://cafebook.vn', N'Website quán'),
(N'LienHe_X', N'https://x.com/', N'Link X quán'),
(N'LienHe_Youtube', N'https://www.youtube.com/@Shu.otaku.t', N'Link Youtube quán'),
(N'LienHe_Zalo', N'https://id.zalo.me/account?continue=https%3A%2F%2Fchat.zalo.me%2F', N'Zalo Quán'),
(N'Sach_PhiThue', N'15000', N'Phí dịch vụ thuê sách được trừ sau khi trả sách'),
(N'Sach_PhiTraTreMoiNgay', N'5000', N'Số tiền (VND) phạt nếu khách trả sách trễ 1 ngày'),
(N'SoDienThoai', N'0376512695', N'Số Điện Thoại Liên Hệ'),
(N'TenQuan', N'Cafe Sách Bookshuheheee', N'Tên quán hiển thị trên hóa đơn, trang web'),
(N'Wifi_MatKhau', N'Shu.0311', N'Mật khẩu Wifi cho khách');
PRINT N'Đã chèn 20 dòng cài đặt chung/web/sách.';

-- 3. Chèn 7 dòng cài đặt Nhân sự (HR) (Từ Module 7)
INSERT INTO [dbo].[CaiDat] (tenCaiDat, giaTri, moTa) VALUES
(N'HR_GioLamChuan', '8', N'Số giờ làm việc chuẩn mỗi ngày (ví dụ: 8 tiếng)'),
(N'HR_HeSoOT', '1.5', N'Hệ số lương khi làm tăng ca (Overtime) (ví dụ: 1.5)'),
(N'HR_PhatDiTre_Phut', '5', N'Số phút cho phép đi trễ. Vượt quá ngưỡng này bắt đầu tính phạt.'),
(N'HR_PhatDiTre_HeSo', '1.0', N'Hệ số phạt khi đi trễ (ví dụ: 1.0 = trễ 1 giờ phạt 1 giờ lương)'),
(N'HR_ChuyenCan_SoNgay', '26', N'Số ngày công yêu cầu để đạt thưởng chuyên cần.'),
(N'HR_ChuyenCan_TienThuong', '500000', N'Số tiền thưởng (VND) khi đạt chuyên cần.'),
(N'HR_PhepNam_MacDinh', '12', N'Số ngày nghỉ phép có lương mặc định cho nhân viên mỗi năm.');
PRINT N'Đã chèn 7 dòng cài đặt nhân sự.';

PRINT N'================================================'
PRINT N'Khôi phục Bảng [CaiDat] thành công. Đã chèn tổng cộng 27 dòng.';
GO

-- 1.2. Bảng [VaiTro] (Module 2)
SET IDENTITY_INSERT [dbo].[VaiTro] ON;
INSERT INTO [dbo].[VaiTro] ([idVaiTro], [tenVaiTro], [moTa]) VALUES
(1, N'Quản trị viên', N'Toàn quyền hệ thống'),
(2, N'Quản lý', N'Quản lý vận hành chung, nhân sự, kho'),
(3, N'Thu ngân', N'Lập hóa đơn, thanh toán, quản lý tiền mặt'),
(4, N'Phục vụ', N'Ghi order, phục vụ bàn'),
(5, N'Pha chế', N'Pha chế đồ uống, quản lý quầy bar');
SET IDENTITY_INSERT [dbo].[VaiTro] OFF;
PRINT N'Đã thêm 5 Vai Trò.';

-- 1.3. Bảng [NhanVien] (Module 1)
SET IDENTITY_INSERT [dbo].[NhanVien] ON;
INSERT INTO [dbo].[NhanVien] ([idNhanVien], [hoTen], [soDienThoai], [email], [ngayVaoLam], [idVaiTro], [luongCoBan], [trangThaiLamViec], [tenDangNhap], [matKhau], [AnhDaiDien]) VALUES
(1, N'Lâm Chu Bảo Toàn', N'0900000001', N'admin@cafebook.com', CAST('2024-01-01' AS Date), 1, 50000.00, N'Đang làm việc', N'admin', N'123456', NULL),
(2, N'Nguyễn Văn Quản Lý', N'0900000002', N'manager@cafebook.com', CAST('2024-03-15' AS Date), 2, 45000.00, N'Đang làm việc', N'manager', N'123456', NULL),
(3, N'Trần Thị Thu Ngân', N'0900000003', N'cashier@cafebook.com', CAST('2024-05-10' AS Date), 3, 30000.00, N'Đang làm việc', N'thungan', N'123456', NULL),
(4, N'Lê Văn Phục Vụ', N'0900000004', N'waiter@cafebook.com', CAST('2024-07-20' AS Date), 4, 25000.00, N'Đang làm việc', N'phucvu', N'123456', NULL),
(5, N'Phạm Thị Pha Chế', N'0900000005', N'barista@cafebook.com', CAST('2024-02-01' AS Date), 5, 35000.00, N'Đang làm việc', N'phache', N'123456', NULL),
(6, N'Ngô Thị Đã Nghỉ', N'0900000006', N'ex@cafebook.com', CAST('2024-01-05' AS Date), 4, 25000.00, N'Nghỉ việc', N'nghiviec', N'123456', NULL);
SET IDENTITY_INSERT [dbo].[NhanVien] OFF;
PRINT N'Đã thêm 6 Nhân Viên.';

-- 1.4. Bảng [Quyen] (Module 3)
INSERT INTO [dbo].[Quyen] ([idQuyen], [TenQuyen], [NhomQuyen]) VALUES
(N'BanHang.ThanhToan', N'Thanh toán hóa đơn', N'Bán Hàng'),
(N'BanHang.XemSoDo', N'Xem sơ đồ bàn', N'Bán Hàng'),
(N'BaoCao.Xem', N'Xem báo cáo', N'Báo Cáo'),
(N'CaiDat.Sua', N'Sửa cài đặt hệ thống', N'Cài Đặt'),
(N'Kho.KiemKe', N'Kiểm kê kho', N'Kho'),
(N'Kho.Nhap', N'Nhập kho', N'Kho'),
(N'Kho.Xem', N'Xem tồn kho', N'Kho'),
(N'NhanSu.QuanLy', N'Quản lý nhân viên, lịch, đơn', N'Nhân Sự'),
(N'NhanSu.TinhLuong', N'Tính và chốt lương', N'Nhân Sự');
PRINT N'Đã thêm 9 Quyền.';

-- 1.5. Bảng [VaiTro_Quyen] (Module 3)
INSERT INTO [dbo].[VaiTro_Quyen] ([idVaiTro], [idQuyen]) VALUES
(1, N'BanHang.ThanhToan'),(1, N'BanHang.XemSoDo'),(1, N'BaoCao.Xem'),(1, N'CaiDat.Sua'),(1, N'Kho.KiemKe'),(1, N'Kho.Nhap'),(1, N'Kho.Xem'),(1, N'NhanSu.QuanLy'),(1, N'NhanSu.TinhLuong'),
(2, N'BanHang.ThanhToan'),(2, N'BanHang.XemSoDo'),(2, N'BaoCao.Xem'),(2, N'Kho.KiemKe'),(2, N'Kho.Nhap'),(2, N'Kho.Xem'),(2, N'NhanSu.QuanLy'),(2, N'NhanSu.TinhLuong'),
(3, N'BanHang.ThanhToan'),(3, N'BanHang.XemSoDo'),
(4, N'BanHang.XemSoDo'),
(5, N'Kho.Xem');
PRINT N'Đã phân quyền cho 5 Vai Trò.';

-- 1.6. Bảng [CaLamViec] (Module 4)
SET IDENTITY_INSERT [dbo].[CaLamViec] ON;
INSERT INTO [dbo].[CaLamViec] ([idCa], [tenCa], [gioBatDau], [gioKetThuc]) VALUES
(1, N'Ca Sáng', CAST(N'07:00:00' AS Time), CAST(N'15:00:00' AS Time)),
(2, N'Ca Tối', CAST(N'15:00:00' AS Time), CAST(N'23:00:00' AS Time)),
(3, N'Ca Gãy (Sáng)', CAST(N'09:00:00' AS Time), CAST(N'13:00:00' AS Time)),
(4, N'Ca Hành Chính', CAST(N'08:00:00' AS Time), CAST(N'17:00:00' AS Time)),
(5, N'Ca Part-time Tối', CAST(N'18:00:00' AS Time), CAST(N'23:00:00' AS Time));
SET IDENTITY_INSERT [dbo].[CaLamViec] OFF;
PRINT N'Đã thêm 5 Ca Làm Việc mẫu.';

-- 1.7. Bảng [LichLamViec] (Module 4)
INSERT INTO [dbo].[LichLamViec] ([idNhanVien], [idCa], [ngayLam]) VALUES
(2, 4, CAST('20251103' AS Date)), -- Quản lý (Hành chính)
(3, 1, CAST('20251103' AS Date)), -- Thu ngân (Sáng)
(4, 2, CAST('20251103' AS Date)), -- Phục vụ (Tối)
(5, 1, CAST('20251103' AS Date)), -- Pha chế (Sáng)
(2, 4, CAST('20251104' AS Date)); -- Quản lý (Mai)
PRINT N'Đã thêm 5 Lịch Làm Việc.';

-- 1.8. Bảng [DonXinNghi] (Module 5)
SET IDENTITY_INSERT [dbo].[DonXinNghi] ON;
INSERT INTO [dbo].[DonXinNghi] ([idDonXinNghi], [idNhanVien], [LoaiDon], [LyDo], [NgayBatDau], [NgayKetThuc], [TrangThai], [idNguoiDuyet], [NgayDuyet], [GhiChuPheDuyet]) VALUES
(1, 3, N'Nghỉ có phép', N'Việc gia đình', CAST(N'2025-11-05T00:00:00' AS DateTime), CAST(N'2025-11-05T00:00:00' AS DateTime), N'Chờ duyệt', NULL, NULL, NULL),
(2, 4, N'Nghỉ bệnh', N'Cảm cúm', CAST(N'2025-11-04T00:00:00' AS DateTime), CAST(N'2025-11-04T00:00:00' AS DateTime), N'Đã duyệt', 2, CAST(N'2025-11-02T00:00:00' AS DateTime), N'OK'),
(3, 5, N'Nghỉ không phép', N'Bận đột xuất', CAST(N'2025-11-01T00:00:00' AS DateTime), CAST(N'2025-11-01T00:00:00' AS DateTime), N'Đã từ chối', 2, CAST(N'2025-11-01T00:00:00' AS DateTime), N'Không hợp lệ'),
(4, 2, N'Nghỉ có phép', N'Về quê', CAST(N'2025-11-10T00:00:00' AS DateTime), CAST(N'2025-11-12T00:00:00' AS DateTime), N'Chờ duyệt', NULL, NULL, NULL),
(5, 3, N'Nghỉ có phép', N'Khám bệnh', CAST(N'2025-11-01T00:00:00' AS DateTime), CAST(N'2025-11-01T00:00:00' AS DateTime), N'Đã duyệt', 2, CAST(N'2025-11-01T00:00:00' AS DateTime), N'');
SET IDENTITY_INSERT [dbo].[DonXinNghi] OFF;
PRINT N'Đã thêm 5 Đơn Xin Nghỉ.';
GO

/*
================================================================================
 PHẦN 2: DỮ LIỆU KINH DOANH (TIER 0 & 1)
================================================================================
*/
PRINT N'PHẦN 2: Bắt đầu chèn dữ liệu Kinh doanh...';
GO

-- 2.1. Bảng [KhuVuc]
SET IDENTITY_INSERT [dbo].[KhuVuc] ON;
INSERT INTO [dbo].[KhuVuc] ([idKhuVuc], [TenKhuVuc], [MoTa]) VALUES
(1, N'Tầng 1 (Trong nhà)', N'Khu vực máy lạnh, bàn ghế thấp'),
(2, N'Tầng 2 (Yên tĩnh)', N'Khu vực đọc sách, làm việc'),
(3, N'Sân vườn', N'Khu vực ngoài trời, thoáng mát'),
(4, N'Giao hàng (App)', N'Khu vực ảo cho đơn hàng online'),
(5, N'Mang về (Tại quầy)', N'Khu vực ảo cho đơn mang về');
SET IDENTITY_INSERT [dbo].[KhuVuc] OFF;
PRINT N'Đã thêm 5 Khu Vực.';

-- 2.2. Bảng [Ban]
SET IDENTITY_INSERT [dbo].[Ban] ON;
INSERT INTO [dbo].[Ban] ([idBan], [soBan], [soGhe], [trangThai], [ghiChu], [idKhuVuc]) VALUES
(1, N'Bàn 101', 4, N'Có khách', NULL, 1),
(2, N'Bàn 102', 2, N'Trống', NULL, 1),
(3, N'Bàn 201 (Cửa sổ)', 2, N'Trống', NULL, 2),
(4, N'Bàn 202 (Nhóm)', 8, N'Đã đặt', N'Khách đặt lúc 8h tối', 2),
(5, N'Bàn V01', 4, N'Bảo trì', N'Gãy chân bàn', 3);
SET IDENTITY_INSERT [dbo].[Ban] OFF;
PRINT N'Đã thêm 5 Bàn.';

-- 2.3. Bảng [KhachHang]
SET IDENTITY_INSERT [dbo].[KhachHang] ON;
INSERT INTO [dbo].[KhachHang] ([idKhachHang], [hoTen], [soDienThoai], [email], [diaChi], [diemTichLuy], [tenDangNhap], [matKhau], [ngayTao], [BiKhoa], [AnhDaiDien]) VALUES
(1, N'Khách vãng lai', N'0000000000', N'vanglai@cafebook.com', NULL, 0, N'vanglai', N'123', CAST('2024-01-01T00:00:00' AS DateTime), 0, NULL),
(2, N'Trần Văn An', N'0987654321', N'an.tran@gmail.com', N'1 Lý Tự Trọng, Q1', 150, N'an.tran', N'123456', CAST('2024-05-10T00:00:00' AS DateTime), 0, NULL),
(3, N'Lê Thị Bình', N'0912345678', N'binh.le@gmail.com', N'2 Võ Thị Sáu, Q3', 50, N'binh.le', N'123456', CAST('2024-07-15T00:00:00' AS DateTime), 0, NULL),
(4, N'Phạm Hùng Cường', N'0903111222', N'cuong.pham@gmail.com', N'3 Hai Bà Trưng, Q1', 0, N'cuong.pham', N'123456', CAST('2024-09-01T00:00:00' AS DateTime), 0, NULL),
(5, N'Đặng Thu Duyên', N'0908555666', N'duyen.dang@gmail.com', N'4 Nguyễn Văn Cừ, Q5', 300, N'duyen.dang', N'123456', CAST('2024-02-20T00:00:00' AS DateTime), 0, NULL);
SET IDENTITY_INSERT [dbo].[KhachHang] OFF;
PRINT N'Đã thêm 5 Khách Hàng.';

-- 2.4. Bảng [NguyenLieu]
SET IDENTITY_INSERT [dbo].[NguyenLieu] ON;
INSERT INTO [dbo].[NguyenLieu] ([idNguyenLieu], [tenNguyenLieu], [donViTinh], [tonKho], [TonKhoToiThieu]) VALUES
(1, N'Hạt Cà Phê Robusta (VN)', N'kg', 10.50, 2.00),
(2, N'Hạt Cà Phê Arabica (Nhập)', N'kg', 5.00, 1.00),
(3, N'Sữa đặc Ông Thọ (lon)', N'lon', 50.00, 10.00),
(4, N'Sữa tươi thanh trùng', N'lít', 12.00, 5.00),
(5, N'Đường cát trắng', N'kg', 20.00, 5.00),
(6, N'Trà túi lọc Lipton', N'túi', 100.00, 20.00),
(7, N'Siro Đào', N'chai', 8.00, 2.00);
SET IDENTITY_INSERT [dbo].[NguyenLieu] OFF;
PRINT N'Đã thêm 7 Nguyên Liệu.';

-- 2.5. Bảng [DonViChuyenDoi]
SET IDENTITY_INSERT [dbo].[DonViChuyenDoi] ON;
INSERT INTO [dbo].[DonViChuyenDoi] ([idChuyenDoi], [idNguyenLieu], [TenDonVi], [GiaTriQuyDoi], [LaDonViCoBan]) VALUES
(1, 1, N'kg', 1.000000, 1),
(2, 1, N'g', 0.001000, 0),
(3, 3, N'lon', 1.000000, 1),
(4, 3, N'ml', 0.002630, 0), -- 1 lon = 380g ~ 380ml
(5, 4, N'lít', 1.000000, 1),
(6, 4, N'ml', 0.001000, 0),
(7, 5, N'kg', 1.000000, 1),
(8, 5, N'g', 0.001000, 0),
(9, 6, N'túi', 1.000000, 1);
SET IDENTITY_INSERT [dbo].[DonViChuyenDoi] OFF;
PRINT N'Đã thêm 9 Đơn Vị Chuyển Đổi.';

-- 2.6. Bảng [DanhMuc]
SET IDENTITY_INSERT [dbo].[DanhMuc] ON;
INSERT INTO [dbo].[DanhMuc] ([idDanhMuc], [tenDanhMuc], [idDanhMucCha]) VALUES
(1, N'Cà Phê', NULL),
(2, N'Trà & Thức uống khác', NULL),
(3, N'Đồ Ăn Kèm', NULL),
(4, N'Cà Phê Việt Nam', 1),
(5, N'Cà Phê Máy (Ý)', 1);
SET IDENTITY_INSERT [dbo].[DanhMuc] OFF;
PRINT N'Đã thêm 5 Danh Mục.';

-- 2.7. Bảng [SanPham]
SET IDENTITY_INSERT [dbo].[SanPham] ON;
INSERT INTO [dbo].[SanPham] ([idSanPham], [tenSanPham], [idDanhMuc], [giaBan], [trangThaiKinhDoanh], [NhomIn]) VALUES
(1, N'Cà Phê Đen (Nóng/Đá)', 4, 35000.00, 1, N'Quầy Bar'),
(2, N'Bạc Xỉu', 4, 45000.00, 1, N'Quầy Bar'),
(3, N'Latte Nóng', 5, 55000.00, 1, N'Quầy Bar'),
(4, N'Trà Đào Cam Sả', 2, 50000.00, 1, N'Quầy Bar'),
(5, N'Bánh Croissant Bơ', 3, 30000.00, 1, N'Quầy Bếp');
SET IDENTITY_INSERT [dbo].[SanPham] OFF;
PRINT N'Đã thêm 5 Sản Phẩm.';

-- 2.8. Bảng [DinhLuong] (Sử dụng 4 cột)
INSERT INTO [dbo].[DinhLuong] ([idSanPham], [idNguyenLieu], [SoLuongSuDung], [idDonViSuDung]) VALUES
(1, 1, 25.00, 2), -- SP 1 (Cafe Đen) -> NL 1 (Robusta) -> 25 (số lượng) -> DV 2 (g)
(1, 5, 10.00, 8), -- SP 1 (Cafe Đen) -> NL 5 (Đường) -> 10 (số lượng) -> DV 8 (g)
(2, 1, 20.00, 2), -- SP 2 (Bạc Xỉu) -> NL 1 (Robusta) -> 20 (số lượng) -> DV 2 (g)
(2, 3, 40.00, 4), -- SP 2 (Bạc Xỉu) -> NL 3 (Sữa đặc) -> 40 (số lượng) -> DV 4 (ml)
(3, 2, 25.00, 2), -- SP 3 (Latte) -> NL 2 (Arabica) -> 25 (số lượng) -> DV 2 (g)
(3, 4, 100.00, 6); -- SP 3 (Latte) -> NL 4 (Sữa tươi) -> 100 (số lượng) -> DV 6 (ml)
PRINT N'Đã thêm 6 Định Lượng.';

-- 2.9. Bảng Sách (Tác Giả, Thể Loại, Nhà Xuất Bản, Sách)
SET IDENTITY_INSERT [dbo].[TheLoai] ON;
INSERT INTO [dbo].[TheLoai] ([idTheLoai], [tenTheLoai]) VALUES (1, N'Văn học Việt Nam'),(2, N'Kỹ năng sống'),(3, N'Kinh điển Thế giới'),(4, N'Kinh tế'),(5, N'Trinh thám');
SET IDENTITY_INSERT [dbo].[TheLoai] OFF;

SET IDENTITY_INSERT [dbo].[TacGia] ON;
-- SỬA LỖI Msg 110: Thêm cột [gioiThieu] vào danh sách
INSERT INTO [dbo].[TacGia] ([idTacGia], [tenTacGia], [gioiThieu]) VALUES 
(1, N'Nguyễn Nhật Ánh', NULL),
(2, N'Dale Carnegie', NULL),
(3, N'George Orwell', NULL),
(4, N'Yuval Noah Harari', NULL),
(5, N'Agatha Christie', NULL);
SET IDENTITY_INSERT [dbo].[TacGia] OFF;

SET IDENTITY_INSERT [dbo].[NhaXuatBan] ON;
INSERT INTO [dbo].[NhaXuatBan] ([idNhaXuatBan], [tenNhaXuatBan]) VALUES (1, N'NXB Trẻ'),(2, N'NXB Kim Đồng'),(3, N'NXB Tổng Hợp TPHCM'),(4, N'NXB Thế Giới'),(5, N'NXB Lao Động');
SET IDENTITY_INSERT [dbo].[NhaXuatBan] OFF;

SET IDENTITY_INSERT [dbo].[Sach] ON;
INSERT INTO [dbo].[Sach] ([idSach], [tenSach], [idTheLoai], [idTacGia], [idNhaXuatBan], [namXuatBan], [soLuongTong], [soLuongHienCo], [GiaBia]) VALUES
(1, N'Tôi thấy hoa vàng trên cỏ xanh', 1, 1, 1, 2010, 5, 5, 120000.00),
(2, N'Đắc nhân tâm', 2, 2, 3, 2015, 10, 8, 99000.00),
(3, N'1984', 3, 3, 4, 2018, 3, 2, 150000.00),
(4, N'Sapiens: Lược sử loài người', 4, 4, 5, 2017, 4, 4, 250000.00),
(5, N'Án mạng trên chuyến tàu tốc hành Phương Đông', 5, 5, 1, 2019, 6, 6, 110000.00);
SET IDENTITY_INSERT [dbo].[Sach] OFF;
PRINT N'Đã thêm 5 Thể Loại, 5 Tác Giả, 5 NXB, 5 Sách.';

-- 2.10. Bảng [PhuThu]
SET IDENTITY_INSERT [dbo].[PhuThu] ON;
INSERT INTO [dbo].[PhuThu] ([idPhuThu], [TenPhuThu], [GiaTri], [LoaiGiaTri]) VALUES
(1, N'VAT (10%)', 10.00, N'%'),
(2, N'Phí phục vụ (5%)', 5.00, N'%'),
(3, N'Phụ thu Lễ/Tết', 20.00, N'%'),
(4, N'Phụ thu khu vực VIP', 50000.00, N'VND'),
(5, N'Phí mang về', 5000.00, N'VND');
SET IDENTITY_INSERT [dbo].[PhuThu] OFF;
PRINT N'Đã thêm 5 Phụ Thu.';

-- 2.11. Bảng [NguoiGiaoHang]
SET IDENTITY_INSERT [dbo].[NguoiGiaoHang] ON;
INSERT INTO [dbo].[NguoiGiaoHang] ([idNguoiGiaoHang], [TenNguoiGiaoHang], [SoDienThoai], [TrangThai]) VALUES
(1, N'GrabFood', N'1900111', N'Sẵn sàng'),
(2, N'ShopeeFood', N'1900222', N'Sẵn sàng'),
(3, N'Baemin', N'1900333', N'Tạm ngưng'),
(4, N'GoJek', N'1900444', N'Sẵn sàng'),
(5, N'A.Shipper (Nội bộ)', N'0908123456', N'Sẵn sàng');
SET IDENTITY_INSERT [dbo].[NguoiGiaoHang] OFF;
PRINT N'Đã thêm 5 Người Giao Hàng.';

-- 2.12. Bảng [NhaCungCap]
SET IDENTITY_INSERT [dbo].[NhaCungCap] ON;
INSERT INTO [dbo].[NhaCungCap] ([idNhaCungCap], [tenNhaCungCap], [soDienThoai], [diaChi]) VALUES
(1, N'NCC Cà Phê Trung Nguyên', N'0911111111', N'1 Hùng Vương'),
(2, N'NCC Sữa Vinamilk', N'0922222222', N'2 Lê Duẩn'),
(3, N'NCC Trái cây Đà Lạt', N'0933333333', N'3 Pasteur'),
(4, N'NCC Bánh ngọt ABC', N'0944444444', N'4 Trần Hưng Đạo'),
(5, N'NCC Văn phòng phẩm', N'0955555555', N'5 Nguyễn Trãi');
SET IDENTITY_INSERT [dbo].[NhaCungCap] OFF;
PRINT N'Đã thêm 5 Nhà Cung Cấp.';

GO

/*
================================================================================
 PHẦN 3: DỮ LIỆU HOẠT ĐỘNG (TIER 2, 3, 4)
================================================================================
*/
PRINT N'PHẦN 3: Bắt đầu chèn dữ liệu Hoạt động...';
GO

-- 3.1. Bảng [HoaDon]
SET IDENTITY_INSERT [dbo].[HoaDon] ON;
INSERT INTO [dbo].[HoaDon] ([idHoaDon], [idBan], [idNhanVien], [idKhachHang], [thoiGianTao], [thoiGianThanhToan], [trangThai], [tongTienGoc], [giamGia], [TongPhuThu], [LoaiHoaDon], [idNguoiGiaoHang]) VALUES
(1, 1, 3, 2, CAST('2025-11-03T19:00:00' AS DateTime), NULL, N'Chưa thanh toán', 90000.00, 0.00, 0.00, N'Tại quán', NULL),
(2, 2, 4, 1, CAST('2025-11-02T10:00:00' AS DateTime), CAST('2025-11-02T11:00:00' AS DateTime), N'Đã thanh toán', 55000.00, 0.00, 0.00, N'Tại quán', NULL),
(3, 3, 4, 3, CAST('2025-11-03T09:00:00' AS DateTime), CAST('2025-11-03T09:30:00' AS DateTime), N'Đã thanh toán', 80000.00, 0.00, 0.00, N'Tại quán', NULL),
(4, NULL, 3, 1, CAST('2025-11-03T11:00:00' AS DateTime), CAST('2025-11-03T11:05:00' AS DateTime), N'Đã thanh toán', 35000.00, 0.00, 5000.00, N'Mang về', NULL),
(5, NULL, 3, 5, CAST('2025-11-03T14:00:00' AS DateTime), CAST('2025-11-03T14:05:00' AS DateTime), N'Đã thanh toán', 100000.00, 10000.00, 0.00, N'Giao hàng', 1);
SET IDENTITY_INSERT [dbo].[HoaDon] OFF;
PRINT N'Đã thêm 5 Hóa Đơn.';

-- 3.2. Bảng [ChiTietHoaDon]
SET IDENTITY_INSERT [dbo].[ChiTietHoaDon] ON;
INSERT INTO [dbo].[ChiTietHoaDon] ([idChiTietHoaDon], [idHoaDon], [idSanPham], [soLuong], [donGia], [ghiChu]) VALUES
(1, 1, 2, 2, 45000.00, N'2 Bạc xỉu'),
(2, 2, 3, 1, 55000.00, N'1 Latte nóng'),
(3, 3, 1, 1, 35000.00, N'1 Cafe đen'),
(4, 3, 5, 1, 30000.00, N'1 Bánh sừng bò'),
(5, 4, 1, 1, 35000.00, N'Mang về'),
(6, 5, 4, 2, 50000.00, N'2 Trà đào');
SET IDENTITY_INSERT [dbo].[ChiTietHoaDon] OFF;
PRINT N'Đã thêm 6 Chi Tiết Hóa Đơn.';

-- 3.3. Bảng [NhatKyHuyMon]
INSERT INTO [dbo].[NhatKyHuyMon] ([idHoaDon], [idSanPham], [SoLuongHuy], [LyDo], [idNhanVienHuy], [ThoiGianHuy]) VALUES
(1, 2, 1, N'Khách đổi ý', 3, CAST('2025-11-03T19:05:00' AS DateTime)),
(3, 5, 1, N'Bếp báo hết bánh', 4, CAST('2025-11-03T09:02:00' AS DateTime));
PRINT N'Đã thêm 2 Nhật Ký Hủy Món.';

-- 3.4. Bảng [ChiTietPhuThuHoaDon]
INSERT INTO [dbo].[ChiTietPhuThuHoaDon] ([idHoaDon], [idPhuThu], [SoTien]) VALUES
(4, 5, 5000.00); -- HĐ 4 (Mang về) -> Phụ thu 5 (Phí mang về)
PRINT N'Đã thêm 1 Chi Tiết Phụ Thu.';

-- 3.5. Bảng [BangChamCong] (Module 6)
BEGIN
    DECLARE @IdLichNV2 INT = (SELECT idLichLamViec FROM LichLamViec WHERE idNhanVien = 2 AND ngayLam = '20251103');
    DECLARE @IdLichNV3 INT = (SELECT idLichLamViec FROM LichLamViec WHERE idNhanVien = 3 AND ngayLam = '20251103');
    DECLARE @IdLichNV5 INT = (SELECT idLichLamViec FROM LichLamViec WHERE idNhanVien = 5 AND ngayLam = '20251103');

    IF @IdLichNV2 IS NOT NULL
        INSERT INTO [dbo].[BangChamCong] ([idLichLamViec], [gioVao], [gioRa]) VALUES
        (@IdLichNV2, CAST(N'2025-11-03T07:58:00' AS DateTime), CAST(N'2025-11-03T17:05:00' AS DateTime)); -- NV 2 (Đúng giờ, OT 5 phút)
    IF @IdLichNV3 IS NOT NULL
        INSERT INTO [dbo].[BangChamCong] ([idLichLamViec], [gioVao], [gioRa]) VALUES
        (@IdLichNV3, CAST(N'2025-11-03T06:59:00' AS DateTime), CAST(N'2025-11-03T15:00:00' AS DateTime)); -- NV 3 (Đúng giờ)
    IF @IdLichNV5 IS NOT NULL
        INSERT INTO [dbo].[BangChamCong] ([idLichLamViec], [gioVao], [gioRa]) VALUES
        (@IdLichNV5, CAST(N'2025-11-03T07:15:00' AS DateTime), CAST(N'2025-11-03T15:00:00' AS DateTime)); -- NV 5 (Đi trễ 15 phút)
END
PRINT N'Đã thêm 3 dữ liệu Chấm Công.';

-- 3.6. Bảng [PhieuLuong] (Module 6 & 8)
SET IDENTITY_INSERT [dbo].[PhieuLuong] ON;
INSERT INTO [dbo].[PhieuLuong] ([idPhieuLuong], [idNhanVien], [thang], [nam], [luongCoBan], [tongGioLam], [tienThuong], [khauTru], [thucLanh], [ngayTao], [trangThai]) VALUES
(1, 1, 10, 2025, 50000.00, 160.00, 500000.00, 0.00, 8500000.00, CAST('2025-11-01T00:00:00' AS DateTime), N'Đã thanh toán'),
(2, 2, 10, 2025, 45000.00, 168.00, 500000.00, 50000.00, 8010000.00, CAST('2025-11-01T00:00:00' AS DateTime), N'Đã thanh toán'),
(3, 3, 10, 2025, 30000.00, 160.00, 0.00, 150000.00, 4650000.00, CAST('2025-11-01T00:00:00' AS DateTime), N'Đã thanh toán'),
(4, 4, 10, 2025, 25000.00, 160.00, 0.00, 0.00, 4000000.00, CAST('2025-11-01T00:00:00' AS DateTime), N'Đã thanh toán'),
(5, 5, 10, 2025, 35000.00, 170.00, 300000.00, 0.00, 6250000.00, CAST('2025-11-01T00:00:00' AS DateTime), N'Đã thanh toán');
SET IDENTITY_INSERT [dbo].[PhieuLuong] OFF;
PRINT N'Đã thêm 5 Phiếu Lương (Tháng 10/2025).';

-- 3.7. Bảng [PhieuThuongPhat] (Module 6)
INSERT INTO [dbo].[PhieuThuongPhat] ([idNhanVien], [idNguoiTao], [NgayTao], [SoTien], [LyDo], [idPhieuLuong]) VALUES
(4, 2, CAST(N'2025-11-01T00:00:00' AS DateTime), -50000.00, N'Làm vỡ ly (Chưa chốt)', NULL),
(5, 2, CAST(N'2025-11-02T00:00:00' AS DateTime), 100000.00, N'Thưởng sáng kiến (Chưa chốt)', NULL),
(2, 1, CAST(N'2025-10-20T00:00:00' AS DateTime), -50000.00, N'Đi trễ 30p (Đã chốt lương T10)', 2),
(3, 2, CAST(N'2025-10-15T00:00:00' AS DateTime), -150000.00, N'Sai đồng phục (Đã chốt lương T10)', 3),
(5, 2, CAST(N'2025-10-10T00:00:00' AS DateTime), 300000.00, N'Thưởng OT đột xuất (Đã chốt lương T10)', 5);
PRINT N'Đã thêm 5 Phiếu Thưởng/Phạt.';

-- 3.8. Bảng [PhieuThueSach]
SET IDENTITY_INSERT [dbo].[PhieuThueSach] ON;
INSERT INTO [dbo].[PhieuThueSach] ([idPhieuThueSach], [idKhachHang], [idNhanVien], [ngayThue], [trangThai], [tongTienCoc]) VALUES
(1, 2, 3, CAST('2025-11-01T00:00:00' AS DateTime), N'Đang thuê', 99000.00),
(2, 3, 3, CAST('2025-11-02T00:00:00' AS DateTime), N'Đang thuê', 150000.00),
(3, 5, 4, CAST('2025-10-15T00:00:00' AS DateTime), N'Đã trả', 120000.00),
(4, 2, 4, CAST('2025-10-20T00:00:00' AS DateTime), N'Đang thuê', 250000.00),
(5, 3, 3, CAST('2025-11-03T00:00:00' AS DateTime), N'Đang thuê', 110000.00);
SET IDENTITY_INSERT [dbo].[PhieuThueSach] OFF;
PRINT N'Đã thêm 5 Phiếu Thuê Sách.';

-- 3.9. Bảng [ChiTietPhieuThue]
INSERT INTO [dbo].[ChiTietPhieuThue] ([idPhieuThueSach], [idSach], [ngayHenTra], [ngayTraThucTe], [tienCoc], [TienPhatTraTre]) VALUES
(1, 2, CAST('2025-11-08T00:00:00' AS DateTime), NULL, 99000.00, 0.00), -- Đắc nhân tâm (Đang thuê)
(2, 3, CAST('2025-11-09T00:00:00' AS DateTime), NULL, 150000.00, 0.00), -- 1984 (Đang thuê)
(3, 1, CAST('2025-10-22T00:00:00' AS DateTime), CAST('2025-10-22T00:00:00' AS DateTime), 120000.00, 0.00), -- Hoa vàng (Đã trả)
(4, 4, CAST('2025-10-27T00:00:00' AS DateTime), NULL, 250000.00, 0.00), -- Sapiens (Đang thuê - TRỄ HẠN)
(5, 5, CAST('2025-11-10T00:00:00' AS DateTime), NULL, 110000.00, 0.00); -- Án mạng (Đang thuê)
PRINT N'Đã thêm 5 Chi Tiết Phiếu Thuê.';

-- 3.10. Bảng [PhieuNhapKho] & [ChiTietNhapKho]
SET IDENTITY_INSERT [dbo].[PhieuNhapKho] ON;
INSERT INTO [dbo].[PhieuNhapKho] ([idPhieuNhapKho], [idNhaCungCap], [idNhanVien], [ngayNhap], [tongTien], [TrangThai]) VALUES
(1, 1, 2, CAST('2025-11-01T00:00:00' AS DateTime), 2500000.00, N'Đã hoàn thành'),
(2, 2, 2, CAST('2025-11-02T00:00:00' AS DateTime), 1000000.00, N'Đã hoàn thành'),
(3, 4, 5, CAST('2025-11-02T00:00:00' AS DateTime), 500000.00, N'Đã hoàn thành'),
(4, 1, 2, CAST('2025-11-03T00:00:00' AS DateTime), 0.00, N'Đang xử lý'),
(5, 3, 5, CAST('2025-11-03T00:00:00' AS DateTime), 0.00, N'Đang xử lý');
SET IDENTITY_INSERT [dbo].[PhieuNhapKho] OFF;

INSERT INTO [dbo].[ChiTietNhapKho] ([idPhieuNhapKho], [idNguyenLieu], [soLuongNhap], [donGiaNhap]) VALUES
(1, 1, 10.00, 250000.00), -- Phiếu 1: 10kg Cafe Robusta
(2, 4, 20.00, 50000.00), -- Phiếu 2: 20 lít Sữa tươi
(3, 5, 50.00, 10000.00); -- Phiếu 3: 50kg Đường
PRINT N'Đã thêm 5 Phiếu Nhập Kho và 3 Chi Tiết Nhập.';

-- 3.11. Bảng [PhieuKiemKho] & [ChiTietKiemKho]
SET IDENTITY_INSERT [dbo].[PhieuKiemKho] ON;
INSERT INTO [dbo].[PhieuKiemKho] ([idPhieuKiemKho], [idNhanVienKiem], [NgayKiem], [TrangThai]) VALUES
(1, 2, CAST('2025-10-30T00:00:00' AS DateTime), N'Đã hoàn thành'),
(2, 5, CAST('2025-10-31T00:00:00' AS DateTime), N'Đã hoàn thành');
SET IDENTITY_INSERT [dbo].[PhieuKiemKho] OFF;

INSERT INTO [dbo].[ChiTietKiemKho] ([idPhieuKiemKho], [idNguyenLieu], [TonKhoHeThong], [TonKhoThucTe], [LyDoChenhLech]) VALUES
(1, 1, 0.50, 0.45, N'Hao hụt pha chế'),
(1, 3, 50.00, 50.00, NULL),
(2, 4, 12.00, 11.00, N'Hết hạn 1 lít');
PRINT N'Đã thêm 2 Phiếu Kiểm Kho và 3 Chi Tiết Kiểm.';

-- 3.12. Bảng [PhieuXuatHuy] & [ChiTietXuatHuy]
SET IDENTITY_INSERT [dbo].[PhieuXuatHuy] ON;
INSERT INTO [dbo].[PhieuXuatHuy] ([idPhieuXuatHuy], [idNhanVienXuat], [NgayXuatHuy], [LyDoXuatHuy], [TongGiaTriHuy]) VALUES
(1, 5, CAST('2025-10-31T00:00:00' AS DateTime), N'Hủy 1 lít sữa tươi hết hạn (sau kiểm kho)', 50000.00),
(2, 2, CAST('2025-11-01T00:00:00' AS DateTime), N'Hủy nguyên liệu hỏng do trời mưa', 125000.00);
SET IDENTITY_INSERT [dbo].[PhieuXuatHuy] OFF;

INSERT INTO [dbo].[ChiTietXuatHuy] ([idPhieuXuatHuy], [idNguyenLieu], [SoLuong], [DonGiaVon]) VALUES
(1, 4, 1.00, 50000.00), -- 1 lít sữa
(2, 1, 0.50, 250000.00); -- 0.5kg cafe
PRINT N'Đã thêm 2 Phiếu Xuất Hủy và 2 Chi Tiết Hủy.';

-- 3.13. Bảng [PhieuDatBan]
INSERT INTO [dbo].[PhieuDatBan] ([idKhachHang], [idBan], [hoTenKhach], [sdtKhach], [thoiGianDat], [soLuongKhach], [trangThai], [ghiChu]) VALUES
(3, 4, N'Lê Thị Bình', N'0912345678', CAST('2025-11-03T20:00:00' AS DateTime), 8, N'Đã xác nhận', N'Bàn 202 (Nhóm)'),
(2, 2, N'Trần Văn An', N'0987654321', CAST('2025-11-04T12:00:00' AS DateTime), 2, N'Chờ xác nhận', NULL),
(5, 5, N'Đặng Thu Duyên', N'0908555666', CAST('2025-11-05T19:00:00' AS DateTime), 4, N'Đã xác nhận', NULL),
(1, 2, N'Khách vãng lai', N'0123456789', CAST('2025-11-02T19:00:00' AS DateTime), 2, N'Đã hoàn thành', N'Khách đã đến'),
(4, 3, N'Phạm Hùng Cường', N'0903111222', CAST('2025-11-01T19:00:00' AS DateTime), 2, N'Đã hủy', N'Khách báo bận');
PRINT N'Đã thêm 5 Phiếu Đặt Bàn.';

-- 3.14. Bảng [ThongBao]
INSERT INTO [dbo].[ThongBao] ([idNhanVienTao], [NoiDung], [ThoiGianTao], [LoaiThongBao], [IdLienQuan], [DaXem]) VALUES
(4, N'Bàn V01 vừa được báo cáo sự cố: Gãy chân bàn', CAST('2025-11-03T10:00:00' AS DateTime), N'SuCoBan', 5, 0),
(2, N'Hết hàng: Hạt Cà Phê Arabica (Nhập)', CAST('2025-11-03T11:00:00' AS DateTime), N'HetHang', 2, 0),
(3, N'Đơn nghỉ mới từ Trần Thị Thu Ngân', CAST('2025-11-03T12:00:00' AS DateTime), N'DonXinNghi', 1, 0),
(1, N'Hệ thống bảo trì 02:00 04/11', CAST('2025-11-03T15:00:00' AS DateTime), N'HeThong', NULL, 1),
(2, N'Đơn nghỉ của Lê Văn Phục Vụ đã được duyệt', CAST('2025-11-02T09:00:00' AS DateTime), N'DonXinNghi', 2, 1);
PRINT N'Đã thêm 5 Thông Báo.';

-- 3.15. Bảng [KhuyenMai]
SET IDENTITY_INSERT [dbo].[KhuyenMai] ON;
INSERT INTO [dbo].[KhuyenMai] ([idKhuyenMai], [maKhuyenMai], [tenChuongTrinh], [loaiGiamGia], [giaTriGiam], [ngayBatDau], [ngayKetThuc], [TrangThai]) VALUES
(1, N'WELCOME10', N'Giảm 10% cho thành viên mới', N'%', 10.00, CAST('2025-01-01T00:00:00' AS DateTime), CAST('2025-12-31T00:00:00' AS DateTime), N'Hoạt động'),
(2, N'COMBOBUA', N'Combo Bữa trưa (1 Cafe + 1 Bánh)', N'VND', 15000.00, CAST('2025-11-01T00:00:00' AS DateTime), CAST('2025-11-30T00:00:00' AS DateTime), N'Hoạt động'),
(3, N'FREESHIP', N'Miễn phí vận chuyển', N'VND', 30000.00, CAST('2025-11-01T00:00:00' AS DateTime), CAST('2025-11-05T00:00:00' AS DateTime), N'Hoạt động'),
(4, N'BLACKFRIDAY', N'Giảm 50% Cà Phê', N'%', 50.00, CAST('2025-11-28T00:00:00' AS DateTime), CAST('2025-11-28T00:00:00' AS DateTime), N'Hoạt động'),
(5, N'HETHD', N'Chương trình đã hết hạn', N'%', 10.00, CAST('2025-10-01T00:00:00' AS DateTime), CAST('2025-10-31T00:00:00' AS DateTime), N'Hết hạn');
SET IDENTITY_INSERT [dbo].[KhuyenMai] OFF;
PRINT N'Đã thêm 5 Khuyến Mãi.';

-- 3.16. Bảng [HoaDon_KhuyenMai]
INSERT INTO [dbo].[HoaDon_KhuyenMai] ([idHoaDon], [idKhuyenMai]) VALUES
(5, 3); -- HĐ 5 (Giao hàng) được áp dụng KM 3 (Freeship)
PRINT N'Đã thêm 1 liên kết Khuyến Mãi.';

-- 3.17. Bảng [ChatLichSu] (5 dòng)
INSERT INTO [dbo].[ChatLichSu] ([idKhachHang], [idNhanVien], [NoiDungHoi], [NoiDungTraLoi], [ThoiGian], [LoaiChat]) VALUES
(2, NULL, N'Quán mở cửa lúc mấy giờ?', N'Chào bạn, quán mở cửa từ 7h sáng đến 11h tối ạ.', CAST('2025-11-01T10:00:00' AS DateTime), N'Web'),
(NULL, 1, N'Công thức pha Bạc Xỉu', N'Công thức là 20g Cà phê và 40ml Sữa đặc.', CAST('2025-11-01T11:00:00' AS DateTime), N'NoiBo'),
(3, NULL, N'Cho tôi đặt bàn 4 người tối nay', N'Dạ vâng, bạn vui lòng cung cấp SĐT ạ.', CAST('2025-11-02T14:00:00' AS DateTime), N'Web'),
(5, NULL, N'Sách 1984 còn không?', N'Dạ sách 1984 hiện còn 2 cuốn ở Tầng 2 ạ.', CAST('2025-11-03T10:00:00' AS DateTime), N'Web'),
(NULL, 2, N'Doanh thu hôm qua là bao nhiêu?', N'Doanh thu hôm qua (02/11/2025) là 55,000 VND.', CAST('2025-11-03T08:00:00' AS DateTime), N'NoiBo');
PRINT N'Đã thêm 5 Lịch sử Chat.';

-- 3.18. Bảng [DeXuatSach] & [DeXuatSanPham]
INSERT INTO [dbo].[DeXuatSach] ([idSachGoc], [idSachDeXuat], [DoLienQuan], [LoaiDeXuat]) VALUES
(3, 5, 0.8, N'Cùng tác giả'),
(1, 2, 0.5, N'Cùng thể loại'),
(2, 4, 0.6, N'Cùng thể loại'),
(4, 3, 0.7, N'Mua cùng'),
(5, 1, 0.4, N'Mua cùng');
INSERT INTO [dbo].[DeXuatSanPham] ([idSanPhamGoc], [idSanPhamDeXuat], [DoLienQuan], [LoaiDeXuat]) VALUES
(1, 2, 0.9, N'Thường mua cùng'),
(1, 5, 0.7, N'Thường mua cùng'),
(3, 5, 0.8, N'Thường mua cùng'),
(4, 5, 0.6, N'Gợi ý'),
(2, 1, 0.9, N'Thường mua cùng');
PRINT N'Đã thêm 5 Gợi ý Sách và 5 Gợi ý Sản Phẩm.';

-- 3.19. Bảng [GiaoDichThanhToan]
INSERT INTO [dbo].[GiaoDichThanhToan] ([idHoaDon], [MaGiaoDichNgoai], [CongThanhToan], [SoTien], [ThoiGianGiaoDich], [TrangThai]) VALUES
(2, N'HD_1102_001', N'Tiền mặt', 55000.00, CAST('2025-11-02T11:00:00' AS DateTime), N'Thành công'),
(3, N'HD_1103_001', N'Tiền mặt', 80000.00, CAST('2025-11-03T09:30:00' AS DateTime), N'Thành công'),
(4, N'HD_1103_002', N'Momo', 40000.00, CAST('2025-11-03T11:05:00' AS DateTime), N'Thành công'),
(5, N'GRAB_XYZ', N'GrabPay', 90000.00, CAST('2025-11-03T14:05:00' AS DateTime), N'Thành công'),
(1, N'VNPAY_ABC', N'VNPAY', 90000.00, CAST('2025-11-03T19:30:00' AS DateTime), N'Đang chờ');
PRINT N'Đã thêm 5 Giao Dịch Thanh Toán.';
GO

PRINT N'================================================'
PRINT N' KỊCH BẢN CHÈN DỮ LIỆU MẪU (v4) HOÀN TẤT!'
PRINT N'================================================'
GO