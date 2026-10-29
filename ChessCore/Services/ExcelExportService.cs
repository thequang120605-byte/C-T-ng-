using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using ChessCore.Controllers;
using ChessCore.Models;
using ChessCore.Services;

namespace ChessCore.Services
{
    public static class ExcelExportService
    {
        // =========================================================================
        // 1. XUẤT BÁO CÁO EXCEL TOÀN HỆ THỐNG DÀNH CHO ADMIN
        // =========================================================================
        public static byte[] ExportAdminReport(
            List<AdminModeStatDto> modeStats,
            LearningStatistics learning,
            List<AdminUserOverviewDto> users,
            string adminUsername)
        {
            using var workbook = new XLWorkbook();
            workbook.Author = "Cờ Tướng Pro";
            workbook.Properties.Title = "Báo cáo Thống kê Toàn hệ thống Cờ Tướng";
            workbook.Properties.Subject = "Báo cáo Quản trị";

            // ---------------------------------------------------------
            // SHEET 1: THỐNG KÊ VÁN ĐẤU THEO CHẾ ĐỘ
            // ---------------------------------------------------------
            var wsModes = workbook.Worksheets.Add("Thống kê Ván đấu");
            wsModes.ShowGridLines = true;

            // Header Banner
            wsModes.Row(1).Height = 10;
            
            var titleRange = wsModes.Range(2, 1, 2, 10);
            titleRange.Merge();
            titleRange.Value = "CỜ TƯỚNG VIỆT NAM (CỜ TƯỚNG PRO) - HỆ THỐNG QUẢN TRỊ";
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 15;
            titleRange.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsModes.Row(2).Height = 28;

            var subRange = wsModes.Range(3, 1, 3, 10);
            subRange.Merge();
            subRange.Value = "BÁO CÁO THỐNG KÊ CHI TIẾT VÁN ĐẤU THEO CHẾ ĐỘ THI ĐẤU";
            subRange.Style.Font.Bold = true;
            subRange.Style.Font.FontSize = 13;
            subRange.Style.Font.FontColor = XLColor.FromHtml("#B45309");
            subRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsModes.Row(3).Height = 24;

            var metaRange = wsModes.Range(4, 1, 4, 10);
            metaRange.Merge();
            metaRange.Value = $"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Người xuất: {adminUsername} | Phạm vi: Toàn hệ thống";
            metaRange.Style.Font.Italic = true;
            metaRange.Style.Font.FontSize = 10;
            metaRange.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            metaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            metaRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsModes.Row(4).Height = 20;

            wsModes.Row(5).Height = 12;

            // Table Header
            string[] modeHeaders = {
                "STT", "Chế độ thi đấu", "Đã tạo", "Đang chơi", "Chờ đấu",
                "Đã kết thúc", "Thắng", "Thua", "Hòa", "Tỉ lệ thắng (%)"
            };

            for (int i = 0; i < modeHeaders.Length; i++)
            {
                var cell = wsModes.Cell(6, i + 1);
                cell.Value = modeHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 11;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#475569");
            }
            wsModes.Row(6).Height = 28;

            int rowIdx = 7;
            int stt = 1;
            int sumCreated = 0, sumInProgress = 0, sumWaiting = 0, sumFinished = 0;
            int sumWins = 0, sumLosses = 0, sumDraws = 0;

            foreach (var r in modeStats)
            {
                sumCreated += r.Created;
                sumInProgress += r.InProgress;
                sumWaiting += r.Waiting;
                sumFinished += r.Finished;
                sumWins += r.Wins;
                sumLosses += r.Losses;
                sumDraws += r.Draws;

                var bg = (stt % 2 == 1) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                wsModes.Cell(rowIdx, 1).Value = stt;
                wsModes.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                wsModes.Cell(rowIdx, 2).Value = r.Label;
                wsModes.Cell(rowIdx, 2).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                wsModes.Cell(rowIdx, 3).Value = r.Created;
                wsModes.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 3).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 4).Value = r.InProgress;
                wsModes.Cell(rowIdx, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 4).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 5).Value = r.Waiting;
                wsModes.Cell(rowIdx, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 5).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 6).Value = r.Finished;
                wsModes.Cell(rowIdx, 6).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 6).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 7).Value = r.Wins;
                wsModes.Cell(rowIdx, 7).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, 7).Style.Font.FontColor = XLColor.FromHtml("#059669");
                wsModes.Cell(rowIdx, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 7).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 8).Value = r.Losses;
                wsModes.Cell(rowIdx, 8).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, 8).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                wsModes.Cell(rowIdx, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 8).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 9).Value = r.Draws;
                wsModes.Cell(rowIdx, 9).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, 9).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                wsModes.Cell(rowIdx, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, 9).Style.NumberFormat.Format = "#,##0";

                wsModes.Cell(rowIdx, 10).Value = r.WinRate / 100.0;
                wsModes.Cell(rowIdx, 10).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, 10).Style.Font.FontColor = XLColor.FromHtml("#B45309");
                wsModes.Cell(rowIdx, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                wsModes.Cell(rowIdx, 10).Style.NumberFormat.Format = "0.0%";

                var rowRange = wsModes.Range(rowIdx, 1, rowIdx, 10);
                rowRange.Style.Fill.BackgroundColor = bg;
                rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                rowRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                wsModes.Row(rowIdx).Height = 22;

                rowIdx++;
                stt++;
            }

            // Summary / Total Row
            var totalRange = wsModes.Range(rowIdx, 1, rowIdx, 2);
            totalRange.Merge();
            totalRange.Value = "TỔNG CỘNG TOÀN HỆ THỐNG";
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Font.FontSize = 11;
            totalRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            wsModes.Cell(rowIdx, 3).Value = sumCreated;
            wsModes.Cell(rowIdx, 4).Value = sumInProgress;
            wsModes.Cell(rowIdx, 5).Value = sumWaiting;
            wsModes.Cell(rowIdx, 6).Value = sumFinished;
            wsModes.Cell(rowIdx, 7).Value = sumWins;
            wsModes.Cell(rowIdx, 8).Value = sumLosses;
            wsModes.Cell(rowIdx, 9).Value = sumDraws;

            double overallWinRate = sumFinished > 0 ? (double)sumWins * 100.0 / sumFinished : 0;
            wsModes.Cell(rowIdx, 10).Value = overallWinRate / 100.0;
            wsModes.Cell(rowIdx, 10).Style.NumberFormat.Format = "0.0%";

            for (int col = 3; col <= 9; col++)
            {
                wsModes.Cell(rowIdx, col).Style.Font.Bold = true;
                wsModes.Cell(rowIdx, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsModes.Cell(rowIdx, col).Style.NumberFormat.Format = "#,##0";
            }
            wsModes.Cell(rowIdx, 10).Style.Font.Bold = true;
            wsModes.Cell(rowIdx, 10).Style.Font.FontColor = XLColor.FromHtml("#B45309");
            wsModes.Cell(rowIdx, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var sumRange = wsModes.Range(rowIdx, 1, rowIdx, 10);
            sumRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            sumRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sumRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sumRange.Style.Border.TopBorderColor = XLColor.FromHtml("#D97706");
            sumRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            sumRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#B45309");
            sumRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            sumRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            sumRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            sumRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#FDE68A");
            wsModes.Row(rowIdx).Height = 24;

            rowIdx += 2;
            var noteRange = wsModes.Range(rowIdx, 1, rowIdx, 10);
            noteRange.Merge();
            noteRange.Value = "(*) Ghi chú: Tỉ lệ thắng = Số ván thắng / Số ván đã hoàn thành. Điểm Elo xếp hạng chỉ được áp dụng trong chế độ PvP Online.";
            noteRange.Style.Font.Italic = true;
            noteRange.Style.Font.FontSize = 9.5;
            noteRange.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            noteRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            wsModes.Columns().AdjustToContents();
            wsModes.Column(1).Width = Math.Max(wsModes.Column(1).Width, 6);
            wsModes.Column(2).Width = Math.Max(wsModes.Column(2).Width, 28);
            for (int c = 3; c <= 9; c++)
            {
                wsModes.Column(c).Width = Math.Max(wsModes.Column(c).Width, 14);
            }
            wsModes.Column(10).Width = Math.Max(wsModes.Column(10).Width, 16);

            // ---------------------------------------------------------
            // SHEET 2: TIẾN ĐỘ HỌC TẬP & GIẢI CỜ THẾ
            // ---------------------------------------------------------
            var wsLearn = workbook.Worksheets.Add("Học tập & Cờ thế");
            wsLearn.ShowGridLines = true;

            wsLearn.Row(1).Height = 10;
            var lTitle = wsLearn.Range(2, 1, 2, 4);
            lTitle.Merge();
            lTitle.Value = "BÁO CÁO TIẾN ĐỘ KHÓA HỌC & GIẢI CỜ THẾ (TOÀN HỆ THỐNG)";
            lTitle.Style.Font.Bold = true;
            lTitle.Style.Font.FontSize = 14;
            lTitle.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            lTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            lTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            wsLearn.Row(2).Height = 26;

            var lMeta = wsLearn.Range(3, 1, 3, 4);
            lMeta.Merge();
            lMeta.Value = $"Dữ liệu cập nhật đến: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Hệ thống Cờ Tướng Pro";
            lMeta.Style.Font.Italic = true;
            lMeta.Style.Font.FontSize = 10;
            lMeta.Style.Font.FontColor = XLColor.FromHtml("#64748B");
            lMeta.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            wsLearn.Row(3).Height = 18;

            wsLearn.Row(4).Height = 10;

            string[] lHeaders = { "STT", "Chỉ số thống kê", "Giá trị", "Đơn vị tính" };
            for (int i = 0; i < lHeaders.Length; i++)
            {
                var cell = wsLearn.Cell(5, i + 1);
                cell.Value = lHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            wsLearn.Row(5).Height = 26;

            var lMetrics = new (string Name, object Value, string Unit)[] {
                ("Tổng số bài học trong khóa học", learning?.TotalLessons ?? 19, "Bài học"),
                ("Số lượt hoàn thành bài học của kỳ thủ", learning?.CompletedLessonRecords ?? 0, "Lượt hoàn thành"),
                ("Tổng số lượt làm bài tập trắc nghiệm (Quiz)", learning?.QuizAttempts ?? 0, "Lượt làm bài"),
                ("Điểm Quiz trung bình cao nhất", (learning?.AverageBestQuizScore ?? 0) / 100.0, "% điểm"),
                ("Tổng số bài cờ thế đã giải thành công", learning?.PuzzlesSolved ?? 0, "Bài giải đúng"),
                ("Tổng số lượt nộp lời giải cờ thế", learning?.PuzzleAttempts ?? 0, "Lượt giải")
            };

            int lRow = 6;
            int lIdx = 1;
            foreach (var m in lMetrics)
            {
                var bg = (lIdx % 2 == 1) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                wsLearn.Cell(lRow, 1).Value = lIdx;
                wsLearn.Cell(lRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                wsLearn.Cell(lRow, 2).Value = m.Name;
                wsLearn.Cell(lRow, 2).Style.Font.Bold = true;
                wsLearn.Cell(lRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                if (m.Value is double dVal)
                {
                    wsLearn.Cell(lRow, 3).Value = dVal;
                    wsLearn.Cell(lRow, 3).Style.NumberFormat.Format = "0.0%";
                }
                else
                {
                    wsLearn.Cell(lRow, 3).Value = Convert.ToInt32(m.Value);
                    wsLearn.Cell(lRow, 3).Style.NumberFormat.Format = "#,##0";
                }
                wsLearn.Cell(lRow, 3).Style.Font.Bold = true;
                wsLearn.Cell(lRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                wsLearn.Cell(lRow, 4).Value = m.Unit;
                wsLearn.Cell(lRow, 4).Style.Font.Italic = true;
                wsLearn.Cell(lRow, 4).Style.Font.FontColor = XLColor.FromHtml("#64748B");
                wsLearn.Cell(lRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var rRange = wsLearn.Range(lRow, 1, lRow, 4);
                rRange.Style.Fill.BackgroundColor = bg;
                rRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                rRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                wsLearn.Row(lRow).Height = 22;

                lRow++;
                lIdx++;
            }

            wsLearn.Columns().AdjustToContents();
            wsLearn.Column(1).Width = Math.Max(wsLearn.Column(1).Width, 6);
            wsLearn.Column(2).Width = Math.Max(wsLearn.Column(2).Width, 42);
            wsLearn.Column(3).Width = Math.Max(wsLearn.Column(3).Width, 18);
            wsLearn.Column(4).Width = Math.Max(wsLearn.Column(4).Width, 18);

            // ---------------------------------------------------------
            // SHEET 3: BẢNG XẾP HẠNG & DANH SÁCH KỲ THỦ
            // ---------------------------------------------------------
            if (users != null && users.Count > 0)
            {
                var wsUsers = workbook.Worksheets.Add("Danh sách Kỳ thủ");
                wsUsers.ShowGridLines = true;

                wsUsers.Row(1).Height = 10;
                var uTitle = wsUsers.Range(2, 1, 2, 16);
                uTitle.Merge();
                uTitle.Value = "DANH SÁCH & BẢNG XẾP HẠNG TẤT CẢ KỲ THỦ (LEADERBOARD)";
                uTitle.Style.Font.Bold = true;
                uTitle.Style.Font.FontSize = 14;
                uTitle.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
                uTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                uTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                wsUsers.Row(2).Height = 26;

                var uMeta = wsUsers.Range(3, 1, 3, 16);
                uMeta.Merge();
                uMeta.Value = $"Tổng số kỳ thủ đăng ký: {users.Count} | Xếp hạng theo điểm Elo từ cao xuống thấp";
                uMeta.Style.Font.Italic = true;
                uMeta.Style.Font.FontSize = 10;
                uMeta.Style.Font.FontColor = XLColor.FromHtml("#64748B");
                uMeta.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsUsers.Row(3).Height = 18;

                wsUsers.Row(4).Height = 10;

                string[] uHeaders = {
                    "Hạng", "Tên kỳ thủ", "Email", "Mã ID", "Kỳ lực", "Điểm Elo",
                    "Tổng ván", "Thắng", "Thua", "Hòa", "Tỉ lệ thắng (%)",
                    "Điểm cờ thế", "Cờ thế đã giải", "Khóa học", "Vai trò", "Trạng thái"
                };

                for (int i = 0; i < uHeaders.Length; i++)
                {
                    var cell = wsUsers.Cell(5, i + 1);
                    cell.Value = uHeaders[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                wsUsers.Row(5).Height = 26;

                int uRow = 6;
                int rank = 1;
                foreach (var u in users)
                {
                    var bg = (rank % 2 == 1) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                    wsUsers.Cell(uRow, 1).Value = rank;
                    wsUsers.Cell(uRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    if (rank <= 3)
                    {
                        wsUsers.Cell(uRow, 1).Style.Font.Bold = true;
                        wsUsers.Cell(uRow, 1).Style.Font.FontColor = rank == 1 ? XLColor.FromHtml("#D97706") : (rank == 2 ? XLColor.FromHtml("#64748B") : XLColor.FromHtml("#B45309"));
                    }

                    wsUsers.Cell(uRow, 2).Value = u.Username;
                    wsUsers.Cell(uRow, 2).Style.Font.Bold = true;
                    wsUsers.Cell(uRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    wsUsers.Cell(uRow, 3).Value = u.Email;
                    wsUsers.Cell(uRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                    wsUsers.Cell(uRow, 4).Value = u.PlayerId;
                    wsUsers.Cell(uRow, 4).Style.Font.FontColor = XLColor.FromHtml("#B45309");
                    wsUsers.Cell(uRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 5).Value = u.Title;
                    wsUsers.Cell(uRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 6).Value = u.Elo;
                    wsUsers.Cell(uRow, 6).Style.Font.Bold = true;
                    wsUsers.Cell(uRow, 6).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                    wsUsers.Cell(uRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    wsUsers.Cell(uRow, 6).Style.NumberFormat.Format = "#,##0";

                    wsUsers.Cell(uRow, 7).Value = u.GamesPlayed;
                    wsUsers.Cell(uRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 8).Value = u.Wins;
                    wsUsers.Cell(uRow, 8).Style.Font.FontColor = XLColor.FromHtml("#059669");
                    wsUsers.Cell(uRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 9).Value = u.Losses;
                    wsUsers.Cell(uRow, 9).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                    wsUsers.Cell(uRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 10).Value = u.Draws;
                    wsUsers.Cell(uRow, 10).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                    wsUsers.Cell(uRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 11).Value = u.WinRate / 100.0;
                    wsUsers.Cell(uRow, 11).Style.Font.Bold = true;
                    wsUsers.Cell(uRow, 11).Style.Font.FontColor = XLColor.FromHtml("#B45309");
                    wsUsers.Cell(uRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    wsUsers.Cell(uRow, 11).Style.NumberFormat.Format = "0.0%";

                    wsUsers.Cell(uRow, 12).Value = u.PuzzleScore;
                    wsUsers.Cell(uRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 13).Value = u.PuzzlesSolved;
                    wsUsers.Cell(uRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 14).Value = $"{u.LessonsCompleted} / 19";
                    wsUsers.Cell(uRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 15).Value = u.Role?.ToUpper() ?? "PLAYER";
                    wsUsers.Cell(uRow, 15).Style.Font.Bold = true;
                    wsUsers.Cell(uRow, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    wsUsers.Cell(uRow, 16).Value = u.IsLocked ? "Đã khóa" : "Bình thường";
                    wsUsers.Cell(uRow, 16).Style.Font.FontColor = u.IsLocked ? XLColor.FromHtml("#DC2626") : XLColor.FromHtml("#059669");
                    wsUsers.Cell(uRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var uRange = wsUsers.Range(uRow, 1, uRow, 16);
                    uRange.Style.Fill.BackgroundColor = bg;
                    uRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    uRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    uRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    uRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                    uRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                    wsUsers.Row(uRow).Height = 22;

                    uRow++;
                    rank++;
                }

                wsUsers.Range(5, 1, uRow - 1, 16).SetAutoFilter();
                wsUsers.Columns().AdjustToContents();
                wsUsers.Column(1).Width = Math.Max(wsUsers.Column(1).Width, 6);
                wsUsers.Column(2).Width = Math.Max(wsUsers.Column(2).Width, 18);
                wsUsers.Column(3).Width = Math.Max(wsUsers.Column(3).Width, 24);
                wsUsers.Column(4).Width = Math.Max(wsUsers.Column(4).Width, 12);
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // =========================================================================
        // 2. XUẤT BÁO CÁO EXCEL CÁ NHÂN DÀNH CHO KỲ THỦ
        // =========================================================================
        public static byte[] ExportPersonalReport(
            List<ModeStatistics> modeStats,
            LearningStatistics learning,
            string username,
            int elo)
        {
            using var workbook = new XLWorkbook();
            workbook.Author = "Cờ Tướng Pro";
            workbook.Properties.Title = $"Báo cáo Thành tích Kỳ thủ - {username}";

            var ws = workbook.Worksheets.Add("Thành tích Thi đấu");
            ws.ShowGridLines = true;

            ws.Row(1).Height = 10;
            var titleRange = ws.Range(2, 1, 2, 8);
            titleRange.Merge();
            titleRange.Value = $"CỜ TƯỚNG PRO - BÁO CÁO THÀNH TÍCH THI ĐẤU";
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 15;
            titleRange.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 28;

            var metaRange = ws.Range(3, 1, 3, 8);
            metaRange.Merge();
            metaRange.Value = $"Kỳ thủ: {username} | Điểm Elo: {elo} | Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            metaRange.Style.Font.Bold = true;
            metaRange.Style.Font.FontSize = 11;
            metaRange.Style.Font.FontColor = XLColor.FromHtml("#B45309");
            metaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            metaRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(3).Height = 22;

            ws.Row(4).Height = 12;

            string[] headers = {
                "STT", "Chế độ thi đấu", "Tổng ván", "Thắng", "Thua", "Hòa", "Tỉ lệ thắng (%)", "Ghi chú"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(5, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(5).Height = 28;

            int rowIdx = 6;
            int stt = 1;
            int totalGames = 0, totalWins = 0, totalLosses = 0, totalDraws = 0;

            foreach (var r in modeStats)
            {
                totalGames += r.Finished;
                totalWins += r.Wins;
                totalLosses += r.Losses;
                totalDraws += r.Draws;

                var bg = (stt % 2 == 1) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                ws.Cell(rowIdx, 1).Value = stt;
                ws.Cell(rowIdx, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(rowIdx, 2).Value = r.Label;
                ws.Cell(rowIdx, 2).Style.Font.Bold = true;

                ws.Cell(rowIdx, 3).Value = r.Finished;
                ws.Cell(rowIdx, 3).Style.Font.Bold = true;
                ws.Cell(rowIdx, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(rowIdx, 4).Value = r.Wins;
                ws.Cell(rowIdx, 4).Style.Font.Bold = true;
                ws.Cell(rowIdx, 4).Style.Font.FontColor = XLColor.FromHtml("#059669");
                ws.Cell(rowIdx, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(rowIdx, 5).Value = r.Losses;
                ws.Cell(rowIdx, 5).Style.Font.Bold = true;
                ws.Cell(rowIdx, 5).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                ws.Cell(rowIdx, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(rowIdx, 6).Value = r.Draws;
                ws.Cell(rowIdx, 6).Style.Font.Bold = true;
                ws.Cell(rowIdx, 6).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                ws.Cell(rowIdx, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(rowIdx, 7).Value = r.WinRate / 100.0;
                ws.Cell(rowIdx, 7).Style.Font.Bold = true;
                ws.Cell(rowIdx, 7).Style.Font.FontColor = XLColor.FromHtml("#B45309");
                ws.Cell(rowIdx, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(rowIdx, 7).Style.NumberFormat.Format = "0.0%";

                ws.Cell(rowIdx, 8).Value = r.Mode == "Online" ? "Tính điểm xếp hạng Elo" : "Tập luyện không đổi điểm";
                ws.Cell(rowIdx, 8).Style.Font.Italic = true;
                ws.Cell(rowIdx, 8).Style.Font.FontColor = XLColor.FromHtml("#64748B");

                var rRange = ws.Range(rowIdx, 1, rowIdx, 8);
                rRange.Style.Fill.BackgroundColor = bg;
                rRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                rRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                ws.Row(rowIdx).Height = 22;

                rowIdx++;
                stt++;
            }

            // Total row
            var totRange = ws.Range(rowIdx, 1, rowIdx, 2);
            totRange.Merge();
            totRange.Value = "TỔNG CỘNG THÀNH TÍCH";
            totRange.Style.Font.Bold = true;

            ws.Cell(rowIdx, 3).Value = totalGames;
            ws.Cell(rowIdx, 4).Value = totalWins;
            ws.Cell(rowIdx, 5).Value = totalLosses;
            ws.Cell(rowIdx, 6).Value = totalDraws;

            double pWinRate = totalGames > 0 ? (double)totalWins * 100.0 / totalGames : 0;
            ws.Cell(rowIdx, 7).Value = pWinRate / 100.0;
            ws.Cell(rowIdx, 7).Style.NumberFormat.Format = "0.0%";

            for (int c = 3; c <= 6; c++)
            {
                ws.Cell(rowIdx, c).Style.Font.Bold = true;
                ws.Cell(rowIdx, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(rowIdx, c).Style.NumberFormat.Format = "#,##0";
            }
            ws.Cell(rowIdx, 7).Style.Font.Bold = true;
            ws.Cell(rowIdx, 7).Style.Font.FontColor = XLColor.FromHtml("#B45309");
            ws.Cell(rowIdx, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var sumP = ws.Range(rowIdx, 1, rowIdx, 8);
            sumP.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            sumP.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            sumP.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sumP.Style.Border.TopBorderColor = XLColor.FromHtml("#D97706");
            sumP.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            sumP.Style.Border.BottomBorderColor = XLColor.FromHtml("#B45309");
            sumP.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            sumP.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            sumP.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            sumP.Style.Border.InsideBorderColor = XLColor.FromHtml("#FDE68A");
            ws.Row(rowIdx).Height = 24;

            // Extra metrics block: Học tập & Cờ thế
            if (learning != null)
            {
                rowIdx += 2;
                var lHeader = ws.Range(rowIdx, 1, rowIdx, 8);
                lHeader.Merge();
                lHeader.Value = "TIẾN ĐỘ HỌC TẬP & RÈN LUYỆN KỸ NĂNG";
                lHeader.Style.Font.Bold = true;
                lHeader.Style.Font.FontColor = XLColor.White;
                lHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563EB");
                lHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(rowIdx).Height = 24;

                rowIdx++;
                var pMetrics = new (string Label, string Value)[] {
                    ("Bài học đã hoàn thành", $"{learning.CompletedLessonRecords} / {learning.TotalLessons} bài"),
                    ("Lượt làm trắc nghiệm (Quiz)", $"{learning.QuizAttempts} lượt"),
                    ("Điểm Quiz tốt nhất trung bình", $"{learning.AverageBestQuizScore}%"),
                    ("Cờ thế đã giải thành công", $"{learning.PuzzlesSolved} bài"),
                    ("Tổng lượt nộp lời giải cờ thế", $"{learning.PuzzleAttempts} lượt")
                };

                foreach (var pm in pMetrics)
                {
                    var r1 = ws.Range(rowIdx, 1, rowIdx, 4);
                    r1.Merge();
                    r1.Value = pm.Label;
                    r1.Style.Font.Bold = true;

                    var r2 = ws.Range(rowIdx, 5, rowIdx, 8);
                    r2.Merge();
                    r2.Value = pm.Value;
                    r2.Style.Font.Bold = true;
                    r2.Style.Font.FontColor = XLColor.FromHtml("#059669");
                    r2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var fullR = ws.Range(rowIdx, 1, rowIdx, 8);
                    fullR.Style.Fill.BackgroundColor = (rowIdx % 2 == 1) ? XLColor.FromHtml("#F8FAFC") : XLColor.White;
                    fullR.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    fullR.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    fullR.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                    fullR.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
                    ws.Row(rowIdx).Height = 20;

                    rowIdx++;
                }
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 6);
            ws.Column(2).Width = Math.Max(ws.Column(2).Width, 26);
            ws.Column(3).Width = Math.Max(ws.Column(3).Width, 14);
            ws.Column(7).Width = Math.Max(ws.Column(7).Width, 16);
            ws.Column(8).Width = Math.Max(ws.Column(8).Width, 24);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
