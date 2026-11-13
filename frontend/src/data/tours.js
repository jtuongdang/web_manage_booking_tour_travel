// Dữ liệu mẫu (mock) — sau này thay bằng API ASP.NET: GET /api/v1/tours
export const HERO_IMAGE = '/images/hero-beach.jpg';

const addDays = (base, d) => {
  const x = new Date(base);
  x.setDate(x.getDate() + d);
  return x.toISOString().slice(0, 10);
};
const today = new Date();

const makeDepartures = (tourCode, basePrice, offsets) =>
  offsets.map(([offset, capacity, booked, priceDelta = 0], i) => ({
    id: `${tourCode}-D${i + 1}`,
    departureDate: addDays(today, offset),
    capacity,
    booked,
    available: capacity - booked,
    adultPrice: basePrice + priceDelta,
    status: capacity - booked <= 0 ? 'Full' : 'Open',
  }));

export const destinations = [
  { name: 'Phú Quốc', count: 24, image: '/images/phu-quoc.jpg' },
  { name: 'Đà Nẵng - Hội An', count: 31, image: '/images/hoi-an.jpg' },
  { name: 'Hạ Long', count: 18, image: '/images/ha-long.jpg' },
  { name: 'Sapa', count: 15, image: '/images/sapa.jpg' },
  { name: 'Nhật Bản', count: 12, image: '/images/nhat-ban.jpg' },
  { name: 'Hàn Quốc', count: 10, image: '/images/han-quoc.jpg' },
];

export const categories = [
  { key: 'all', label: 'Tất cả' },
  { key: 'domestic', label: 'Trong nước' },
  { key: 'international', label: 'Nước ngoài' },
  { key: 'beach', label: 'Biển đảo' },
  { key: 'mountain', label: 'Núi rừng' },
  { key: 'culture', label: 'Văn hóa' },
];

