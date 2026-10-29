import { Component, OnInit, OnDestroy, ViewChild, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subscription } from 'rxjs';
import { AiChatService } from './ai-chat.service';
import { ChatMessage, ChatSession, MealOption } from './ai-chat.models';

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-chat.component.html',
  styleUrls: ['./ai-chat.component.css']
})
export class AiChatComponent implements OnInit, OnDestroy {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef<HTMLDivElement>;

  public isOpen = false;
  public isMobile = false;
  public showHistory = false;
  public isLoading = false;
  public inputText = '';
  public currentSessionId = '';
  public messages: ChatMessage[] = [];
  public chatSessions: ChatSession[] = [];

  private streamSubscription?: Subscription;

  constructor(
    private chatService: AiChatService,
    private sanitizer: DomSanitizer
  ) {}

  public ngOnInit(): void {
    this.checkScreenSize();
    this.currentSessionId = this.chatService.getSessionId();
    this.loadHistoryFromStorage();

    if (this.messages.length === 0) {
      this.addWelcomeMessage();
    }
  }

  public ngOnDestroy(): void {
    this.streamSubscription?.unsubscribe();
  }

  @HostListener('window:resize')
  public onResize(): void {
    this.checkScreenSize();
  }

  public toggleChat(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.scrollToBottom();
    }
  }

  public toggleHistory(): void {
    this.showHistory = !this.showHistory;
    if (this.showHistory) {
      this.loadHistoryFromStorage();
    }
  }

  public startNewChat(): void {
    this.currentSessionId = this.chatService.resetSession();
    this.messages = [];
    this.addWelcomeMessage();
    this.showHistory = false;
  }

  public loadSession(session: ChatSession): void {
    this.currentSessionId = session.sessionId;
    this.messages = [...session.messages];
    this.showHistory = false;
    this.scrollToBottom();
  }

  public clearAllHistory(): void {
    this.chatService.clearHistory();
    this.chatSessions = [];
    this.showHistory = false;
  }

  public sendQuickPrompt(promptText: string): void {
    this.inputText = promptText;
    this.sendMessage();
  }

  public sendMessage(): void {
    const text = this.inputText.trim();
    if (!text || this.isLoading) {
      return;
    }

    // 1. Thêm tin nhắn của User
    const userMsg: ChatMessage = {
      id: 'usr_' + Date.now(),
      role: 'user',
      content: text,
      timestamp: new Date()
    };
    this.messages.push(userMsg);
    this.inputText = '';
    this.isLoading = true;
    this.scrollToBottom();

    // 2. Gọi backend lập mâm cơm & tư vấn qua service
    this.chatService.planMealAndShop(text).subscribe({
      next: (res) => {
        const fullResponse = res?.data?.assistantMessage || res?.assistantMessage || res?.data?.reply || res?.reply || 'Mình đã tìm thấy nguyên liệu phù hợp cho bạn!';
        const mealData = res?.data || res;

        this.isLoading = false;

        // 3. Chuẩn bị bubble của Assistant với cờ Streaming = true
        const assistantMsg: ChatMessage = {
          id: 'ast_' + Date.now(),
          role: 'assistant',
          content: '',
          timestamp: new Date(),
          isStreaming: true,
          mealData: mealData
        };
        this.messages.push(assistantMsg);
        this.scrollToBottom();

        // 4. Kích hoạt hiệu ứng Streaming chữ từng token (typewriter)
        this.streamSubscription?.unsubscribe();
        this.streamSubscription = this.chatService.streamText(fullResponse, 16).subscribe({
          next: (partialText) => {
            assistantMsg.content = partialText;
            this.scrollToBottom();
          },
          complete: () => {
            assistantMsg.isStreaming = false;
            this.persistCurrentSession();
          }
        });
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Lỗi khi trò chuyện với AI:', err);
        this.messages.push({
          id: 'err_' + Date.now(),
          role: 'assistant',
          content: 'Dạ, hiện tại kết nối đến máy chủ AI đang bận. Bạn vui lòng thử lại sau giây lát nhé!',
          timestamp: new Date()
        });
        this.scrollToBottom();
      }
    });
  }

  public addMealTrayToCart(option: MealOption): void {
    const productIds = option.ingredients.map(i => i.productId);
    this.chatService.injectCart(this.currentSessionId, productIds).subscribe({
      next: () => {
        this.messages.push({
          id: 'cart_' + Date.now(),
          role: 'assistant',
          content: `🛒 **Đã nạp thành công:** Toàn bộ nguyên liệu món **"${option.dishName}"** (Tổng cộng ${option.totalEstimatedCost.toLocaleString('vi-VN')} đ) đã được đưa vào giỏ hàng của bạn!`,
          timestamp: new Date()
        });
        this.persistCurrentSession();
        this.scrollToBottom();
      },
      error: () => {
        this.messages.push({
          id: 'cart_err_' + Date.now(),
          role: 'assistant',
          content: 'Không thể thêm vào giỏ hàng lúc này, bạn vui lòng thử lại nhé.',
          timestamp: new Date()
        });
      }
    });
  }

  // Bộ phân tích Markdown đơn giản, an toàn
  public renderMarkdown(raw: string): SafeHtml {
    if (!raw) return '';

    let html = raw
      // Escape HTML tags cơ bản
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      // Bold
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      // Italic
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      // Inline code
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      // Headers
      .replace(/^### (.*$)/gim, '<h5 style="margin:6px 0; font-size:13px; font-weight:700;">$1</h5>')
      .replace(/^## (.*$)/gim, '<h4 style="margin:8px 0; font-size:14px; font-weight:700;">$1</h4>')
      // Lists
      .replace(/^\s*-\s+(.*$)/gim, '<li style="margin-left:14px;">$1</li>')
      // Line breaks
      .replace(/\n/g, '<br/>');

    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  private addWelcomeMessage(): void {
    this.messages.push({
      id: 'welcome',
      role: 'assistant',
      content: 'Xin chào bạn! 👋 Mình là **Trợ Lý AI Đi Chợ & Đầu Bếp Thông Minh**.\n\n' +
               'Mình có thể tư vấn các món ăn gia đình ngon miệng (theo cẩm nang Cookpad), ' +
               'tối ưu chi phí theo ngân sách và tự động săn hàng **Flash Sale cận date giảm 30% - 50%** tại siêu thị.\n\n' +
               'Hôm nay bạn muốn nấu món gì hay ngân sách đi chợ khoảng bao nhiêu?',
      timestamp: new Date()
    });
  }

  private persistCurrentSession(): void {
    if (this.messages.length <= 1) return;

    const firstUserMsg = this.messages.find(m => m.role === 'user');
    const title = firstUserMsg ? firstUserMsg.content.substring(0, 32) + '...' : 'Cuộc trò chuyện mới';

    const session: ChatSession = {
      sessionId: this.currentSessionId,
      title: title,
      createdAt: new Date(),
      messages: this.messages
    };

    this.chatService.saveSession(session);
  }

  private loadHistoryFromStorage(): void {
    this.chatSessions = this.chatService.getHistory();
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    }, 40);
  }

  private checkScreenSize(): void {
    this.isMobile = window.innerWidth <= 768;
  }
}
