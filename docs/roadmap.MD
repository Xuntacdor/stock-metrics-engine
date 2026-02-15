# 🚀 QuantIQ Platform - Advanced Development Roadmap

Tài liệu này quy định lộ trình phát triển cho các module nghiệp vụ chứng khoán chuyên sâu (Wealth, Risk, Payment, AI & Infra).

**Giai đoạn:** Phase 2 (Sau khi hoàn thành Core: Crawler, DB, Chart).  
**Team Size:** 5 Members.  
**Tech Stack:** .NET 8 (Backend), Angular 17 (Frontend), Python (AI/Data), SQL Server (DB).

---

## 📅 Tuần 1-2: Module Quản lý Tài sản & Thuế (Wealth Management)
*Mục tiêu: Biến hệ thống từ "xem giá" thành "quản lý danh mục đầu tư" thực thụ.*

### Task 1: Thiết kế Database nghiệp vụ (Backend/DB)
- [ ] **Subtask 1.1:** Tạo bảng `Transactions` (Lịch sử giao dịch).
    - *Fields:* `TransID`, `UserID`, `Symbol`, `Type` (Buy/Sell/Dividend), `Price`, `Quantity`, `Fee`, `Tax`, `Time`.
- [ ] **Subtask 1.2:** Tạo bảng `Portfolios` (Lưu trạng thái hiện tại).
    - *Fields:* `UserID`, `Symbol`, `TotalQuantity`, `AvgPrice` (Giá vốn bình quân).

### Task 2: Logic tính toán P&L (Backend - C#)
- [ ] **Subtask 2.1:** Viết thuật toán tính **Giá vốn bình quân gia quyền (Weighted Average Cost)**.
    - *Logic:* `(Giá cũ * SL cũ + Giá mới * SL mới) / Tổng SL`.
- [ ] **Subtask 2.2:** Viết API tính **Realized P&L** (Lãi thực nhận khi bán).
    - *Logic:* `(Giá bán - Giá vốn) * SL Bán - (Phí + Thuế)`.
- [ ] **Subtask 2.3:** Viết API tính **Unrealized P&L** (Lãi tạm tính theo giá thị trường).
    - *Logic:* `(Giá thị trường hiện tại - Giá vốn) * SL đang giữ`.

### Task 3: Xử lý Quyền & Cổ tức (Corporate Actions - Advanced)
- [ ] **Subtask 3.1:** Tạo bảng `CorporateActions` lưu lịch chia cổ tức.
- [ ] **Subtask 3.2:** Viết **Background Job (Hangfire/Worker Service)** chạy định kỳ 00:00 hàng ngày.
    - *Job:* Quét các user đang giữ mã cổ phiếu đến ngày GDKHQ -> Tự động cộng tiền hoặc cổ phiếu thưởng vào tài khoản.

---

## 📅 Tuần 3: Module Nạp/Rút tiền & E-KYC (Payment Gateway)
*Mục tiêu: Xử lý dòng tiền vào/ra hệ thống.*

### Task 1: Tích hợp định danh (E-KYC)
- [ ] **Subtask 1.1:** Tích hợp API OCR (FPT.AI hoặc Google Vision) để đọc CCCD.
- [ ] **Subtask 1.2:** Tạo quy trình duyệt tài khoản (User upload ảnh -> Admin/AI duyệt -> Active tài khoản).

### Task 2: Cổng thanh toán (Payment)
- [ ] **Subtask 2.1:** Giả lập cổng thanh toán (Mock Payment API) hoặc tích hợp Sandbox (VNPay/Sepay).
    - *Flow:* User tạo lệnh nạp -> Chuyển sang trang thanh toán -> Webhook nhận kết quả -> Cộng tiền vào `CashBalance`.
- [ ] **Subtask 2.2:** Xử lý logic **Race Condition** (Tranh chấp dữ liệu).
    - *Yêu cầu:* Đảm bảo không bao giờ cộng tiền 2 lần cho 1 giao dịch (Sử dụng Database Transaction & Locking).

---

## 📅 Tuần 4-5: Module Quản trị rủi ro (Risk & Compliance)
*Mục tiêu: "Sống còn" - Đảm bảo an toàn vốn cho công ty và khách hàng.*

