import { Link } from 'react-router-dom';
import { Clock, Heart, MapPin } from 'lucide-react';
import { vnd } from '../utils/format';

export default function TourCard({ tour }) {
  const discount = Math.round((1 - tour.price / tour.originalPrice) * 100);
  const nextDep = tour.departures.find((d) => d.available > 0);
  const score10 = (tour.rating * 2).toFixed(1);

  return (
    <article className="tour-card">
      <Link to={`/tours/${tour.slug}`} className="tour-card-media">
        <img src={tour.images[0]} alt={tour.title} loading="lazy" width="800" height="560" onError={(e) => { e.currentTarget.style.opacity = '0'; }} />
        <span className="loc-badge"><MapPin size={12} /> {tour.destination}</span>
        {discount > 0 && <span className="deal-badge">Tiết kiệm {discount}%</span>}
      </Link>
      <button type="button" className="wish" aria-label="Thêm vào yêu thích"><Heart size={16} /></button>
      <div className="tour-card-body">
        <Link to={`/tours/${tour.slug}`} className="tour-title">{tour.title}</Link>
        <div className="meta"><Clock size={13} /> {tour.durationDays}N{tour.durationNights}Đ · Khởi hành {tour.departureFrom}</div>
        <div className="tvk-rating">
          <span className="tvk-score">{score10}/10</span>
          <span className="muted">({tour.reviewCount.toLocaleString('vi-VN')} đánh giá)</span>
        </div>
        <div className="price-row">
          <div>
            <s className="muted small">{vnd(tour.originalPrice)}</s>
            <div className="price">{vnd(tour.price)}</div>
          </div>
          {nextDep && nextDep.available <= 10 && <span className="urgent">Còn {nextDep.available} chỗ</span>}
        </div>
      </div>
    </article>
  );
}
