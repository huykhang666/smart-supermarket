import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, from } from 'rxjs';
import {
  ChatMessage,
  ChatSession,
  MealPlanChatResponse,
  AiChatRequest,
  AiChatResponse
} from './ai-chat.models';

@Injectable({
  providedIn: 'root'
})
export class AiChatService {
  private readonly baseUrl = 'http://localhost:5137/api/v1/ai';
  private readonly storageKey = 'smart_supermarket_ai_sessions';
  private currentSessionId: string = this.generateSessionId();

  constructor(private http: HttpClient) {}

  public getSessionId(): string {
    return this.currentSessionId;
  }

  public resetSession(): string {
    this.currentSessionId = this.generateSessionId();
    return this.currentSessionId;
  }

  // Gửi tin nhắn chat thông thường
  public sendMessage(request: AiChatRequest): Observable<any> {
    const payload = {
      ...request,
      sessionId: request.sessionId || this.currentSessionId
    };
    return this.http.post<any>(`${this.baseUrl}/chat`, payload);
  }

  // Tư vấn mâm cơm và nguyên liệu Cookpad
  public planMealAndShop(prompt: string, budget?: number, people?: number): Observable<any> {
    const payload = {
      userPrompt: prompt,
      targetBudget: budget,
      numberOfPeople: people,
      prioritizePromotions: true
    };
    return this.http.post<any>(`${this.baseUrl}/shopping/meal-plan`, payload);
  }

  // Stream phản hồi theo từng token (Typewriter / Streaming simulation)
  public streamText(fullText: string, speedMs: number = 18): Observable<string> {
    const subject = new Subject<string>();
    let currentIndex = 0;
    let accumulated = '';

    const intervalId = setInterval(() => {
      if (currentIndex < fullText.length) {
        // Đọc cụm từ 1-3 ký tự mỗi nhịp để mượt mà
        const step = Math.min(Math.floor(Math.random() * 3) + 1, fullText.length - currentIndex);
        accumulated += fullText.substring(currentIndex, currentIndex + step);
        currentIndex += step;
        subject.next(accumulated);
      } else {
        clearInterval(intervalId);
        subject.complete();
      }
    }, speedMs);

    return subject.asObservable();
  }

  // Quản lý lịch sử hội thoại trên LocalStorage
  public getHistory(): ChatSession[] {
    try {
      const data = localStorage.getItem(this.storageKey);
      if (data) {
        return JSON.parse(data);
      }
    } catch (e) {
      console.error('Lỗi khi đọc lịch sử chat:', e);
    }
    return [];
  }

  public saveSession(session: ChatSession): void {
    try {
      const sessions = this.getHistory();
      const existingIdx = sessions.findIndex(s => s.sessionId === session.sessionId);
      if (existingIdx >= 0) {
        sessions[existingIdx] = session;
      } else {
        sessions.unshift(session);
      }
      // Giữ tối đa 20 phiên gần nhất
      const trimmed = sessions.slice(0, 20);
      localStorage.setItem(this.storageKey, JSON.stringify(trimmed));
    } catch (e) {
      console.error('Lỗi khi lưu lịch sử chat:', e);
    }
  }

  public clearHistory(): void {
    localStorage.removeItem(this.storageKey);
  }

  // Nạp nguyên liệu mâm cơm vào giỏ hàng
  public injectCart(sessionId: string, productIds: number[]): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/cart/inject`, { sessionId, productIds });
  }

  private generateSessionId(): string {
    return 'session_' + Math.random().toString(36).substring(2, 11) + '_' + Date.now();
  }
}
