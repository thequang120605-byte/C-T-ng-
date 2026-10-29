using System;
using System.Threading;

namespace ChessCore.Services
{
    /// <summary>
    /// Điều phối số lượng tác vụ AI tính toán đồng thời để tránh cạn kiệt CPU và ThreadPool Starvation.
    /// </summary>
    public sealed class AiCapacity
    {
        public SemaphoreSlim Gate { get; } = new(Math.Max(1, Environment.ProcessorCount / 2));
    }
}
