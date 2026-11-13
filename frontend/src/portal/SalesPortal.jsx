import { useState } from 'react';
import { BadgeCheck, Clock3, Search, ShoppingBag, UserPlus } from 'lucide-react';
import { portalApi } from '../api/portalApi';
import { ActionForm, DataTable, PageTitle, Panel, PortalTabs, SearchBox, Stat, TabPanel, date, money, runAction, statusPill, useEndpoint } from './common';

export default function SalesPortal() {
  const customers = useEndpoint('/api/v1/customers?pageIndex=1&pageSize=50');
  const bookings = useEndpoint('/api/v1/bookings?pageIndex=1&pageSize=50');
  const [activeTab, setActiveTab] = useState('bookings');
  const [term, setTerm] = useState('');
  const [qr, setQr] = useState(null);
  const [customer, setCustomer] = useState(null);

  const visible = bookings.rows.filter((b) => `${b.bookingCode} ${b.contactName} ${b.contactPhone}`.toLowerCase().includes(term.toLowerCase()));

  const tabs = [
    { id: 'bookings', label: 'Quản lý Đơn hàng & Giữ chỗ', icon: ShoppingBag, badge: bookings.rows.length },
    { id: 'create', label: 'Tạo Booking hộ & VietQR', icon: Clock3 },
    { id: 'crm', label: 'Hồ sơ Khách hàng CRM', icon: UserPlus, badge: customers.data?.totalCount ?? customers.rows.length },
  ];

  return (
    <>
      <PageTitle
        title="Bán tour & Chăm sóc Khách hàng"
        description="Theo dõi đơn đặt chỗ, hỗ trợ tạo booking tại quầy/Zalo/Hotline và quản lý hồ sơ khách hàng CRM."
      />
      <div className="portal-stat-grid three">
        <Stat label="Đơn đang theo dõi" value={bookings.rows.length} detail="Booking gần đây" icon={ShoppingBag} />
        <Stat label="Khách hàng CRM" value={customers.data?.totalCount ?? customers.rows.length} detail="Hồ sơ khách hàng" icon={UserPlus} tone="violet" />
        <Stat label="Đang giữ chỗ" value={bookings.rows.filter((b) => b.status === 'PendingPayment').length} detail="Chờ thanh toán · theo chính sách TTL" icon={Clock3} tone="orange" />
      </div>

      <PortalTabs label="Phân hệ kinh doanh" tabs={tabs} active={activeTab} onChange={setActiveTab} />

      <TabPanel id="bookings" active={activeTab}>
        <Panel title="Đơn hàng & Yêu cầu cần xử lý" subtitle="Tra cứu nhanh, duyệt hủy hoặc hỗ trợ đổi lịch khởi hành cho khách">
          <div className="panel-toolbar">
            <SearchBox value={term} onChange={setTerm} placeholder="Mã đơn, họ tên khách hoặc số điện thoại…" />
            <button type="button" className="portal-button" onClick={() => bookings.reload()}><Search size={15} /> Tra cứu</button>
          </div>
          <DataTable
            label="Danh sách đơn hàng booking"
            loading={bookings.loading}
            error={bookings.error}
            onRefresh={bookings.reload}
            rows={visible}
            columns={[
              { key: 'bookingCode', label: 'Mã đơn', render: (r) => <strong className="mono-badge">{r.bookingCode}</strong> },
              { key: 'contactName', label: 'Khách hàng' },
              { key: 'contactPhone', label: 'Liên hệ' },
              { key: 'status', label: 'Trạng thái', render: (r) => statusPill(r.status) },
              { key: 'totalAmount', label: 'Tổng tiền', render: (r) => <strong>{money(r.totalAmount)}</strong> },
              { key: 'createdAt', label: 'Ngày tạo', render: (r) => date(r.createdAt) },
              { key: 'actions', label: 'Thao tác', render: (r) => <div className="row-actions"><button type="button" className="portal-button sm" onClick={() => runAction(() => portalApi.post(`/api/v1/bookings/${r.id}/approve-cancel`, {}), bookings.reload)}>Duyệt hủy</button><button type="button" className="portal-button sm" onClick={() => { const departureId = window.prompt('Nhập mã lịch khởi hành mới'); if (departureId) runAction(() => portalApi.post(`/api/v1/bookings/${r.id}/reschedule`, { newDepartureId: Number(departureId), reason: 'Hỗ trợ khách từ portal' }), bookings.reload); }}>Đổi lịch</button></div> },
            ]}
          />
        </Panel>
      </TabPanel>

      <TabPanel id="create" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel title="Tạo Booking hộ Khách hàng" subtitle="Backend tự động tính giá, áp mã khuyến mãi và kiểm tra sức chứa đoàn">
            <ActionForm
              key={customer?.id ?? 'walk-in'}
              initialValues={customer ? { contactName: customer.fullName, contactPhone: customer.phone, contactEmail: customer.email || '', bookingChannel: 'WalkIn', childCount: 0, infantCount: 0 } : { bookingChannel: 'WalkIn', adultCount: 1, childCount: 0, infantCount: 0 }}
              fields={[
                { label: 'Mã lịch khởi hành (Departure ID)', name: 'departureId', type: 'number', min: 1, required: true },
                { label: 'Kênh tiếp nhận', name: 'bookingChannel', defaultValue: 'WalkIn', options: ['WalkIn', 'Zalo', 'Hotline', 'WebB2C'].map((v) => ({ value: v, label: v })) },
                { label: 'Họ tên người liên hệ', name: 'contactName', required: true },
                { label: 'Số điện thoại', name: 'contactPhone', required: true },
                { label: 'Email nhận xác nhận', name: 'contactEmail', type: 'email' },
                { label: 'Số người lớn', name: 'adultCount', type: 'number', min: 1, required: true },
                { label: 'Số trẻ em', name: 'childCount', type: 'number', min: 0, defaultValue: 0 },
                { label: 'Số em bé', name: 'infantCount', type: 'number', min: 0, defaultValue: 0 },
                { label: 'Mã khuyến mãi (nếu có)', name: 'promoCode' },
                { label: 'Ghi chú đặc biệt', name: 'specialNote', type: 'textarea', wide: true },
              ]}
              submitLabel="Tạo giữ chỗ & tính giá"
              hint={customer ? `Đã nạp khách CRM: ${customer.customerCode} (${customer.fullName}).` : 'Có thể chọn khách từ tab CRM hoặc nhập trực tiếp thông tin khách lẻ.'}
              onSubmit={async (v) => {
                const booking = await portalApi.post('/api/v1/bookings', {
                  ...v,
                  departureId: Number(v.departureId),
                  adultCount: Number(v.adultCount || 1),
                  childCount: Number(v.childCount || 0),
                  infantCount: Number(v.infantCount || 0),
                  bookingChannel: v.bookingChannel || 'WalkIn',
                  payOption: 'Deposit',
                });
                await bookings.reload();
                setQr(booking);
              }}
            />
          </Panel>

          <Panel className="portal-sticky-panel" title="Phát hành Mã VietQR Thanh toán" subtitle="Tạo mã QR thu tiền cọc ngay sau khi giữ chỗ thành công">
            {qr ? (
              <>
                <div className="created-booking">
                  <BadgeCheck size={22} />
                  <div>
                    <b>{qr.bookingCode || `Booking #${qr.id}`}</b>
                    <span>Giữ chỗ đến {date(qr.holdExpiresAt)} · {money(qr.totalAmount)}</span>
                  </div>
                  <button
                    type="button"
                    className="portal-button primary"
                    onClick={async () => {
                      try {
                        const payment = await portalApi.post('/api/v1/payments/qr-create', { bookingId: qr.id, bookingCode: qr.bookingCode, mode: 'Deposit' });
                        setQr({ ...qr, payment });
                      } catch (e) {
                        window.alert(e.message);
                      }
                    }}
                  >
                    {qr.payment ? 'QR đã tạo' : 'Tạo QR thanh toán'}
                  </button>
                </div>
                {qr.payment && <pre className="qr-result">{JSON.stringify(qr.payment, null, 2)}</pre>}
              </>
            ) : (
              <p className="portal-empty-note">Tạo booking giữ chỗ ở biểu mẫu bên trái để sinh mã VietQR thu tiền cọc cho khách.</p>
            )}
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="crm" active={activeTab}>
        <Panel title="Danh bạ Khách hàng CRM" subtitle="Chọn một khách hàng để nạp nhanh thông tin sang tab Tạo Booking hộ">
          <DataTable
            label="Danh sách khách hàng CRM"
            loading={customers.loading}
            error={customers.error}
            onRefresh={customers.reload}
            rows={customers.rows}
            columns={[
              { key: 'customerCode', label: 'Mã KH', render: (r) => <span className="mono-badge">{r.customerCode}</span> },
              { key: 'fullName', label: 'Họ và tên', render: (r) => <strong>{r.fullName}</strong> },
              { key: 'phone', label: 'Điện thoại' },
              { key: 'loyaltyTier', label: 'Hạng thẻ', render: (r) => statusPill(r.loyaltyTier) },
              { key: 'totalBookings', label: 'Số đơn' },
              { key: 'totalSpent', label: 'Tổng chi tiêu', render: (r) => money(r.totalSpent) },
              { key: 'pick', label: 'Thao tác', render: (r) => <button type="button" className="portal-button sm primary" onClick={() => { setCustomer(r); setActiveTab('create'); }}>Chọn tạo đơn</button> },
            ]}
          />
        </Panel>
      </TabPanel>
    </>
  );
}
