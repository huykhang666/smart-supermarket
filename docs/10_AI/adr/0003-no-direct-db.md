# ADR-0003: CẤM AGENT & LLM TRUY CẬP CƠ SỞ DỮ LIỆU TRỰC TIẾP

## Trạng Thái (Status)
Đã chấp thuận (Accepted)

## Bối Cảnh (Context)
Một số giải pháp Text-to-SQL cho phép LLM tự do sinh mã SQL và thực thi trực tiếp trên cơ sở dữ liệu quan hệ. Tuy nhiên, cách tiếp cận này tiềm ẩn rủi ro bảo mật nghiêm trọng: SQL Injection, rò rỉ thông tin giá vốn nhập hàng (`CostPrice`), vô tình làm lộ dữ liệu cá nhân khách hàng, hoặc gây nghẽn database do sinh các câu lệnh truy vấn phức tạp không có Index.

## Quyết Định (Decision)
**Tuyệt đối cấm Agent và LLM truy cập cơ sở dữ liệu trực tiếp.**
- Mọi truy vấn bắt buộc phải đi qua chuỗi: `Tool -> Business Service -> Repository (EF Core) -> SQL Server`.
- Mọi câu lệnh đọc dữ liệu bắt buộc sử dụng `.AsNoTracking()`.
- Các trường nhạy cảm (`CostPrice`, `PasswordHash`, `SupplierPrivateInfo`) bị loại bỏ hoàn toàn khỏi DTO trả về cho Agent.

## Lý Do (Reason)
- Bảo vệ an toàn 100% cho cơ sở dữ liệu sản xuất.
- Đảm bảo các chỉ mục (Indexes) của EF Core luôn được tận dụng tối đa, kiểm soát thời gian truy vấn $<50\text{ms}$.
- Tránh việc LLM vô tình ghi đè hoặc xóa dữ liệu sản phẩm/đơn hàng.

## Hệ Quả (Consequences)
- Không hỗ trợ truy vấn các câu hỏi phân tích dữ liệu tùy biến (Ad-hoc Text-to-SQL) mà phải định nghĩa trước chữ ký trong Tool Layer.
