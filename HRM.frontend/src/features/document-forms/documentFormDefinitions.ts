export type DocumentFieldType = "text" | "date" | "number" | "textarea" | "time";

export type DocumentFieldDefinition = {
  key: string;
  label: string;
  type: DocumentFieldType;
  required?: boolean;
  placeholder?: string;
  defaultValue?: string;
  span?: "full";
};

export type DocumentFormDefinition = {
  key: string;
  category: string;
  displayName: string;
  documentTitle: string;
  numberPrefix: string;
  signerTitle: string;
  fields: DocumentFieldDefinition[];
  renderBody: (values: Record<string, string>) => string;
};

const employeeFields: DocumentFieldDefinition[] = [
  { key: "employeeName", label: "Họ và tên", type: "text", required: true },
  { key: "employeeCode", label: "Mã nhân sự", type: "text", required: true },
  { key: "department", label: "Phòng ban", type: "text", required: true },
  { key: "position", label: "Chức danh", type: "text", required: true },
];

const decisionEmployeeFields: DocumentFieldDefinition[] = [
  { key: "employeeName", label: "Người lao động", type: "text", required: true },
  { key: "employeeCode", label: "Mã nhân sự", type: "text", required: true },
  { key: "currentDepartment", label: "Đơn vị hiện tại", type: "text", required: true },
  { key: "currentPosition", label: "Chức danh hiện tại", type: "text", required: true },
];

const paragraph = (content: string) => `<p>${content}</p>`;

