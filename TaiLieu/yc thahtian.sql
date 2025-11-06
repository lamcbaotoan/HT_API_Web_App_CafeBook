Dựa trên **CSDL `CAFEBOOKDB_v2`** (file bạn cung cấp) và yêu cầu mô tả, sau đây là **bản đặc tả chi tiết (yêu cầu cho AI code)** để phát triển **chức năng “Quản lý Phiếu Đặt Bàn”** cho hệ thống *Cafebook* — bao gồm logic, UI, và xử lý backend:

---

## 🧩 **1. Mục tiêu chức năng**

Xây dựng module **Quản lý Đặt Bàn (DatBanView)** dùng cho nhân viên và khách hàng đặt bàn trực tuyến, với các tính năng:

* Nhân viên thêm/sửa/xóa phiếu đặt bàn.

* Khách hàng tự đặt bàn qua web.

* Gửi **thông báo (bảng `ThongBao`)** tới nhân viên khi có phiếu đặt mới từ web.

* Gửi **email xác nhận** đến khách hàng.

* Tự động nhận diện khách hàng cũ qua số điện thoại/email.

* Xác nhận khách đến → mở `GoiMonView`.

* Hủy phiếu → cập nhật trạng thái bàn.

---

## 🧱 **2. Cấu trúc CSDL liên quan**

Các bảng sẽ được sử dụng:

* **`PhieuDatBan`**

  * Trạng thái: `"Đã xác nhận"`, `"Chờ xác nhận"`, `"Đã hủy"`, `"Khách đã đến"`.

* **`Ban`** – cập nhật trạng thái `"Có khách"` khi khách đến.

* **`KhachHang`** – tìm theo `soDienThoai` hoặc `email`.

* **`ThongBao`** – lưu thông báo khi có đặt bàn mới online.

---

## 💡 **3. Chức năng chi tiết**

### 🔹 3.1. Quản lý Phiếu Đặt Bàn (nhân viên)

Tại **DatBanView.xaml**:

* Hiển thị danh sách phiếu đặt bàn (DataGrid).

* Chức năng:

  * ➕ **Thêm mới** phiếu đặt bàn.

  * ✏️ **Sửa** thông tin (khách, bàn, thời gian, ghi chú...).

  * ❌ **Xóa** phiếu.

  * 🔍 **Tìm kiếm/Lọc** theo tên khách, số bàn, ngày đặt.

  * ✅ **Xác nhận khách đến** → đổi `PhieuDatBan.trangThai = 'Khách đã đến'`, cập nhật `Ban.trangThai = 'Có khách'` và tự động mở `GoiMonView`.

  * 🚫 **Hủy phiếu** → đổi `trangThai = 'Đã hủy'` và trả `Ban.trangThai = 'Trống'`.

---

### 🔹 3.2. Khách hàng đặt bàn qua web

Tại **WebApp**:

* Form nhập: Họ tên, SĐT, Email, Số lượng khách, Khu vực (chọn bàn gợi ý), Thời gian đặt, Ghi chú.

* Khi gửi yêu cầu:

  1. Kiểm tra `KhachHang` bằng SĐT/Email:

     * Nếu có → tự động điền thông tin.

     * Nếu chưa có → tạo mới.

  2. Thêm phiếu vào bảng `PhieuDatBan` (`trangThai = 'Chờ xác nhận'`).

  3. Tạo bản ghi trong `ThongBao`:

     ```sql

     INSERT INTO ThongBao (idNhanVienTao, NoiDung, LoaiThongBao, IdLienQuan, DaXem)

     VALUES (NULL, N'Khách hàng Nguyễn Văn A vừa đặt bàn #B12 cho 4 người vào 19:00', N'DatBan', @idPhieuDatBan, 0)

     ```

  4. Gửi **email xác nhận** (SMTP hoặc MailKit) nếu `email` không null.

---

### 🔹 3.3. Màn hình nhân viên (`ManHinhNhanVien.xaml`)

* Góc phải trên có **biểu tượng chuông thông báo** (`IconNotification`).

* Khi có `ThongBao.DaXem = 0`, hiển thị badge đỏ 🔴.

* Ấn chuông → xổ danh sách thông báo.

* Ấn 1 thông báo loại `"DatBan"` → mở `DatBanView` và cuộn tới phiếu tương ứng.

---

### 🔹 3.4. Gửi Email xác nhận

* Khi thêm phiếu đặt bàn (từ web hoặc nhân viên):

  * Nếu có `email` → gửi mail nội dung:

    ```

    [Cafebook] Xác nhận đặt bàn thành công

    Xin chào [Họ tên],

    Cảm ơn bạn đã đặt bàn tại Cafebook.

    Thông tin đặt bàn:

    - Bàn: [soBan]

    - Thời gian: [thoiGianDat]

    - Số khách: [soLuongKhach]

    - Ghi chú: [ghiChu]

    Rất mong được đón tiếp bạn!

    ```

  * Nếu khách vãng lai không có email → bỏ qua.

---

### 🔹 3.5. Xử lý sự kiện (WPF code-behind)

**Trong `BtnDatBan_Click`:**

```csharp

private void BtnDatBan_Click(object sender, RoutedEventArgs e)

{

    DatBanView datBanView = new DatBanView();

    MainContentFrame.Navigate(datBanView);

}

``

**Khi xác nhận khách đến:**

```csharp

private void XacNhanKhachDen(int idPhieu)

{

    var phieu = db.PhieuDatBans.Find(idPhieu);

    if (phieu != null)

    {

        phieu.trangThai = "Khách đã đến";

        var ban = db.Bans.Find(phieu.idBan);

        if (ban != null) ban.trangThai = "Đang phục vụ";

        db.SaveChanges();



        GoiMonView goiMon = new GoiMonView(phieu.idBan);

        MainContentFrame.Navigate(goiMon);

    }

}

```

---

## 🧮 **4. Quy trình tổng thể**



| Bước | Tác nhân   | Mô tả hành động                    | Kết quả                                |

| ---- | ---------- | ---------------------------------- | -------------------------------------- |

| 1    | Khách hàng | Đặt bàn online                     | Phiếu mới được thêm (`Chờ xác nhận`)   |

| 2    | Hệ thống   | Tạo `ThongBao` cho nhân viên       | Thông báo hiển thị ở `ManHinhNhanVien` |

| 3    | Nhân viên  | Mở `DatBanView`, xác nhận hoặc hủy | Cập nhật `trangThai` và bàn            |

| 4    | Hệ thống   | Khi xác nhận, mở `GoiMonView`      | Sẵn sàng ghi món                       |

| 5    | Hệ thống   | Gửi email xác nhận (nếu có)        | Khách nhận thông báo                   |



---

  "SmtpSettings": {

    "Host": "smtp.gmail.com",

    "Port": 587,

    "Username": "cafebook.hotro@gmail.com",

    "Password": "raja nenx mxhk vtvn",

    "EnableSsl": true,

    "FromName": "Cafebook Hỗ Trợ"

////////////

phát triển App Dto, Controllers, xaml.cs, xaml.