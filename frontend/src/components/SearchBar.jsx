import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Baby, BedDouble, Calendar, ChevronLeft, ChevronRight, Compass, Globe,
  Hotel, MapPin, Minus, Plane, Plus, Search, Ship, Sparkles, Ticket, User, Users,
} from 'lucide-react';
import { destinations } from '../data/tours';

const PRODUCT_TABS = [
  { id: 'tours', label: 'Tour trọn gói', icon: Compass },
  { id: 'domestic', label: 'Trong nước', icon: MapPin },
  { id: 'international', label: 'Nước ngoài', icon: Globe },
  { id: 'cruise', label: 'Du thuyền', icon: Ship },
  { id: 'activities', label: 'Vé vui chơi', icon: Ticket },
  { id: 'flight', label: 'Combo Bay + KS', icon: Plane },
  { id: 'hotel', label: 'Khách sạn', icon: Hotel },
  { id: 'more', label: 'Khác', icon: Sparkles },
];

const SUB_FILTERS = [
  { id: 'all', label: 'Tất cả' },
  { id: 'domestic', label: 'Tour ghép đoàn' },
  { id: 'beach', label: 'Biển đảo' },
  { id: 'culture', label: 'Văn hóa' },
];

const WEEKDAYS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
const MONTHS = ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'];
const shortFmt = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: 'numeric', month: 'short' });
const fullFmt = new Intl.DateTimeFormat('vi-VN', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' });

const toIso = (d) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
const parseIso = (iso) => {
  if (!iso) return null;
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
};
const addDaysDate = (d, n) => new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);

