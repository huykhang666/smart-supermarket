# 10_AI - TÀI LIỆU 08: QUY TẮC PHÁT TRIỂN & CHUẨN MÃ NGUỒN (CODING GUIDELINES)

---

## MỤC LỤC
1. [Nguyên tắc tối thượng (Core Coding Standards)](#1-nguyên-tắc-tối-thượng)
2. [Quy định nghiêm ngặt về chú thích mã nguồn (Comments Policy)](#2-quy-định-nghiêm-ngặt-về-chú-thích-mã-nguồn)
3. [Chuẩn mực xưng hô & Ngôn ngữ đối thoại](#3-chuẩn-mực-xưng-hô--ngôn-ngữ-đối-thoại)
4. [Quy chuẩn căn lề giao diện người dùng (UI Left-Alignment)](#4-quy-chuẩn-căn-lề-giao-diện-người-dùng)
5. [Quy chuẩn truy vấn CSDL & Cách ly danh mục](#5-quy-chuẩn-truy-vấn-csdl--cách-ly-danh-mục)
6. [Quy chuẩn xử lý lỗi & Chuỗi dự phòng (Resilience & Fallback)](#6-quy-chuẩn-xử-lý-lỗi--chuỗi-dự-phòng)
7. [Tiêu chuẩn kiểm thử & Đóng gói (Build & CI Verification)](#7-tiêu-chuẩn-kiểm-thử--đóng-gói)

---

## 1. NGUYÊN TẮC TỐI THƯỢNG

Toàn bộ lập trình viên tham gia phát triển phân hệ **10_AI** trong dự án **Smart SuperMarket** bắt buộc phải tuân thủ 6 quy chuẩn kỹ thuật dưới đây. Bất kỳ Pull Request nào vi phạm sẽ bị từ chối phê duyệt (Reject PR).

---

## 2. QUY ĐỊNH NGHIÊM NGẶT VỀ CHÚ THÍCH MÃ NGUỒN (COMMENTS POLICY)

### 2.1. Đối với các file DTO (Data Transfer Objects)
- **Tuyệt đối KHÔNG viết bất kỳ dòng comment nào** trong toàn bộ các file DTO:
  * Không dùng `//`
  * Không dùng `/* ... */`
  * Không dùng XML documentation `/// <summary>`
  * Áp dụng tại: `Backend/Features/AI/DTOs/Requests/` và `Backend/Features/AI/DTOs/Responses/`.
- **Lý do**: Giữ file DTO hoàn toàn tinh gọn, thuần túy là dữ liệu truyền tải (Pure Data Contracts), tránh ô nhiễm mã nguồn khi serialize/deserialize.

### 2.2. Đối với các file Non-DTO (Services, Controllers, Clients)
- Chỉ viết chú thích ở mức tối thiểu (`//`) cho những thuật toán thực sự phức tạp hoặc những quyết định kiến trúc quan trọng.
- Không viết các comment hiển nhiên lặp lại tên hàm hoặc tên biến.

---

## 3. CHUẨN MỰC XƯNG HÔ & NGÔN NGỮ ĐỐI THOẠI

- **Đại từ xưng hô bắt buộc**:
  - Trợ lý AI luôn xưng là **`"mình"`** và gọi khách hàng là **`"bạn"`**.
  - **Tuyệt đối KHÔNG xưng `"em"`** trong bất kỳ hoàn cảnh nào.
  - Không xưng `"tôi"`, `"chúng tôi"`, `"chúng mình"`.
  - Không xưng hô máy móc kiểu robot: *"hệ thống ghi nhận"*, *"tôi là AI"*.
- **Văn phong**: Lịch thiệp, ấm áp, nhiệt tình, am hiểu văn hóa đi chợ và ẩm thực gia đình chuẩn phong vị Cookpad Việt Nam.

---

## 4. QUY CHUẨN CĂN LỀ GIAO DIỆN NGƯỜI DÙNG (UI LEFT-ALIGNMENT)

- **Căn lề trái tuyệt đối (`text-align: left`)**:
  - Toàn bộ nội dung hội thoại, danh sách gạch đầu dòng, bảng giá và các thẻ chọn mâm (`MealOptionCards`) trên Web (`AiShoppingAssistant.tsx`) **phải được căn lề trái**.
  - **Tuyệt đối KHÔNG dùng `text-align: center`** cho bong bóng hội thoại và thẻ mâm cơm vì gây lệch tầm nhìn và mất tính thẩm mỹ trên thiết bị di động/máy tính.
- **Triệt tiêu lặp văn bản (No Text Duplication)**:
  - Lời thoại của đầu bếp chỉ tóm tắt ngắn gọn 3 phương án và chi phí ước tính.
  - Không in lại toàn bộ danh sách gạch đầu dòng nguyên liệu trong tin nhắn text vì các thẻ mâm cơm tương tác bên dưới đã thể hiện trực quan.

---

## 5. QUY CHUẨN TRUY VẤN CSDL & CÁCH LY DANH MỤC

- **Cách ly danh mục (Category Isolation)**:
  - Khi tìm kiếm nguyên liệu cho mâm cơm, bắt buộc phải dùng cấu trúc `IngredientQuery` chỉ định mã `CategoryId` cụ thể:
    * `CategoryId = 9`: Thịt, cá, trứng tươi sống.
    * `CategoryId = 4`: Rau củ quả tươi.
    * `CategoryId = 10`: Mì tôm, đồ ăn liền, đồ hộp.
    * `CategoryId = 5`: Gia vị, đồ khô.
  - **Tuyệt đối KHÔNG dùng** `products.Where(p => p.ProductName.Contains(kw))` thuần túy trên toàn bộ kho hàng vì sẽ gây lỗi ghép nhầm (tìm sườn ra mì tôm, tìm cà chua ra cá hộp).

---

## 6. QUY CHUẨN XỬ LÝ LỖI & CHUỖI DỰ PHÒNG (RESILIENCE & FALLBACK)

- **Bảo vệ toàn diện (Graceful Degradation)**:
  - Mọi lời gọi HTTP ra bên ngoài tới Google Gemini hoặc Groq API đều phải đặt trong khối `try/catch`.
  - Nếu gặp lỗi mạng, sai API key (400) hoặc không tìm thấy model (404), hệ thống phải tự động chuyển tiếp:
    $$\text{Gemini 2.5 Flash} \longrightarrow \text{Groq Llama 3.3} \longrightarrow \text{Local Cookpad Rule Engine}$$
  - Kết quả trả về cho Frontend luôn là HTTP `200 OK` với mâm cơm chuẩn xác từ kho, không bao giờ để người dùng nhìn thấy màn hình lỗi kỹ thuật.

---

## 7. TIÊU CHUẨN KIỂM THỬ & ĐÓNG GÓI (BUILD & CI VERIFICATION)

Trước khi commit và push lên nhánh `feature/AI`, lập trình viên bắt buộc phải chạy kiểm tra cục bộ:

1. **Backend Verification**:
   ```powershell
   dotnet build "Backend/SmartSupermarket.Backend.csproj" --no-restore
   ```
   *Yêu cầu*: Đạt **0 Error(s)**.

2. **Frontend Verification**:
   ```powershell
   npm run build --prefix "Frontend"
   ```
   *Yêu cầu*: Vượt qua TypeScript strict type-check (`tsc -b`) và Vite build đạt **0 Error(s)**.