export const documentFormDefinitions: DocumentFormDefinition[] = [
  {
    key: "LEAVE_APPLICATION",
    category: "Đơn đề nghị",
    displayName: "Đơn xin nghỉ phép",
    documentTitle: "ĐƠN XIN NGHỈ PHÉP",
    numberPrefix: "ĐN",
    signerTitle: "Người làm đơn",
    fields: [
      ...employeeFields,
      { key: "leaveFrom", label: "Nghỉ từ ngày", type: "date", required: true },
      { key: "leaveTo", label: "Đến ngày", type: "date", required: true },
      { key: "totalDays", label: "Số ngày nghỉ", type: "number", required: true },
      { key: "handoverTo", label: "Người nhận bàn giao", type: "text" },
      { key: "reason", label: "Lý do nghỉ", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`Kính gửi: Ban Giám đốc và Phòng Nhân sự Công ty.`),
      paragraph(
        `Tôi tên là ${values.employeeName}, mã nhân sự ${values.employeeCode}, hiện đang làm việc tại ${values.department} với chức danh ${values.position}.`,
      ),
      paragraph(
        `Tôi làm đơn này kính đề nghị được nghỉ phép từ ngày ${values.leaveFrom} đến ngày ${values.leaveTo}, tổng số ${values.totalDays} ngày làm việc.`,
      ),
      paragraph(`Lý do nghỉ: ${values.reason}.`),
      values.handoverTo
        ? paragraph(`Trong thời gian nghỉ, tôi đã bàn giao công việc cho ${values.handoverTo}.`)
        : "",
      paragraph(
        `Tôi cam kết thực hiện đúng quy định nghỉ phép của Công ty và trở lại làm việc đúng thời hạn nêu trên.`,
      ),
    ].join(""),
  },
  {
    key: "RESIGNATION_APPLICATION",
    category: "Đơn đề nghị",
    displayName: "Đơn xin nghỉ việc",
    documentTitle: "ĐƠN XIN NGHỈ VIỆC",
    numberPrefix: "ĐN",
    signerTitle: "Người làm đơn",
    fields: [
      ...employeeFields,
      { key: "expectedLastWorkingDate", label: "Ngày làm việc cuối dự kiến", type: "date", required: true },
      { key: "handoverTo", label: "Người nhận bàn giao", type: "text" },
      { key: "reason", label: "Lý do nghỉ việc", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`Kính gửi: Ban Giám đốc và Phòng Nhân sự Công ty.`),
      paragraph(
        `Tôi tên là ${values.employeeName}, mã nhân sự ${values.employeeCode}, thuộc ${values.department}, chức danh ${values.position}.`,
      ),
      paragraph(
        `Tôi kính đề nghị Công ty xem xét chấm dứt quan hệ lao động với tôi kể từ ngày ${values.expectedLastWorkingDate}.`,
      ),
      paragraph(`Lý do nghỉ việc: ${values.reason}.`),
      values.handoverTo
        ? paragraph(`Tôi sẽ phối hợp bàn giao công việc, tài sản và hồ sơ liên quan cho ${values.handoverTo}.`)
        : paragraph(`Tôi cam kết hoàn tất bàn giao công việc, tài sản và hồ sơ liên quan theo quy định của Công ty.`),
      paragraph(`Kính mong Ban Giám đốc xem xét và chấp thuận.`),
    ].join(""),
  },
  {
    key: "OVERTIME_APPLICATION",
    category: "Đơn đề nghị",
    displayName: "Đơn đề nghị làm thêm giờ",
    documentTitle: "ĐƠN ĐỀ NGHỊ LÀM THÊM GIỜ",
    numberPrefix: "ĐN",
    signerTitle: "Người đề nghị",
    fields: [
      ...employeeFields,
      { key: "overtimeDate", label: "Ngày làm thêm", type: "date", required: true },
      { key: "startTime", label: "Từ giờ", type: "time", required: true },
      { key: "endTime", label: "Đến giờ", type: "time", required: true },
      { key: "estimatedHours", label: "Số giờ dự kiến", type: "number", required: true },
      { key: "reason", label: "Nội dung/lý do làm thêm", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`Kính gửi: Trưởng bộ phận, Phòng Nhân sự và Ban Giám đốc Công ty.`),
      paragraph(
        `Tôi tên là ${values.employeeName}, mã nhân sự ${values.employeeCode}, thuộc ${values.department}, chức danh ${values.position}.`,
      ),
      paragraph(
        `Tôi đề nghị được làm thêm giờ vào ngày ${values.overtimeDate}, từ ${values.startTime} đến ${values.endTime}, tổng thời lượng dự kiến ${values.estimatedHours} giờ.`,
      ),
      paragraph(`Nội dung/lý do làm thêm: ${values.reason}.`),
      paragraph(`Tôi cam kết ghi nhận thời gian làm thêm trung thực và tuân thủ quy định làm thêm giờ của Công ty.`),
    ].join(""),
  },
  {
    key: "RECRUITMENT_PROPOSAL",
    category: "Tờ trình",
    displayName: "Tờ trình đề nghị tuyển dụng",
    documentTitle: "TỜ TRÌNH ĐỀ NGHỊ TUYỂN DỤNG",
    numberPrefix: "TT",
    signerTitle: "Người lập tờ trình",
    fields: [
      { key: "department", label: "Đơn vị đề nghị", type: "text", required: true },
      { key: "position", label: "Vị trí tuyển dụng", type: "text", required: true },
      { key: "headcount", label: "Số lượng", type: "number", required: true },
      { key: "expectedStartDate", label: "Thời điểm cần nhân sự", type: "date", required: true },
      { key: "budgetRange", label: "Khoảng ngân sách", type: "text" },
      { key: "reason", label: "Lý do tuyển dụng", type: "textarea", required: true, span: "full" },
      { key: "requirements", label: "Yêu cầu chính", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`Kính gửi: Ban Giám đốc Công ty.`),
      paragraph(
        `${values.department} kính trình Ban Giám đốc xem xét nhu cầu tuyển dụng vị trí ${values.position}, số lượng ${values.headcount} nhân sự, thời điểm cần nhân sự từ ngày ${values.expectedStartDate}.`,
      ),
      paragraph(`Lý do tuyển dụng: ${values.reason}.`),
      paragraph(`Yêu cầu chính đối với ứng viên: ${values.requirements}.`),
      values.budgetRange ? paragraph(`Khoảng ngân sách dự kiến: ${values.budgetRange}.`) : "",
      paragraph(`Kính đề nghị Ban Giám đốc xem xét phê duyệt để Phòng Nhân sự triển khai quy trình tuyển dụng.`),
    ].join(""),
  },
  {
    key: "WORKING_MINUTES",
    category: "Biên bản",
    displayName: "Biên bản làm việc",
    documentTitle: "BIÊN BẢN LÀM VIỆC",
    numberPrefix: "BB",
    signerTitle: "Người lập biên bản",
    fields: [
      { key: "meetingDate", label: "Ngày làm việc", type: "date", required: true },
      { key: "meetingTime", label: "Thời gian", type: "time", required: true },
      { key: "location", label: "Địa điểm", type: "text", required: true },
      { key: "participants", label: "Thành phần tham dự", type: "textarea", required: true, span: "full" },
      { key: "content", label: "Nội dung làm việc", type: "textarea", required: true, span: "full" },
      { key: "conclusion", label: "Kết luận/ý kiến thống nhất", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`Hôm nay, vào lúc ${values.meetingTime} ngày ${values.meetingDate}, tại ${values.location}, các bên tiến hành lập biên bản làm việc.`),
      paragraph(`<strong>Thành phần tham dự:</strong><br/>${values.participants}`),
      paragraph(`<strong>Nội dung làm việc:</strong><br/>${values.content}`),
      paragraph(`<strong>Kết luận/ý kiến thống nhất:</strong><br/>${values.conclusion}`),
      paragraph(`Biên bản được lập thành các bản có giá trị như nhau và được các bên thống nhất nội dung trước khi ký.`),
    ].join(""),
  },
  {
    key: "TRANSFER_DECISION",
    category: "Quyết định nhân sự",
    displayName: "Quyết định thuyên chuyển nội bộ",
    documentTitle: "QUYẾT ĐỊNH THUYÊN CHUYỂN NỘI BỘ",
    numberPrefix: "QĐ",
    signerTitle: "Đại diện Công ty",
    fields: [
      ...decisionEmployeeFields,
      { key: "newDepartment", label: "Đơn vị mới", type: "text", required: true },
      { key: "newPosition", label: "Chức danh mới", type: "text", required: true },
      { key: "effectiveDate", label: "Ngày hiệu lực", type: "date", required: true },
      { key: "reason", label: "Căn cứ/lý do", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`<strong>Điều 1.</strong> Thuyên chuyển ông/bà ${values.employeeName}, mã nhân sự ${values.employeeCode}, từ ${values.currentDepartment} - ${values.currentPosition} sang ${values.newDepartment} - ${values.newPosition}.`),
      paragraph(`<strong>Điều 2.</strong> Quyết định này có hiệu lực kể từ ngày ${values.effectiveDate}. Các chế độ liên quan được thực hiện theo quy định hiện hành của Công ty và thỏa thuận lao động có liên quan.`),
      paragraph(`<strong>Điều 3.</strong> Người lao động, các đơn vị liên quan và Phòng Nhân sự chịu trách nhiệm thi hành Quyết định này.`),
      paragraph(`<strong>Căn cứ/lý do:</strong> ${values.reason}.`),
    ].join(""),
  },
  {
    key: "APPOINTMENT_DECISION",
    category: "Quyết định nhân sự",
    displayName: "Quyết định bổ nhiệm",
    documentTitle: "QUYẾT ĐỊNH BỔ NHIỆM",
    numberPrefix: "QĐ",
    signerTitle: "Đại diện Công ty",
    fields: [
      ...decisionEmployeeFields,
      { key: "appointmentTitle", label: "Chức danh bổ nhiệm", type: "text", required: true },
      { key: "effectiveDate", label: "Ngày hiệu lực", type: "date", required: true },
      { key: "responsibilities", label: "Nhiệm vụ/chức trách chính", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`<strong>Điều 1.</strong> Bổ nhiệm ông/bà ${values.employeeName}, mã nhân sự ${values.employeeCode}, giữ chức danh ${values.appointmentTitle} kể từ ngày ${values.effectiveDate}.`),
      paragraph(`<strong>Điều 2.</strong> Ông/bà ${values.employeeName} thực hiện chức trách, nhiệm vụ: ${values.responsibilities}.`),
      paragraph(`<strong>Điều 3.</strong> Phòng Nhân sự, các đơn vị liên quan và ông/bà ${values.employeeName} chịu trách nhiệm thi hành Quyết định này.`),
    ].join(""),
  },
  {
    key: "PROMOTION_DECISION",
    category: "Quyết định nhân sự",
    displayName: "Quyết định thăng chức/chuyển chính thức",
    documentTitle: "QUYẾT ĐỊNH THĂNG CHỨC / CHUYỂN CHÍNH THỨC",
    numberPrefix: "QĐ",
    signerTitle: "Đại diện Công ty",
    fields: [
      ...decisionEmployeeFields,
      { key: "newPosition", label: "Chức danh mới", type: "text", required: true },
      { key: "newJobLevel", label: "Cấp bậc mới", type: "text" },
      { key: "newEmployeeType", label: "Loại nhân sự mới", type: "text" },
      { key: "effectiveDate", label: "Ngày hiệu lực", type: "date", required: true },
      { key: "reason", label: "Căn cứ/lý do", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`<strong>Điều 1.</strong> Điều chỉnh chức danh/loại nhân sự của ông/bà ${values.employeeName}, mã nhân sự ${values.employeeCode}, sang chức danh ${values.newPosition}${values.newJobLevel ? `, cấp bậc ${values.newJobLevel}` : ""}${values.newEmployeeType ? `, loại nhân sự ${values.newEmployeeType}` : ""}.`),
      paragraph(`<strong>Điều 2.</strong> Quyết định có hiệu lực kể từ ngày ${values.effectiveDate}. Các chế độ liên quan được thực hiện theo quy định và thỏa thuận lao động hiện hành.`),
      paragraph(`<strong>Điều 3.</strong> Ông/bà ${values.employeeName}, Phòng Nhân sự và các đơn vị liên quan chịu trách nhiệm thi hành Quyết định này.`),
      paragraph(`<strong>Căn cứ/lý do:</strong> ${values.reason}.`),
    ].join(""),
  },
  {
    key: "DISCIPLINARY_DECISION",
    category: "Quyết định nhân sự",
    displayName: "Quyết định kỷ luật/chấm dứt",
    documentTitle: "QUYẾT ĐỊNH KỶ LUẬT / CHẤM DỨT",
    numberPrefix: "QĐ",
    signerTitle: "Đại diện Công ty",
    fields: [
      ...decisionEmployeeFields,
      { key: "violation", label: "Hành vi/sự việc", type: "textarea", required: true, span: "full" },
      { key: "disciplinaryForm", label: "Hình thức xử lý", type: "text", required: true },
      { key: "effectiveDate", label: "Ngày hiệu lực", type: "date", required: true },
      { key: "legalBasis", label: "Căn cứ xử lý", type: "textarea", required: true, span: "full" },
    ],
    renderBody: (values) => [
      paragraph(`<strong>Điều 1.</strong> Áp dụng hình thức xử lý ${values.disciplinaryForm} đối với ông/bà ${values.employeeName}, mã nhân sự ${values.employeeCode}.`),
      paragraph(`<strong>Điều 2.</strong> Nội dung sự việc/hành vi làm căn cứ xử lý: ${values.violation}.`),
      paragraph(`<strong>Điều 3.</strong> Quyết định có hiệu lực kể từ ngày ${values.effectiveDate}. Phòng Nhân sự, các đơn vị liên quan và ông/bà ${values.employeeName} chịu trách nhiệm thi hành Quyết định này.`),
      paragraph(`<strong>Căn cứ xử lý:</strong> ${values.legalBasis}.`),
    ].join(""),
  },
];
