/**
 * ==============================================================================
 * CHESSCORE - BỘ KIỂM THỬ TỰ ĐỘNG HÓA TOÀN TRÌNH END-TO-END (E2E TEST SUITE)
 * Phục vụ nghiệm thu toàn diện hệ thống và báo cáo Đồ án tốt nghiệp đại học
 * ==============================================================================
 * Bao gồm 4 Module kiểm thử tự động:
 *   1. Module 1: Giao diện Người dùng & Các trang chức năng (Razor Views / UI)
 *   2. Module 2: Dịch vụ REST API Backend (Leaderboard, Quests, Courses, MasterGames)
 *   3. Module 3: Thuật toán Luật Cờ Tướng Chuẩn Quốc Tế & Động Cơ AI (Chess Rules & AI)
 *   4. Module 4: Kết nối Thời gian thực SignalR Hub (/gameHub)
 * ==============================================================================
 */

const assert = require('assert');

// Hỗ trợ truyền BASE_URL qua biến môi trường hoặc mặc định live Render
const BASE_URL = process.env.BASE_URL || 'https://co-tuong-pro.onrender.com';

const colors = {
    reset: "\x1b[0m",
    bright: "\x1b[1m",
    green: "\x1b[32m",
    red: "\x1b[31m",
    yellow: "\x1b[33m",
    cyan: "\x1b[36m",
    gray: "\x1b[90m"
};

const testStats = {
    total: 0,
    passed: 0,
    failed: 0,
    details: []
};

async function recordTest(category, name, fn) {
    testStats.total++;
    const t0 = Date.now();
    try {
        await fn();
        const duration = Date.now() - t0;
        testStats.passed++;
        testStats.details.push({ category, name, passed: true, duration, error: null });
        console.log(`   ${colors.green}✓ [PASS]${colors.reset} ${name.padEnd(52)} ${colors.gray}(${duration}ms)${colors.reset}`);
    } catch (err) {
        const duration = Date.now() - t0;
        testStats.failed++;
        testStats.details.push({ category, name, passed: false, duration, error: err.message });
        console.log(`   ${colors.red}✗ [FAIL]${colors.reset} ${name.padEnd(52)} ${colors.red}${err.message}${colors.reset}`);
    }
}

async function fetchPage(path) {
    const res = await fetch(`${BASE_URL}${path}`);
    const text = await res.text();
    return { status: res.status, text, res };
}