function MonthGrid({ year, month, todayIso, startIso, endIso, onPick }) {
  const firstDay = new Date(year, month, 1);
  const startOffset = (firstDay.getDay() + 6) % 7; // Monday = 0, Sunday = 6
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const cells = [];
  for (let i = 0; i < startOffset; i++) cells.push(null);
  for (let d = 1; d <= daysInMonth; d++) cells.push(new Date(year, month, d));

  return (
    <div className="cal-month">
      <div className="cal-month-title">{MONTHS[month]} {year}</div>
      <div className="cal-weekdays">
        {WEEKDAYS.map((w, idx) => <span key={w} className={idx === 6 ? 'sun' : ''}>{w}</span>)}
      </div>
      <div className="cal-days">
        {cells.map((d, idx) => {
          if (!d) return <span key={`e-${idx}`} />;
          const iso = toIso(d);
          const isPast = iso < todayIso;
          const isSun = d.getDay() === 0;
          const isStart = iso === startIso;
          const isEnd = iso === endIso;
          const inRange = startIso && endIso && iso > startIso && iso < endIso;
          const cls = [
            'cal-day',
            isSun ? 'sun' : '',
            isStart ? 'start' : '',
            isEnd ? 'end' : '',
            inRange ? 'in-range' : '',
          ].filter(Boolean).join(' ');
          return (
            <button key={iso} type="button" className={cls} disabled={isPast} onClick={() => onPick(iso)}>
              <span>{d.getDate()}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

export default function SearchBar({ initial = {}, compact = false }) {
  const navigate = useNavigate();
  const wrapRef = useRef(null);

  const today = new Date();
  const todayIso = toIso(today);
  const [activeTab, setActiveTab] = useState('tours');
  const [subFilter, setSubFilter] = useState('all');
  const [q, setQ] = useState(initial.q ?? '');
  const [from, setFrom] = useState(initial.from ?? '');
  const [startIso, setStartIso] = useState(initial.date || todayIso);
  const [endIso, setEndIso] = useState(() => initial.endDate || toIso(addDaysDate(today, 2)));
  const [adults, setAdults] = useState(Number(initial.a) || 2);
  const [children, setChildren] = useState(Number(initial.c) || 0);
  const [rooms, setRooms] = useState(Number(initial.r) || 1);

  const [openPanel, setOpenPanel] = useState(null); // null | 'dest' | 'date' | 'guests'
  const [calBase, setCalBase] = useState(() => {
    const base = parseIso(initial.date) ?? today;
    return new Date(base.getFullYear(), base.getMonth(), 1);
  });

  useEffect(() => {
    const onDown = (e) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target)) setOpenPanel(null);
    };
    const onKey = (e) => {
      if (e.key === 'Escape') setOpenPanel(null);
    };
    document.addEventListener('pointerdown', onDown);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('pointerdown', onDown);
      document.removeEventListener('keydown', onKey);
    };
  }, []);

  const handleDatePick = (iso) => {
    if (!startIso || (startIso && endIso) || iso < startIso) {
      setStartIso(iso);
      setEndIso('');
    } else if (iso === startIso) {
      setEndIso(toIso(addDaysDate(parseIso(iso), 1)));
    } else {
      setEndIso(iso);
    }
  };

  const nights = startIso && endIso ? Math.max(1, Math.round((parseIso(endIso) - parseIso(startIso)) / 86400000)) : 0;
  const dateDisplay = startIso
    ? endIso
      ? `${shortFmt.format(parseIso(startIso))} – ${shortFmt.format(parseIso(endIso))} (${nights} đêm)`
      : `${shortFmt.format(parseIso(startIso))} – Chọn ngày về`
    : 'Chọn ngày khởi hành';

  const nextMonth = new Date(calBase.getFullYear(), calBase.getMonth() + 1, 1);
  const filteredDest = destinations.filter((d) => !q.trim() || d.name.toLowerCase().includes(q.trim().toLowerCase()));

  const submit = (e) => {
    e.preventDefault();
    setOpenPanel(null);
    const params = new URLSearchParams();
    if (q) params.set('q', q);
    if (from) params.set('from', from);
    if (startIso) params.set('date', startIso);
    params.set('a', String(adults));
    if (children) params.set('c', String(children));
    if (activeTab === 'domestic' || activeTab === 'international') params.set('cat', activeTab);
    else if (subFilter !== 'all') params.set('cat', subFilter);
    navigate(`/tours?${params}`);
  };

  return (
    <div ref={wrapRef} className={`tvk-search-card ${compact ? 'compact' : ''}`}>
      {!compact && (
        <>
          <div className="tvk-tabs" role="tablist">
            {PRODUCT_TABS.map((t) => {
              const Icon = t.icon;
              return (
                <button
                  key={t.id}
                  type="button"
                  role="tab"
                  aria-selected={activeTab === t.id}
                  className={`tvk-tab ${activeTab === t.id ? 'active' : ''}`}
                  onClick={() => setActiveTab(t.id)}
                >
                  <Icon size={17} />
                  <span>{t.label}</span>
                </button>
              );
            })}
          </div>

          <div className="tvk-subfilters">
            {SUB_FILTERS.map((f) => (
              <button
                key={f.id}
                type="button"
                className={`tvk-subpill ${subFilter === f.id ? 'active' : ''}`}
                onClick={() => setSubFilter(f.id)}
              >
                {f.label}
              </button>
            ))}
          </div>
        </>
      )}

      <form className="tvk-form" onSubmit={submit} role="search">
        <div className="tvk-field-group">
          {/* Cột 1: Điểm đến */}
          <div className={`tvk-col ${openPanel === 'dest' ? 'is-open' : ''}`}>
            <label htmlFor="tvk-q">Thành phố, điểm đến hoặc tên tour</label>
            <div className="tvk-input-box" onClick={() => setOpenPanel('dest')}>
              <MapPin size={18} className="blue-ic" />
              <input
                id="tvk-q"
                type="search"
                value={q}
                onFocus={() => setOpenPanel('dest')}
                onChange={(e) => { setQ(e.target.value); setOpenPanel('dest'); }}
                placeholder="Thành phố, điểm đến hoặc tour..."
                autoComplete="off"
              />
            </div>

            {openPanel === 'dest' && (
              <div className="tvk-popover tvk-dest-pop">
                <div className="pop-head">Điểm đến phổ biến</div>
                {(filteredDest.length ? filteredDest : destinations).map((d) => (
                  <button
                    key={d.name}
                    type="button"
                    className="dest-opt"
                    onClick={() => {
                      setQ(d.name.split(' - ')[0]);
                      setOpenPanel('date');
                    }}
                  >
                    <MapPin size={16} className="blue-ic" />
                    <div>
                      <b>{d.name}</b>
                      <span className="muted small">Điểm du lịch nổi bật</span>
                    </div>
                    <span className="dest-count">{d.count} tour</span>
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Cột 2: Ngày đi & Ngày về (Lịch 2 tháng) */}
          <div className={`tvk-col ${openPanel === 'date' ? 'is-open' : ''}`}>
            <span className="tvk-label">Ngày đi &amp; Ngày về</span>
            <button
              type="button"
              className="tvk-trigger-btn"
              aria-expanded={openPanel === 'date'}
              onClick={() => setOpenPanel(openPanel === 'date' ? null : 'date')}
            >
              <Calendar size={18} className="blue-ic" />
              <span>{dateDisplay}</span>
            </button>

            {openPanel === 'date' && (
              <div className="tvk-popover tvk-date-pop">
                <div className="pop-title">Lịch trình dự kiến</div>
                <div className="date-summary">
                  <div>
                    <span className="muted small">Ngày khởi hành</span>
                    <b>{startIso ? fullFmt.format(parseIso(startIso)) : 'Chưa chọn'}</b>
                  </div>
                  <div>
                    <span className="muted small">Ngày kết thúc</span>
                    <b>{endIso ? fullFmt.format(parseIso(endIso)) : 'Chưa chọn'}</b>
                  </div>
                </div>
                <div className="cal-nav-wrap">
                  <button
                    type="button"
                    className="cal-nav prev"
                    aria-label="Tháng trước"
                    onClick={() => setCalBase(new Date(calBase.getFullYear(), calBase.getMonth() - 1, 1))}
                  >
                    <ChevronLeft size={18} />
                  </button>
                  <button
                    type="button"
                    className="cal-nav next"
                    aria-label="Tháng sau"
                    onClick={() => setCalBase(new Date(calBase.getFullYear(), calBase.getMonth() + 1, 1))}
                  >
                    <ChevronRight size={18} />
                  </button>
                  <div className="cal-grid">
                    <MonthGrid year={calBase.getFullYear()} month={calBase.getMonth()} todayIso={todayIso} startIso={startIso} endIso={endIso} onPick={handleDatePick} />
                    <MonthGrid year={nextMonth.getFullYear()} month={nextMonth.getMonth()} todayIso={todayIso} startIso={startIso} endIso={endIso} onPick={handleDatePick} />
                  </div>
                </div>
                <div className="pop-footer">
                  <button type="button" className="btn-link" onClick={() => { setStartIso(''); setEndIso(''); }}>Bỏ chọn ngày</button>
                  <button type="button" className="btn-done" onClick={() => setOpenPanel(null)}>Xong</button>
                </div>
              </div>
            )}
          </div>

          {/* Cột 3: Khách & Phòng */}
          <div className={`tvk-col ${openPanel === 'guests' ? 'is-open' : ''}`}>
            <span className="tvk-label">Số khách &amp; Phòng</span>
            <button
              type="button"
              className="tvk-trigger-btn"
              aria-expanded={openPanel === 'guests'}
              onClick={() => setOpenPanel(openPanel === 'guests' ? null : 'guests')}
            >
              <Users size={18} className="blue-ic" />
              <span>{adults} người lớn, {children} trẻ em, {rooms} phòng</span>
            </button>

            {openPanel === 'guests' && (
              <div className="tvk-popover tvk-guests-pop">
                <div className="guest-row">
                  <div className="guest-info"><User size={18} className="blue-ic" /><div><b>Người lớn</b><span className="muted small">Từ 12 tuổi</span></div></div>
                  <div className="tvk-stepper">
                    <button type="button" aria-label="Giảm người lớn" disabled={adults <= 1} onClick={() => setAdults(adults - 1)}><Minus size={15} /></button>
                    <span>{adults}</span>
                    <button type="button" aria-label="Tăng người lớn" onClick={() => setAdults(adults + 1)}><Plus size={15} /></button>
                  </div>
                </div>
                <div className="guest-row">
                  <div className="guest-info"><Baby size={18} className="blue-ic" /><div><b>Trẻ em</b><span className="muted small">2 – 11 tuổi</span></div></div>
                  <div className="tvk-stepper">
                    <button type="button" aria-label="Giảm trẻ em" disabled={children <= 0} onClick={() => setChildren(children - 1)}><Minus size={15} /></button>
                    <span>{children}</span>
                    <button type="button" aria-label="Tăng trẻ em" onClick={() => setChildren(children + 1)}><Plus size={15} /></button>
                  </div>
                </div>
                <div className="guest-row">
                  <div className="guest-info"><BedDouble size={18} className="blue-ic" /><div><b>Phòng</b><span className="muted small">Tiêu chuẩn 2 khách</span></div></div>
                  <div className="tvk-stepper">
                    <button type="button" aria-label="Giảm phòng" disabled={rooms <= 1} onClick={() => setRooms(rooms - 1)}><Minus size={15} /></button>
                    <span>{rooms}</span>
                    <button type="button" aria-label="Tăng phòng" onClick={() => setRooms(rooms + 1)}><Plus size={15} /></button>
                  </div>
                </div>
                <div className="guest-from">
                  <span className="muted small">Khởi hành từ:</span>
                  <div className="from-pills">
                    {[['', 'Tất cả'], ['TP. Hồ Chí Minh', 'TP.HCM'], ['Hà Nội', 'Hà Nội']].map(([val, lbl]) => (
                      <button key={lbl} type="button" className={from === val ? 'active' : ''} onClick={() => setFrom(val)}>{lbl}</button>
                    ))}
                  </div>
                </div>
                <div className="pop-footer end">
                  <button type="button" className="btn-done" onClick={() => setOpenPanel(null)}>Xong</button>
                </div>
              </div>
            )}
          </div>

          <button className="tvk-search-btn" type="submit" aria-label="Tìm tour">
            <Search size={20} />
          </button>
        </div>
      </form>

      {!compact && (
        <div className="tvk-trust-strip">
          <span>Đối tác tin cậy</span>
          <div className="tvk-partners">
            <span>VIETNAM AIRLINES</span>
            <span>SUN WORLD</span>
            <span>VINPEARL</span>
            <span>BAMBOO AIRWAYS</span>
            <span>MUONG THANH</span>
          </div>
        </div>
      )}
    </div>
  );
}
