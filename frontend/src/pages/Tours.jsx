import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { SlidersHorizontal } from 'lucide-react';
import SearchBar from '../components/SearchBar';
import TourCard from '../components/TourCard';
import { categories } from '../data/tours';
import { toursApi } from '../api/toursApi';

export default function Tours() {
  const [params, setParams] = useSearchParams();
  const q = params.get('q') ?? '';
  const from = params.get('from') ?? '';
  const date = params.get('date') ?? '';
  const cat = params.get('cat') ?? 'all';
  const [maxPrice, setMaxPrice] = useState(35000000);
  const [duration, setDuration] = useState('all');
  const [sort, setSort] = useState('popular');
  const [tours, setTours] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    const request = {
      keyword: q || undefined,
      departureFrom: from || undefined,
      departureDate: date || undefined,
      maxPrice,
      pageIndex: 1,
      pageSize: 100,
      scopeType: cat === 'domestic' ? 'Domestic' : cat === 'international' ? 'International' : undefined,
      tags: !['all', 'domestic', 'international'].includes(cat) ? cat : undefined,
    };
    setLoading(true);
    toursApi.list(request).then((result) => {
      if (active) {
        setTours(result.items);
        setError('');
      }
    }).catch((requestError) => {
      if (active) setError(requestError.message || 'Không thể tải danh sách tour.');
    }).finally(() => {
      if (active) setLoading(false);
    });
    return () => { active = false; };
  }, [q, from, date, cat, maxPrice]);

  const result = useMemo(() => {
    let list = tours.filter((t) => {
      if (duration === 'short' && t.durationDays > 3) return false;
      if (duration === 'mid' && (t.durationDays < 4 || t.durationDays > 5)) return false;
      if (duration === 'long' && t.durationDays < 6) return false;
      return true;
    });
    const sorters = {
      popular: (a, b) => b.reviewCount - a.reviewCount,
      priceAsc: (a, b) => a.price - b.price,
      priceDesc: (a, b) => b.price - a.price,
      rating: (a, b) => b.rating - a.rating,
    };
    return [...list].sort(sorters[sort]);
  }, [tours, duration, sort]);

  const setCat = (key) => {
    const p = new URLSearchParams(params);
    if (key === 'all') p.delete('cat'); else p.set('cat', key);
    setParams(p);
  };

  return (
    <div className="container page">
      <div className="list-search"><SearchBar compact key={params.toString()} initial={{ q, from, date }} /></div>

      <div className="cat-tabs" role="tablist">
        {categories.map((c) => (
          <button key={c.key} type="button" role="tab" aria-selected={cat === c.key} className={cat === c.key ? 'active' : ''} onClick={() => setCat(c.key)}>{c.label}</button>
        ))}
      </div>

      <div className="list-layout">
        <aside className="filters">
          <h3><SlidersHorizontal size={18} /> Bộ lọc</h3>
          <fieldset>
            <legend>Ngân sách tối đa</legend>
            <input type="range" min="2000000" max="35000000" step="500000" value={maxPrice} onChange={(e) => setMaxPrice(+e.target.value)} aria-label="Ngân sách tối đa" />
            <div className="small">{maxPrice.toLocaleString('vi-VN')} ₫</div>
          </fieldset>
          <fieldset>
            <legend>Thời lượng</legend>
            {[['all', 'Tất cả'], ['short', '1 – 3 ngày'], ['mid', '4 – 5 ngày'], ['long', 'Từ 6 ngày']].map(([k, l]) => (
              <label key={k} className="radio"><input type="radio" name="dur" checked={duration === k} onChange={() => setDuration(k)} /> {l}</label>
            ))}
          </fieldset>
        </aside>

        <section>
          <div className="list-toolbar">
            <span><b>{result.length}</b> tour phù hợp{q && <> cho “{q}”</>}</span>
            <label>Sắp xếp:&nbsp;
              <select value={sort} onChange={(e) => setSort(e.target.value)}>
                <option value="popular">Phổ biến nhất</option>
                <option value="priceAsc">Giá thấp → cao</option>
                <option value="priceDesc">Giá cao → thấp</option>
                <option value="rating">Đánh giá cao</option>
              </select>
            </label>
          </div>
          {error && <p className="error-text" role="alert">Không thể tải tour từ API: {error}</p>}
          {loading ? <p className="muted">Đang tải danh sách tour…</p> : result.length ? (
            <div className="tour-grid three">{result.map((t) => <TourCard key={t.id} tour={t} />)}</div>
          ) : !error && (
            <div className="empty">Không tìm thấy tour phù hợp. Hãy thử từ khóa hoặc bộ lọc khác.</div>
          )}
        </section>
      </div>
    </div>
  );
}
