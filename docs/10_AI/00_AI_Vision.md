# 10_AI - TÀI LIỆU 00: TẦM NHÌN & ĐỊNH HƯỚNG CHIẾN LƯỢC AI (AI VISION)

---

## MỤC LỤC
1. [Giới thiệu & Bối cảnh](#1-giới-thiệu--bối-cảnh)
2. [Tầm nhìn & Sứ mệnh (Vision & Mission)](#2-tầm-nhìn--sứ-mệnh)
3. [Các trụ cột giá trị cốt lõi](#3-các-trụ-cột-giá-trị-cốt-lõi)
4. [Đối tượng thụ hưởng & Giá trị mang lại](#4-đối-tượng-thụ-hưởng--giá-trị-mang-lại)
5. [Nguyên tắc thiết kế hệ sinh thái AI](#5-nguyên-tắc-thiết-kế-hệ-sinh-thái-ai)

---

## 1. GIỚI THIỆU & BỐI CẢNH

Trong ngành bán lẻ thực phẩm và siêu thị hiện đại, người tiêu dùng hàng ngày phải đối mặt với câu hỏi quen thuộc: *"Hôm nay ăn gì?", "Nấu món gì vừa đủ dinh dưỡng, hợp ngân sách mà không lãng phí thực phẩm?"*. Về phía đơn vị vận hành siêu thị, bài toán quản trị tồn kho hàng tươi sống, hạn chế hao hụt do hết hạn sử dụng (Shelf-life & Expiry waste) và tối ưu hóa quy trình nhập hàng luôn là thách thức lớn nhất.

Phân hệ **10_AI** trong hệ thống **Smart SuperMarket** ra đời nhằm số hóa và nâng tầm trải nghiệm của cả hai phía:
- **Người tiêu dùng (Shopper)**: Trợ lý Ảo kiêm Đầu Bếp Gia Đình chuẩn vị Cookpad tư vấn thực đơn, tự động gom nguyên liệu từ kho và tối ưu ngân sách theo thời gian thực.
- **Nhà quản lý & Nhân viên (Supermarket Operations)**: Hệ thống AI Copilot phân tích dữ liệu bán lẻ, phát hiện sớm rủi ro đứt hàng hoặc hàng cận date để tự động kích hoạt Flash Sale và lập đơn nhập hàng nháp.

---

## 2. TẦM NHÌN & SỨ MỆNH

### 2.1. Tầm nhìn (Vision)
Xây dựng Smart SuperMarket trở thành nền tảng siêu thị thông minh dẫn đầu về ứng dụng AI tạo sinh (Generative AI) và Trí tuệ bán lẻ (Retail Intelligence) tại Việt Nam, biến việc đi chợ từ một nghĩa vụ mất thời gian thành trải nghiệm ẩm thực được cá nhân hóa trọn vẹn.

### 2.2. Sứ mệnh (Mission)
1. **Kết nối Ẩm thực & Kho vận**: Chuyển đổi mọi ý tưởng món ăn hàng ngày thành rổ hàng nguyên liệu chính xác, có sẵn trong kho với đơn giá minh bạch.
2. **Không lãng phí thực phẩm (Zero Food Waste)**: Ứng dụng AI nhận diện hạn sử dụng động để giải cứu thực phẩm cận date với mức chiết khấu thông minh.
3. **Trao quyền vận hành**: Cung cấp công cụ AI đắc lực hỗ trợ thu ngân và quản lý ra quyết định nhập kho chỉ với 1 cú nhấp chuột.

---

## 3. CÁC TRỤ CỘT GIÁ TRỊ CỐT LÕI

| Trụ cột | Tên gọi | Mô tả trọng tâm |
| :--- | :--- | :--- |
| **Trụ cột 1** | **Culinary AI Concierge** | Đầu bếp ảo am hiểu cẩm nang nấu nướng Cookpad Việt Nam, gợi ý 2-3 option thực đơn theo ngân sách (từ 30k sinh viên đến đại tiệc lẩu gia đình). |
| **Trụ cột 2** | **Dynamic Shelf-Life & Flash Sale** | Giám sát lô hàng (Batch Expiry Date), tự động liên kết hàng cận date vào thực đơn đề xuất kèm giảm giá 30% - 50%. |
| **Trụ cột 3** | **Retail Operations Copilot** | Phân tích giỏ hàng thị trường (Market Basket Analysis), cảnh báo sản phẩm có nguy cơ tồn kho/đứt hàng và gợi ý phiếu nhập kho tự động. |
| **Trụ cột 4** | **Hybrid AI Engine** | Kiến trúc kép linh hoạt: Sử dụng LLM hàng đầu (Google Gemini 2.5 Flash / Groq Llama 3.3) kết hợp bộ quy tắc dự phòng nội bộ (Deterministic Fallback) đảm bảo 100% thời gian hoạt động. |

---

## 4. ĐỐI TƯỢNG THỤ HƯỞNG & GIÁ TRỊ MANG LẠI

### 4.1. Khách hàng cá nhân & Sinh viên
- Giải quyết bài toán *"Có 30k - 50k ăn gì hôm nay?"* với những mâm cơm tiết kiệm, ngon miệng và đủ chất (cơm trứng chiên canh rau, mì xào xúc xích cải ngọt, cá hộp dưa leo).
- Chỉ 1 thao tác nhấn *"Chọn mâm này"* là toàn bộ nguyên liệu được tính toán định lượng và đưa thẳng vào giỏ hàng.
- Học hỏi được các bí quyết nấu ăn chuẩn vị: mẹo luộc rau xanh giòn, cách ướp thịt bò không bị dai, cách khử tanh hải sản...

### 4.2. Ban quản lý & Bộ phận mua hàng siêu thị
- Cắt giảm 70% thời gian phân tích số liệu tồn kho thủ công.
- Tự động hóa đề xuất nhập hàng dựa trên số lượng tồn kho và chu kỳ quay vòng hàng hóa.
- Chủ động giải phóng hàng hóa cận date thông qua các gói gợi ý món ăn, giảm thiểu thất thoát hủy hàng.

---

## 5. NGUYÊN TẮC THIẾT KẾ HỆ SINH THÁI AI

1. **Khách quan & Thực tế (Real Inventory Grounding)**: AI chỉ đề xuất những nguyên liệu thực sự có trong danh mục kho hàng của siêu thị với đúng đơn giá niêm yết.
2. **Chuẩn mực giao tiếp**: AI xưng là `"mình"` và gọi người dùng là `"bạn"`, giọng điệu thân thiện, chu đáo, không tự cao và không sử dụng đại từ không phù hợp.
3. **Căn lề chuẩn mực & Trực quan**: Toàn bộ nội dung tư vấn được căn lề trái rõ ràng, phân cấp danh mục món ăn mạch lạc, tránh lặp lại chi tiết giá thừa thãi.
4. **Bảo mật & Độ tin cậy cao**: Dữ liệu khách hàng được bảo vệ an toàn; hệ thống luôn duy trì bộ công thức dự phòng nội bộ khi kết nối mạng hoặc nhà cung cấp LLM gặp sự cố.
