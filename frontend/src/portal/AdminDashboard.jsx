import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Activity,
  BadgePercent,
  CircleDollarSign,
  Users,
  RefreshCw,
  BarChart3,
  TicketPercent,
  Landmark,
  SlidersHorizontal,
  FileClock,
  ArrowRight,
  ShieldCheck,
} from 'lucide-react';
import { portalApi } from '../api/portalApi';
import {
  ActionForm,
  DataTable,
  PageTitle,
  Panel,
  PortalTabs,
  SearchBox,
  Stat,
  TabPanel,
  date,
  dateTime,
  money,
  runAction,
  statusPill,
  useEndpoint,
} from './common';

export default function AdminDashboard() {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get('tab') || 'overview';
  const setActiveTab = (tabId) => setSearchParams({ tab: tabId });

  const revenue = useEndpoint('/api/v1/reports/revenue');
  const occupancy = useEndpoint('/api/v1/reports/occupancy');
  const staff = useEndpoint('/api/v1/users?pageIndex=1&pageSize=50');
  const banks = useEndpoint('/api/v1/bank-accounts');
  const promo = useEndpoint('/api/v1/promotions/admin?pageIndex=1&pageSize=50');
  const settings = useEndpoint('/api/v1/settings');
  const audit = useEndpoint('/api/v1/audit-logs?pageIndex=1&pageSize=50');

  const [staffSearch, setStaffSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('ALL');
  const [promoSearch, setPromoSearch] = useState('');
  const [settingSearch, setSettingSearch] = useState('');
  const [selectedSetting, setSelectedSetting] = useState({});
  const [auditSearch, setAuditSearch] = useState('');

  const refresh = () =>
    Promise.all([
      revenue.reload(),
      occupancy.reload(),
      staff.reload(),
      banks.reload(),
      promo.reload(),
      settings.reload(),
      audit.reload(),
    ]);

  const recognizedAmount = Number(revenue.data?.recognizedRevenue || 0);
  const paidAmount = Number(revenue.data?.paid || 0);
  const outstandingAmount = Number(revenue.data?.outstanding ?? Math.max(0, recognizedAmount - paidAmount));
  const collectionPercent = recognizedAmount > 0 ? Math.min(100, Math.round((paidAmount / recognizedAmount) * 100)) : 0;
  const occupancyPercent = Number(occupancy.data?.occupancyPercent || 0);

  const tabs = [
    { id: 'overview', label: 'Tổng quan & Báo cáo', icon: BarChart3 },
    { id: 'staff', label: 'Nhân sự & Phân quyền', icon: Users, badge: staff.data?.totalCount ?? staff.rows.length },
    { id: 'promotions', label: 'Khuyến mãi & Ưu đãi', icon: TicketPercent, badge: promo.data?.totalCount ?? promo.rows.length },
    { id: 'banks', label: 'Tài khoản VietQR', icon: Landmark, badge: banks.rows.length },
    { id: 'settings', label: 'Cấu hình Hệ thống', icon: SlidersHorizontal, badge: settings.rows.length },
    { id: 'audit', label: 'Nhật ký Kiểm toán', icon: FileClock, badge: audit.data?.totalCount ?? audit.rows.length },
  ];

  const filteredStaff = staff.rows.filter((u) => {
    const matchesText = `${u.fullName || ''} ${u.username || ''} ${u.email || ''} ${u.phone || ''}`
      .toLowerCase()
      .includes(staffSearch.toLowerCase());
    const matchesRole = roleFilter === 'ALL' || (u.roles || []).includes(roleFilter);
    return matchesText && matchesRole;
  });

  const filteredPromos = promo.rows.filter((p) =>
    `${p.code || ''} ${p.name || ''} ${p.description || ''}`.toLowerCase().includes(promoSearch.toLowerCase())
  );

  const filteredSettings = settings.rows.filter((s) =>
    `${s.settingKey || ''} ${s.settingValue || ''} ${s.description || ''}`
      .toLowerCase()
      .includes(settingSearch.toLowerCase())
  );

  const filteredAudits = audit.rows.filter((a) =>
    `${a.action || ''} ${a.entityName || ''} ${a.entityId || ''} ${a.actorRole || ''}`
      .toLowerCase()
      .includes(auditSearch.toLowerCase())
  );

  return (
    <>
      <PageTitle
        eyebrow="VIETTOUR · SYSTEM ADMINISTRATION"
        title="Trung tâm Quản trị & Điều hành Hệ thống"
        description="Quản lý tập trung theo từng phân hệ chuyên biệt: Báo cáo tài chính, Nhân sự, Khuyến mãi, Ngân hàng VietQR, Cấu hình và Nhật ký kiểm toán."
        action={
          <button type="button" className="portal-button" onClick={refresh}>
            <RefreshCw size={15} /> Làm mới dữ liệu
          </button>
        }
      />

      <div className="portal-stat-grid">
        <Stat
          label="Doanh thu ghi nhận"
          value={money(recognizedAmount)}
          detail={`Thực thu ${money(paidAmount)} (${collectionPercent}%)`}
          icon={CircleDollarSign}
        />
        <Stat
          label="Tỷ lệ lấp đầy chỗ"
          value={`${occupancyPercent.toFixed(1)}%`}
          detail={`${occupancy.data?.bookedSlots ?? 0} / ${occupancy.data?.totalCapacity ?? 0} chỗ khởi hành`}
          icon={Activity}
          tone="green"
        />
        <Stat
          label="Nhân sự nội bộ"
          value={staff.data?.totalCount ?? staff.rows.length}
          detail={`${staff.rows.filter((u) => u.isActive).length} tài khoản đang hoạt động`}
          icon={Users}
          tone="violet"
        />
        <Stat
          label="Khuyến mãi & VietQR"
          value={`${promo.rows.filter((p) => p.isActive).length} ưu đãi`}
          detail={`${banks.rows.length} tài khoản ngân hàng nhận QR`}
          icon={BadgePercent}
          tone="orange"
        />
      </div>

      <PortalTabs label="Các phân hệ quản trị" tabs={tabs} active={activeTab} onChange={setActiveTab} />

      <TabPanel id="overview" active={activeTab}>
        <div className="portal-columns">
          <Panel
            title="Hiệu suất Tài chính & Công suất Đoàn"
            subtitle="Tổng hợp doanh thu, dòng tiền thực thu và tỷ lệ lấp đầy chỗ toàn hệ thống"
          >
            <div className="portal-kpi-cards">
              <div className="portal-kpi-box">
                <span>Doanh thu ghi nhận</span>
                <strong>{money(recognizedAmount)}</strong>
                <small>Tổng giá trị các booking đủ điều kiện</small>
              </div>
              <div className="portal-kpi-box success">
                <span>Đã thu thực tế</span>
                <strong>{money(paidAmount)}</strong>
                <small>Đã đối soát qua VietQR & Sổ quỹ</small>
              </div>
              <div className="portal-kpi-box warning">
                <span>Công nợ phải thu</span>
                <strong>{money(outstandingAmount)}</strong>
                <small>Cần thu bổ sung trước ngày khởi hành</small>
              </div>
            </div>

            <div className="portal-progress-block">
              <div className="portal-progress-head">
                <span>Tỷ lệ thu hồi dòng tiền</span>
                <strong>{collectionPercent}%</strong>
              </div>
              <div className="portal-progress-track">
                <div className="portal-progress-fill blue" style={{ width: `${collectionPercent}%` }} />
              </div>
            </div>

            <div className="portal-progress-block">
              <div className="portal-progress-head">
                <span>Công suất lấp đầy lịch khởi hành ({occupancy.data?.bookedSlots ?? 0}/{occupancy.data?.totalCapacity ?? 0} chỗ)</span>
                <strong>{occupancyPercent.toFixed(1)}%</strong>
              </div>
              <div className="portal-progress-track">
                <div className="portal-progress-fill green" style={{ width: `${Math.min(100, occupancyPercent)}%` }} />
              </div>
            </div>
          </Panel>

          <Panel
            title="Truy cập nhanh Phân hệ Quản trị"
            subtitle="Chọn một phân hệ bên dưới để xem danh sách chi tiết và thực hiện thao tác"
          >
            <div className="portal-module-grid">
              <button type="button" className="portal-module-card" onClick={() => setActiveTab('staff')}>
                <div className="module-card-icon violet"><Users size={18} /></div>
                <div className="module-card-body">
                  <strong>Nhân sự & Phân quyền</strong>
                  <span>{staff.rows.length} tài khoản · Cấp mới, khóa/mở khóa quyền nội bộ</span>
                </div>
                <ArrowRight size={16} />
              </button>

              <button type="button" className="portal-module-card" onClick={() => setActiveTab('promotions')}>
                <div className="module-card-icon orange"><TicketPercent size={18} /></div>
                <div className="module-card-body">
                  <strong>Khuyến mãi & Mã ưu đãi</strong>
                  <span>{promo.rows.length} chương trình · Cấu hình giảm giá %, theo khách/booking</span>
                </div>
                <ArrowRight size={16} />
              </button>

              <button type="button" className="portal-module-card" onClick={() => setActiveTab('banks')}>
                <div className="module-card-icon blue"><Landmark size={18} /></div>
                <div className="module-card-body">
                  <strong>Tài khoản Ngân hàng VietQR</strong>
                  <span>{banks.rows.length} tài khoản · Quản lý BIN & số tài khoản nhận thanh toán</span>
                </div>
                <ArrowRight size={16} />
              </button>

              <button type="button" className="portal-module-card" onClick={() => setActiveTab('settings')}>
                <div className="module-card-icon green"><SlidersHorizontal size={18} /></div>
                <div className="module-card-body">
                  <strong>Cấu hình Tham số Hệ thống</strong>
                  <span>{settings.rows.length} khóa cấu hình · Đặt cọc, thời gian giữ chỗ, chính sách hoàn hủy</span>
                </div>
                <ArrowRight size={16} />
              </button>
            </div>
          </Panel>
        </div>

        <Panel
          title="Nhật ký Kiểm toán Gần đây"
          subtitle="5 thao tác quản trị và nghiệp vụ mới nhất được ghi vết tự động"
          action={
            <button type="button" className="portal-button" onClick={() => setActiveTab('audit')}>
              Xem toàn bộ ({audit.data?.totalCount ?? audit.rows.length}) <ArrowRight size={14} />
            </button>
          }
        >
          <DataTable
            label="Nhật ký kiểm toán gần đây"
            loading={audit.loading}
            error={audit.error}
            onRefresh={audit.reload}
            rows={audit.rows.slice(0, 5)}
            columns={[
              { key: 'createdAt', label: 'Thời gian', render: (r) => dateTime(r.createdAt) },
              { key: 'action', label: 'Thao tác', render: (r) => statusPill(r.action) },
              { key: 'entityName', label: 'Đối tượng' },
              { key: 'entityId', label: 'Mã tham chiếu' },
              {
                key: 'actorRole',
                label: 'Người thực hiện',
                render: (r) =>
                  r.actorRole
                    ? `${r.actorRole}${r.actorUserId ? ` #${r.actorUserId}` : ''}`
                    : r.actorUserId
                      ? `#${r.actorUserId}`
                      : 'Hệ thống',
              },
            ]}
          />
        </Panel>
      </TabPanel>

      <TabPanel id="staff" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel
            title="Danh sách Tài khoản Nhân sự"
            subtitle="Quản lý quyền truy cập các cổng Kinh doanh, Điều hành, Hướng dẫn viên và Kế toán"
          >
            <div className="portal-toolbar">
              <SearchBox
                value={staffSearch}
                onChange={setStaffSearch}
                placeholder="Tìm theo họ tên, tài khoản, email, SĐT…"
              />
              <div className="portal-filter-pills" role="group" aria-label="Lọc theo vai trò">
                {['ALL', 'ADMIN', 'SALES', 'OPERATOR', 'GUIDE', 'ACCOUNTANT'].map((role) => (
                  <button
                    key={role}
                    type="button"
                    className={`portal-pill-btn ${roleFilter === role ? 'active' : ''}`}
                    onClick={() => setRoleFilter(role)}
                  >
                    {role === 'ALL' ? 'Tất cả' : role}
                  </button>
                ))}
              </div>
            </div>

            <DataTable
              label="Danh sách nhân sự nội bộ"
              loading={staff.loading}
              error={staff.error}
              onRefresh={staff.reload}
              rows={filteredStaff}
              columns={[
                {
                  key: 'fullName',
                  label: 'Nhân viên',
                  render: (r) => (
                    <div className="cell-stack">
                      <strong>{r.fullName}</strong>
                      <small>{r.email || '—'}</small>
                    </div>
                  ),
                },
                {
                  key: 'username',
                  label: 'Tài khoản & SĐT',
                  render: (r) => (
                    <div className="cell-stack">
                      <span>@{r.username}</span>
                      <small>{r.phone || 'Chưa cập nhật SĐT'}</small>
                    </div>
                  ),
                },
                {
                  key: 'roles',
                  label: 'Vai trò',
                  render: (r) => (
                    <div className="cell-tags">
                      {(r.roles || []).map((role) => (
                        <span key={role} className="role-badge">
                          <ShieldCheck size={12} /> {role}
                        </span>
                      ))}
                    </div>
                  ),
                },
                {
                  key: 'isActive',
                  label: 'Trạng thái',
                  render: (r) => statusPill(r.isActive ? 'ACTIVE' : 'LOCKED'),
                },
                {
                  key: 'actions',
                  label: 'Thao tác',
                  render: (r) => (
                    <button
                      type="button"
                      className="portal-button sm"
                      onClick={() =>
                        runAction(
                          () => portalApi.patch(`/api/v1/users/${r.id}/status`, { isActive: !r.isActive }),
                          staff.reload
                        )
                      }
                    >
                      {r.isActive ? 'Khóa TK' : 'Mở khóa'}
                    </button>
                  ),
                },
              ]}
            />
          </Panel>

          <Panel
            className="portal-sticky-panel"
            title="Cấp Tài khoản Nhân sự Mới"
            subtitle="Khởi tạo tài khoản nội bộ và gán vai trò nghiệp vụ tương ứng"
          >
            <ActionForm
              fields={[
                { label: 'Tên đăng nhập', name: 'username', required: true, placeholder: 'vd: sales_hanoi' },
                { label: 'Email công vụ', name: 'email', type: 'email', required: true, placeholder: 'staff@viettour.local' },
                { label: 'Họ và tên', name: 'fullName', required: true, placeholder: 'Nguyễn Văn A' },
                { label: 'Số điện thoại', name: 'phone', placeholder: '090xxxxxxx' },
                { label: 'Mật khẩu khởi tạo', name: 'password', type: 'password', required: true },
                {
                  label: 'Vai trò nghiệp vụ',
                  name: 'roleCode',
                  required: true,
                  options: [
                    { value: 'SALES', label: 'SALES — Kinh doanh & Booking' },
                    { value: 'OPERATOR', label: 'OPERATOR — Điều hành Tour' },
                    { value: 'GUIDE', label: 'GUIDE — Hướng dẫn viên' },
                    { value: 'ACCOUNTANT', label: 'ACCOUNTANT — Kế toán & Đối soát' },
                  ],
                },
              ]}
              submitLabel="Tạo tài khoản nhân sự"
              onSubmit={async (v) => {
                await portalApi.post('/api/v1/users/staff', v);
                await staff.reload();
              }}
            />
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="promotions" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel
            title="Danh sách Chương trình Khuyến mãi"
            subtitle="Quản lý mã giảm giá, hạn mức sử dụng và trạng thái kích hoạt"
          >
            <SearchBox
              value={promoSearch}
              onChange={setPromoSearch}
              placeholder="Tìm theo mã ưu đãi hoặc tên chương trình…"
            />
            <DataTable
              label="Danh sách chương trình khuyến mãi"
              loading={promo.loading}
              error={promo.error}
              onRefresh={promo.reload}
              rows={filteredPromos}
              columns={[
                {
                  key: 'code',
                  label: 'Mã ưu đãi',
                  render: (r) => <strong className="mono-badge">{r.code}</strong>,
                },
                {
                  key: 'name',
                  label: 'Chương trình',
                  render: (r) => (
                    <div className="cell-stack">
                      <strong>{r.name}</strong>
                      <small>{r.description || 'Áp dụng toàn bộ tour'}</small>
                    </div>
                  ),
                },
                {
                  key: 'discountValue',
                  label: 'Mức giảm',
                  render: (r) => (
                    <div className="cell-stack">
                      <strong>
                        {r.discountType === 'Percent' ? `${r.discountValue}%` : money(r.discountValue)}
                      </strong>
                      <small>
                        {r.maxDiscountAmount ? `Tối đa ${money(r.maxDiscountAmount)}` : r.discountType}
                      </small>
                    </div>
                  ),
                },
                {
                  key: 'validTo',
                  label: 'Thời hạn',
                  render: (r) => (
                    <div className="cell-stack">
                      <span>{date(r.validFrom)} → {date(r.validTo)}</span>
                      <small>Đơn tối thiểu: {money(r.minOrderAmount)}</small>
                    </div>
                  ),
                },
                {
                  key: 'isActive',
                  label: 'Trạng thái',
                  render: (r) => statusPill(r.isActive ? 'ACTIVE' : 'PAUSED'),
                },
                {
                  key: 'actions',
                  label: 'Thao tác',
                  render: (r) => (
                    <button
                      type="button"
                      className="portal-button sm"
                      onClick={() =>
                        runAction(
                          () =>
                            portalApi.put(`/api/v1/promotions/${r.id}`, {
                              code: r.code,
                              name: r.name,
                              description: r.description,
                              discountType: r.discountType,
                              discountValue: r.discountValue,
                              maxDiscountAmount: r.maxDiscountAmount,
                              minOrderAmount: r.minOrderAmount,
                              minPax: r.minPax,
                              applyAllTours: r.applyAllTours,
                              usageLimit: r.usageLimit,
                              maxUsagePerCustomer: r.maxUsagePerCustomer,
                              validFrom: r.validFrom,
                              validTo: r.validTo,
                              isActive: !r.isActive,
                              tourIds: [],
                            }),
                          promo.reload
                        )
                      }
                    >
                      {r.isActive ? 'Tạm dừng' : 'Kích hoạt'}
                    </button>
                  ),
                },
              ]}
            />
          </Panel>

          <Panel
            className="portal-sticky-panel"
            title="Tạo Chương trình Khuyến mãi"
            subtitle="Thiết lập điều kiện giảm giá tự động khi khách đặt tour"
          >
            <ActionForm
              fields={[
                { label: 'Mã ưu đãi (Code)', name: 'code', required: true, placeholder: 'HE2026' },
                { label: 'Tên chương trình', name: 'name', required: true, placeholder: 'Ưu đãi Hè Rực Rỡ' },
                {
                  label: 'Loại giảm giá',
                  name: 'discountType',
                  required: true,
                  options: [
                    { value: 'Percent', label: 'Phần trăm (%)' },
                    { value: 'FixedBooking', label: 'Số tiền cố định / Booking' },
                    { value: 'FixedPerPax', label: 'Số tiền cố định / Khách' },
                  ],
                },
                { label: 'Giá trị giảm', name: 'discountValue', type: 'number', min: 1, required: true },
                { label: 'Giảm tối đa (VNĐ)', name: 'maxDiscountAmount', type: 'number', min: 0 },
                { label: 'Đơn tối thiểu (VNĐ)', name: 'minOrderAmount', type: 'number', min: 0, defaultValue: 0 },
                { label: 'Số khách tối thiểu', name: 'minPax', type: 'number', min: 1, defaultValue: 1 },
                { label: 'Tổng lượt sử dụng', name: 'usageLimit', type: 'number', min: 1 },
                { label: 'Hiệu lực từ', name: 'validFrom', type: 'datetime-local', required: true },
                { label: 'Hiệu lực đến', name: 'validTo', type: 'datetime-local', required: true },
                { label: 'Mô tả điều kiện', name: 'description', wide: true },
              ]}
              submitLabel="Phát hành mã khuyến mãi"
              onSubmit={async (v) => {
                await portalApi.post('/api/v1/promotions', {
                  ...v,
                  discountValue: Number(v.discountValue),
                  maxDiscountAmount: v.maxDiscountAmount ? Number(v.maxDiscountAmount) : null,
                  minOrderAmount: Number(v.minOrderAmount || 0),
                  minPax: Number(v.minPax || 1),
                  usageLimit: v.usageLimit ? Number(v.usageLimit) : null,
                  maxUsagePerCustomer: 1,
                  applyAllTours: true,
                  isActive: true,
                  validFrom: new Date(v.validFrom).toISOString(),
                  validTo: new Date(v.validTo).toISOString(),
                });
                await promo.reload();
              }}
            />
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="banks" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel
            title="Tài khoản Ngân hàng VietQR"
            subtitle="Danh sách tài khoản thụ hưởng dùng để sinh mã QR thanh toán tự động cho khách hàng"
          >
            <DataTable
              label="Danh sách tài khoản ngân hàng VietQR"
              loading={banks.loading}
              error={banks.error}
              onRefresh={banks.reload}
              rows={banks.rows}
              columns={[
                {
                  key: 'bankName',
                  label: 'Ngân hàng',
                  render: (r) => (
                    <div className="cell-stack">
                      <strong>{r.bankName}</strong>
                      <small>Mã NH: {r.bankCode || '—'}</small>
                    </div>
                  ),
                },
                { key: 'bankBin', label: 'Mã BIN Napas', render: (r) => <span className="mono-badge">{r.bankBin}</span> },
                { key: 'accountNumber', label: 'Số tài khoản', render: (r) => <strong>{r.accountNumber}</strong> },
                { key: 'accountHolder', label: 'Chủ tài khoản' },
                {
                  key: 'isDefault',
                  label: 'Vai trò QR',
                  render: (r) => statusPill(r.isDefault ? 'DEFAULT' : 'ACTIVE'),
                },
              ]}
            />
          </Panel>

          <Panel
            className="portal-sticky-panel"
            title="Thêm Tài khoản Thụ hưởng Mới"
            subtitle="Cấu hình chuẩn Napas BIN để tích hợp tạo mã VietQR động"
          >
            <ActionForm
              fields={[
                { label: 'Mã viết tắt ngân hàng', name: 'bankCode', required: true, placeholder: 'VCB, TCB, MB…' },
                { label: 'Mã BIN Napas (6 số)', name: 'bankBin', required: true, placeholder: '970436' },
                { label: 'Tên ngân hàng đầy đủ', name: 'bankName', required: true, placeholder: 'Vietcombank', wide: true },
                { label: 'Số tài khoản nhận tiền', name: 'accountNumber', required: true, placeholder: '0011001234567' },
                { label: 'Tên chủ tài khoản (in hoa)', name: 'accountHolder', required: true, placeholder: 'CONG TY DU LICH VIETTOUR' },
              ]}
              submitLabel="Thêm tài khoản VietQR"
              onSubmit={async (v) => {
                await portalApi.post('/api/v1/bank-accounts', v);
                await banks.reload();
              }}
            />
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="settings" active={activeTab}>
        <div className="portal-workspace-grid">
          <Panel
            title="Tham số Cấu hình Hệ thống"
            subtitle="Nhấn 'Chỉnh sửa' trên dòng bất kỳ để nạp nhanh thông tin sang biểu mẫu bên phải"
          >
            <SearchBox
              value={settingSearch}
              onChange={setSettingSearch}
              placeholder="Tìm theo khóa cấu hình (key), giá trị hoặc mô tả…"
            />
            <DataTable
              label="Bảng cấu hình hệ thống"
              loading={settings.loading}
              error={settings.error}
              onRefresh={settings.reload}
              rows={filteredSettings}
              columns={[
                {
                  key: 'settingKey',
                  label: 'Khóa cấu hình (Key)',
                  render: (r) => (
                    <div className="cell-stack">
                      <strong className="mono-badge">{r.settingKey}</strong>
                      <small>{r.description || 'Tham số vận hành'}</small>
                    </div>
                  ),
                },
                {
                  key: 'settingValue',
                  label: 'Giá trị hiện hành',
                  render: (r) => <code className="setting-value-preview">{String(r.settingValue ?? '')}</code>,
                },
                { key: 'valueType', label: 'Kiểu dữ liệu', render: (r) => statusPill(r.valueType || 'String') },
                { key: 'updatedAt', label: 'Cập nhật', render: (r) => date(r.updatedAt) },
                {
                  key: 'actions',
                  label: 'Thao tác',
                  render: (r) => (
                    <button
                      type="button"
                      className="portal-button sm"
                      onClick={() =>
                        setSelectedSetting({
                          key: r.settingKey,
                          value: r.settingValue,
                          valueType: r.valueType || 'String',
                          description: r.description || '',
                        })
                      }
                    >
                      Chỉnh sửa
                    </button>
                  ),
                },
              ]}
            />
          </Panel>

          <Panel
            className="portal-sticky-panel"
            title={selectedSetting.key ? `Cập nhật: ${selectedSetting.key}` : 'Cập nhật / Thêm Cấu hình'}
            subtitle="Thay đổi có hiệu lực ngay cho toàn bộ quy trình tính giá, đặt cọc và hoàn hủy"
            action={
              selectedSetting.key && (
                <button type="button" className="text-button" onClick={() => setSelectedSetting({})}>
                  Nhập mới
                </button>
              )
            }
          >
            <ActionForm
              key={selectedSetting.key || 'new-setting'}
              initialValues={selectedSetting}
              fields={[
                { label: 'Khóa setting (Key)', name: 'key', required: true, placeholder: 'Booking.HoldMinutes', wide: true },
                {
                  label: 'Kiểu dữ liệu',
                  name: 'valueType',
                  defaultValue: 'String',
                  options: ['String', 'Int', 'Decimal', 'Bool', 'Json'].map((v) => ({ value: v, label: v })),
                },
                { label: 'Mô tả ý nghĩa', name: 'description', placeholder: 'Thời gian giữ chỗ tạm thời' },
                { label: 'Giá trị cấu hình', name: 'value', type: 'textarea', required: true, wide: true },
              ]}
              submitLabel="Lưu cấu hình hệ thống"
              onSubmit={async (v) => {
                await portalApi.put(`/api/v1/settings/${encodeURIComponent(v.key)}`, {
                  value: String(v.value),
                  valueType: v.valueType || 'String',
                  description: v.description,
                });
                setSelectedSetting({});
                await settings.reload();
              }}
            />
          </Panel>
        </div>
      </TabPanel>

      <TabPanel id="audit" active={activeTab}>
        <Panel
          title="Nhật ký Kiểm toán & Truy vết Hệ thống (Audit Logs)"
          subtitle="Theo dõi chi tiết mọi thay đổi dữ liệu, phân quyền và giao dịch nội bộ theo thời gian thực"
        >
          <SearchBox
            value={auditSearch}
            onChange={setAuditSearch}
            placeholder="Tìm theo thao tác (CREATE, UPDATE…), đối tượng (Booking, Tour, User…) hoặc vai trò…"
          />
          <DataTable
            label="Bảng nhật ký kiểm toán"
            loading={audit.loading}
            error={audit.error}
            onRefresh={audit.reload}
            rows={filteredAudits}
            columns={[
              { key: 'createdAt', label: 'Thời gian ghi nhận', render: (r) => dateTime(r.createdAt) },
              { key: 'action', label: 'Hành động', render: (r) => statusPill(r.action) },
              { key: 'entityName', label: 'Phân hệ / Đối tượng', render: (r) => <strong>{r.entityName}</strong> },
              { key: 'entityId', label: 'Mã tham chiếu', render: (r) => <span className="mono-badge">{r.entityId || '—'}</span> },
              {
                key: 'actorRole',
                label: 'Tác nhân thực hiện',
                render: (r) =>
                  r.actorRole
                    ? `${r.actorRole}${r.actorUserId ? ` (#${r.actorUserId})` : ''}`
                    : r.actorUserId
                      ? `User #${r.actorUserId}`
                      : 'Hệ thống',
              },
              { key: 'ipAddress', label: 'Địa chỉ IP', render: (r) => r.ipAddress || 'Nội bộ' },
            ]}
          />
        </Panel>
      </TabPanel>
    </>
  );
}