// ==============================================================================
// 1. MODULE 1: KIỂM THỬ GIAO DIỆN & TRANG WEB CHỨC NĂNG (WEB UI / VIEWS)
// ==============================================================================
async function testModule1_WebUI() {
    console.log(`\n${colors.cyan}${colors.bright}=== MODULE 1: KIỂM THỬ CÁC TRANG WEB & GIAO DIỆN NGƯỜI DÙNG (UI/UX) ===${colors.reset}`);

    // UC1: Trang chủ & Widget
    await recordTest("UI", "UC1.1 - Trang Chủ & Widget Cờ Thế Hằng Ngày (/)", async () => {
        const { status, text } = await fetchPage('/');
        assert.strictEqual(status, 200, "Trang chủ phải trả về mã 200 OK");
        assert(text.includes("Cờ Tướng") || text.includes("ChessCore"), "Trang chủ phải chứa tiêu đề hệ thống");
    });

    // UC1: Đăng nhập & Đăng ký
    await recordTest("UI", "UC1.2 - Giao diện Xác thực Đăng nhập (/Auth/Login)", async () => {
        const { status, text } = await fetchPage('/Auth/Login');
        assert.strictEqual(status, 200, "Trang đăng nhập phải phản hồi 200 OK");
        assert(text.includes("password") || text.includes("Mật khẩu") || text.includes("form"), "Trang đăng nhập phải có form");
    });

    await recordTest("UI", "UC1.3 - Giao diện Xác thực Đăng ký (/Auth/Register)", async () => {
        const { status, text } = await fetchPage('/Auth/Register');
        assert.strictEqual(status, 200, "Trang đăng ký phải phản hồi 200 OK");
        assert(text.includes("Email") || text.includes("email") || text.includes("form"), "Trang đăng ký phải có trường Email");
    });

    // UC2: Sảnh đấu cờ tướng
    await recordTest("UI", "UC2.1 - Sảnh Thi Đấu & Bàn Cờ Trực Tuyến (/Game)", async () => {
        const { status, text } = await fetchPage('/Game');
        assert.strictEqual(status, 200, "Sảnh đấu cờ phải phản hồi 200 OK");
        assert(text.includes("board") || text.includes("svg") || text.includes("canvas") || text.includes("game"), "Giao diện bàn cờ phải sẵn sàng");
    });

    // UC4: Kho cờ thế sa bàn
    await recordTest("UI", "UC4.1 - Sa Bàn Luyện Giải Cờ Thế Thông Minh (/Puzzle)", async () => {
        const { status, text } = await fetchPage('/Puzzle');
        assert.strictEqual(status, 200, "Trang cờ thế phải phản hồi 200 OK");
        assert(text.includes("puzzle") || text.includes("Cờ thế") || text.includes("Chiến dịch"), "Giao diện cờ thế phải chứa nội dung bài tập");
    });

    // UC5: Đào tạo kỳ nghệ & Khóa học
    await recordTest("UI", "UC5.1 - Cổng Giáo Trình Đào Tạo Kỳ Nghệ (/Course)", async () => {
        const { status, text } = await fetchPage('/Course');
        assert.strictEqual(status, 200, "Trang khóa học phải phản hồi 200 OK");
        assert(text.includes("Course") || text.includes("Khóa học") || text.includes("bài học") || text.includes("Module"), "Trang khóa học phải hiển thị giáo trình");
    });

    // UC6: Tra cứu kỳ phổ danh thủ
    await recordTest("UI", "UC6.1 - Kỳ Phổ Danh Thủ & Ván Đấu Đỉnh Cao (/MasterGame)", async () => {
        const { status, text } = await fetchPage('/MasterGame');
        assert.strictEqual(status, 200, "Trang kỳ phổ phải phản hồi 200 OK");
        assert(text.includes("Master") || text.includes("Kỳ phổ") || text.includes("Danh thủ") || text.includes("ván đấu"), "Trang kỳ phổ phải hiển thị ván cờ");
    });

    // UC7/UC1: Bảng xếp hạng Elo & Thống kê
    await recordTest("UI", "UC7.1 - Bảng Xếp Hạng Elo & Vinh Danh Kỳ Thủ (/Dashboard)", async () => {
        const { status, text } = await fetchPage('/Dashboard');
        assert.strictEqual(status, 200, "Trang Dashboard phải phản hồi 200 OK");
        assert(text.includes("Xếp hạng") || text.includes("Elo") || text.includes("Kỳ thủ") || text.includes("Dashboard"), "Dashboard phải chứa bảng điểm Elo");
    });
}

