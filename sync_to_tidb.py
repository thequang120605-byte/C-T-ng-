#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Script đồng bộ cơ sở dữ liệu cờ tướng từ MySQL cục bộ lên TiDB Cloud.
Hỗ trợ 2 chế độ:
  1. Full: Đồng bộ toàn bộ CSDL (tất cả các bảng, cấu trúc và dữ liệu).
  2. Delta: Chỉ đồng bộ/cập nhật dữ liệu tài khoản TrongNguyen34 & QuocTrung.
"""

import sys
import os
import subprocess
import getpass
import argparse

# Thiết lập stdout UTF-8 cho Windows console
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

def find_mysql_client():
    candidates = [
        "mysql",
        r"D:\TRONGNGUYEN_HK7\LapTrinhMaNguonMo\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe",
        r"C:\laragon\bin\mysql\mysql-8.4.3-winx64\bin\mysql.exe",
        r"C:\laragon\bin\mysql\mysql-8.0.30-winx64\bin\mysql.exe"
    ]
    for c in candidates:
        try:
            res = subprocess.run([c, "--version"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            if res.returncode == 0:
                return c
        except Exception:
            continue
    return None

def main():
    parser = argparse.ArgumentParser(description="Đồng bộ CSDL Cờ tướng lên TiDB Cloud")
    parser.add_argument("--host", dest="host", help="TiDB Cloud Host")
    parser.add_argument("--port", dest="port", type=int, default=4000, help="TiDB Port (mặc định 4000)")
    parser.add_argument("--user", dest="user", help="TiDB Cloud User")
    parser.add_argument("--password", dest="password", help="TiDB Cloud Password")
    parser.add_argument("--database", dest="database", default="chess_db", help="Database name (mặc định chess_db)")
    parser.add_argument("--mode", dest="mode", choices=["Full", "Delta", "full", "delta"], help="Chế độ: Full hoặc Delta")

    args = parser.parse_args()

    print("\n" + "=" * 74)
    print("       CÔNG CỤ ĐỒNG BỘ CƠ SỞ DỮ LIỆU CỜ TƯỚNG LÊN TiDB CLOUD")
    print("=" * 74)

    mysql_exe = find_mysql_client()
    if not mysql_exe:
        print("[!] Không tìm thấy công cụ mysql client (mysql.exe).")
        print("    Bạn có thể dùng Cách 2: Mở https://tidbcloud.com -> SQL Editor -> Dán file SQL.")
        sys.exit(1)

    print(f"[OK] Đã nhận diện MySQL Client: {mysql_exe}\n")

    host = args.host
    if not host:
        host = input("Nhập Host TiDB Cloud (ví dụ: gateway01.ap-southeast-1.prod.aws.tidbcloud.com): ").strip()
    if not host:
        print("[!] Host không được để trống!")
        sys.exit(1)

    port = str(args.port or 4000)
    user = args.user
    if not user:
        user = input("Nhập User TiDB Cloud (ví dụ: 3rxxxxx.root): ").strip()
    if not user:
        print("[!] User không được để trống!")
        sys.exit(1)

    password = args.password
    if not password:
        password = getpass.getpass("Nhập Password TiDB Cloud: ").strip()

    database = args.database or "chess_db"

    mode = args.mode
    if not mode:
        print("\nChọn chế độ đồng bộ:")
        print("  [1] Đồng bộ TOÀN BỘ cơ sở dữ liệu (Full Sync - Khuyên dùng)")
        print("  [2] Chỉ đồng bộ tài khoản TrongNguyen34 & QuocTrung (Delta Sync)")
        choice = input("Nhập lựa chọn (1 hoặc 2, mặc định là 1): ").strip()
        if choice == "2":
            mode = "Delta"
        else:
            mode = "Full"
    else:
        mode = "Delta" if mode.lower() == "delta" else "Full"

    script_dir = os.path.dirname(os.path.abspath(__file__))
    if mode == "Delta":
        sql_file = os.path.join(script_dir, "ChessCore", "Database", "sync_users_activity_delta.sql")
        print(f"\n>>> Chế độ: DELTA SYNC ({sql_file})")
    else:
        sql_file = os.path.join(script_dir, "ChessCore", "Database", "sync_tidb_cloud_full.sql")
        print(f"\n>>> Chế độ: FULL SYNC ({sql_file})")

    if not os.path.exists(sql_file):
        print(f"[!] Không tìm thấy tệp SQL: {sql_file}")
        sys.exit(1)

    file_size_kb = os.path.getsize(sql_file) / 1024
    print(f"[OK] Dung lượng tệp SQL: {file_size_kb:.1f} KB")
    print(f"\nĐang kết nối và truyền dữ liệu lên TiDB Cloud ({host}:{port} / {database})...")

    cmd = [
        mysql_exe,
        "-h", host,
        "-P", str(port),
        "-u", user,
        f"-p{password}",
        "-D", database,
        "--ssl-mode=REQUIRED",
        "--default-character-set=utf8mb4"
    ]

    try:
        with open(sql_file, "r", encoding="utf-8") as f:
            res = subprocess.run(cmd, stdin=f, capture_output=True, text=True, encoding="utf-8")
        
        if res.returncode == 0:
            print("\n" + "=" * 74)
            print("   🎉 ĐỒNG BỘ DỮ LIỆU LÊN TiDB CLOUD THÀNH CÔNG VƯỢT TRỘI!")
            print("=" * 74)
            print("[✓] Tài khoản TrongNguyen34 và QuocTrung đã được đồng bộ lên Cloud.")
            print("[✓] Toàn bộ ván đấu, nước đi, lịch sử cờ thế và bài học đã cập nhật.")
            print("[✓] Bạn có thể truy cập web trên Render để kiểm tra ngay lập tức.\n")
        else:
            print("\n" + "=" * 74)
            print(f"   [!] ĐỒNG BỘ THẤT BẠI. MÃ LỖI: {res.returncode}")
            print("=" * 74)
            if res.stderr:
                print("Chi tiết lỗi:\n", res.stderr)
            print("\nLỜI KHUYÊN DỰ PHÒNG (Cách 2):")
            print("  1. Truy cập https://tidbcloud.com -> Cluster -> SQL Editor.")
            print(f"  2. Mở file: {sql_file}")
            print("  3. Copy toàn bộ nội dung -> Dán vào SQL Editor -> Nhấn 'Run'.\n")
    except Exception as e:
        print(f"[!] Xảy ra lỗi ngoại lệ khi thực thi: {e}")

if __name__ == "__main__":
    main()
