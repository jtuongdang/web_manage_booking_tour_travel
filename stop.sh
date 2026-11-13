#!/usr/bin/env bash
echo "=========================================================="
echo "   🛑 DỪNG TOÀN BỘ TIẾN TRÌNH DỰ ÁN VIETTOUR              "
echo "=========================================================="

KILLED=0
for PORT in 5141 5173; do
  PIDS=$(lsof -ti:$PORT 2>/dev/null || true)
  if [ -n "$PIDS" ]; then
    for PID in $PIDS; do
      echo "  -> Đang dừng tiến trình PID $PID trên cổng $PORT..."
      kill -9 "$PID" 2>/dev/null || true
      KILLED=1
    done
  fi
done

if [ "$KILLED" -eq 1 ]; then
  echo "  ✅ Đã giải phóng các cổng 5141 (Backend) và 5173 (Frontend)."
else
  echo "  ℹ️  Không có tiến trình nào đang chạy trên cổng 5141 và 5173."
fi

echo "=========================================================="
