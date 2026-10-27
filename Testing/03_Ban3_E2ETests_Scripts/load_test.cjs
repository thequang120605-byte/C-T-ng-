/**
 * BỘ KIỂM THỬ TỰ ĐỘNG HIỆU NĂNG & TẢI TRÊN CLOUD (AUTOMATED LOAD & LATENCY TEST)
 * Mục tiêu: Đo lường thời gian phản hồi, khả năng chịu tải của trang web https://co-tuong-pro.onrender.com
 */

const https = require('https');
const http = require('http');

const TARGET_URL = 'https://co-tuong-pro.onrender.com';
const CONCURRENT_USERS = 25; // Giả lập 25 người dùng ảo gửi request đồng thời
const REQUESTS_PER_USER = 4; // Mỗi người dùng gửi 4 requests

const ENDPOINTS = [
    '/',
    '/Game',
    '/Puzzle',
    '/MasterGame',
    '/Course',
    '/Dashboard'
];

function fetchEndpoint(url) {
    return new Promise((resolve) => {
        const start = Date.now();
        https.get(url, (res) => {
            let data = '';
            res.on('data', chunk => data += chunk);
            res.on('end', () => {
                const latency = Date.now() - start;
                resolve({
                    statusCode: res.statusCode,
                    latency: latency,
                    size: data.length,
                    success: res.statusCode >= 200 && res.statusCode < 400
                });
            });
        }).on('error', (err) => {
            resolve({
                statusCode: 0,
                latency: Date.now() - start,
                error: err.message,
                success: false
            });
        });
    });
}

async function runLoadTest() {
    console.log("==========================================================================");
    console.log("🚀 BẮT ĐẦU CHẠY KIỂM THỬ TẢI & HIỆU NĂNG HỆ THỐNG CỜ TƯỚNG PRO");
    console.log(`🌐 Target: ${TARGET_URL}`);
    console.log(`👥 Số người dùng đồng thời: ${CONCURRENT_USERS} Virtual Users`);
    console.log(`🔄 Tổng số lượt truy vấn: ${CONCURRENT_USERS * REQUESTS_PER_USER} Requests`);
    console.log("==========================================================================\n");

    const tasks = [];
    const startTime = Date.now();

    for (let u = 0; u < CONCURRENT_USERS; u++) {
        for (let r = 0; r < REQUESTS_PER_USER; r++) {
            const endpoint = ENDPOINTS[(u + r) % ENDPOINTS.length];
            tasks.push(fetchEndpoint(`${TARGET_URL}${endpoint}`));
        }
    }

    const results = await Promise.all(tasks);
    const totalDuration = Date.now() - startTime;

    const successCount = results.filter(r => r.success).length;
    const failCount = results.length - successCount;
    const latencies = results.map(r => r.latency).sort((a, b) => a - b);
    const avgLatency = Math.round(latencies.reduce((a, b) => a + b, 0) / latencies.length);
    const minLatency = latencies[0];
    const maxLatency = latencies[latencies.length - 1];
    const p95Latency = latencies[Math.floor(latencies.length * 0.95)];

    console.log("📊 KẾT QUẢ KIỂM THỬ HIỆU NĂNG CHI TIẾT:");
    console.log(`--------------------------------------------------------------------------`);
    console.log(`✅ Tổng request thành công: ${successCount} / ${results.length} (${((successCount/results.length)*100).toFixed(1)}%)`);
    console.log(`❌ Tổng request thất bại:  ${failCount}`);
    console.log(`⏱️ Thời gian phản hồi trung bình (Avg): ${avgLatency} ms`);
    console.log(`⚡ Thời gian phản hồi nhanh nhất (Min): ${minLatency} ms`);
    console.log(`🐢 Thời gian phản hồi chậm nhất (Max): ${maxLatency} ms`);
    console.log(`🎯 95% Requests hoàn thành dưới (P95): ${p95Latency} ms`);
    console.log(`⌛ Tổng thời gian test toàn bộ: ${(totalDuration/1000).toFixed(2)} giây`);
    console.log("==========================================================================\n");

    if (successCount === results.length) {
        console.log("🎉 ĐÁNH GIÁ CHUNG: Hệ thống đạt độ ổn định 100%, chịu tải xuất sắc!");
    } else {
        console.log("⚠️ ĐÁNH GIÁ CHUNG: Có một số request bị ảnh hưởng mạng hoặc tải máy chủ.");
    }
}

runLoadTest();
