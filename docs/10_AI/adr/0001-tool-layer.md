# ADR-0001: THIẾT KẾ PHÂN TÁCH LỚP CÔNG CỤ (TOOL LAYER SEPARATION)

## Trạng Thái (Status)
Đã chấp thuận (Accepted)

## Bối Cảnh (Context)
Trong các hệ thống AI sơ khởi, các AI Agent thường gọi trực tiếp các phương thức nghiệp vụ trong `BusinessService` hoặc truy vấn trực tiếp Entity Framework. Điều này dẫn đến sự phụ thuộc chặt chẽ giữa logic điều phối của LLM và logic nghiệp vụ cốt lõi, khiến việc ghi log, kiểm thử đơn vị (Unit Test) và đo lường hiệu năng của LLM trở nên vô cùng khó khăn.

## Quyết Định (Decision)
**Agent tuyệt đối không được gọi Service trực tiếp.** Mọi tương tác của Agent với dữ liệu hệ thống bắt buộc phải đi qua **Tool Layer** thông qua **Tool Dispatcher**.
- Agent $\rightarrow$ Tool Dispatcher $\rightarrow$ Tool $\rightarrow$ Business Service $\rightarrow$ Repository $\rightarrow$ SQL Server.

## Lý Do (Reason)
1. **Dễ kiểm thử (Testability)**: Có thể dễ dàng mock các Tool để kiểm thử prompt và phản ứng của LLM mà không cần dựng database thật.
2. **Dễ quan sát (Audit & Logging)**: Tool Dispatcher đóng vai trò interceptor ghi lại toàn bộ tham số input/output của LLM, phục vụ truy vết sai lệch.
3. **Dễ thay thế (Interchangeability)**: Khi nâng cấp hoặc sửa đổi Business Service, chữ ký của Tool phục vụ LLM hoàn toàn không đổi.
4. **Phòng chống treo hệ thống (Resilience)**: Có thể áp dụng Timeout (1500ms) và Retry tự động tại Tool Dispatcher.

## Hệ Quả (Consequences)
- Cần phát triển thêm một tầng trung gian (`Tools/` và `Dispatcher/`).
- Cần duy trì mapping rõ ràng giữa tham số JSON Schema của Tool và DTO C#.
