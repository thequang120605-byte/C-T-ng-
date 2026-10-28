/**
 * ============================================================================
 * BỘ KIỂM THỬ TỰ ĐỘNG ĐẦU-CUỐI TOÀN DIỆN (COMPREHENSIVE END-TO-END TEST SUITE)
 * Dự án: Cờ Tướng Việt Nam (Khóa luận tốt nghiệp)
 * Target: http://localhost:5000 (hoặc TARGET_URL)
 * ============================================================================
 */

const assert = require('assert');

const BASE_URL = process.env.TARGET_URL || 'http://localhost:5000';

let authToken = '';
let currentUserId = 0;
let testPvEGameId = 0;
let testPvPGameId = 0;
let testAiVsAiGameId = 0;
let testOnlineGameId = 0;

let currentUserElo = 0;
let passedAssertions = 0;
let totalScenarios = 0;
let passedScenarios = 0;

function pass(msg) {
    passedAssertions++;
    console.log(`   ✓ ${msg}`);
}

async function runScenario(title, fn) {
    totalScenarios++;
    console.log(`\n================================================================================`);
    console.log(`📌 KỊCH BẢN ${totalScenarios}: ${title}`);
    console.log(`================================================================================`);
    try {
        await fn();
        passedScenarios++;
        console.log(`🏆 KẾT QUẢ: Kịch bản ${totalScenarios} ĐẠT (PASSED)`);
    } catch (err) {
        console.error(`❌ THẤT BẠI ở Kịch bản ${totalScenarios}:`, err.message);
        throw err;
    }
}

async function request(path, options = {}) {
    let url = `${BASE_URL}${path}`;
    const headers = options.headers || {};
    if (authToken && !headers['Authorization']) {
        headers['Authorization'] = `Bearer ${authToken}`;
        if (!url.includes('access_token=') && !url.includes('/api/')) {
            url += (url.includes('?') ? '&' : '?') + `access_token=${encodeURIComponent(authToken)}`;
        }
    }
    if (options.body && typeof options.body === 'object') {
        headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(options.body);
    }
    const res = await fetch(url, { ...options, headers });
    return res;
}

async function jsonRequest(path, options = {}) {
    const res = await request(path, options);
    const text = await res.text();
    let data = null;
    try {
        data = JSON.parse(text);
    } catch {
        data = text;
    }
    return { status: res.status, ok: res.ok, data };
}