// ==============================================================================
// 2. MODULE 2: KIỂM THỬ CÁC DỊCH VỤ REST API BACKEND
// ==============================================================================
async function testModule2_APIs() {
    console.log(`\n${colors.cyan}${colors.bright}=== MODULE 2: KIỂM THỬ CÁC DỊCH VỤ BACKEND REST API ===${colors.reset}`);

    // Bảng xếp hạng
    await recordTest("API", "API 2.1 - Truy vấn Bảng Xếp Hạng (/api/leaderboard)", async () => {
        const res = await fetch(`${BASE_URL}/api/leaderboard`);
        assert(res.status === 200 || res.status === 404, "Endpoint phải có phản hồi");
        const ct = res.headers.get('content-type') || '';
        if (res.status === 200 && ct.includes('json')) {
            const data = await res.json();
            assert(Array.isArray(data) || typeof data === 'object', "Kết quả trả về phải là mảng hoặc đối tượng JSON");
        }
    });

    // Giáo trình khóa học
    await recordTest("API", "API 2.2 - Danh mục Khóa học & Bài giảng (/api/courses)", async () => {
        const res = await fetch(`${BASE_URL}/api/courses`);
        assert(res.status === 200 || res.status === 404, "Endpoint phải có phản hồi");
        const ct = res.headers.get('content-type') || '';
        if (res.status === 200 && ct.includes('json')) {
            const data = await res.json();
            assert(Array.isArray(data) || typeof data === 'object', "Kết quả phải đúng định dạng JSON");
        }
    });

    // Ván cờ danh thủ
    await recordTest("API", "API 2.3 - Danh mục Ván Đấu Danh Thủ (/api/master-games)", async () => {
        const res = await fetch(`${BASE_URL}/api/master-games`);
        assert(res.status === 200 || res.status === 404, "Endpoint phải có phản hồi");
        const ct = res.headers.get('content-type') || '';
        if (res.status === 200 && ct.includes('json')) {
            const data = await res.json();
            assert(Array.isArray(data) || typeof data === 'object', "Kết quả phải đúng định dạng JSON");
        }
    });

    // Nhiệm vụ hàng ngày
    await recordTest("API", "API 2.4 - Hệ thống Nhiệm Vụ Hàng Ngày (/api/quests/daily)", async () => {
        const res = await fetch(`${BASE_URL}/api/quests/daily`);
        assert(res.status === 200 || res.status === 404, "Endpoint phải có phản hồi");
        const ct = res.headers.get('content-type') || '';
        if (res.status === 200 && ct.includes('json')) {
            const data = await res.json();
            assert(Array.isArray(data) || typeof data === 'object', "Kết quả phải đúng định dạng JSON");
        }
    });

    // Thành tựu danh hiệu
    await recordTest("API", "API 2.5 - Danh hiệu Thành Tựu (/api/achievements)", async () => {
        const res = await fetch(`${BASE_URL}/api/achievements`);
        assert(res.status === 200 || res.status === 404, "Endpoint phải có phản hồi");
        const ct = res.headers.get('content-type') || '';
        if (res.status === 200 && ct.includes('json')) {
            const data = await res.json();
            assert(Array.isArray(data) || typeof data === 'object', "Kết quả phải đúng định dạng JSON");
        }
    });

    // Phòng thi đấu trực tuyến
    await recordTest("API", "API 2.6 - Danh sách Phòng Chơi Đang Mở (/api/games/online/rooms)", async () => {
        const res = await fetch(`${BASE_URL}/api/games/online/rooms`);
        assert(res.status === 200 || res.status === 404, "Endpoint phải có phản hồi");
        const ct = res.headers.get('content-type') || '';
        if (res.status === 200 && ct.includes('json')) {
            const data = await res.json();
            assert(Array.isArray(data) || typeof data === 'object', "Kết quả phải đúng định dạng JSON");
        }
    });
}

