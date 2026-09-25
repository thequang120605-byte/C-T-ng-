# CHƯƠNG: THIẾT KẾ HÀM ĐÁNH GIÁ (EVALUATION FUNCTION)

## PHẦN H1: ĐIỂM QUÂN CỜ (PIECE VALUE / MATERIAL VALUE)

### 1. Mục đích và Định nghĩa

- **Bản chất:** Hệ tích $H_1$ đại diện cho yếu tố cơ bản và cốt lõi nhất trong hàm đánh giá tổng điểm của cờ tướng: **Tương quan lực lượng vật chất trên bàn cờ**[cite: 2, 3].
- **Nguyên tắc hoạt động:** Hệ thống sẽ tiến hành duyệt qua toàn bộ bàn cờ, đếm tổng số lượng quân cờ hiện có của bên mình trừ đi tổng số lượng quân cờ của đối phương, sau đó nhân với trọng số (điểm số cố định) quy định cho từng loại quân[cite: 2, 3].
- **Đặc điểm:** $H_1$ là hệ tích tĩnh, mang tính độc lập với tọa độ không gian (không xét đến vị trí ô đứng hay chiến lược cục bộ, những yếu tố này thuộc về $H_2$ và $H_3$).

---

### 2. Công thức toán học của hệ tích H1

Công thức tính điểm cơ bản của hệ tích $H_1$ cho một bên chơi được biểu diễn như sau:

$$H_1 = \sum_{i \in \text{Quân mình}} V(i) - \sum_{j \in \text{Quân địch}} V(j)$$

Trong đó:

- $V(i)$: Giá trị cố định của loại quân cờ thứ $i$.
- Tổng điểm $H_1$ dương thể hiện bên mình đang ưu thế về số lượng/chất quân, ngược lại điểm âm thể hiện sự bất lợi về lực lượng.

---

### 3. Bảng điểm chuẩn cho các quân cờ (Material Values)

Giá trị quân cờ trong cờ tướng không hoàn toàn tĩnh ở mọi thời điểm mà có sự dịch chuyển nhẹ tùy theo giai đoạn trận đấu (**Khai/Trung cuộc** so với **Tàn cuộc**), do sự thay đổi về tầm ảnh hưởng của quân cờ khi bàn cờ thưa dần[cite: 2]:

| Loại quân cờ          | Ký hiệu (Notation) | Giá trị Khai/Trung cuộc |    Giá trị Tàn cuộc     | Ghi chú chiến lược & Chức năng                                                     |
| :-------------------- | :----------------: | :---------------------: | :---------------------: | :--------------------------------------------------------------------------------- |
| **Tướng (King)**      |         K          | 10,000 (hoặc $\infty$)  | 10,000 (hoặc $\infty$)  | Quân cốt lõi bảo vệ Cửu cung; mất Tướng xem như thua trận[cite: 2].                |
| **Xe (Rook)**         |         R          |           900           |           900           | Quân chủ lực mạnh nhất bàn cờ, linh hoạt, tầm hoạt động không hạn chế[cite: 2].    |
| **Pháo (Cannon)**     |         C          |        450 - 500        |        400 - 430        | Tấn công tầm xa mạnh ở khai cuộc; suy yếu dần ở tàn cuộc do thiếu "ngòi"[cite: 2]. |
| **Mã (Knight)**       |         N          |           400           |        430 - 450        | Cận chiến linh hoạt; mạnh lên rõ rệt ở tàn cuộc nhờ ít bị cản chân[cite: 2].       |
| **Tượng (Bishop)**    |         B          |           200           |           250           | Chuyên trách phòng thủ vòng trong, bị giới hạn phạm vi ở sân nhà[cite: 2].         |
| **Sĩ (Advisor)**      |         A          |           200           |           250           | Hộ vệ trực tiếp cho Tướng bên trong Cửu cung[cite: 2].                             |
| **Tốt / Chốt (Pawn)** |         P          |   100 (Chưa qua sông)   | 250 - 300 (Đã qua sông) | Số lượng đông; giá trị tăng vọt khi đã qua sông và áp sát Cửu cung địch[cite: 2].  |

### 4. Triển khai lập trình thực tế (Implementation)

Trong mã nguồn của chương trình AI, bảng điểm $H_1$ được cấu trúc dưới dạng các hằng số (Constants) để hàm đánh giá dễ dàng truy xuất giá trị cơ bản:

