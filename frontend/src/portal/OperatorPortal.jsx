import { useState } from 'react';
import { Bus, CalendarDays, MapPinned, RefreshCw } from 'lucide-react';
import { portalApi } from '../api/portalApi';
import { ActionForm, DataTable, PageTitle, Panel, PortalTabs, Stat, TabPanel, date, money, runAction, statusPill, useEndpoint } from './common';

export default function OperatorPortal() {
  const tours = useEndpoint('/api/v1/tours?includeInternal=true&pageIndex=1&pageSize=30');
  const departures = useEndpoint('/api/v1/departures?includeInternal=true&pageIndex=1&pageSize=50');
  const suppliers = useEndpoint('/api/v1/suppliers');
  const guides = useEndpoint('/api/v1/users/guides');
  const [activeTab, setActiveTab] = useState('departures');
  const [selected, setSelected] = useState('');
  const [guideTick, setGuideTick] = useState(0);
  const [checkTick, setCheckTick] = useState(0);
  const [serviceTick, setServiceTick] = useState(0);

  const tabs = [
    { id: 'departures', label: 'Lịch khởi hành & Điều phối đoàn', icon: CalendarDays, badge: departures.data?.totalCount ?? departures.rows.length },
    { id: 'tours', label: 'Danh mục Sản phẩm Tour', icon: MapPinned, badge: tours.data?.totalCount ?? tours.rows.length },
    { id: 'suppliers', label: 'Nhà cung cấp & Dịch vụ NCC', icon: Bus, badge: suppliers.rows.length },
  ];

  const refresh = () => Promise.all([tours.reload(), departures.reload(), suppliers.reload(), guides.reload()]);

  return (
    <>
      <PageTitle
        title="Điều hành Tour & Lịch khởi hành"
        description="Quản lý danh mục sản phẩm tour, sức chứa đoàn, phân công hướng dẫn viên, checklist và dịch vụ nhà cung cấp."
        action={<button type="button" className="portal-button" onClick={refresh}><RefreshCw size={15} /> Làm mới</button>}
      />
      <div className="portal-stat-grid three">
        <Stat label="Tour trong danh mục" value={tours.data?.totalCount ?? tours.rows.length} detail="Draft · Published · Custom" icon={MapPinned} />
        <Stat label="Lịch khởi hành" value={departures.data?.totalCount ?? departures.rows.length} detail="Đang mở và sắp khởi hành" icon={CalendarDays} tone="green" />
        <Stat label="Nhà cung cấp" value={suppliers.rows.length} detail="Đối tác dịch vụ đang hoạt động" icon={Bus} tone="violet" />
      </div>

      <PortalTabs label="Phân hệ điều hành tour" tabs={tabs} active={activeTab} onChange={setActiveTab} />

      <TabPanel id="departures" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel title="Lịch khởi hành & Chỗ ngồi" subtitle="Chọn mã đoàn (#ID) để phân công HDV và quản lý checklist bên dưới">
            <DataTable
              label="Danh sách lịch khởi hành"
              loading={departures.loading}
              error={departures.error}
              onRefresh={departures.reload}
              rows={departures.rows}
              columns={[
                { key: 'id', label: 'Chọn đoàn', render: (r) => <button type="button" className={`portal-button sm ${String(r.id) === selected ? 'primary' : ''}`} onClick={() => setSelected(String(r.id))}>#{r.id}</button> },
                { key: 'departureCode', label: 'Mã đoàn', render: (r) => <strong className="mono-badge">{r.departureCode}</strong> },
                { key: 'tourId', label: 'Tour ID' },
                { key: 'departureDate', label: 'Khởi hành', render: (r) => date(r.departureDate) },
                { key: 'status', label: 'Trạng thái', render: (r) => statusPill(r.status) },
                { key: 'capacity', label: 'Chỗ (Đã đặt/Tổng)', render: (r) => `${r.bookedSlots} / ${r.totalCapacity} (Giữ: ${r.heldSlots})` },
                { key: 'statusAction', label: 'Cập nhật TT', render: (r) => <select aria-label={`Trạng thái đoàn ${r.departureCode}`} value={r.status} onChange={(e) => runAction(() => portalApi.patch(`/api/v1/departures/${r.id}/status`, { status: e.target.value }), departures.reload)}>{['Open', 'Full', 'Confirmed', 'InProgress', 'Completed', 'Cancelled'].map((s) => <option key={s}>{s}</option>)}</select> },
              ]}
            />
          </Panel>
          <Panel className="portal-sticky-panel" title="Mở Lịch Khởi hành Mới" subtitle="Thiết lập ngày đi, giá bán và tổng sức chứa đoàn">
            <ActionForm fields={[{ label: 'Mã tour (Tour ID)', name: 'tourId', type: 'number', min: 1, required: true }, { label: 'Mã đoàn khởi hành', name: 'departureCode', required: true }, { label: 'Ngày khởi hành', name: 'departureDate', type: 'date', required: true }, { label: 'Ngày về', name: 'returnDate', type: 'date', required: true }, { label: 'Hạn chót nhận khách', name: 'cutoffAt', type: 'datetime-local', required: true }, { label: 'Giá người lớn (VNĐ)', name: 'adultPrice', type: 'number', min: 0, required: true }, { label: 'Tổng sức chứa', name: 'totalCapacity', type: 'number', min: 1, required: true }, { label: 'Khách tối thiểu', name: 'minParticipants', type: 'number', min: 1, defaultValue: 1 }]} submitLabel="Tạo lịch khởi hành" onSubmit={async (v) => { await portalApi.post('/api/v1/departures', { ...v, tourId: Number(v.tourId), adultPrice: Number(v.adultPrice), totalCapacity: Number(v.totalCapacity), minParticipants: Number(v.minParticipants || 1), cutoffAt: new Date(v.cutoffAt).toISOString(), isPublic: true }); await departures.reload(); }} />
          </Panel>
        </div>

        {selected && (
          <div className="portal-columns">
            <Panel title={`Phân công HDV & Xe — Đoàn #${selected}`} subtitle="Gán một HDV Lead, HDV Assistant và thông tin phương tiện">
              <DepartureGuides id={selected} refreshKey={guideTick} />
              <ActionForm fields={[{ label: 'Mã user HDV Lead', name: 'lead', type: 'number', required: true }, { label: 'Mã user HDV hỗ trợ', name: 'assistant', type: 'number' }, { label: 'Loại xe', name: 'vehicleType' }, { label: 'Biển số xe', name: 'vehiclePlate' }, { label: 'Tên tài xế', name: 'driverName' }, { label: 'SĐT tài xế', name: 'driverPhone' }]} submitLabel="Lưu phân công đoàn" hint={`Nhân sự GUIDE: ${guides.rows.map((u) => `#${u.id} ${u.fullName}`).join(' · ') || 'Chưa có tài khoản GUIDE.'}`} onSubmit={async (v) => { await portalApi.put(`/api/v1/operations/departures/${selected}/guides`, { guides: [{ guideUserId: Number(v.lead), role: 'Lead' }, ...(v.assistant ? [{ guideUserId: Number(v.assistant), role: 'Assistant' }] : [])], vehicleType: v.vehicleType, vehiclePlate: v.vehiclePlate, driverName: v.driverName, driverPhone: v.driverPhone }); setGuideTick((n) => n + 1); await departures.reload(); }} />
            </Panel>
            <Panel title={`Checklist Chuẩn bị — Đoàn #${selected}`} subtitle="Theo dõi tiến độ chuẩn bị hậu cần trước giờ khởi hành">
              <DepartureChecklist id={selected} refreshKey={checkTick} />
              <ActionForm fields={[{ label: 'Tên đầu việc', name: 'taskName', required: true }, { label: 'Thứ tự ưu tiên', name: 'sortOrder', type: 'number', defaultValue: 0 }, { label: 'Ghi chú', name: 'notes', type: 'textarea', wide: true }]} submitLabel="Thêm đầu việc checklist" onSubmit={async (v) => { await portalApi.post(`/api/v1/operations/departures/${selected}/checklists`, { ...v, sortOrder: Number(v.sortOrder || 0) }); setCheckTick((n) => n + 1); }} />
            </Panel>
          </div>
        )}
      </TabPanel>

      <TabPanel id="tours" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel title="Danh mục Sản phẩm Tour" subtitle="Quản lý trạng thái phát hành và đơn giá tiêu chuẩn">
            <DataTable
              label="Danh mục tour"
              loading={tours.loading}
              error={tours.error}
              onRefresh={tours.reload}
              rows={tours.rows}
              columns={[
                { key: 'code', label: 'Mã tour', render: (r) => <strong className="mono-badge">{r.code}</strong> },
                { key: 'title', label: 'Tên sản phẩm tour' },
                { key: 'tourType', label: 'Loại hình' },
                { key: 'status', label: 'Trạng thái', render: (r) => statusPill(r.status) },
                { key: 'baseAdultPrice', label: 'Giá chuẩn NL', render: (r) => money(r.baseAdultPrice) },
                { key: 'publish', label: 'Phát hành', render: (r) => r.status === 'Published' ? 'Đã xuất bản' : <button type="button" className="portal-button sm" onClick={() => runAction(() => portalApi.put(`/api/v1/tours/${r.id}/publish`, {}), tours.reload)}>Xuất bản</button> },
              ]}
            />
          </Panel>
          <Panel className="portal-sticky-panel" title="Khởi tạo Sản phẩm Tour (Draft)" subtitle="Tạo tour nháp trước khi kiểm duyệt và xuất bản">
            <ActionForm fields={[{ label: 'Mã tour', name: 'code', required: true }, { label: 'Slug URL', name: 'slug', required: true }, { label: 'Tên chương trình tour', name: 'title', required: true, wide: true }, { label: 'Loại tour', name: 'tourType', required: true, defaultValue: 'GroupTour', options: ['GroupTour', 'PrivateTour', 'CustomTour'].map((v) => ({ value: v, label: v })) }, { label: 'Phạm vi', name: 'scopeType', required: true, defaultValue: 'Domestic', options: ['Domestic', 'International'].map((v) => ({ value: v, label: v })) }, { label: 'Điểm khởi hành', name: 'departureFrom', required: true }, { label: 'Điểm đến', name: 'destination', required: true }, { label: 'Số ngày', name: 'durationDays', type: 'number', min: 1, required: true }, { label: 'Số đêm', name: 'durationNights', type: 'number', min: 0, required: true }, { label: 'Phương tiện', name: 'transport', required: true }, { label: 'Giá người lớn', name: 'baseAdultPrice', type: 'number', min: 0, required: true }, { label: 'Mã chính sách hủy', name: 'cancellationPolicyId', type: 'number', min: 1, defaultValue: 1, required: true }, { label: 'Mô tả tóm tắt', name: 'summary', type: 'textarea', wide: true }]} submitLabel="Tạo tour Draft" onSubmit={async (v) => { await portalApi.post('/api/v1/tours', { ...v, durationDays: Number(v.durationDays), durationNights: Number(v.durationNights), baseAdultPrice: Number(v.baseAdultPrice), cancellationPolicyId: Number(v.cancellationPolicyId), childPricePercent: 75, infantPrice: 0, singleSupplement: 0, isPublic: false }); await tours.reload(); }} />
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="suppliers" active={activeTab}>
        <div className="portal-columns">
          <Panel title="Danh bạ Nhà cung cấp" subtitle="Đối tác vận chuyển, lưu trú, nhà hàng và vé tham quan">
            <DataTable
              label="Danh sách nhà cung cấp"
              loading={suppliers.loading}
              error={suppliers.error}
              onRefresh={suppliers.reload}
              rows={suppliers.rows}
              columns={[
                { key: 'code', label: 'Mã NCC', render: (r) => <span className="mono-badge">{r.code}</span> },
                { key: 'name', label: 'Tên nhà cung cấp' },
                { key: 'supplierType', label: 'Nhóm dịch vụ' },
                { key: 'phone', label: 'Điện thoại' },
                { key: 'isActive', label: 'Trạng thái', render: (r) => statusPill(r.isActive ? 'ACTIVE' : 'LOCKED') },
              ]}
            />
          </Panel>
          <Panel title={selected ? `Dịch vụ NCC gắn với Đoàn #${selected}` : 'Gắn dịch vụ NCC theo Đoàn'} subtitle="Chọn một đoàn ở tab Lịch khởi hành để gán dịch vụ và dự toán chi phí">
            {selected ? (
              <>
                <DepartureServices id={selected} refreshKey={serviceTick} />
                <ActionForm fields={[{ label: 'Mã nhà cung cấp', name: 'supplierId', type: 'number', required: true }, { label: 'Tên hạng mục dịch vụ', name: 'serviceName', required: true }, { label: 'Số lượng', name: 'quantity', type: 'number', min: 1, defaultValue: 1, required: true }, { label: 'Đơn giá', name: 'unitPrice', type: 'number', min: 0, required: true }, { label: 'Chi phí thực tế', name: 'actualCost', type: 'number', min: 0, defaultValue: 0 }, { label: 'Mã đặt dịch vụ (Booking Ref)', name: 'bookingReference' }]} submitLabel="Gắn dịch vụ vào đoàn" onSubmit={async (v) => { await portalApi.post(`/api/v1/departures/${selected}/services`, { ...v, supplierId: Number(v.supplierId), quantity: Number(v.quantity || 1), unitPrice: Number(v.unitPrice || 0), actualCost: Number(v.actualCost || 0) }); setServiceTick((n) => n + 1); }} />
              </>
            ) : (
              <p className="portal-empty-note">Vui lòng chọn một đoàn (#ID) tại tab Lịch khởi hành để gắn dịch vụ nhà cung cấp.</p>
            )}
          </Panel>
        </div>
      </TabPanel>
    </>
  );
}
function DepartureGuides({ id, refreshKey }) {
  const list = useEndpoint(`/api/v1/operations/departures/${id}/guides?_=${refreshKey}`);
  return <DataTable label="Hướng dẫn viên phân công" loading={list.loading} error={list.error} onRefresh={list.reload} rows={list.rows} empty="Chưa phân công hướng dẫn viên." columns={[{ key: 'guideUserId', label: 'User ID' }, { key: 'fullName', label: 'Hướng dẫn viên' }, { key: 'role', label: 'Vai trò' }]} />;
}
function DepartureChecklist({ id, refreshKey }) {
  const list = useEndpoint(`/api/v1/operations/departures/${id}/checklists?_=${refreshKey}`);
  return <div className="checklist-list">{list.loading ? 'Đang tải checklist…' : list.error ? list.error : list.rows.map((item) => <label key={item.id}><input type="checkbox" checked={item.isCompleted} onChange={() => runAction(() => portalApi.patch(`/api/v1/operations/departures/${id}/checklists/${item.id}`, { isCompleted: !item.isCompleted, notes: item.notes }), list.reload)} />{item.taskName}</label>)}</div>;
}
function DepartureServices({ id, refreshKey }) {
  const services = useEndpoint(`/api/v1/departures/${id}/services?_=${refreshKey}`);
  return <DataTable label="Dịch vụ nhà cung cấp của đoàn" loading={services.loading} error={services.error} onRefresh={services.reload} rows={services.rows} empty="Chưa gắn dịch vụ nhà cung cấp." columns={[{ key: 'serviceName', label: 'Dịch vụ' }, { key: 'supplierId', label: 'Mã NCC' }, { key: 'quantity', label: 'SL' }, { key: 'actualCost', label: 'Chi phí', render: (r) => money(r.actualCost) }, { key: 'paymentStatus', label: 'Thanh toán' }]} />;
}