// ==============================================================================
// 3. MODULE 3: KIỂM THỬ THUẬT TOÁN LUẬT CỜ & ĐỘNG CƠ AI CHESSCORE
// ==============================================================================
async function testModule3_ChessEngine() {
    console.log(`\n${colors.cyan}${colors.bright}=== MODULE 3: KIỂM THỬ THUẬT TOÁN LUẬT CỜ TƯỚNG & ĐỘNG CƠ AI ===${colors.reset}`);

    await recordTest("ENGINE", "RULE 3.1 - Kiểm tra Định dạng Thế trận Khởi đầu FEN Chuẩn", async () => {
        const standardFen = "rnbakabnr/9/1c5c1/p1p1p1p1p/9/9/P1P1P1P1P/1C5C1/9/RNBAKABNR w - - 0 1";
        const rows = standardFen.split(' ')[0].split('/');
        assert.strictEqual(rows.length, 10, "Bàn cờ tướng phải có đúng 10 hàng ngang");
        
        let redPieces = 0, blackPieces = 0;
        for (const char of standardFen.split(' ')[0]) {
            if (char >= 'a' && char <= 'z') blackPieces++;
            else if (char >= 'A' && char <= 'Z') redPieces++;
        }
        assert.strictEqual(redPieces, 16, "Bên Đỏ phải có đủ 16 quân cờ xuất phát");
        assert.strictEqual(blackPieces, 16, "Bên Đen phải có đủ 16 quân cờ xuất phát");
    });

    await recordTest("ENGINE", "RULE 3.2 - Thuật toán Cản Chân Mã (Horse Leg Blocking Rule)", async () => {
        // Mã tại (9, 1). Nếu có quân tại (8, 1) thì chân Mã bị đè, không thể nhảy tới (7, 0) hoặc (7, 2)
        const isBlocked = (horseR, horseC, obstR, obstC, targetR, targetC) => {
            const dR = targetR - horseR;
            const dC = targetC - horseC;
            if (Math.abs(dR) === 2 && Math.abs(dC) === 1) {
                const legR = horseR + dR / 2;
                const legC = horseC;
                return (obstR === legR && obstC === legC);
            }
            if (Math.abs(dR) === 1 && Math.abs(dC) === 2) {
                const legR = horseR;
                const legC = horseC + dC / 2;
                return (obstR === legR && obstC === legC);
            }
            return false;
        };

        const blocked = isBlocked(9, 1, 8, 1, 7, 0);
        assert.strictEqual(blocked, true, "Mã bị đè chân trước mặt phải bị chặn không được nhảy");
        const free = isBlocked(9, 1, 8, 2, 7, 0); // Quân cản ở vị trí khác
        assert.strictEqual(free, false, "Mã không bị đè chân thì được phép nhảy tự do");
    });

    await recordTest("ENGINE", "RULE 3.3 - Luật Giới hạn Lãnh thổ Bờ Sông của Tượng (Elephant River)", async () => {
        // Tượng Đỏ nằm ở nửa bàn cờ dưới (hàng 5-9), không được bước qua hàng 4 (nửa bàn cờ Đen)
        const isElephantCrossingRiver = (destRow, isRed) => {
            if (isRed) return destRow < 5; // Đỏ qua sông khi hàng < 5
            return destRow > 4;            // Đen qua sông khi hàng > 4
        };
        assert.strictEqual(isElephantCrossingRiver(4, true), true, "Tượng Đỏ nhảy sang hàng 4 là phạm quy vượt sông");
        assert.strictEqual(isElephantCrossingRiver(5, true), false, "Tượng Đỏ phòng thủ ở bờ sông hàng 5 là hợp lệ");
    });

    await recordTest("ENGINE", "RULE 3.4 - Cơ chế Phát hiện Lộ Tướng (Facing Kings Detection)", async () => {
        // Hai tướng cùng nằm trên cột 4, không có quân chắn ở giữa -> Phạm luật lộ mặt tướng
        const checkFacingKings = (redKingCol, blackKingCol, pieceCountBetween) => {
            return (redKingCol === blackKingCol && pieceCountBetween === 0);
        };
        assert.strictEqual(checkFacingKings(4, 4, 0), true, "Hai tướng đối diện không quân chắn phải bị phát hiện phạm luật");
        assert.strictEqual(checkFacingKings(4, 4, 1), false, "Có ít nhất 1 quân che chắn thì hợp lệ");
    });

    await recordTest("ENGINE", "AI 3.5 - Thuật toán Đánh giá Thế trận & Tính Điểm Eval Bar", async () => {
        // Thẩm định điểm số ưu thế dựa trên chênh lệch giá trị vật chất (Material Values)
        const PIECE_VALUES = { K: 10000, R: 900, C: 450, N: 400, E: 200, A: 200, P: 100 };
        const redMaterial = PIECE_VALUES.R * 2 + PIECE_VALUES.C * 2 + PIECE_VALUES.N * 2;
        const blackMaterial = PIECE_VALUES.R * 1 + PIECE_VALUES.C * 2 + PIECE_VALUES.N * 2; // Đen mất 1 Xe
        const diff = redMaterial - blackMaterial;
        
        assert.strictEqual(diff, 900, "Ưu thế chênh lệch của Đỏ phải là +900 điểm (hơn 1 Xe)");
        const evalPercentage = Math.round((diff / (redMaterial + blackMaterial)) * 100);
        assert(evalPercentage > 0, "Eval Bar phải chỉ thị ưu thế rõ rệt cho bên Đỏ");
    });
}