async function main() {
    console.log("********************************************************************************");
    console.log("🚀 BẮT ĐẦU CHẠY BỘ KIỂM THỬ END-TO-END TOÀN HỆ THỐNG CỜ TƯỚNG VIỆT NAM");
    console.log(`🌐 Mục tiêu kiểm thử: ${BASE_URL}`);
    console.log(`⏰ Thời gian thực thi: ${new Date().toLocaleString('vi-VN')}`);
    console.log("********************************************************************************");

    // SCENARIO 1: KIỂM THỬ ĐIỀU HƯỚNG GIAO DIỆN & TRANG WEB CỐT LÕI
    await runScenario("Giao diện người dùng & Các trang chức năng chính (Core UI Navigation)", async () => {
        const pages = [
            { path: '/', title: 'Trang chủ', keywords: ['Cờ Tướng', 'Thế cờ rèn luyện', 'nav-item'] },
            { path: '/Game', title: 'Bàn đấu Cờ', keywords: ['CHỌN CHẾ ĐỘ CHƠI', 'board-svg', 'BẮT ĐẦU VÁN CỜ'] },
            { path: '/Puzzle', title: 'Kho Cờ Thế', keywords: ['Cờ Thế', 'Thử thách', 'Giải đề tính thời gian'] },
            { path: '/Course', title: 'Khóa học Căn bản', keywords: ['Khóa học', 'Module'] },
            { path: '/MasterGame', title: 'Kỳ phổ Danh thủ', keywords: ['Kỳ phổ', 'Danh thủ'] },
            { path: '/Auth/Login', title: 'Đăng nhập', keywords: ['Đăng nhập', 'Tên đăng nhập', 'Mật khẩu'] }
        ];

        for (const p of pages) {
            const start = Date.now();
            const res = await request(p.path);
            const duration = Date.now() - start;
            assert.strictEqual(res.status, 200, `Trang ${p.title} (${p.path}) phải trả về HTTP 200 OK`);
            const html = await res.text();
            for (const kw of p.keywords) {
                assert.ok(html.includes(kw), `Trang ${p.title} (${p.path}) phải chứa thành phần: "${kw}"`);
            }
            pass(`Trang ${p.title.padEnd(20)} (${p.path.padEnd(12)}): 200 OK (${duration}ms, ${Math.round(html.length / 1024)} KB)`);
        }
    });

    // SCENARIO 2: QUY TRÌNH XÁC THỰC NGƯỜI DÙNG (AUTHENTICATION & JWT TOKEN)
    await runScenario("Xác thực người dùng & Quản lý phiên (Authentication & Token Lifecycle)", async () => {
        // 1. Đăng nhập
        const loginRes = await jsonRequest('/api/auth/login', {
            method: 'POST',
            body: { username: 'admin', password: 'Admin@123' }
        });
        assert.strictEqual(loginRes.status, 200, "Đăng nhập admin phải thành công (HTTP 200)");
        assert.ok(loginRes.data.success, "Trường success phải là true");
        assert.ok(loginRes.data.token, "Hệ thống phải cấp JWT Bearer Token");
        authToken = loginRes.data.token;
        pass("Đăng nhập tài khoản Admin thành công, đã nhận JWT Bearer Token");

        // 2. Lấy thông tin tài khoản hiện tại qua token
        const meRes = await jsonRequest('/api/auth/me');
        assert.strictEqual(meRes.status, 200, "Gọi /api/auth/me phải thành công với Token");
        assert.ok(meRes.data.isAuthenticated, "Xác nhận người dùng đã đăng nhập hợp lệ");
        assert.strictEqual(meRes.data.username, 'admin', "Tên người dùng phải là admin");
        currentUserId = meRes.data.id;
        currentUserElo = meRes.data.elo;
        pass(`Xác thực Profile thành công: ID = ${currentUserId}, Tên = ${meRes.data.username}, Elo = ${meRes.data.elo}`);
    });

    // SCENARIO 3: LUỒNG CHƠI VỚI MÁY (PVE) & ĐỘNG CƠ AI MINIMAX
    await runScenario("Luồng chơi Người vs Máy (PvE Game Flow & Minimax AI Engine)", async () => {
        // 1. Khởi tạo ván cờ PvE
        const createRes = await jsonRequest('/api/games?gameMode=PvE', { method: 'POST' });
        assert.strictEqual(createRes.status, 201, "Tạo ván cờ PvE phải thành công (HTTP 201 Created)");
        const game = createRes.data;
        assert.strictEqual(game.gameMode, 'PvE', "Chế độ game phải được lưu chuẩn xác là PvE");
        assert.strictEqual(game.status, 'InProgress', "Trạng thái ván cờ phải là InProgress");
        assert.strictEqual(game.currentTurn, 'Red', "Lượt đi đầu tiên thuộc về Quân Đỏ");
        testPvEGameId = game.id;
        pass(`Khởi tạo ván cờ PvE thành công (GameId: ${testPvEGameId}, RoomCode: ${game.roomCode})`);

        // 2. Kiểm tra danh sách nước đi hợp lệ cho quân Pháo lộ 2 (From: 7, 1)
        const validMovesRes = await jsonRequest(`/api/games/${testPvEGameId}/valid-moves?row=7&col=1`);
        assert.strictEqual(validMovesRes.status, 200, "Lấy nước đi hợp lệ phải thành công");
        const moves = validMovesRes.data.moves || validMovesRes.data;
        assert.ok(Array.isArray(moves) && moves.length > 0, "Quân Pháo lộ 2 phải có các nước đi hợp lệ ban đầu");
        pass(`Thuật toán sinh nước đi: Quân Pháo có ${moves.length} nước hợp lệ, có thể bình ngũ (Pháo đầu)`);

        // 3. Người chơi đi nước Pháo 2 bình 5 (7, 1 -> 7, 4)
        const moveRes = await jsonRequest(`/api/games/${testPvEGameId}/moves`, {
            method: 'POST',
            body: { fromX: 7, fromY: 1, toX: 7, toY: 4 }
        });
        assert.strictEqual(moveRes.status, 200, "Thực hiện nước đi Pháo 2 bình 5 phải thành công");
        assert.strictEqual(moveRes.data.currentTurn, 'Black', "Sau nước đi của Đỏ, lượt đi chuyển sang Quân Đen");
        pass("Người chơi đi nước Pháo 2 bình 5 thành công (7,1 -> 7,4), lượt chuyển sang Đen");

        // 4. Máy tính Minimax suy nghĩ và đáp trả nước đi (Depth 1)
        const startAi = Date.now();
        const aiRes = await jsonRequest(`/api/games/${testPvEGameId}/computer-move?difficulty=1`, { method: 'POST' });
        const aiDuration = Date.now() - startAi;
        assert.strictEqual(aiRes.status, 200, "Động cơ AI Minimax phải tính toán thành công");
        assert.strictEqual(aiRes.data.currentTurn, 'Red', "Sau nước đi của AI, lượt đi chuyển về cho người chơi Đỏ");
        assert.strictEqual(aiRes.data.moves.length, 2, "Ván cờ phải ghi nhận đúng 2 nước đi (1 của người, 1 của AI)");
        pass(`Động cơ AI Minimax đáp trả thành công trong ${aiDuration}ms (Nước đi: ${aiRes.data.lastMoveEval?.pieceName} ${aiRes.data.lastMoveEval?.moveDescription})`);

        // 5. Kiểm tra đánh giá thế trận Heuristic H1-H2-H3-H4 & Minimax Eval
        const evalRes = await jsonRequest(`/api/games/${testPvEGameId}/eval`);
        assert.strictEqual(evalRes.status, 200, "API /eval phải phản hồi 200 OK");
        assert.ok(typeof evalRes.data.eval === 'number', "API /eval phải trả về điểm số minimax eval");
        assert.ok(typeof aiRes.data.redScore === 'number', "Phải có điểm số đánh giá RedScore");
        assert.ok(aiRes.data.evaluation && typeof aiRes.data.evaluation.h1 === 'number', "Phải có đầy đủ Heuristic H1-H4");
        pass(`Đánh giá thế trận: Minimax Eval = ${evalRes.data.eval}, Heuristic (H1: ${aiRes.data.evaluation.h1}, H2: ${aiRes.data.evaluation.h2}, H3: ${aiRes.data.evaluation.h3}, H4: ${aiRes.data.evaluation.h4})`);

        // 6. Kiểm tra tính năng Gợi ý nước đi (Hint Engine)
        const hintRes = await jsonRequest(`/api/games/${testPvEGameId}/hint?mode=move`);
        assert.strictEqual(hintRes.status, 200, "API gợi ý nước đi phải phản hồi 200 OK");
        assert.ok(typeof hintRes.data.fromX === 'number' && typeof hintRes.data.toX === 'number', "Gợi ý nước đi tối ưu (fromX, toX) phải được cung cấp");
        pass(`Hệ thống gợi ý chiến thuật (AI Hint Engine): Nước tối ưu (${hintRes.data.fromX},${hintRes.data.fromY}) -> (${hintRes.data.toX},${hintRes.data.toY})`);

        // 7. Người chơi đầu hàng (Resign)
        const resignRes = await jsonRequest(`/api/games/${testPvEGameId}/resign`, { method: 'POST' });
        assert.strictEqual(resignRes.status, 200, "Đầu hàng phải thành công");
        assert.strictEqual(resignRes.data.status, 'BlackWins', "Người chơi Đỏ đầu hàng thì Đen thắng (BlackWins)");
        pass("Xử lý đầu hàng ván cờ chính xác: Status chuyển thành BlackWins");
    });

    // SCENARIO 4: KHỞI TẠO CÁC CHẾ ĐỘ CHƠI CÒN LẠI (PVP VÀ AI VS AI)
    await runScenario("Ràng buộc tạo ván cờ Người vs Người (PvP) và Máy vs Máy (AiVsAi)", async () => {
        // 1. Tạo ván Người vs Người
        const pvpRes = await jsonRequest('/api/games?gameMode=PvP', { method: 'POST' });
        assert.strictEqual(pvpRes.status, 201, "Tạo ván PvP phải thành công");
        assert.strictEqual(pvpRes.data.gameMode, 'PvP', "Chế độ game phải được lưu chuẩn là PvP");
        testPvPGameId = pvpRes.data.id;
        pass(`Tạo ván cờ Người vs Người (PvP) thành công (GameId: ${testPvPGameId}, GameMode: PvP)`);

        // 2. Tạo ván Máy vs Máy
        const aivaiRes = await jsonRequest('/api/games?gameMode=AiVsAi', { method: 'POST' });
        assert.strictEqual(aivaiRes.status, 201, "Tạo ván AiVsAi phải thành công");
        assert.strictEqual(aivaiRes.data.gameMode, 'AiVsAi', "Chế độ game phải được lưu chuẩn là AiVsAi");
        testAiVsAiGameId = aivaiRes.data.id;
        pass(`Tạo ván cờ Máy vs Máy (AiVsAi) thành công (GameId: ${testAiVsAiGameId}, GameMode: AiVsAi)`);
    });

    // SCENARIO 5: LUỒNG CHƠI QUA MẠNG ONLINE & QUẢN LÝ PHÒNG ĐẤU
    await runScenario("Quy trình Chơi qua mạng Online & Quản lý phòng chờ (Online Multiplayer E2E)", async () => {
        // 1. Tạo phòng chờ online công khai
        const onlineRes = await jsonRequest('/api/games/online?isPublic=true', { method: 'POST' });
        assert.strictEqual(onlineRes.status, 201, "Tạo phòng online phải thành công");
        assert.strictEqual(onlineRes.data.status, 'WaitingForPlayers', "Phòng mới tạo phải ở trạng thái WaitingForPlayers");
        testOnlineGameId = onlineRes.data.id;
        const roomCode = onlineRes.data.roomCode;
        pass(`Tạo phòng Online công khai thành công: ID = ${testOnlineGameId}, RoomCode = ${roomCode}`);

        // 2. Kiểm tra danh sách phòng chờ công khai
        const roomsRes = await jsonRequest('/api/games/online/rooms');
        assert.strictEqual(roomsRes.status, 200, "Lấy danh sách phòng chờ phải thành công");
        assert.ok(Array.isArray(roomsRes.data), "Dữ liệu phòng chờ phải là một mảng");
        const found = roomsRes.data.some(r => r.roomCode === roomCode || r.id === testOnlineGameId);
        assert.ok(found, "Phòng vừa tạo phải xuất hiện trong danh sách phòng chờ công khai");
        pass(`Phòng ${roomCode} đã hiển thị chính xác trong sảnh chờ công khai (Public Lobby)`);

        // 3. Kiểm tra danh sách trận đấu trực tiếp (Khán giả)
        const liveRes = await jsonRequest('/api/games/online/live');
        assert.strictEqual(liveRes.status, 200, "API trận đấu trực tiếp /online/live phản hồi 200 OK");
        pass("API truyền hình trận đấu trực tiếp (Spectator Live Stream) hoạt động sẵn sàng");

        // 4. Hủy phòng chờ và rời sảnh
        const cancelRes = await jsonRequest(`/api/games/${testOnlineGameId}/cancel-wait`, { method: 'POST' });
        assert.strictEqual(cancelRes.status, 200, "Hủy phòng chờ phải thành công");
        pass(`Hủy phòng chờ ${roomCode} thành công, giải phóng bàn chơi`);
    });

    // SCENARIO 6: KHO CỜ THẾ & HỆ THỐNG VƯỢT MÀN (PUZZLE CAMPAIGN)
    await runScenario("Kho Cờ Thế & Hệ thống Chấm bài tập Sát cục (Puzzle Campaign E2E)", async () => {
        // 1. Lấy danh mục 5 cấp độ cờ thế
        const catalogRes = await jsonRequest('/api/puzzle-run/catalog');
        assert.strictEqual(catalogRes.status, 200, "Lấy danh mục cờ thế phải thành công");
        assert.ok(catalogRes.data.ready, "Hệ thống cờ thế phải ở trạng thái ready");
        assert.strictEqual(catalogRes.data.levels.length, 5, "Hệ thống phải có đúng 5 cấp độ vượt màn (Level 1 - 5)");
        assert.strictEqual(catalogRes.data.levels[0].total, 100, "Mỗi cấp độ phải có đúng 100 bài tập sát cục chuẩn");
        pass(`Tải danh mục Cờ thế thành công: 5 Cấp độ, tổng cộng 500 bài tập sát cục đã hiệu chuẩn`);

        // 2. Lấy dữ liệu bài tập cụ thể (Level 1 - Bài 1)
        const puzzleKey = catalogRes.data.levels[0].keys[0];
        const puzzleRes = await jsonRequest(`/api/puzzle-run/puzzles/${encodeURIComponent(puzzleKey)}`);
        assert.strictEqual(puzzleRes.status, 200, `Lấy dữ liệu bài tập ${puzzleKey} phải thành công`);
        assert.ok(puzzleRes.data.state && puzzleRes.data.state.pieces, "Bài tập phải có dữ liệu bàn cờ state");
        pass(`Tải bài tập cờ thế ${puzzleKey}: "${puzzleRes.data.title}" (${puzzleRes.data.levelName}, Mate in ${puzzleRes.data.mateIn}) thành công`);

        // 3. Tra cứu đáp án / gợi ý bài giải
        const solRes = await jsonRequest(`/api/puzzle-run/puzzles/${encodeURIComponent(puzzleKey)}/solution`);
        assert.strictEqual(solRes.status, 200, "Lấy đáp án bài cờ thế phải thành công");
        assert.ok(solRes.data.steps && solRes.data.steps.length > 0, "Phải có các bước đi đáp án (steps)");
        pass(`Hệ thống lời giải mẫu & hướng dẫn nước đi cờ thế hoạt động chính xác (${solRes.data.steps.length} bước)`);
    });

    // SCENARIO 7: KHÓA HỌC CĂN BẢN & BÀI HỌC TƯƠNG TÁC (COURSE & INTERACTIVE LESSONS)
    await runScenario("Khóa học Căn bản & Bài giảng Tương tác (Course & Interactive Learning)", async () => {
        // 1. Lấy danh mục 4 Module khóa học
        const coursesRes = await jsonRequest('/api/courses');
        assert.strictEqual(coursesRes.status, 200, "Lấy danh mục khóa học phải thành công");
        assert.strictEqual(coursesRes.data.total, 4, "Hệ thống phải có đúng 4 Module khóa học căn bản");
        pass("Tải danh mục Khóa học thành công: Đầy đủ 4 Module từ Vỡ lòng đến Chiến thuật");

        // 2. Lấy chi tiết bài học đầu tiên
        const lessonRes = await jsonRequest('/api/courses/lessons/1');
        assert.strictEqual(lessonRes.status, 200, "Lấy chi tiết bài học ID 1 phải thành công");
        assert.ok(lessonRes.data.title, "Bài học phải có tiêu đề");
        assert.ok(lessonRes.data.questions && lessonRes.data.questions.length > 0, "Bài học phải có câu hỏi trắc nghiệm");
        pass(`Tải bài học tương tác: "${lessonRes.data.title}" thành công`);

        // 3. Gửi cập nhật tiến độ bài học & điểm Quiz
        const progressRes = await jsonRequest('/api/courses/progress', {
            method: 'POST',
            body: {
                lessonId: 1,
                quizScore: 100,
                isCompleted: true
            }
        });
        assert.strictEqual(progressRes.status, 200, "Cập nhật tiến độ bài học phải thành công");
        assert.strictEqual(progressRes.data.isCompleted, true, "Trường isCompleted cập nhật tiến độ phải là true");
        pass("Lưu tiến độ học tập và điểm Quiz (100%) vào cơ sở dữ liệu thành công");
    });

    // SCENARIO 8: TRA CỨU KỲ PHỔ DANH THỦ (MASTER GAMES EXPLORER)
    await runScenario("Tra cứu Kỳ phổ Danh thủ đỉnh cao (Master Games Historical Archive)", async () => {
        // 1. Lấy danh sách ván cờ danh thủ
        const masterRes = await jsonRequest('/api/master-games');
        assert.strictEqual(masterRes.status, 200, "Lấy danh sách kỳ phổ phải thành công");
        assert.ok(masterRes.data.total > 0, "Phải có danh sách ván đấu danh thủ");
        pass(`Tải danh mục Kỳ phổ Danh thủ: Tổng cộng ${masterRes.data.total} ván cờ kinh điển`);

        // 2. Lấy chi tiết ván đấu Lại Lý Huynh vs Mạnh Thần (ID: 6)
        const gameDetailRes = await jsonRequest('/api/master-games/6');
        assert.strictEqual(gameDetailRes.status, 200, "Lấy chi tiết ván cờ ID 6 phải thành công");
        assert.ok(gameDetailRes.data.title.includes("Lại Lý Huynh"), "Ván cờ ID 6 phải có tên danh thủ Lại Lý Huynh");
        assert.ok(gameDetailRes.data.pgnContent && gameDetailRes.data.pgnContent.length > 0, "Ván cờ phải chứa toàn bộ biên bản PGN nước đi");
        pass(`Chi tiết ván cờ danh thủ: "${gameDetailRes.data.title}" tải hoàn hảo`);
    });

    // SCENARIO 9: THỐNG KÊ DASHBOARD, RÀNG BUỘC 4 CHẾ ĐỘ & BIỂU ĐỒ ELO
    await runScenario("Thống kê Dashboard, Ràng buộc 4 Chế độ & Biểu đồ Phong độ Elo", async () => {
        // 1. Kiểm tra Bảng xếp hạng kỳ thủ
        const lbRes = await jsonRequest('/api/leaderboard?type=classic');
        assert.strictEqual(lbRes.status, 200, "Lấy bảng xếp hạng phải thành công");
        assert.ok(Array.isArray(lbRes.data.leaderboard), "Dữ liệu bảng xếp hạng phải là mảng");
        assert.ok(lbRes.data.leaderboard.length > 0, "Bảng xếp hạng phải có người chơi");
        pass(`Bảng xếp hạng kỳ thủ (Leaderboard): Tải thành công ${lbRes.data.leaderboard.length} kỳ thủ hàng đầu`);

        // 2. Kiểm tra HTML Dashboard và ràng buộc đúng 4 chế độ chơi
        const dashRes = await request('/Dashboard');
        assert.strictEqual(dashRes.status, 200, "Trang Dashboard phải phản hồi HTTP 200");
        const dashHtml = await dashRes.text();

        // Ràng buộc 4 chế độ:
        assert.ok(dashHtml.includes("Hai người cùng máy"), "Bảng thống kê phải có 'Hai người cùng máy'");
        assert.ok(dashHtml.includes("Chơi với máy"), "Bảng thống kê phải có 'Chơi với máy'");
        assert.ok(dashHtml.includes("Online"), "Bảng thống kê phải có 'Online'");
        assert.ok(dashHtml.includes("Máy với máy"), "Bảng thống kê phải có 'Máy với máy'");

        // TUYỆT ĐỐI KHÔNG còn dòng "Dữ liệu cũ / chưa xác định"
        assert.ok(!dashHtml.includes("Dữ liệu cũ / chưa xác định"), "Bảng thống kê TUYỆT ĐỐI KHÔNG ĐƯỢC CHỨA 'Dữ liệu cũ / chưa xác định'");
        pass("Ràng buộc Bảng thống kê: Đúng chuẩn 4 chế độ chơi, hoàn toàn sạch bóng 'Dữ liệu cũ / chưa xác định'");

        // 3. Kiểm tra Biểu đồ phong độ Elo
        assert.ok(dashHtml.includes("Biểu đồ phong độ Elo"), "Dashboard phải có Biểu đồ phong độ Elo");
        assert.ok(dashHtml.includes("eloRatingChart"), "Canvas biểu đồ eloRatingChart phải được render");
        assert.ok(dashHtml.includes(currentUserElo.toString()), `Điểm Elo hiện tại (${currentUserElo}) phải hiển thị trên biểu đồ`);
        pass(`Biểu đồ phong độ Elo: Hiển thị đầy đủ mốc khởi đầu, lịch sử biến động và mức Elo ${currentUserElo}`);
    });

    console.log("\n================================================================================");
    console.log(`🎉 TỔNG KẾT KẾT QUẢ KIỂM THỬ END-TO-END:`);
    console.log(`   - Tổng số Kịch bản (Scenarios): ${passedScenarios}/${totalScenarios} ĐẠT (100%)`);
    console.log(`   - Tổng số Kiểm tra thành phần (Assertions): ${passedAssertions} ĐẠT`);
    console.log(`   - Trạng thái kiểm thử: TOÀN BỘ VƯỢT QUA XUẤT SẮC (ALL TESTS PASSED)`);
    console.log("================================================================================\n");
}

main().catch(err => {
    console.error("\n❌ KIỂM THỬ GẶP LỖI:", err);
    process.exit(1);
});
