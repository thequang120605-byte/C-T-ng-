@echo off
chcp 65001 > nul
echo ================================================================================
echo       CHESSCORE - CÔNG CỤ CHẠY BỘ KIỂM THỬ ĐỘC LẬP 137 TEST CASES
echo ================================================================================
echo.
echo 1. Tự động kiểm tra & Pull mã nguồn mới nhất từ branch main trên GitHub Cloud.
echo 2. Tự động ghi nhận Commit Hash, Author và Thời gian Pull vào file Excel.
echo 3. Chạy kiểm thử tự động 137 Test Cases trên máy chủ Web localhost:5000.
echo 4. Ghi nhận trung thực kết quả Pass/Fail và tự động ghi Bug vào Sheet Defect Log.
echo.
python Testing/03_Ban3_E2ETests_Scripts/run_suite_137.py
echo.
echo ================================================================================
pause
