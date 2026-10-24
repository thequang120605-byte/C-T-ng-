# Engine luyện tập trên thiết bị

Các file trong Offline/ lấy từ source ChessCore_H4_GitHub.zip do người dùng cung cấp, đổi namespace để không kéo theo EF/ASP.NET. Đây không phải engine mới tự nhận đạt chuẩn liên đoàn.

Minimax thêm cancellation, time budget, giới hạn transposition table 60.000 entries và kết quả dự phòng hợp lệ nếu hết ngân sách sớm. Board search được clone, nên cancellation không làm hỏng session. OfflineGameService tuần tự hóa mutation từng ván, chạy việc CPU trong Task.Run; snapshot trả ra không chứa tham chiếu list có thể sửa session.

Session ID âm chỉ có ý nghĩa nội bộ ứng dụng. HybridApiService chỉ chuyển ID âm sang local engine; ID dương và auth/online giữ API server. Không gửi ID âm lên backend, không dùng kết quả luyện tập để đổi Elo.

Nullable warnings được tắt riêng trong phần engine kế thừa vì Board dùng null làm ô trống; annotations vẫn bật. Các service mới dùng nullable thông thường. Cần tiếp tục kiểm thử nhiều thế cờ và luật thi đấu nếu mở rộng ngoài phạm vi luyện tập.
