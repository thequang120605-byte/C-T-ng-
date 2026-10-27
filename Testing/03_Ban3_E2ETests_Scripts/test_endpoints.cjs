/**
 * TEST SUITE 1: KIỂM THỬ HIỆU NĂNG & ĐỘ TRỄ CÁC TRANG CỐT LÕI (PERFORMANCE & LATENCY TEST)
 */
const https = require('https');

const PAGES = [
    { name: 'Trang Chủ', path: '/' },
    { name: 'Bàn Đấu Cờ', path: '/Game' },
    { name: 'Kho Cờ Thế', path: '/Puzzle' },
    { name: 'Kỳ Phổ Danh Thủ', path: '/MasterGame' },
    { name: 'Khóa Học Căn Bản', path: '/Course' },
    { name: 'Xếp Hạng & Thống Kê', path: '/Dashboard' }
];

async function measurePage(page) {
    const start = Date.now();
    try {
        const response = await fetch(`https://co-tuong-pro.onrender.com${page.path}`);
        const duration = Date.now() - start;
        const text = await response.text();
        return {
            name: page.name,
            path: page.path,
            status: response.status,
            duration: duration,
            bytes: text.length,
            passed: response.status === 200
        };
    } catch (err) {
        return {
            name: page.name,
            path: page.path,
            status: 0,
            duration: Date.now() - start,
            error: err.message,
            passed: false
        };
    }
}

async function run() {
    console.log("================================================================================");
    console.log("📊 BỘ KIỂM THỬ TỰ ĐỘNG HIỆU NĂNG & TÍNH KHẢ DỤNG (LIVE ENDPOINT BENCHMARK)");
    console.log("🌐 Mục tiêu: https://co-tuong-pro.onrender.com");
    console.log("================================================================================\n");

    const results = [];
    for (const p of PAGES) {
        process.stdout.write(`Đang kiểm tra: ${p.name.padEnd(22)} (${p.path.padEnd(14)})... `);
        const res = await measurePage(p);
        results.push(res);
        if (res.passed) {
            console.log(`[PASS] ${res.status} OK  -  ${res.duration}ms  -  ${(res.bytes/1024).toFixed(1)} KB`);
        } else {
            console.log(`[FAIL] Mã lỗi ${res.status}: ${res.error || 'Lỗi server'}`);
        }
    }

    const avg = Math.round(results.reduce((a, b) => a + b.duration, 0) / results.length);
    const passCount = results.filter(r => r.passed).length;

    console.log("\n--------------------------------------------------------------------------------");
    console.log(`🎯 KẾT QUẢ TỔNG HỢP: ${passCount}/${results.length} trang phản hồi xuất sắc.`);
    console.log(`⚡ Độ trễ trung bình toàn site: ${avg} ms`);
    console.log("================================================================================\n");
}

run();