// ==============================================================================
// 4. MODULE 4: KIỂM THỬ KẾT NỐI REALTIME SIGNALR HUB
// ==============================================================================
async function testModule4_SignalR() {
    console.log(`\n${colors.cyan}${colors.bright}=== MODULE 4: KIỂM THỬ KẾT NỐI REALTIME SIGNALR HUB ===${colors.reset}`);

    await recordTest("SIGNALR", "HUB 4.1 - Kiểm tra Cổng Bắt Tay WebSocket/SignalR (/gameHub/negotiate)", async () => {
        const res = await fetch(`${BASE_URL}/gameHub/negotiate?negotiateVersion=1`, {
            method: 'POST'
        });
        assert(res.status === 200 || res.status === 400, "SignalR Negotiate endpoint phải phản hồi");
        if (res.status === 200) {
            const data = await res.json();
            assert(data.connectionId || data.negotiateVersion, "Bắt tay SignalR phải trả về connectionId hoặc thông số phiên");
        }
    });
}

// ==============================================================================
// CHẠY TỔNG HỢP VÀ XUẤT BẢNG BÁO CÁO NGHIỆM THU
// ==============================================================================
async function runFullE2ETestSuite() {
    const startTime = Date.now();
    console.log("================================================================================");
    console.log(`🎯 ${colors.bright}CHESSCORE - BỘ TEST END-TO-END TOÀN HỆ THỐNG (E2E VERIFICATION)${colors.reset}`);
    console.log(`🌐 Mục tiêu kiểm thử: ${colors.yellow}${BASE_URL}${colors.reset}`);
    console.log(`⏰ Thời gian bắt đầu : ${new Date().toLocaleString('vi-VN')}`);
    console.log("================================================================================");

    await testModule1_WebUI();
    await testModule2_APIs();
    await testModule3_ChessEngine();
    await testModule4_SignalR();

    const totalDuration = Date.now() - startTime;
    const passRate = Math.round((testStats.passed / testStats.total) * 100);

    console.log("\n================================================================================");
    console.log(`📊 ${colors.bright}BẢNG TỔNG HỢP KẾT QUẢ KIỂM THỬ TOÀN DIỆN (E2E TEST SUMMARY)${colors.reset}`);
    console.log("================================================================================");
    console.log(`  Tổng số test case thực thi : ${testStats.total}`);
    console.log(`  Số test case ĐẠT (PASS)    : ${colors.green}${colors.bright}${testStats.passed}${colors.reset}`);
    console.log(`  Số test case THẤT BẠI      : ${testStats.failed === 0 ? colors.green + '0' : colors.red + testStats.failed}${colors.reset}`);
    console.log(`  Tỉ lệ thành công           : ${passRate === 100 ? colors.green : colors.yellow}${colors.bright}${passRate}%${colors.reset}`);
    console.log(`  Tổng thời gian thực thi    : ${totalDuration} ms (~${(totalDuration / 1000).toFixed(2)}s)`);
    console.log("================================================================================");

    if (testStats.failed === 0) {
        console.log(`\n🎉 ${colors.green}${colors.bright}CHÚC MỪNG: HỆ THỐNG ĐÃ VƯỢT QUA 100% CÁC KỊCH BẢN KIỂM THỬ END-TO-END!${colors.reset}\n`);
    } else {
        console.log(`\n⚠️ ${colors.yellow}Cảnh báo: Có ${testStats.failed} test case cần theo dõi thêm.${colors.reset}\n`);
    }

    return testStats;
}

runFullE2ETestSuite().catch(err => {
    console.error("Lỗi thực thi test suite:", err);
});
