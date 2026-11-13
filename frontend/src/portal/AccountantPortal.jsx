import { useState } from 'react';
import { Banknote, CircleDollarSign, Scale, RefreshCw } from 'lucide-react';
import { portalApi } from '../api/portalApi';
import { ActionForm, DataTable, PageTitle, Panel, PortalTabs, Stat, TabPanel, date, money, runAction, statusPill, useEndpoint } from './common';

export default function AccountantPortal() {
  const queue = useEndpoint('/api/v1/payments/reconciliation-queue');
  const refunds = useEndpoint('/api/v1/refunds');
  const invoices = useEndpoint('/api/v1/invoices?pageIndex=1&pageSize=20');
  const ledger = useEndpoint('/api/v1/ledger?pageIndex=1&pageSize=50');
  const departures = useEndpoint('/api/v1/departures?includeInternal=true&pageIndex=1&pageSize=50');
  const [activeTab, setActiveTab] = useState('reconcile');

  const refresh = () => Promise.all([queue.reload(), refunds.reload(), invoices.reload(), ledger.reload(), departures.reload()]);

  const tabs = [
    { id: 'reconcile', label: 'Đối soát VietQR & Sổ quỹ', icon: Scale, badge: queue.rows.length },
    { id: 'refunds', label: 'Hoàn tiền & Hóa đơn VAT', icon: CircleDollarSign, badge: refunds.rows.length },
    { id: 'settlement', label: 'Công nợ NCC & Quyết toán đoàn', icon: Banknote, badge: departures.rows.filter((d) => d.status === 'Completed').length },
  ];

  return (
    <>
      <PageTitle
        title="Kế toán & Kiểm soát Dòng tiền"
        description="Đối soát giao dịch VietQR, phê duyệt hoàn tiền, phát hành hóa đơn VAT, thanh toán NCC và khóa sổ đoàn."
        action={<button type="button" className="portal-button" onClick={refresh}><RefreshCw size={15} /> Làm mới</button>}
      />
      <div className="portal-stat-grid three">
        <Stat label="Chờ đối soát VietQR" value={queue.rows.length} detail="Khớp đủ, thiếu hoặc thừa" icon={Scale} tone="orange" />
        <Stat label="Yêu cầu hoàn tiền" value={refunds.rows.length} detail="Chờ kế toán phê duyệt" icon={CircleDollarSign} />
        <Stat label="Dòng sổ quỹ" value={ledger.data?.entries?.length ?? ledger.rows.length} detail={`Thu ${money(ledger.data?.totalIn)} · Chi ${money(ledger.data?.totalOut)}`} icon={Banknote} tone="green" />
      </div>

      <PortalTabs label="Phân hệ kế toán" tabs={tabs} active={activeTab} onChange={setActiveTab} />

      <TabPanel id="reconcile" active={activeTab}>
        <Panel title="Hàng đợi Đối soát VietQR" subtitle="BankRef phải duy nhất theo ngân hàng; chênh lệch được ghi nhận theo số tiền thực nhận">
          <DataTable
            label="Hàng đợi đối soát VietQR"
            loading={queue.loading}
            error={queue.error}
            onRefresh={queue.reload}
            rows={queue.rows}
            columns={[
              { key: 'transactionCode', label: 'Giao dịch', render: (r) => <strong className="mono-badge">{r.transactionCode}</strong> },
              { key: 'bookingId', label: 'Booking' },
              { key: 'expectedAmount', label: 'Kỳ vọng', render: (r) => money(r.expectedAmount) },
              { key: 'actualReceivedAmount', label: 'Thực nhận', render: (r) => money(r.actualReceivedAmount) },
              { key: 'bankCode', label: 'Ngân hàng' },
              { key: 'bankRef', label: 'BankRef' },
              { key: 'difference', label: 'Chênh lệch', render: (r) => money(Number(r.actualReceivedAmount || 0) - Number(r.expectedAmount || 0)) },
              { key: 'action', label: 'Đối soát', render: (r) => <button type="button" className="portal-button sm primary" onClick={() => reconcile(r, queue.reload)}>Xác nhận</button> },
            ]}
          />
        </Panel>
        <Panel title="Sổ quỹ Thu Chi Tổng hợp" subtitle="Các khoản thu khách, hoàn tiền, chi phí vận hành và thanh toán nhà cung cấp">
          <DataTable
            label="Sổ quỹ thu chi"
            loading={ledger.loading}
            error={ledger.error}
            onRefresh={ledger.reload}
            rows={ledger.rows}
            columns={[
              { key: 'occurredAt', label: 'Ngày ghi nhận', render: (r) => date(r.occurredAt ?? r.recordedAt) },
              { key: 'direction', label: 'Loại dòng tiền', render: (r) => statusPill(r.direction) },
              { key: 'category', label: 'Hạng mục' },
              { key: 'amount', label: 'Số tiền', render: (r) => <strong>{money(r.amount)}</strong> },
              { key: 'description', label: 'Diễn giải' },
              { key: 'departureId', label: 'Mã đoàn' },
            ]}
          />
        </Panel>
      </TabPanel>

      <TabPanel id="refunds" active={activeTab}>
        <div className="portal-columns">
          <Panel title="Phê duyệt & Chi hoàn tiền" subtitle="Kiểm tra chính sách hủy trước khi duyệt hoặc xác nhận chuyển khoản hoàn tiền">
            <DataTable
              label="Danh sách yêu cầu hoàn tiền"
              loading={refunds.loading}
              error={refunds.error}
              onRefresh={refunds.reload}
              rows={refunds.rows}
              columns={[
                { key: 'id', label: 'Mã' },
                { key: 'bookingId', label: 'Booking' },
                { key: 'refundAmount', label: 'Số tiền hoàn', render: (r) => <strong>{money(r.refundAmount ?? r.amount)}</strong> },
                { key: 'reason', label: 'Lý do' },
                { key: 'status', label: 'Trạng thái', render: (r) => statusPill(r.status) },
                { key: 'actions', label: 'Thao tác', render: (r) => <div className="row-actions">{r.status === 'PendingApproval' && <button type="button" className="portal-button sm" onClick={() => runAction(() => portalApi.post(`/api/v1/refunds/${r.id}/approve`, {}), refunds.reload)}>Duyệt</button>}{r.status === 'Approved' && <button type="button" className="portal-button sm primary" onClick={() => { const bankCode = window.prompt('Mã ngân hàng'); const transferRef = window.prompt('Mã tham chiếu chuyển khoản'); if (bankCode && transferRef) runAction(async () => { await portalApi.post(`/api/v1/refunds/${r.id}/complete`, { bankCode, transferRef }); await ledger.reload(); }, refunds.reload); }}>Hoàn tất</button>}</div> },
              ]}
            />
          </Panel>
          <Panel title="Hóa đơn Điện tử VAT" subtitle="Tra cứu và phát hành hóa đơn theo mã booking">
            <DataTable
              label="Danh sách hóa đơn VAT"
              loading={invoices.loading}
              error={invoices.error}
              onRefresh={invoices.reload}
              rows={invoices.rows}
              columns={[
                { key: 'invoiceNumber', label: 'Số hóa đơn', render: (r) => <strong className="mono-badge">{r.invoiceNumber}</strong> },
                { key: 'bookingId', label: 'Booking' },
                { key: 'status', label: 'Trạng thái', render: (r) => statusPill(r.status) },
                { key: 'totalAmount', label: 'Giá trị', render: (r) => money(r.totalAmount) },
                { key: 'createdAt', label: 'Ngày phát hành', render: (r) => date(r.createdAt) },
              ]}
            />
            <ActionForm fields={[{ label: 'Mã booking cần xuất hóa đơn', name: 'bookingId', type: 'number', required: true }]} submitLabel="Phát hành hóa đơn VAT" onSubmit={async (v) => { await portalApi.post(`/api/v1/bookings/${v.bookingId}/invoice-issue`, {}); await invoices.reload(); }} />
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="settlement" active={activeTab}>
        <div className="portal-columns">
          <Panel title="Thanh toán Công nợ Nhà cung cấp" subtitle="Ghi nhận thanh toán từng dịch vụ NCC vào sổ quỹ">
            <ActionForm fields={[{ label: 'Mã dịch vụ NCC (Service ID)', name: 'serviceId', type: 'number', required: true }, { label: 'Số tiền thanh toán (VNĐ)', name: 'amount', type: 'number', min: 1, required: true }, { label: 'Phương thức', name: 'method', required: true, defaultValue: 'BankTransfer', options: [{ value: 'BankTransfer', label: 'Chuyển khoản' }, { value: 'Cash', label: 'Tiền mặt' }] }, { label: 'Mã giao dịch ngân hàng (BankRef)', name: 'bankRef' }, { label: 'Ghi chú chứng từ', name: 'notes', wide: true }]} submitLabel="Ghi nhận thanh toán NCC" onSubmit={async (v) => { await portalApi.post(`/api/v1/departure-services/${v.serviceId}/payments`, { ...v, amount: Number(v.amount) }); await ledger.reload(); }} />
          </Panel>
          <Panel title="Duyệt Chi phí Phát sinh Đoàn" subtitle="Phê duyệt hoặc từ chối khoản chi phát sinh do HDV đề xuất">
            <ActionForm fields={[{ label: 'Mã chi phí phát sinh (Expense ID)', name: 'expenseId', type: 'number', required: true }, { label: 'Quyết định phê duyệt', name: 'status', required: true, defaultValue: 'Approved', options: [{ value: 'Approved', label: 'Phê duyệt (Approved)' }, { value: 'Rejected', label: 'Từ chối (Rejected)' }] }, { label: 'Ghi chú kế toán', name: 'note', wide: true }]} submitLabel="Gửi quyết định phê duyệt" onSubmit={async (v) => { await portalApi.post(`/api/v1/operations/extra-expenses/${v.expenseId}/review`, v); await ledger.reload(); }} />
          </Panel>
        </div>

        <Panel title="Quyết toán & Khóa sổ Đoàn (Settlement)" subtitle="Chỉ các đoàn ở trạng thái Completed và không còn khoản chờ duyệt mới có thể khóa sổ">
          <DataTable
            label="Bảng quyết toán đoàn"
            loading={departures.loading}
            error={departures.error}
            onRefresh={departures.reload}
            rows={departures.rows}
            columns={[
              { key: 'departureCode', label: 'Mã đoàn', render: (r) => <strong className="mono-badge">{r.departureCode}</strong> },
              { key: 'tourId', label: 'Tour ID' },
              { key: 'departureDate', label: 'Ngày đi', render: (r) => date(r.departureDate) },
              { key: 'status', label: 'Trạng thái', render: (r) => statusPill(r.status) },
              { key: 'settle', label: 'Thao tác khóa sổ', render: (r) => r.status === 'Completed' ? <button type="button" className="portal-button sm primary" onClick={() => runAction(() => portalApi.post(`/api/v1/departures/${r.id}/settle`, {}), departures.reload)}>Khóa sổ đoàn</button> : r.status === 'Settled' ? 'Đã khóa sổ (Settled)' : 'Chờ hoàn tất đoàn' },
            ]}
          />
        </Panel>
      </TabPanel>
    </>
  );
}
function reconcile(row, reload) {
  const bankCode = window.prompt('Mã ngân hàng (BankCode)', row.bankCode || ''); if (!bankCode) return;
  const bankRef = window.prompt('Mã tham chiếu sao kê duy nhất (BankRef)', row.bankRef || ''); if (!bankRef) return;
  const amount = window.prompt('Số tiền thực nhận', row.actualReceivedAmount ?? row.expectedAmount); if (!amount) return;
  runAction(() => portalApi.post(`/api/v1/payments/${row.id}/reconcile`, { bankCode, bankRef, actualReceivedAmount: Number(amount), reconciliationNote: 'Đối soát thủ công từ portal' }), reload);
}
