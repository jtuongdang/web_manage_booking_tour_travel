#!/usr/bin/env bash
set -e

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

echo "=========================================================="
echo "   🚀 KHỞI ĐỘNG DỰ ÁN QUẢN LÝ TOUR DU LỊCH (VIETTOUR)     "
echo "=========================================================="

# 1. Kiểm tra và khởi động Docker SQL Server
echo "[1/4] Kiểm tra SQL Server Docker..."
if docker ps -a --format '{{.Names}}' | grep -q "^musing_volhard$"; then
  if ! docker ps --format '{{.Names}}' | grep -q "^musing_volhard$"; then
    echo "  -> Đang bật container musing_volhard..."
    docker start musing_volhard > /dev/null
  else
    echo "  -> Container musing_volhard đã hoạt động."
  fi
else
  echo "  [Cảnh báo] Không tìm thấy container musing_volhard. Vui lòng đảm bảo SQL Server đang chạy tại localhost:1433."
fi

# 2. Dọn dẹp tiến trình cũ nếu còn chạy trên port 5141 hoặc 5173
echo "[2/4] Dọn dẹp cổng chạy cũ (5141, 5173)..."
for PORT in 5141 5173; do
  PID=$(lsof -ti:$PORT 2>/dev/null || true)
  if [ -n "$PID" ]; then
    echo "  -> Giải phóng cổng $PORT (PID: $PID)..."
    kill -9 $PID 2>/dev/null || true
  fi
done

# 3. Quản lý dọn dẹp khi nhấn Ctrl+C
cleanup() {
  echo ""
  echo "=========================================================="
  echo "   🛑 Đang dừng hệ thống VietTour..."
  echo "=========================================================="
  if [ -n "$BACKEND_PID" ]; then
    kill "$BACKEND_PID" 2>/dev/null || true
  fi
  if [ -n "$FRONTEND_PID" ]; then
    kill "$FRONTEND_PID" 2>/dev/null || true
  fi
  # Dọn triệt để các sub-process con nếu có
  for PORT in 5141 5173; do
    P=$(lsof -ti:$PORT 2>/dev/null || true)
    if [ -n "$P" ]; then
      kill -9 $P 2>/dev/null || true
    fi
  done
  echo "   ✅ Đã dừng toàn bộ tiến trình Backend & Frontend."
  exit 0
}
trap cleanup SIGINT SIGTERM EXIT

# 4. Chạy Backend ASP.NET Core
echo "[3/4] Đang khởi chạy Backend API (.NET 10)..."
ASPNETCORE_ENVIRONMENT=Development dotnet run --project QuanLyTourDuLich.Api/QuanLyTourDuLich.Api.csproj --urls http://localhost:5141 &
BACKEND_PID=$!

# Đợi Backend sẵn sàng
echo "  -> Đang đợi Backend mở cổng 5141..."
for i in {1..20}; do
  if lsof -ti:5141 >/dev/null 2>&1; then
    echo "  -> Backend API đã sẵn sàng!"
    break
  fi
  sleep 1
done

# 5. Chạy Frontend Vite
echo "[4/4] Đang khởi chạy Frontend (Vite)..."
cd "$ROOT_DIR/frontend"
npm run dev -- --host localhost --port 5173 &
FRONTEND_PID=$!

echo ""
echo "=========================================================="
echo "   ✨ HỆ THỐNG ĐÃ KHỞI CHẠY THÀNH CÔNG!"
echo "   🌐 Trang khách hàng: http://localhost:5173"
echo "   🛡️  Trang Quản trị:   http://localhost:5173/admin"
echo "   🔌 Backend API:      http://localhost:5141"
echo "=========================================================="
echo "   Nhấn Ctrl+C để dừng toàn bộ hệ thống bất cứ lúc nào."
echo "=========================================================="
echo ""

# Chờ các tiến trình kết thúc
wait $FRONTEND_PID $BACKEND_PID