export const tours = [
  {
    id: 1,
    code: 'PQ-3N2D',
    slug: 'phu-quoc-3n2d-cap-treo-hon-thom',
    title: 'Phú Quốc 3N2Đ: Cáp treo Hòn Thơm - Grand World - Lặn ngắm san hô',
    destination: 'Phú Quốc',
    departureFrom: 'TP. Hồ Chí Minh',
    durationDays: 3,
    durationNights: 2,
    transport: 'Máy bay',
    type: 'GroupTour',
    tags: ['domestic', 'beach'],
    badges: ['Bán chạy', 'Xác nhận ngay'],
    rating: 4.8,
    reviewCount: 1243,
    bookedCount: 5200,
    originalPrice: 5990000,
    price: 4990000,
    childPercent: 75,
    infantPrice: 500000,
    singleSupplement: 1200000,
    freeCancelDays: 15,
    images: ['/images/phu-quoc.jpg', '/images/coral-diving.jpg', '/images/luxury-resort.jpg', '/images/hero-beach.jpg', '/images/hoi-an.jpg'],
    highlights: [
      'Đi cáp treo vượt biển dài nhất thế giới đến Hòn Thơm',
      'Khám phá Grand World - "thành phố không ngủ"',
      'Lặn ngắm san hô tại quần đảo An Thới',
      'Khách sạn 4 sao gần biển, ăn sáng buffet',
    ],
    includes: ['Vé máy bay khứ hồi + 7kg hành lý', 'Khách sạn 4★ (2 khách/phòng)', 'Các bữa ăn theo chương trình', 'Vé tham quan, cáp treo', 'HDV suốt tuyến', 'Bảo hiểm du lịch 120 triệu/vụ'],
    excludes: ['Chi phí cá nhân, đồ uống ngoài', 'Phụ thu phòng đơn', 'Tiền tip HDV & tài xế'],
    itinerary: [
      { day: 1, title: 'TP.HCM - Phú Quốc - Bắc Đảo', meals: 'Trưa, Tối', description: 'Đón khách tại sân bay Phú Quốc. Tham quan Vinpearl Safari (tự túc vé), Grand World buổi tối, xem show "Tinh hoa Việt Nam".' },
      { day: 2, title: 'Nam Đảo - Cáp treo Hòn Thơm - Lặn san hô', meals: 'Sáng, Trưa, Tối', description: 'Đi tàu tham quan 3 đảo An Thới, lặn ngắm san hô. Chiều trải nghiệm cáp treo Hòn Thơm & công viên nước Aquatopia.' },
      { day: 3, title: 'Chợ Dương Đông - Mua sắm - TP.HCM', meals: 'Sáng', description: 'Tham quan cơ sở nước mắm, vườn tiêu, chợ đêm. Ra sân bay đáp chuyến bay về TP.HCM. Kết thúc chương trình.' },
    ],
    get departures() {
      return makeDepartures(this.code, this.price, [[5, 30, 30], [9, 30, 22], [16, 30, 12, 300000], [23, 35, 4], [30, 35, 0, 500000]]);
    },
  },
  {
    id: 2,
    code: 'DN-4N3D',
    slug: 'da-nang-hoi-an-ba-na-4n3d',
    title: 'Đà Nẵng - Hội An - Bà Nà Hills - Cầu Vàng 4N3Đ',
    destination: 'Đà Nẵng - Hội An',
    departureFrom: 'Hà Nội',
    durationDays: 4,
    durationNights: 3,
    transport: 'Máy bay',
    type: 'GroupTour',
    tags: ['domestic', 'beach', 'culture'],
    badges: ['Ưu đãi -15%'],
    rating: 4.7,
    reviewCount: 892,
    bookedCount: 3100,
    originalPrice: 7290000,
    price: 6190000,
    childPercent: 75,
    infantPrice: 600000,
    singleSupplement: 1800000,
    freeCancelDays: 15,
    images: ['/images/da-nang.jpg', '/images/hoi-an.jpg', '/images/luxury-resort.jpg', '/images/hero-beach.jpg', '/images/coral-diving.jpg'],
    highlights: ['Check-in Cầu Vàng trên đỉnh Bà Nà', 'Dạo phố cổ Hội An về đêm, thả hoa đăng', 'Tắm biển Mỹ Khê', 'Bán đảo Sơn Trà - chùa Linh Ứng'],
    includes: ['Vé máy bay khứ hồi', 'Khách sạn 4★', 'Vé cáp treo Bà Nà + buffet trưa', 'Xe đời mới đưa đón', 'HDV, bảo hiểm'],
    excludes: ['VAT xuất hóa đơn công ty (nếu có yêu cầu riêng)', 'Chi phí cá nhân'],
    itinerary: [
      { day: 1, title: 'Hà Nội - Đà Nẵng - Sơn Trà', meals: 'Trưa, Tối', description: 'Đến Đà Nẵng, tham quan chùa Linh Ứng, bán đảo Sơn Trà, tắm biển Mỹ Khê.' },
      { day: 2, title: 'Bà Nà Hills - Cầu Vàng', meals: 'Sáng, Trưa, Tối', description: 'Cáp treo lên Bà Nà, check-in Cầu Vàng, làng Pháp, Fantasy Park.' },
      { day: 3, title: 'Ngũ Hành Sơn - Phố cổ Hội An', meals: 'Sáng, Trưa, Tối', description: 'Tham quan Ngũ Hành Sơn, làng đá Non Nước. Chiều tối dạo phố cổ Hội An, thả hoa đăng sông Hoài.' },
      { day: 4, title: 'Chợ Hàn - Hà Nội', meals: 'Sáng', description: 'Tự do mua sắm đặc sản tại chợ Hàn. Ra sân bay về Hà Nội.' },
    ],
    get departures() {
      return makeDepartures(this.code, this.price, [[7, 25, 18], [14, 25, 9], [21, 25, 25], [28, 30, 3, 400000]]);
    },
  },
  {
    id: 3,
    code: 'HL-2N1D',
    slug: 'ha-long-du-thuyen-5-sao-2n1d',
    title: 'Hạ Long 2N1Đ: Ngủ đêm du thuyền 5 sao - Hang Sửng Sốt - Chèo Kayak',
    destination: 'Hạ Long',
    departureFrom: 'Hà Nội',
    durationDays: 2,
    durationNights: 1,
    transport: 'Xe Limousine',
    type: 'GroupTour',
    tags: ['domestic', 'beach'],
    badges: ['Cao cấp'],
    rating: 4.9,
    reviewCount: 2107,
    bookedCount: 8700,
    originalPrice: 4500000,
    price: 3890000,
    childPercent: 70,
    infantPrice: 0,
    singleSupplement: 1500000,
    freeCancelDays: 7,
    images: ['/images/ha-long.jpg', '/images/luxury-resort.jpg', '/images/coral-diving.jpg', '/images/hero-beach.jpg', '/images/phu-quoc.jpg'],
    highlights: ['Ngủ đêm trên du thuyền 5 sao giữa vịnh', 'Chèo kayak hang Luồn', 'Lớp học Thái Cực Quyền buổi sáng', 'Tiệc BBQ hải sản hoàng hôn'],
    includes: ['Xe Limousine Hà Nội - Hạ Long khứ hồi', 'Cabin du thuyền 5★', 'Toàn bộ bữa ăn trên tàu', 'Vé thắng cảnh, kayak'],
    excludes: ['Đồ uống trên tàu', 'Spa, massage'],
    itinerary: [
      { day: 1, title: 'Hà Nội - Hạ Long - Hang Sửng Sốt', meals: 'Trưa, Tối', description: 'Xe Limousine đón tại phố cổ. Lên du thuyền, ăn trưa, tham quan hang Sửng Sốt, chèo kayak, tiệc hoàng hôn.' },
      { day: 2, title: 'Hang Luồn - Hà Nội', meals: 'Sáng, Trưa', description: 'Tập Thái Cực Quyền trên boong, tham quan hang Luồn, brunch và trả phòng. Về Hà Nội.' },
    ],
    get departures() {
      return makeDepartures(this.code, this.price, [[3, 20, 14], [6, 20, 20], [10, 20, 5], [13, 20, 2, 200000]]);
    },
  },
  {
    id: 4,
    code: 'SP-3N2D',
    slug: 'sapa-fansipan-3n2d',
    title: 'Sapa 3N2Đ: Chinh phục Fansipan - Bản Cát Cát - Đèo Ô Quy Hồ',
    destination: 'Sapa',
    departureFrom: 'Hà Nội',
    durationDays: 3,
    durationNights: 2,
    transport: 'Xe giường nằm',
    type: 'GroupTour',
    tags: ['domestic', 'mountain', 'culture'],
    badges: ['Giá tốt'],
    rating: 4.6,
    reviewCount: 534,
    bookedCount: 1900,
    originalPrice: 3290000,
    price: 2790000,
    childPercent: 70,
    infantPrice: 0,
    singleSupplement: 800000,
    freeCancelDays: 7,
    images: ['/images/sapa.jpg', '/images/da-nang.jpg', '/images/hoi-an.jpg', '/images/ha-long.jpg', '/images/luxury-resort.jpg'],
    highlights: ['Cáp treo lên "nóc nhà Đông Dương" Fansipan', 'Trekking bản Cát Cát của người H\'Mông', 'Săn mây đèo Ô Quy Hồ'],
    includes: ['Xe giường nằm cabin khứ hồi', 'Khách sạn 3★ trung tâm', 'Bữa ăn theo chương trình', 'HDV, bảo hiểm'],
    excludes: ['Vé cáp treo Fansipan (tự chọn)', 'Chi phí cá nhân'],
    itinerary: [
      { day: 1, title: 'Hà Nội - Sapa - Cát Cát', meals: 'Trưa, Tối', description: 'Khởi hành sáng sớm, đến Sapa nhận phòng, trekking bản Cát Cát.' },
      { day: 2, title: 'Fansipan - Nhà thờ Đá', meals: 'Sáng, Trưa, Tối', description: 'Cáp treo chinh phục Fansipan. Tối dạo nhà thờ Đá, chợ đêm.' },
      { day: 3, title: 'Đèo Ô Quy Hồ - Hà Nội', meals: 'Sáng, Trưa', description: 'Săn mây đèo Ô Quy Hồ, mua đặc sản, về Hà Nội.' },
    ],
    get departures() {
      return makeDepartures(this.code, this.price, [[4, 40, 31], [11, 40, 12], [18, 40, 0]]);
    },
  },
  {
    id: 5,
    code: 'JP-6N5D',
    slug: 'nhat-ban-cung-duong-vang-6n5d',
    title: 'Nhật Bản 6N5Đ: Tokyo - Núi Phú Sĩ - Kyoto - Osaka (Cung đường vàng)',
    destination: 'Nhật Bản',
    departureFrom: 'TP. Hồ Chí Minh',
    durationDays: 6,
    durationNights: 5,
    transport: 'Máy bay',
    type: 'GroupTour',
    tags: ['international', 'culture', 'mountain'],
    badges: ['Bán chạy', 'Bao visa'],
    rating: 4.9,
    reviewCount: 678,
    bookedCount: 2400,
    originalPrice: 32990000,
    price: 28990000,
    childPercent: 85,
    infantPrice: 6000000,
    singleSupplement: 6500000,
    freeCancelDays: 30,
    images: ['/images/nhat-ban.jpg', '/images/han-quoc.jpg', '/images/luxury-resort.jpg', '/images/hoi-an.jpg', '/images/sapa.jpg'],
    highlights: ['Ngắm núi Phú Sĩ từ hồ Kawaguchi', 'Chùa Vàng Kinkakuji & rừng trúc Arashiyama', 'Lâu đài Osaka, phố Dotonbori', 'Tắm Onsen truyền thống'],
    includes: ['Vé máy bay quốc tế', 'Visa Nhật Bản', 'Khách sạn 3-4★', 'Tàu cao tốc Shinkansen 1 chặng', 'HDV tiếng Việt'],
    excludes: ['Hộ chiếu', 'Tiền tip (khoảng 7 USD/ngày)'],
    itinerary: [
      { day: 1, title: 'TP.HCM - Tokyo (bay đêm)', meals: '-', description: 'Tập trung sân bay Tân Sơn Nhất, đáp chuyến bay đêm đi Tokyo.' },
      { day: 2, title: 'Tokyo - Asakusa - Tokyo Skytree', meals: 'Sáng, Trưa, Tối', description: 'Tham quan chùa Sensoji, khu Asakusa, chụp ảnh tháp Skytree, mua sắm Ginza.' },
      { day: 3, title: 'Núi Phú Sĩ - Hồ Kawaguchi - Onsen', meals: 'Sáng, Trưa, Tối', description: 'Lên trạm 5 núi Phú Sĩ (tùy thời tiết), làng cổ Oshino Hakkai, tắm Onsen.' },
      { day: 4, title: 'Shinkansen - Kyoto', meals: 'Sáng, Trưa, Tối', description: 'Đi tàu cao tốc đến Kyoto, tham quan chùa Vàng, rừng trúc Arashiyama.' },
      { day: 5, title: 'Nara - Osaka', meals: 'Sáng, Trưa, Tối', description: 'Công viên hươu Nara, lâu đài Osaka, phố ẩm thực Dotonbori.' },
      { day: 6, title: 'Osaka - TP.HCM', meals: 'Sáng', description: 'Mua sắm tại outlet Rinku, ra sân bay Kansai về Việt Nam.' },
    ],
    get departures() {
      return makeDepartures(this.code, this.price, [[25, 25, 20], [40, 25, 8], [55, 25, 0, 2000000]]);
    },
  },
  {
    id: 6,
    code: 'KR-5N4D',
    slug: 'han-quoc-seoul-nami-everland-5n4d',
    title: 'Hàn Quốc 5N4Đ: Seoul - Đảo Nami - Everland - Mặc Hanbok',
    destination: 'Hàn Quốc',
    departureFrom: 'Hà Nội',
    durationDays: 5,
    durationNights: 4,
    transport: 'Máy bay',
    type: 'GroupTour',
    tags: ['international', 'culture'],
    badges: ['Ưu đãi -10%'],
    rating: 4.7,
    reviewCount: 455,
    bookedCount: 1600,
    originalPrice: 17990000,
    price: 15990000,
    childPercent: 80,
    infantPrice: 3000000,
    singleSupplement: 4000000,
    freeCancelDays: 21,
    images: ['/images/han-quoc.jpg', '/images/nhat-ban.jpg', '/images/luxury-resort.jpg', '/images/hoi-an.jpg', '/images/sapa.jpg'],
    highlights: ['Đảo Nami lãng mạn', 'Công viên Everland', 'Mặc Hanbok dạo cung Gyeongbokgung', 'Tháp Namsan'],
    includes: ['Vé máy bay', 'Visa Hàn Quốc', 'Khách sạn 3★', 'Vé Everland'],
    excludes: ['Hộ chiếu', 'Tiền tip'],
    itinerary: [
      { day: 1, title: 'Hà Nội - Seoul', meals: 'Tối', description: 'Bay đến Incheon, về khách sạn nghỉ ngơi.' },
      { day: 2, title: 'Đảo Nami - Trượt tuyết / Vườn hoa', meals: 'Sáng, Trưa, Tối', description: 'Đảo Nami, công viên theo mùa.' },
      { day: 3, title: 'Everland', meals: 'Sáng, Trưa, Tối', description: 'Cả ngày vui chơi tại Everland.' },
      { day: 4, title: 'Gyeongbokgung - Namsan - Myeongdong', meals: 'Sáng, Trưa, Tối', description: 'Mặc Hanbok tham quan cung điện, tháp Namsan, mua sắm Myeongdong.' },
      { day: 5, title: 'Seoul - Hà Nội', meals: 'Sáng', description: 'Ra sân bay về Việt Nam.' },
    ],
    get departures() {
      return makeDepartures(this.code, this.price, [[20, 30, 26], [34, 30, 10], [48, 30, 1]]);
    },
  },
];

