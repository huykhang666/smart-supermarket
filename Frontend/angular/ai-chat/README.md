# Angular AI Chat Component (Smart SuperMarket)

Thành phần Chatbot Trí Tuệ Nhân Tạo dành cho ứng dụng Angular (v16, v17, v18+ standalone component).

---

## Tính năng nổi bật

1. **Chat UI hoàn chỉnh**: Giao diện mượt mà, căn lề trái chuẩn UX, hiển thị avatar AI và các thẻ gợi ý mâm cơm Cookpad.
2. **Streaming & Typewriter**: Giả lập hoặc đọc dòng dữ liệu trực tiếp, hiển thị từng ký tự với con trỏ nhấp nháy (`|`).
3. **Markdown Renderer**: Hỗ trợ định dạng in đậm, in nghiêng, tiêu đề, mã code, danh sách gạch đầu dòng.
4. **Typing Indicator**: Hiệu ứng 3 dấu chấm sóng động nhấp nháy khi đang chờ AI xử lý.
5. **History & Session Persistence**: Lưu trữ lịch sử các phiên hội thoại trong `localStorage`, cho phép mở lại hoặc xóa phiên cũ.
6. **Responsive**: Tự động co giãn theo màn hình (Widget nổi trên máy tính, Fullscreen/Bottom-sheet trên điện thoại di động).
7. **1-Click Add to Cart**: Nạp toàn bộ nguyên liệu của mâm cơm vào giỏ hàng chỉ với 1 cú click.

---

## Hướng dẫn tích hợp vào dự án Angular

### 1. Khai báo Component trong ứng dụng
Vì `AiChatComponent` là Standalone Component, bạn có thể import trực tiếp vào component cha:

```typescript
import { Component } from '@angular/core';
import { AiChatComponent } from './ai-chat';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AiChatComponent],
  template: `
    <router-outlet></router-outlet>
    <!-- Widget Trợ lý AI Đi Chợ -->
    <app-ai-chat></app-ai-chat>
  `
})
export class AppComponent {}
```

### 2. Cung cấp HttpClient
Đảm bảo ứng dụng Angular của bạn đã cấu hình `provideHttpClient()` trong `app.config.ts`:

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient()
  ]
};
```
