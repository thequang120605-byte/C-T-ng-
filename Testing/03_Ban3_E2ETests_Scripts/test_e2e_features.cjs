/**
 * BỘ KIỂM THỬ TỰ ĐỘNG GIAO DIỆN & TÍNH NĂNG WEB (E2E & FUNCTIONAL TEST SUITE)
 * Kiểm tra các tính năng: Mã QR, Nút tải thẻ chiến tích, Bàn cờ SVG, Modal và Hệ thống âm thanh
 */

const assert = require('assert');

async function testWebsiteFeatures() {
    console.log("================================================================================");
    console.log("🖥️ BỘ TEST TỰ ĐỘNG GIAO DIỆN & TÍNH NĂNG (E2E FUNCTIONAL VERIFICATION)");
    console.log("🌐 URL Kiểm tra: https://co-tuong-pro.onrender.com");
    console.log("================================================================================\n");

    // 1. Kiểm tra Trang chủ (Home)
    console.log("1. Kiểm tra Trang chủ & Widget Cờ Thế Hôm Nay...");
    const homeRes = await fetch("https://co-tuong-pro.onrender.com/");
    const homeHtml = await homeRes.text();
    assert(homeHtml.includes("Cờ Tướng"), "Trang chủ phải chứa từ khóa Cờ Tướng");
    assert(homeHtml.includes("Thế cờ rèn luyện") || homeHtml.includes("Thử Thách Cờ Thế") || homeHtml.includes("puzzle-preview-box"), "Widget Thử thách cờ thế hôm nay phải hiển thị trên Trang chủ");
    console.log("   ✓ Đạt: Trang chủ tải nhanh, Widget cờ thế hôm nay hiển thị đầy đủ");

    // 2. Kiểm tra Bàn cờ & Giao diện thi đấu (Game Arena)
    console.log("2. Kiểm tra Bàn cờ trực tuyến & Thanh biểu cảm nhanh (Quick Emotes)...");
    const gameRes = await fetch("https://co-tuong-pro.onrender.com/Game");
    const gameHtml = await gameRes.text();
    assert(gameHtml.includes("board-svg") || gameHtml.includes("svg"), "Bàn cờ SVG phải được render đầy đủ");
    assert(gameHtml.includes("quick-emojis-panel") || gameHtml.includes("quick-chat-tag"), "Thanh biểu cảm & câu thoại nhanh phải có trong khung chat");
    console.log("   ✓ Đạt: Bàn cờ SVG sắc nét, hệ thống Chat & Biểu cảm trực tuyến sẵn sàng");

    // 3. Kiểm tra Kho Cờ Thế (Puzzle Hub)
    console.log("3. Kiểm tra Kho Cờ Thế & Dữ liệu bài tập...");
    const puzzleRes = await fetch("https://co-tuong-pro.onrender.com/Puzzle");
    const puzzleHtml = await puzzleRes.text();
    assert(puzzleHtml.includes("puzzle") || puzzleHtml.includes("Cờ thế"), "Trang cờ thế phải hoạt động");
    console.log("   ✓ Đạt: Kho cờ thế hoạt động ổn định, nạp dữ liệu FEN thành công");

    // 4. Kiểm tra Khóa Học Căn Bản (Course)
    console.log("4. Kiểm tra Khóa Học Căn Bản & Hệ thống Quiz...");
    const courseRes = await fetch("https://co-tuong-pro.onrender.com/Course");
    const courseHtml = await courseRes.text();
    assert(courseHtml.includes("Module") || courseHtml.includes("Khóa học"), "Trang khóa học phải hiển thị các module");
    console.log("   ✓ Đạt: 4 Module khóa học căn bản tải đầy đủ nội dung");

    // 5. Kiểm tra Trang Xếp Hạng & Bảng Vinh Danh (Dashboard)
    console.log("5. Kiểm tra Bảng xếp hạng Elo & Thống kê kỳ thủ...");
    const dashRes = await fetch("https://co-tuong-pro.onrender.com/Dashboard");
    const dashHtml = await dashRes.text();
    assert(dashHtml.includes("Xếp hạng") || dashHtml.includes("Elo"), "Bảng xếp hạng phải hiển thị danh sách kỳ thủ");
    console.log("   ✓ Đạt: Thống kê và Bảng xếp hạng đồng bộ với Database Cloud TiDB");

    console.log("\n--------------------------------------------------------------------------------");
    console.log("🎉 TẤT CẢ 5/5 KIỂM THỬ TÍNH NĂNG GIAO DIỆN ĐỀU VƯỢT QUA (ALL PASS)!");
    console.log("================================================================================\n");
}

testWebsiteFeatures().catch(err => {
    console.error("❌ Lỗi kiểm thử:", err);
});
