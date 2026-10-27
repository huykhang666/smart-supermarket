# WORKFLOW: QUY TRÌNH TƯ VẤN MÂM CƠM (MEAL PLANNING WORKFLOW)

```mermaid
flowchart TD
    User([Khách gửi yêu cầu]) --> Ctrl[AIController]
    Ctrl --> Conv[ConversationService]
    Conv --> PB[PromptBuilder]
    PB --> Orch[AgentOrchestrator]
    Orch --> Shop[ShoppingAgent]
    
    Shop --> Rec[RecipeAgent tra cứu công thức Cookpad]
    Shop --> Inv[InventoryAgent kiểm tra tồn kho & cận date]
    Shop --> Promo[PromotionAgent áp dụng Flash Sale]
    Shop --> Bud[BudgetAgent cân đối ngân sách]
    
    Bud --> Synth[ShoppingAgent tổng hợp 3 Option mâm cơm]
    Synth --> Ctrl
    Ctrl --> UI[Hiển thị mâm cơm căn trái lên giao diện]
```

---

## Các Bước Thực Thi Chi Tiết
1. **Tiếp nhận yêu cầu**: Lọc số người ăn, ngân sách dự kiến và từ khóa món ăn.
2. **Khớp nguyên liệu**: Gọi `RecipeTool` để lấy danh sách nguyên liệu và định lượng theo `servings`.
3. **Kiểm tra tồn kho**: Gọi `InventoryTool` để đối chiếu tồn kho thực tế, nếu hết hàng tự động đổi sang món thay thế tương đương.
4. **Ưu tiên Flash Sale**: Tự động ghép các lô hàng $\le 3$ ngày giảm 30% để giảm chi phí bữa ăn.
5. **Cân đối chi phí**: Nếu tổng tiền $> \text{Budget}$, kích hoạt `BudgetAgent` hạ bớt món đắt tiền để bảo toàn ngân sách.
