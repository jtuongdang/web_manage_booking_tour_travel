import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, RefreshCw, Search, ArrowUpRight } from 'lucide-react';
import { portalApi } from '../api/portalApi';

export const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Number(value || 0));
export const date = (value) => value ? new Date(value).toLocaleDateString('vi-VN') : '—';
export const rowsOf = (value) => Array.isArray(value) ? value : value?.items ?? value?.entries ?? value?.departures ?? [];
export const field = (label, name, type = 'text', placeholder = '') => ({ label, name, type, placeholder });

export function useEndpoint(path, enabled = true) {
  const [data, setData] = useState(null); const [loading, setLoading] = useState(false); const [error, setError] = useState('');
  const reload = useCallback(async () => { if (!enabled || !path) return; setLoading(true); setError(''); try { setData(await portalApi.get(path)); } catch (e) { setError(e.message || 'Không tải được dữ liệu.'); } finally { setLoading(false); } }, [path, enabled]);
  useEffect(() => { reload(); }, [reload]);
  return { data, rows: rowsOf(data), loading, error, reload, setData };
}

export function PageTitle({ eyebrow = 'VIETTOUR · PORTAL', title, description, action }) { return <div className="portal-page-title"><div><div className="portal-eyebrow">{eyebrow}</div><h1>{title}</h1><p>{description}</p></div>{action}</div>; }
export function Panel({ title, subtitle, action, children, className = '' }) { return <section className={`portal-panel ${className}`}><div className="portal-panel-head"><div><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>{action}</div>{children}</section>; }
export function Stat({ label, value, detail, icon: Icon, tone = 'blue' }) { return <div className="portal-stat"><div className={`stat-icon ${tone}`}>{Icon && <Icon size={19} />}</div><div><span>{label}</span><strong>{value ?? '—'}</strong><small>{detail}</small></div><ArrowUpRight className="stat-arrow" size={16} /></div>; }
export function DataTable({ columns, rows, loading, error, empty = 'Chưa có dữ liệu', onRefresh }) {
  return <div className="portal-table-wrap"><table className="portal-table"><thead><tr>{columns.map((col) => <th key={col.key}>{col.label}</th>)}</tr></thead><tbody>{loading ? <tr><td colSpan={columns.length} className="table-state"><RefreshCw className="spin" size={18} /> Đang tải dữ liệu…</td></tr> : error ? <tr><td colSpan={columns.length} className="table-state error"><AlertCircle size={18} />{error} {onRefresh && <button className="text-button" onClick={onRefresh}>Thử lại</button>}</td></tr> : rows.length ? rows.map((row, i) => <tr key={row.id ?? row.bookingCode ?? row.transactionCode ?? i}>{columns.map((col) => <td key={col.key}>{col.render ? col.render(row) : row[col.key] ?? '—'}</td>)}</tr>) : <tr><td colSpan={columns.length} className="table-state">{empty}</td></tr>}</tbody></table></div>;
}
export function ActionForm({ title, fields, submitLabel = 'Lưu thông tin', onSubmit, hint, initialValues = {} }) {
  const defaults = Object.fromEntries(fields.filter((f) => f.defaultValue !== undefined).map((f) => [f.name, f.defaultValue]));
  const baseValues = { ...defaults, ...initialValues };
  const [values, setValues] = useState(baseValues); const [busy, setBusy] = useState(false); const [message, setMessage] = useState('');
  return <form className="portal-form" onSubmit={async (event) => { event.preventDefault(); setBusy(true); setMessage(''); try { await onSubmit({ ...defaults, ...values }); setValues(baseValues); setMessage('Đã lưu thành công.'); } catch (e) { setMessage(e.message || 'Không thể lưu dữ liệu.'); } finally { setBusy(false); } }}>
    {title && <h3>{title}</h3>}{hint && <p className="portal-hint">{hint}</p>}<div className="portal-form-grid">{fields.map((item) => <label className={`portal-field ${item.wide ? 'wide' : ''}`} key={item.name}><span>{item.label}</span>{item.options ? <select required={item.required} value={values[item.name] ?? item.defaultValue ?? ''} onChange={(e) => setValues({ ...values, [item.name]: e.target.value })}><option value="">Chọn…</option>{item.options.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}</select> : item.type === 'textarea' ? <textarea required={item.required} rows="3" value={values[item.name] ?? ''} placeholder={item.placeholder} onChange={(e) => setValues({ ...values, [item.name]: e.target.value })} /> : <input required={item.required} type={item.type || 'text'} min={item.min} value={values[item.name] ?? ''} placeholder={item.placeholder} onChange={(e) => setValues({ ...values, [item.name]: item.type === 'number' ? Number(e.target.value) : e.target.value })} />}</label>)}</div><div className="portal-form-footer"><span role="status">{message}</span><button className="portal-button primary" disabled={busy}>{busy ? 'Đang lưu…' : submitLabel}</button></div>
  </form>;
}
export function SearchBox({ value, onChange, placeholder = 'Tìm theo mã, tên…' }) { return <label className="portal-search"><Search size={16} /><input value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} /></label>; }
export async function runAction(action, reload) { try { await action(); await reload?.(); } catch (e) { window.alert(e.message || 'Không thể thực hiện thao tác.'); } }
export const statusPill = (value) => <span className={`status-pill ${String(value || '').toLowerCase().replaceAll(/[^a-z0-9]+/g, '-')}`}>{value || '—'}</span>;
