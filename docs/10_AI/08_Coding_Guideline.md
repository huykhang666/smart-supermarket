# 10_AI - TÀI LIỆU 08: QUY CHUẨN LẬP TRÌNH & PHÁT TRIỂN AI (CODING GUIDELINES)

---

## MỤC LỤC
1. [Nguyên tắc cốt lõi (Core Tenets)](#1-nguyên-tắc-cốt-lõi)
2. [Quy định về chú thích mã nguồn (Comments Policy)](#2-quy-định-về-chú-thích-mã-nguồn)
3. [Quy chuẩn xưng hô & Ngôn ngữ đối thoại](#3-quy-chuẩn-xưng-hô--ngôn-ngữ-đối-thoại)
4. [Quy chuẩn giao diện người dùng (UI Alignment Rules)](#4-quy-chuẩn-giao-diện-người-dùng)
5. [Quy chuẩn truy vấn & Khớp dữ liệu kho hàng](#5-quy-chuẩn-truy-vấn--khớp-dữ-liệu-kho-hàng)
6. [Quy chuẩn bảo vệ lỗi & Dự phòng (Resilience & Fallback)](#6-quy-chuẩn-bảo-vệ-lỗi--dự-phòng)
7. [Kiểm thử tự động & Tiêu chuẩn Build](#7-kiểm-thử-tự-động--tiêu-chuẩn-build)

---

## 1. NGUYÊN TẮC CỐT LÕI

Mọi lập trình viên tham gia phát triển và bảo trì phân hệ **10_AI** trong dự án **Smart SuperMarket** phải tuân thủ nghiêm ngặt các quy chuẩn kỹ thuật dưới đây nhằm đảm bảo tính ổn định, sạch đẹp của mã nguồn và trải nghiệm người dùng nhất quán.

---

## 2. QUY ĐỊNH VỀ CHÚ THÍCH MÃ NGUỒN (COMMENTS POLICY)

- **Các file DTO (Data Transfer Objects)**:
  - **Tuyệt đối KHÔNG viết comment** (Không có `//`, `/* */` hay XML doc comments `<summary>`) trong các file DTO như:
    * `AiMealPlanRequests.cs`
    * `AiMealPlanResponses.cs`
    * Các file DTO tương đương trong thư mục `Features/AI/DTOs/`.
- **Các file Non-DTO (Services, Controllers, Clients)**:
  - Chỉ viết comment ngắn gọn ở mức tối thiểu (`//`) cho những thuật toán hoặc nghiệp vụ thực sự phức tạp.
  - Không viết comment hiển nhiên hoặc lặp lại tên hàm/tên biến.

---

## 3. QUY CHUẨN XƯNG HÔ & NGÔN NGỮ ĐỐI THOẠI

- **Đại từ xưng hô chuẩn mực**:
  - Trợ lý AI luôn xưng là **`"mình"`** và gọi khách hàng là **`"bạn"`**.
  - **Tuyệt đối KHÔNG xưng `"em"`** trong bất kỳ trường hợp nào.
  - Không xưng hô máy móc kiểu *"tôi là chatbot"*, *"hệ thống ghi nhận"*.
- **Giọng điệu**: Ấm áp, nhiệt tình, am hiểu ẩm thực gia đình chuẩn phong vị Cookpad Việt Nam.

---

## 4. QUY CHUẨN GIAO DIỆN NGƯỜI DÙNG (UI ALIGNMENT RULES)

- **Căn lề trái tuyệt đối (`text-align: left`)**:
  - Toàn bộ bong bóng chat, danh sách gạch đầu dòng món ăn, giá tiền và các thẻ chọn mâm (`MealOptionCards`) trên Web (`AiShoppingAssistant.tsx`) **phải được căn lề trái**.
  - Tuyệt đối không dùng `text-align: center` cho nội dung hội thoại vì gây khó đọc và mất thẩm mỹ trên màn hình di động/máy tính.
- **Tránh lặp lại văn bản (No Text Duplication)**:
  - Lời thoại của đầu bếp chỉ tóm tắt ngắn gọn tên 3 mâm cơm và đặc điểm chính.
  - Không in lại toàn bộ danh sách gạch đầu dòng nguyên liệu trong tin nhắn text vì các thẻ mâm cơm bên dưới đã đảm nhiệm việc này một cách trực quan.

---

## 5. QUY CHUẨN TRUY VẤN & KHỚP DỮ LIỆU KHO HÀNG

- **Cách ly danh mục (Category Isolation)**:
  - Khi tìm kiếm nguyên liệu cho mâm cơm, bắt buộc chỉ định mã `CategoryId` cụ thể qua cấu trúc `IngredientQuery`:
    * Thịt, cá, trứng: Bắt buộc `CategoryId = 9`.
    * Rau củ quả tươi: Bắt buộc `CategoryId = 4`.
    * Mì tôm, đồ hộp: Bắt buộc `CategoryId = 10`.
    * Gia vị: Bắt buộc `CategoryId = 5`.
  - Không bao giờ dùng `products.Where(p => p.ProductName.Contains(kw))` thuần túy trên toàn bộ kho hàng vì sẽ gây lỗi ghép nhầm (ví dụ: tìm sườn ra mì Omachi, tìm cà chua ra cá hộp).

---

## 6. QUY CHUẨN BẢO VỆ LỖI & DỰ PHÒNG (RESILIENCE & FALLBACK)

- **Không bao giờ làm gián đoạn trải nghiệm người dùng**:
  - Mọi lời gọi HTTP ra bên ngoài tới Google Gemini hoặc Groq API đều phải đặt trong khối `try/catch`.
  - Nếu gặp lỗi mạng, sai API key (400) hoặc không tìm thấy model (404), hệ thống phải tự động chuyển tiếp sang:
    1. Model thay thế (`gemini-2.0-flash` / `gemini-1.5-flash`).
    2. Nhà cung cấp thay thế (Groq LPU).
    3. Bộ quy tắc dự phòng nội bộ (Deterministic Cookpad Rule Engine).
  - Kết quả trả về cho Frontend luôn là HTTP `200 OK` với mâm cơm chuẩn xác từ kho.

---

## 7. KIỂM THỬ TỰ ĐỘNG & TIÊU CHUẨN BUILD

Mọi Pull Request hoặc commit trước khi đẩy lên Git (`origin/feature/AI`) bắt buộc phải đạt tiêu chuẩn:
1. **Backend**: Lệnh `dotnet build` đạt **0 Error(s)**.
2. **Frontend**: Lệnh `npm run build` (gồm `tsc -b && vite build`) vượt qua kiểm tra type-checking và đóng gói đạt **0 Error(s)**.
3. Không làm ảnh hưởng đến các màn hình nghiệp vụ POS, Quản lý kho hay Báo cáo doanh thu đã có.