### Task 1: Quản lý Sức mua & Margin
- [ ] **Subtask 1.1:** Thiết lập bảng `MarginRatios` (Tỷ lệ ký quỹ cho từng mã, ví dụ: FPT 50%, Penny 0%).
- [ ] **Subtask 1.2:** Viết Middleware kiểm tra **Pre-Trade Risk Check**.
    - *Logic:* Trước khi lệnh được gửi vào DB, hệ thống phải tính: `Sức mua = Tiền mặt + (Giá trị chứng khoán * Tỷ lệ Margin)`. Nếu không đủ -> Từ chối lệnh.

### Task 2: Xử lý Call Margin & Force Sell
- [ ] **Subtask 2.1:** Viết Service tính **Tỷ lệ tài khoản (Rtt)** theo thời gian thực.
    - *Công thức:* `Rtt = Tài sản ròng / Tổng nợ vay`.
- [ ] **Subtask 2.2:** Tạo hệ thống cảnh báo tự động.
    - *Mức 1:* Rtt < 85% -> Gửi Email/Noti cảnh báo nộp tiền (Call Margin).
    - *Mức 2:* Rtt < 80% -> Kích hoạt lệnh **Force Sell** (Bán giải chấp tự động) để thu hồi nợ.

---

## 📅 Tuần 6-7: Module Tin tức & AI Sentiment (News & Social)
*Mục tiêu: Cung cấp Insight đầu tư thông minh.*

### Task 1: News Aggregator (Python Crawler)
- [ ] **Subtask 1.1:** Viết Crawler quét tin tức từ CafeF, Vietstock.
- [ ] **Subtask 1.2:** Lưu tin tức vào ElasticSearch hoặc SQL với Full-text Search.

### Task 2: AI Sentiment Analysis (Python Model)
- [ ] **Subtask 2.1:** Sử dụng thư viện `Transformers` (Phobit/BERT) hoặc OpenAI API.
- [ ] **Subtask 2.2:** Phân loại tin tức: `Tích cực` (Bullish) / `Tiêu cực` (Bearish) / `Trung lập`.
- [ ] **Subtask 2.3:** Hiển thị điểm số Sentiment lên biểu đồ giá (Ví dụ: Tin xấu ra -> Giá giảm).

### Task 3: Social Trading
- [ ] **Subtask 3.1:** Xây dựng tính năng Comment/Thảo luận dưới mỗi mã cổ phiếu.
- [ ] **Subtask 3.2:** Tạo bảng xếp hạng "Top Trader" dựa trên % lãi thực tế (Realized P&L).

---

## 📅 Tuần 8: Module Hạ tầng & Vận hành (Infrastructure)
*Mục tiêu: Đảm bảo hệ thống không sập (High Availability).*

### Task 1: Logging & Monitoring
- [ ] **Subtask 1.1:** Cài đặt **ELK Stack** (Elasticsearch, Logstash, Kibana) hoặc **Prometheus + Grafana**.
- [ ] **Subtask 1.2:** Ghi log toàn bộ hành động nhạy cảm (Audit Trail): Đặt lệnh, Hủy lệnh, Nạp tiền, Sửa thông tin User.

### Task 2: High Availability (HA) & Caching
- [ ] **Subtask 2.1:** Cấu hình **Redis** để cache bảng giá và thông tin User (giảm tải cho SQL Server).
- [ ] **Subtask 2.2:** Thiết lập **Nginx Load Balancer** (chia tải nếu chạy nhiều container Backend).
- [ ] **Subtask 2.3:** Thiết lập **Database Replication** (Master-Slave) để backup dữ liệu phòng khi Server chính bị cháy.

---

### 📝 Ghi chú cho Team

1.  **Tester:** Tập trung kỹ vào tuần 4 (Risk). Test case phải bao gồm các trường hợp cực đoan (thị trường sập sàn, user âm vốn).
2.  **Backend:** Chú ý các giao dịch tiền tệ (Money) phải dùng `DECIMAL`, tuyệt đối không dùng `FLOAT`.
3.  **Frontend:** Cập nhật UI Real-time bằng **SignalR** hoặc **WebSocket** khi tài sản biến động.