export const reviews = [
  { id: 1, name: 'Nguyễn Minh Anh', type: 'Gia đình', rating: 5, date: '2026-10-28', content: 'Lịch trình hợp lý, HDV nhiệt tình, khách sạn sạch đẹp. Bé nhà mình rất thích lặn san hô. Sẽ quay lại!', verified: true, reply: 'Cảm ơn chị Minh Anh đã tin tưởng! Hẹn gặp lại gia đình ở hành trình tiếp theo.' },
  { id: 2, name: 'Trần Quốc Bảo', type: 'Cặp đôi', rating: 4, date: '2026-10-15', content: 'Tour tốt, giá hợp lý. Chỉ có buổi tối hơi gấp. Thanh toán QR rất nhanh, nhận xác nhận ngay.', verified: true },
  { id: 3, name: 'Lê Thu Hà', type: 'Nhóm bạn', rating: 5, date: '2026-09-30', content: 'Đặt cho nhóm 8 người, được giữ chỗ và có ưu đãi nhóm. Mọi thứ minh bạch, có hóa đơn đầy đủ.', verified: true },
];

export const promotions = {
  WELCOME10: { type: 'percent', value: 10, max: 500000, label: 'Giảm 10% (tối đa 500.000đ) cho khách mới' },
  GROUP300: { type: 'fixed', value: 300000, minPax: 4, label: 'Giảm 300.000đ cho nhóm từ 4 khách' },
};

export const findTour = (slug) => tours.find((t) => t.slug === slug);
