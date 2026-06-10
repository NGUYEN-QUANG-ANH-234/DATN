import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Check, Download, Eye, FilePenLine, FileText, RefreshCw, Send, X } from "lucide-react";
import { contractApi } from "../api/contractApi";
import type { ContractDocumentPreviewDto, ContractDto, ReviewContractPayload, CreateDraftPayload } from "../api/contractApi";
import { useNotification } from "../../../core/context/NotificationContext";
import {
  dangerButtonClass,
  EmptyState,
  FeatureCard,
  FeaturePage,
  fieldClass,
  primaryButtonClass,
  secondaryButtonClass,
  textareaClass,
} from "../../../core/components/FeatureShell";

const CONTRACT_TYPES = ["Probation", "FixedTerm", "Indefinite", "PartTime"];

const CONTRACT_TYPE_LABELS: Record<string, string> = {
  Probation: "Thử việc",
  FixedTerm: "Có thời hạn",
  Definite: "Có thời hạn",
  Indefinite: "Không thời hạn",
  PartTime: "Bán thời gian",
};

const STATUS_LABELS: Record<string, string> = {
  PendingDept: "Chờ Trưởng phòng",
  PendingHR: "Chờ HR soạn thảo",
  PendingManagerContentReview: "Chờ Trưởng phòng duyệt nội dung",
  PendingEmployee: "Chờ người lao động xác nhận",
  PendingHRRevision: "Chờ HR chỉnh sửa",
  Negotiating: "Đang thương lượng",
  Draft: "Chờ nhân viên xác nhận",
  PendingDirector: "Chờ Giám đốc duyệt",
  ApprovedByDirector: "Đã duyệt, chờ phát hành",
  Active: "Có hiệu lực",
  Rejected: "Bị từ chối",
  Draft_Cancelled: "Hết hạn xác nhận",
};

type TabKey = "pending-dept" | "pending-hr" | "all";

type DraftForm = {
  contractType: string;
  basicSalary: string;
  salaryPercentage: string;
  insuranceSalary: string;
  startDate: string;
  endDate: string;
  employerLegalName: string;
  employerTaxCode: string;
  employerAddress: string;
  employerRepresentativeName: string;
  employerRepresentativeTitle: string;
  employerRepresentativeAuthorization: string;
  signingLocation: string;
  employeeFullNameSnapshot: string;
  employeeBirthDateSnapshot: string;
  employeeGenderSnapshot: string;
  employeeIdentityNumberSnapshot: string;
  employeeIdentityIssueDate: string;
  employeeIdentityIssuePlace: string;
  employeeResidenceAddressSnapshot: string;
  employeeDepartmentSnapshot: string;
  employeePositionSnapshot: string;
  employeeJobLevelSnapshot: string;
  jobTitle: string;
  jobDescription: string;
  workLocation: string;
  workingMode: string;
  workingHours: string;
  restTime: string;
  directManagerSnapshot: string;
  salaryPaymentMethod: string;
  salaryPaymentDate: string;
  allowanceDescription: string;
  additionalBenefits: string;
  salaryReviewPolicy: string;
  bonusPolicy: string;
  kpiBonusTargetAmount: string;
  kpiBonusPolicyCode: string;
  kpiBonusPolicyVersionCode: string;
  kpiScoreFormula: string;
  kpiPayoutFormula: string;
  kpiBonusEligibilityRule: string;
  kpiBonusPaymentPeriod: string;
  kpiBonusApproverRole: string;
  insurancePolicy: string;
  laborProtectionPolicy: string;
  trainingPolicy: string;
  employeeObligations: string;
  employerObligations: string;
  confidentialityClause: string;
  intellectualPropertyClause: string;
  terminationClause: string;
  disputeResolutionClause: string;
  legalDocumentNumber: string;
  documentTemplateCode: string;
  issuedAt: string;
};

const defaultDraft: DraftForm = {
  contractType: "FixedTerm",
  basicSalary: "",
  salaryPercentage: "100",
  insuranceSalary: "",
  startDate: "",
  endDate: "",
  employerLegalName: "",
  employerTaxCode: "",
  employerAddress: "",
  employerRepresentativeName: "",
  employerRepresentativeTitle: "",
  employerRepresentativeAuthorization: "",
  signingLocation: "",
  employeeFullNameSnapshot: "",
  employeeBirthDateSnapshot: "",
  employeeGenderSnapshot: "",
  employeeIdentityNumberSnapshot: "",
  employeeIdentityIssueDate: "",
  employeeIdentityIssuePlace: "",
  employeeResidenceAddressSnapshot: "",
  employeeDepartmentSnapshot: "",
  employeePositionSnapshot: "",
  employeeJobLevelSnapshot: "",
  jobTitle: "",
  jobDescription: "",
  workLocation: "",
  workingMode: "",
  workingHours: "",
  restTime: "",
  directManagerSnapshot: "",
  salaryPaymentMethod: "",
  salaryPaymentDate: "",
  allowanceDescription: "",
  additionalBenefits: "",
  salaryReviewPolicy: "",
  bonusPolicy: "",
  kpiBonusTargetAmount: "",
  kpiBonusPolicyCode: "",
  kpiBonusPolicyVersionCode: "",
  kpiScoreFormula: "",
  kpiPayoutFormula: "",
  kpiBonusEligibilityRule: "",
  kpiBonusPaymentPeriod: "",
  kpiBonusApproverRole: "",
  insurancePolicy: "",
  laborProtectionPolicy: "",
  trainingPolicy: "",
  employeeObligations: "",
  employerObligations: "",
  confidentialityClause: "",
  intellectualPropertyClause: "",
  terminationClause: "",
  disputeResolutionClause: "",
  legalDocumentNumber: "",
  documentTemplateCode: "",
  issuedAt: "",
};

const tabs: { key: TabKey; label: string }[] = [
  { key: "pending-dept", label: "Chờ Trưởng phòng" },
  { key: "pending-hr", label: "Chờ HR soạn thảo" },
  { key: "all", label: "Tất cả hợp đồng" },
];

const fmt = (v: number) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(v || 0);

const dateText = (value?: string | null) =>
  value ? new Date(value).toLocaleDateString("vi-VN") : "Chưa thiết lập";

const statusClass = (status: string) => {
  if (status === "Rejected" || status === "Draft_Cancelled") return "bg-red-50 text-red-700 border-red-200";
  if (status === "Active") return "bg-green-50 text-green-700 border-green-200";
  if (status === "ApprovedByDirector") return "bg-teal-50 text-teal-700 border-teal-200";
  if (status === "Negotiating" || status === "PendingHRRevision" || status === "PendingEmployee") return "bg-amber-50 text-amber-700 border-amber-200";
  if (status === "PendingDirector") return "bg-purple-50 text-purple-700 border-purple-200";
  return "bg-blue-50 text-blue-700 border-blue-200";
};

const dateInput = (value?: string | null) => (value ? value.slice(0, 10) : "");
const optional = (value: string) => {
  const trimmed = value.trim();
  return trimmed ? trimmed : undefined;
};

const saveBlob = (blob: Blob, fileName: string) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

export const HRContractManagement = () => {
  const [tab, setTab] = useState<TabKey>("pending-dept");
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [draftTarget, setDraftTarget] = useState<ContractDto | null>(null);
  const [draft, setDraft] = useState<DraftForm>(defaultDraft);
  const [submittingDraft, setSubmittingDraft] = useState(false);
  const [rejectTarget, setRejectTarget] = useState<ContractDto | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [rejectCtx, setRejectCtx] = useState<"dept" | "hr">("hr");
  const [documentPreview, setDocumentPreview] = useState<ContractDocumentPreviewDto | null>(null);
  const [documentTarget, setDocumentTarget] = useState<ContractDto | null>(null);
  const [documentLoading, setDocumentLoading] = useState(false);

  const { triggerAlert } = useNotification();
  const alertRef = useRef(triggerAlert);

  useEffect(() => {
    alertRef.current = triggerAlert;
  }, [triggerAlert]);

  const fetchContracts = useCallback(async () => {
    setLoading(true);
    try {
      const res =
        tab === "pending-dept"
          ? await contractApi.getPendingRequests()
          : tab === "pending-hr"
            ? await contractApi.getHrPendingRequests()
            : await contractApi.getAllContracts();

      const raw = res as unknown as { data?: ContractDto[]; Data?: ContractDto[] };
      setContracts(raw.data || raw.Data || []);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể tải dữ liệu", e?.message || "Vui lòng thử lại.");
    } finally {
      setLoading(false);
    }
  }, [tab]);

  useEffect(() => {
    fetchContracts();
  }, [fetchContracts]);

  const counts = useMemo(
    () => ({
      pendingDept: contracts.filter(c => c.status === "PendingDept" || c.status === "PendingManagerContentReview").length,
      pendingHR: contracts.filter(c => c.status === "PendingHR" || c.status === "PendingHRRevision" || c.status === "Negotiating").length,
      all: contracts.length,
    }),
    [contracts],
  );

  const buildDraftForm = (source: ContractDto): DraftForm => ({
    ...defaultDraft,
    contractType: source.contractType || defaultDraft.contractType,
    basicSalary: source.basicSalary ? String(source.basicSalary) : "",
    salaryPercentage: source.salaryPercentage ? String(source.salaryPercentage) : "100",
    insuranceSalary: source.insuranceSalary ? String(source.insuranceSalary) : "",
    startDate: dateInput(source.startDate),
    endDate: dateInput(source.endDate),
    employerLegalName: source.employerLegalName || "",
    employerTaxCode: source.employerTaxCode || "",
    employerAddress: source.employerAddress || "",
    employerRepresentativeName: source.employerRepresentativeName || "",
    employerRepresentativeTitle: source.employerRepresentativeTitle || "",
    employerRepresentativeAuthorization: source.employerRepresentativeAuthorization || "",
    signingLocation: source.signingLocation || "",
    employeeFullNameSnapshot: source.employeeFullNameSnapshot || source.employeeName || "",
    employeeBirthDateSnapshot: dateInput(source.employeeBirthDateSnapshot),
    employeeGenderSnapshot: source.employeeGenderSnapshot || "",
    employeeIdentityNumberSnapshot: source.employeeIdentityNumberSnapshot || "",
    employeeIdentityIssueDate: dateInput(source.employeeIdentityIssueDate),
    employeeIdentityIssuePlace: source.employeeIdentityIssuePlace || "",
    employeeResidenceAddressSnapshot: source.employeeResidenceAddressSnapshot || "",
    employeeDepartmentSnapshot: source.employeeDepartmentSnapshot || "",
    employeePositionSnapshot: source.employeePositionSnapshot || "",
    employeeJobLevelSnapshot: source.employeeJobLevelSnapshot || "",
    jobTitle: source.jobTitle || source.employeePositionSnapshot || "",
    jobDescription: source.jobDescription || "",
    workLocation: source.workLocation || "",
    workingMode: source.workingMode || "",
    workingHours: source.workingHours || "",
    restTime: source.restTime || "",
    directManagerSnapshot: source.directManagerSnapshot || "",
    salaryPaymentMethod: source.salaryPaymentMethod || "",
    salaryPaymentDate: source.salaryPaymentDate || "",
    allowanceDescription: source.allowanceDescription || "",
    additionalBenefits: source.additionalBenefits || "",
    salaryReviewPolicy: source.salaryReviewPolicy || "",
    bonusPolicy: source.bonusPolicy || "",
    kpiBonusTargetAmount: source.kpiBonusTargetAmount ? String(source.kpiBonusTargetAmount) : "",
    kpiBonusPolicyCode: source.kpiBonusPolicyCode || "",
    kpiBonusPolicyVersionCode: source.kpiBonusPolicyVersionCode || "",
    kpiScoreFormula: source.kpiScoreFormula || "",
    kpiPayoutFormula: source.kpiPayoutFormula || "",
    kpiBonusEligibilityRule: source.kpiBonusEligibilityRule || "",
    kpiBonusPaymentPeriod: source.kpiBonusPaymentPeriod || "",
    kpiBonusApproverRole: source.kpiBonusApproverRole || "",
    insurancePolicy: source.insurancePolicy || "",
    laborProtectionPolicy: source.laborProtectionPolicy || "",
    trainingPolicy: source.trainingPolicy || "",
    employeeObligations: source.employeeObligations || "",
    employerObligations: source.employerObligations || "",
    confidentialityClause: source.confidentialityClause || "",
    intellectualPropertyClause: source.intellectualPropertyClause || "",
    terminationClause: source.terminationClause || "",
    disputeResolutionClause: source.disputeResolutionClause || "",
    legalDocumentNumber: source.legalDocumentNumber || source.contractNumber || "",
    documentTemplateCode: source.documentTemplateCode || "",
    issuedAt: dateInput(source.issuedAt),
  });

  const openDraft = async (contract: ContractDto) => {
    setDraftTarget(contract);
    setDraft(buildDraftForm(contract));

    try {
      const res = await contractApi.getDraftDefaults(contract.id);
      const raw = res as unknown as { data?: ContractDto; Data?: ContractDto };
      const defaults = raw.data || raw.Data;
      if (defaults) setDraft(buildDraftForm(defaults));
    } catch {
      alertRef.current("warning", "Chưa tải được dữ liệu gợi ý", "Form đang dùng thông tin hiện có của hợp đồng.");
    }
  };

  const validateDraft = () => {
    const basicSalary = Number(draft.basicSalary);
    const salaryPercentage = Number(draft.salaryPercentage || 100);
    const insuranceSalary = Number(draft.insuranceSalary || 0);

    if (!basicSalary || basicSalary <= 0 || !draft.startDate) {
      alertRef.current("warning", "Thiếu thông tin", "Vui lòng nhập lương cơ bản hợp lệ và ngày bắt đầu.");
      return null;
    }

    if (salaryPercentage <= 0 || salaryPercentage > 100) {
      alertRef.current("warning", "Tỷ lệ chưa hợp lệ", "Tỷ lệ thực lĩnh phải nằm trong khoảng 1-100%.");
      return null;
    }

    if (insuranceSalary < 0) {
      alertRef.current("warning", "Lương BHXH chưa hợp lệ", "Lương đóng BHXH không được âm.");
      return null;
    }

    if ((draft.contractType === "Probation" || draft.contractType === "FixedTerm") && !draft.endDate) {
      alertRef.current("warning", "Thiếu ngày kết thúc", "Hợp đồng thử việc/có thời hạn cần có ngày kết thúc.");
      return null;
    }

    if (draft.endDate && new Date(draft.endDate) < new Date(draft.startDate)) {
      alertRef.current("warning", "Thời hạn chưa hợp lệ", "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
      return null;
    }

    return {
      contractType: draft.contractType,
      basicSalary,
      salaryPercentage,
      insuranceSalary,
      startDate: draft.startDate,
      endDate: draft.endDate || undefined,
      employer: {
        legalName: optional(draft.employerLegalName),
        taxCode: optional(draft.employerTaxCode),
        address: optional(draft.employerAddress),
        representativeName: optional(draft.employerRepresentativeName),
        representativeTitle: optional(draft.employerRepresentativeTitle),
        representativeAuthorization: optional(draft.employerRepresentativeAuthorization),
        signingLocation: optional(draft.signingLocation),
      },
      employee: {
        fullName: optional(draft.employeeFullNameSnapshot),
        birthDate: optional(draft.employeeBirthDateSnapshot),
        gender: optional(draft.employeeGenderSnapshot),
        identityNumber: optional(draft.employeeIdentityNumberSnapshot),
        identityIssueDate: optional(draft.employeeIdentityIssueDate),
        identityIssuePlace: optional(draft.employeeIdentityIssuePlace),
        residenceAddress: optional(draft.employeeResidenceAddressSnapshot),
        department: optional(draft.employeeDepartmentSnapshot),
        position: optional(draft.employeePositionSnapshot),
        jobLevel: optional(draft.employeeJobLevelSnapshot),
      },
      work: {
        jobTitle: optional(draft.jobTitle),
        jobDescription: optional(draft.jobDescription),
        workLocation: optional(draft.workLocation),
        workingMode: optional(draft.workingMode),
        workingHours: optional(draft.workingHours),
        restTime: optional(draft.restTime),
        directManager: optional(draft.directManagerSnapshot),
      },
      compensation: {
        salaryPaymentMethod: optional(draft.salaryPaymentMethod),
        salaryPaymentDate: optional(draft.salaryPaymentDate),
        allowanceDescription: optional(draft.allowanceDescription),
        additionalBenefits: optional(draft.additionalBenefits),
        salaryReviewPolicy: optional(draft.salaryReviewPolicy),
        bonusPolicy: optional(draft.bonusPolicy),
        insurancePolicy: optional(draft.insurancePolicy),
        laborProtectionPolicy: optional(draft.laborProtectionPolicy),
        trainingPolicy: optional(draft.trainingPolicy),
      },
      clauses: {
        employeeObligations: optional(draft.employeeObligations),
        employerObligations: optional(draft.employerObligations),
        confidentialityClause: optional(draft.confidentialityClause),
        intellectualPropertyClause: optional(draft.intellectualPropertyClause),
        terminationClause: optional(draft.terminationClause),
        disputeResolutionClause: optional(draft.disputeResolutionClause),
      },
      issuance: {
        legalDocumentNumber: optional(draft.legalDocumentNumber),
        documentTemplateCode: optional(draft.documentTemplateCode),
        issuedAt: optional(draft.issuedAt),
      },
    } satisfies CreateDraftPayload;
  };

  /*
  const openDraftOld = (contract: ContractDto) => {
    setDraftTarget(contract);
    setDraft({
      contractType: contract.contractType || defaultDraft.contractType,
      basicSalary: contract.basicSalary ? String(contract.basicSalary) : "",
      salaryPercentage: contract.salaryPercentage ? String(contract.salaryPercentage) : "100",
      insuranceSalary: contract.insuranceSalary ? String(contract.insuranceSalary) : "",
      startDate: contract.startDate ? contract.startDate.slice(0, 10) : "",
      endDate: contract.endDate ? contract.endDate.slice(0, 10) : "",
    });
  };

  const validateDraftOld = () => {
    const basicSalary = Number(draft.basicSalary);
    const salaryPercentage = Number(draft.salaryPercentage || 100);
    const insuranceSalary = Number(draft.insuranceSalary || 0);

    if (!basicSalary || basicSalary <= 0 || !draft.startDate) {
      alertRef.current("warning", "Thiếu thông tin", "Vui lòng nhập lương cơ bản hợp lệ và ngày bắt đầu.");
      return null;
    }

    if (salaryPercentage <= 0 || salaryPercentage > 100) {
      alertRef.current("warning", "Tỷ lệ chưa hợp lệ", "Tỷ lệ thực lĩnh phải nằm trong khoảng 1-100%.");
      return null;
    }

    if (insuranceSalary < 0) {
      alertRef.current("warning", "Lương BHXH chưa hợp lệ", "Lương đóng BHXH không được âm.");
      return null;
    }

    if ((draft.contractType === "Probation" || draft.contractType === "FixedTerm") && !draft.endDate) {
      alertRef.current("warning", "Thiếu ngày kết thúc", "Hợp đồng thử việc/có thời hạn cần có ngày kết thúc.");
      return null;
    }

    if (draft.endDate && new Date(draft.endDate) < new Date(draft.startDate)) {
      alertRef.current("warning", "Thời hạn chưa hợp lệ", "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.");
      return null;
    }

    return {
      contractType: draft.contractType,
      basicSalary,
      salaryPercentage,
      insuranceSalary,
      startDate: draft.startDate,
      endDate: draft.endDate || undefined,
    } satisfies CreateDraftPayload;
  };
  */

  const handleCreateDraft = async () => {
    if (!draftTarget) return;
    const payload = validateDraft();
    if (!payload) return;

    setSubmittingDraft(true);
    try {
      await contractApi.hrCreateDraft(draftTarget.id, payload);
      alertRef.current(
        "success",
        "Đã lưu bản nháp",
        draftTarget.status === "Negotiating" || draftTarget.status === "PendingHRRevision"
          ? "Bản nháp mới đã được gửi lại cho Trưởng phòng duyệt nội dung."
          : "Bản nháp đã được gửi cho Trưởng phòng duyệt nội dung.",
      );
      setDraftTarget(null);
      setDraft(defaultDraft);
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Lỗi", e?.message || "Không thể lưu bản nháp.");
    } finally {
      setSubmittingDraft(false);
    }
  };

  const handleDeptApprove = (id: number) => {
    alertRef.current(
      "confirm",
      "Xác nhận chuyển HR",
      "Bạn muốn xác nhận đề xuất này và chuyển cho HR soạn thảo hợp đồng?",
      async () => {
        try {
          await contractApi.deptReview(id, { isApproved: true });
          alertRef.current("success", "Đã chuyển HR", "Yêu cầu hợp đồng đã được chuyển sang bộ phận HR.");
          fetchContracts();
        } catch (err: unknown) {
          const e = err as { message?: string };
          alertRef.current("error", "Lỗi", e?.message || "Không thể xác nhận yêu cầu.");
        }
      },
    );
  };

  const openReject = (contract: ContractDto, ctx: "dept" | "hr") => {
    setRejectTarget(contract);
    setRejectCtx(ctx);
    setRejectReason("");
  };

  const handleReject = async () => {
    if (!rejectTarget) return;
    if (!rejectReason.trim()) {
      alertRef.current(
        "warning",
        "Thiếu lý do",
        rejectTarget.status === "PendingManagerContentReview"
          ? "Vui lòng nhập lý do yêu cầu chỉnh sửa."
          : "Vui lòng nhập lý do từ chối.",
      );
      return;
    }

    try {
      const payload: ReviewContractPayload = { isApproved: false, rejectReason: rejectReason.trim() };
      if (rejectCtx === "dept" && rejectTarget.status === "PendingManagerContentReview") {
        await contractApi.requestRevision(rejectTarget.id, { reason: rejectReason.trim() });
      } else if (rejectCtx === "dept") {
        await contractApi.deptReview(rejectTarget.id, payload);
      } else {
        await contractApi.hrReject(rejectTarget.id, payload);
      }
      alertRef.current(
        "success",
        rejectTarget.status === "PendingManagerContentReview" ? "Đã gửi yêu cầu chỉnh sửa" : "Đã từ chối",
        rejectTarget.status === "PendingManagerContentReview"
          ? "Hợp đồng đã được chuyển về HR chỉnh sửa."
          : "Yêu cầu hợp đồng đã được cập nhật.",
      );
      setRejectTarget(null);
      setRejectReason("");
      fetchContracts();
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể xử lý", e?.message || "Vui lòng thử lại.");
    }
  };

  const openDocumentPreview = async (contract: ContractDto) => {
    setDocumentTarget(contract);
    setDocumentLoading(true);
    try {
      const res = await contractApi.previewDocument(contract.id);
      const raw = res as unknown as { data?: ContractDocumentPreviewDto; Data?: ContractDocumentPreviewDto };
      setDocumentPreview(raw.data || raw.Data || null);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể xem trước", e?.message || "Hợp đồng chưa có đủ dữ liệu văn bản.");
      setDocumentTarget(null);
    } finally {
      setDocumentLoading(false);
    }
  };

  const downloadDocument = async (contract: ContractDto, type: "doc" | "pdf") => {
    try {
      const blob = type === "doc"
        ? await contractApi.downloadDocumentDoc(contract.id)
        : await contractApi.downloadDocumentPdf(contract.id);
      saveBlob(blob, `${contract.legalDocumentNumber || contract.contractNumber || `contract-${contract.id}`}.${type}`);
    } catch (err: unknown) {
      const e = err as { message?: string };
      alertRef.current("error", "Không thể tải văn bản", e?.message || "Vui lòng thử lại.");
    }
  };

  const issueDocument = async (contract: ContractDto) => {
    alertRef.current("confirm", "Phát hành hợp đồng", "Văn bản hợp đồng sẽ được đánh dấu đã phát hành và sẵn sàng tải DOC.", async () => {
      try {
        const res = await contractApi.issueDocument(contract.id, {
          legalDocumentNumber: contract.legalDocumentNumber || contract.contractNumber,
          documentTemplateCode: contract.documentTemplateCode || undefined,
          issuedAt: new Date().toISOString(),
          employerSignedAt: new Date().toISOString(),
        });
        const raw = res as unknown as { data?: ContractDocumentPreviewDto; Data?: ContractDocumentPreviewDto };
        setDocumentTarget(contract);
        setDocumentPreview(raw.data || raw.Data || null);
        alertRef.current("success", "Đã phát hành", "Văn bản hợp đồng đã sẵn sàng để tải.");
        fetchContracts();
      } catch (err: unknown) {
        const e = err as { message?: string };
        alertRef.current("error", "Không thể phát hành", e?.message || "Vui lòng thử lại.");
      }
    });
  };

  const updateDraft = (field: keyof DraftForm, value: string) => {
    setDraft(prev => ({ ...prev, [field]: value }));
  };

  const textField = (field: keyof DraftForm, label: string, type: "text" | "number" | "date" = "text", span = false) => (
    <label className={span ? "sm:col-span-2" : undefined}>
      <span className="mb-1 block text-sm font-medium text-gray-700">{label}</span>
      <input
        className={fieldClass}
        type={type}
        value={draft[field]}
        onChange={e => updateDraft(field, e.target.value)}
      />
    </label>
  );

  const textAreaField = (field: keyof DraftForm, label: string) => (
    <label className="sm:col-span-2">
      <span className="mb-1 block text-sm font-medium text-gray-700">{label}</span>
      <textarea
        className={`${textareaClass} min-h-[92px]`}
        value={draft[field]}
        onChange={e => updateDraft(field, e.target.value)}
      />
    </label>
  );

  const sectionTitle = (title: string) => (
    <h3 className="sm:col-span-2 border-t border-gray-100 pt-4 text-sm font-semibold text-gray-900">
      {title}
    </h3>
  );

  return (
    <FeaturePage
      title="Quản lý hợp đồng"
      description="Xử lý đề xuất từ phòng ban, soạn thảo bản nháp và theo dõi trạng thái phê duyệt hợp đồng."
      actions={
        <button className={secondaryButtonClass} onClick={fetchContracts} disabled={loading}>
          <RefreshCw size={16} />
          Làm mới
        </button>
      }
    >
      <FeatureCard>
        <div className="flex flex-wrap gap-2">
          {tabs.map(item => {
            const active = tab === item.key;
            const badge =
              item.key === "pending-dept"
                ? counts.pendingDept
                : item.key === "pending-hr"
                  ? counts.pendingHR
                  : counts.all;

            return (
              <button
                key={item.key}
                onClick={() => setTab(item.key)}
                className={`inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-semibold transition ${
                  active
                    ? "border-blue-600 bg-blue-600 text-white"
                    : "border-gray-200 bg-white text-gray-700 hover:bg-gray-50"
                }`}
              >
                {item.label}
                <span className={`rounded-full px-2 py-0.5 text-xs ${active ? "bg-white/20" : "bg-gray-100 text-gray-600"}`}>
                  {badge}
                </span>
              </button>
            );
          })}
        </div>
      </FeatureCard>

      {loading ? (
        <FeatureCard>
          <div className="py-10 text-center text-sm text-gray-500">Đang tải dữ liệu...</div>
        </FeatureCard>
      ) : contracts.length === 0 ? (
        <FeatureCard>
          <EmptyState title="Chưa có hợp đồng cần xử lý" description="Các mục mới sẽ xuất hiện tại đây khi có yêu cầu phù hợp." />
        </FeatureCard>
      ) : (
        <div className="space-y-3">
          {contracts.map(contract => (
            <FeatureCard key={contract.id} className="p-4">
              <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className={`rounded-md border px-2.5 py-1 text-xs font-semibold ${statusClass(contract.status)}`}>
                      {STATUS_LABELS[contract.status] ?? contract.status}
                    </span>
                    <span className="text-xs font-medium text-gray-500">Phiên bản v{contract.version || 0}</span>
                  </div>
                  <h2 className="mt-2 text-base font-semibold text-gray-900">
                    {contract.contractNumber || `Hợp đồng #${contract.id}`}
                  </h2>
                  <p className="mt-1 text-sm text-gray-500">
                    {contract.employeeName || "Chưa có tên nhân viên"} · {(CONTRACT_TYPE_LABELS[contract.contractType] ?? contract.contractType) || "Chưa chọn loại"}
                  </p>
                </div>

                <div className="flex flex-wrap gap-2">
                  {tab === "pending-dept" && (
                    <>
                      <button className={dangerButtonClass} onClick={() => openReject(contract, "dept")}>
                        <X size={16} />
                        {contract.status === "PendingManagerContentReview" ? "Yêu cầu chỉnh sửa" : "Từ chối"}
                      </button>
                      <button className={primaryButtonClass} onClick={() => handleDeptApprove(contract.id)}>
                        <Check size={16} />
                        Chuyển HR
                      </button>
                    </>
                  )}

                  {tab === "pending-hr" && (
                    <>
                      {contract.status === "PendingHR" && (
                        <button className={dangerButtonClass} onClick={() => openReject(contract, "hr")}>
                          <X size={16} />
                          Từ chối
                        </button>
                      )}
                      <button className={primaryButtonClass} onClick={() => openDraft(contract)}>
                        <FilePenLine size={16} />
                        {contract.status === "Negotiating" || contract.status === "PendingHRRevision" ? "Cập nhật bản nháp" : "Lập bản nháp"}
                      </button>
                    </>
                  )}
                  {contract.status !== "PendingDept" && (
                    <>
                      <button className={secondaryButtonClass} onClick={() => openDocumentPreview(contract)}>
                        <Eye size={16} />
                        Xem trước
                      </button>
                      <button className={secondaryButtonClass} onClick={() => downloadDocument(contract, "doc")}>
                        <Download size={16} />
                        Tải DOC
                      </button>
                      {contract.documentPdfFilePath && (
                        <button className={secondaryButtonClass} onClick={() => downloadDocument(contract, "pdf")}>
                          <Download size={16} />
                          PDF
                        </button>
                      )}
                      {!contract.issuedAt && (contract.status === "ApprovedByDirector" || contract.status === "Active") && (
                        <button className={primaryButtonClass} onClick={() => issueDocument(contract)}>
                          <Send size={16} />
                          Phát hành
                        </button>
                      )}
                    </>
                  )}
                </div>
              </div>

              <div className="mt-4 grid gap-3 border-t border-gray-100 pt-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
                <div>
                  <p className="text-xs font-medium text-gray-500">Lương cơ bản</p>
                  <p className="mt-1 font-semibold text-gray-900">{fmt(contract.basicSalary)}</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500">Tỷ lệ thực lĩnh</p>
                  <p className="mt-1 font-semibold text-gray-900">{contract.salaryPercentage || 0}%</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500">BHXH</p>
                  <p className="mt-1 font-semibold text-gray-900">{fmt(contract.insuranceSalary)}</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500">Hiệu lực</p>
                  <p className="mt-1 font-semibold text-gray-900">
                    {dateText(contract.startDate)} - {contract.endDate ? dateText(contract.endDate) : "Không thời hạn"}
                  </p>
                </div>
              </div>

              {contract.negotiationNote && (
                <div className="mt-4 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
                  <span className="font-semibold">Ghi chú: </span>
                  {contract.negotiationNote}
                </div>
              )}
            </FeatureCard>
          ))}
        </div>
      )}

      {draftTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="max-h-[92vh] w-full max-w-5xl overflow-y-auto rounded-lg bg-white p-6 shadow-2xl">
            <div className="mb-5 flex items-start justify-between gap-4 border-b border-gray-100 pb-4">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  {draftTarget.status === "Negotiating" || draftTarget.status === "PendingHRRevision"
                    ? "Cập nhật bản nháp hợp đồng"
                    : "Lập bản nháp hợp đồng"}
                </h2>
                <p className="mt-1 text-sm text-gray-500">
                  SLA phản hồi của nhân viên sẽ bắt đầu sau khi bản nháp được gửi.
                </p>
              </div>
              <FileText className="text-blue-600" size={22} />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <label className="sm:col-span-2">
                <span className="mb-1 block text-sm font-medium text-gray-700">Loại hợp đồng</span>
                <select
                  className={fieldClass}
                  value={draft.contractType}
                  onChange={e => setDraft(prev => ({ ...prev, contractType: e.target.value }))}
                >
                  {CONTRACT_TYPES.map(type => (
                    <option key={type} value={type}>
                      {CONTRACT_TYPE_LABELS[type] ?? type}
                    </option>
                  ))}
                </select>
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương cơ bản</span>
                <input
                  className={fieldClass}
                  type="number"
                  min={0}
                  value={draft.basicSalary}
                  onChange={e => setDraft(prev => ({ ...prev, basicSalary: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Tỷ lệ thực lĩnh (%)</span>
                <input
                  className={fieldClass}
                  type="number"
                  min={1}
                  max={100}
                  value={draft.salaryPercentage}
                  onChange={e => setDraft(prev => ({ ...prev, salaryPercentage: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Lương đóng BHXH</span>
                <input
                  className={fieldClass}
                  type="number"
                  min={0}
                  value={draft.insuranceSalary}
                  onChange={e => setDraft(prev => ({ ...prev, insuranceSalary: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Ngày bắt đầu</span>
                <input
                  className={fieldClass}
                  type="date"
                  value={draft.startDate}
                  onChange={e => setDraft(prev => ({ ...prev, startDate: e.target.value }))}
                />
              </label>

              <label>
                <span className="mb-1 block text-sm font-medium text-gray-700">Ngày kết thúc</span>
                <input
                  className={fieldClass}
                  type="date"
                  value={draft.endDate}
                  onChange={e => setDraft(prev => ({ ...prev, endDate: e.target.value }))}
                />
              </label>

              {sectionTitle("Thông tin công ty")}
              {textField("employerLegalName", "Tên pháp lý công ty", "text", true)}
              {textField("employerTaxCode", "Mã số thuế")}
              {textField("signingLocation", "Địa điểm ký")}
              {textField("employerAddress", "Địa chỉ công ty", "text", true)}
              {textField("employerRepresentativeName", "Người đại diện ký")}
              {textField("employerRepresentativeTitle", "Chức vụ người đại diện")}
              {textField("employerRepresentativeAuthorization", "Căn cứ ủy quyền", "text", true)}

              {sectionTitle("Người lao động")}
              {textField("employeeFullNameSnapshot", "Họ và tên")}
              {textField("employeeBirthDateSnapshot", "Ngày sinh", "date")}
              {textField("employeeGenderSnapshot", "Giới tính")}
              {textField("employeeIdentityNumberSnapshot", "CCCD/CMND")}
              {textField("employeeIdentityIssueDate", "Ngày cấp", "date")}
              {textField("employeeIdentityIssuePlace", "Nơi cấp")}
              {textField("employeeResidenceAddressSnapshot", "Địa chỉ cư trú", "text", true)}
              {textField("employeeDepartmentSnapshot", "Phòng ban")}
              {textField("employeePositionSnapshot", "Chức danh")}
              {textField("employeeJobLevelSnapshot", "Cấp bậc")}

              {sectionTitle("Công việc")}
              {textField("jobTitle", "Chức danh công việc")}
              {textField("directManagerSnapshot", "Quản lý trực tiếp")}
              {textField("workLocation", "Địa điểm làm việc", "text", true)}
              {textField("workingMode", "Hình thức làm việc")}
              {textField("workingHours", "Thời giờ làm việc")}
              {textField("restTime", "Thời giờ nghỉ ngơi", "text", true)}
              {textAreaField("jobDescription", "Mô tả công việc")}

              {sectionTitle("Lương và phúc lợi")}
              {textField("salaryPaymentMethod", "Hình thức trả lương")}
              {textField("salaryPaymentDate", "Ngày trả lương")}
              {textAreaField("allowanceDescription", "Phụ cấp")}
              {textAreaField("additionalBenefits", "Phúc lợi bổ sung")}
              {textAreaField("salaryReviewPolicy", "Chính sách nâng lương")}
              {textAreaField("bonusPolicy", "Thưởng/KPI")}
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 md:col-span-2">
                <p className="text-sm font-semibold text-amber-900">Quy chế thưởng KPI</p>
                <div className="mt-3 grid gap-3 text-sm text-amber-950 md:grid-cols-2">
                  <div>
                    <span className="block text-xs font-medium uppercase tracking-wide text-amber-700">Mức thưởng tối đa</span>
                    <strong>{draft.kpiBonusTargetAmount ? fmt(Number(draft.kpiBonusTargetAmount)) : "Theo cấu hình lương của nhân viên"}</strong>
                  </div>
                  <div>
                    <span className="block text-xs font-medium uppercase tracking-wide text-amber-700">Policy</span>
                    <strong>{draft.kpiBonusPolicyCode || "HICAS_KPI_BONUS_2026"}</strong>
                    {draft.kpiBonusPolicyVersionCode && <span className="ml-2 text-xs text-amber-700">{draft.kpiBonusPolicyVersionCode}</span>}
                  </div>
                  <p className="md:col-span-2"><strong>Cách tính điểm:</strong> {draft.kpiScoreFormula || "Điểm KPI chính thức do trưởng phòng chốt."}</p>
                  <p className="md:col-span-2"><strong>Quy đổi thành tiền:</strong> {draft.kpiPayoutFormula || "Thưởng KPI thực nhận = mức thưởng KPI tối đa * điểm KPI / 100."}</p>
                  <p className="md:col-span-2"><strong>Điều kiện/Kỳ chi trả:</strong> {draft.kpiBonusEligibilityRule || "Theo quy chế lương thưởng hiện hành."} {draft.kpiBonusPaymentPeriod || ""}</p>
                </div>
              </div>
              {textAreaField("insurancePolicy", "BHXH/BHYT/BHTN")}
              {textAreaField("laborProtectionPolicy", "Bảo hộ lao động")}
              {textAreaField("trainingPolicy", "Đào tạo/bồi dưỡng")}

              {sectionTitle("Điều khoản")}
              {textAreaField("employeeObligations", "Quyền và nghĩa vụ người lao động")}
              {textAreaField("employerObligations", "Quyền và nghĩa vụ công ty")}
              {textAreaField("confidentialityClause", "Bảo mật thông tin")}
              {textAreaField("intellectualPropertyClause", "Sở hữu trí tuệ")}
              {textAreaField("terminationClause", "Chấm dứt hợp đồng")}
              {textAreaField("disputeResolutionClause", "Giải quyết tranh chấp")}

              {sectionTitle("Phát hành văn bản")}
              {textField("legalDocumentNumber", "Số hợp đồng")}
              {textField("documentTemplateCode", "Mẫu biểu")}
              {textField("issuedAt", "Ngày phát hành", "date")}
            </div>

            <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button
                className={secondaryButtonClass}
                onClick={() => {
                  setDraftTarget(null);
                  setDraft(defaultDraft);
                }}
              >
                Hủy
              </button>
              <button className={primaryButtonClass} onClick={handleCreateDraft} disabled={submittingDraft}>
                <Check size={16} />
                {submittingDraft ? "Đang lưu..." : "Lưu và gửi"}
              </button>
            </div>
          </div>
        </div>
      )}

      {documentTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="flex max-h-[92vh] w-full max-w-5xl flex-col rounded-lg bg-white shadow-2xl">
            <div className="flex items-start justify-between gap-4 border-b border-gray-100 p-5">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">Xem trước văn bản hợp đồng</h2>
                <p className="mt-1 text-sm text-gray-500">
                  {documentPreview?.documentNumber || documentTarget.contractNumber} · {documentPreview?.templateCode || documentTarget.documentTemplateCode || "Mẫu hợp đồng"}
                </p>
              </div>
              <button className={secondaryButtonClass} onClick={() => { setDocumentTarget(null); setDocumentPreview(null); }}>
                Đóng
              </button>
            </div>
            <div className="flex-1 overflow-y-auto bg-gray-100 p-4">
              {documentLoading ? (
                <div className="rounded-lg bg-white p-8 text-center text-sm text-gray-500">Đang tải bản xem trước...</div>
              ) : documentPreview?.html ? (
                <div className="rounded-lg bg-white shadow-sm" dangerouslySetInnerHTML={{ __html: documentPreview.html }} />
              ) : (
                <div className="rounded-lg bg-white p-8 text-center text-sm text-gray-500">Chưa có nội dung xem trước.</div>
              )}
            </div>
            <div className="flex flex-col-reverse gap-2 border-t border-gray-100 p-4 sm:flex-row sm:justify-end">
              <button className={secondaryButtonClass} onClick={() => downloadDocument(documentTarget, "doc")}>
                <Download size={16} />
                Tải DOC
              </button>
              {documentPreview?.canDownloadPdf && (
                <button className={secondaryButtonClass} onClick={() => downloadDocument(documentTarget, "pdf")}>
                  <Download size={16} />
                  Tải PDF
                </button>
              )}
              {!documentTarget.issuedAt && (documentTarget.status === "ApprovedByDirector" || documentTarget.status === "Active") && (
                <button className={primaryButtonClass} onClick={() => issueDocument(documentTarget)}>
                  <Send size={16} />
                  Phát hành
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {rejectTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-lg bg-white p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-gray-900">
              {rejectTarget.status === "PendingManagerContentReview" ? "Yêu cầu chỉnh sửa hợp đồng" : "Từ chối yêu cầu hợp đồng"}
            </h2>
            <p className="mt-1 text-sm text-gray-500">
              {rejectTarget.status === "PendingManagerContentReview"
                ? "Nội dung này sẽ được chuyển về HR để cập nhật bản nháp."
                : "Lý do sẽ được lưu vào ghi chú hợp đồng để các bên liên quan theo dõi."}
            </p>

            <label className="mt-4 block">
              <span className="mb-1 block text-sm font-medium text-gray-700">
                {rejectTarget.status === "PendingManagerContentReview" ? "Lý do yêu cầu chỉnh sửa" : "Lý do từ chối"}
              </span>
              <textarea
                className={textareaClass}
                value={rejectReason}
                onChange={e => setRejectReason(e.target.value)}
                placeholder={rejectTarget.status === "PendingManagerContentReview" ? "Nhập nội dung cần HR chỉnh sửa..." : "Nhập lý do từ chối..."}
              />
            </label>

            <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <button className={secondaryButtonClass} onClick={() => setRejectTarget(null)}>
                Hủy
              </button>
              <button className={dangerButtonClass} onClick={handleReject}>
                <X size={16} />
                {rejectTarget.status === "PendingManagerContentReview" ? "Gửi yêu cầu chỉnh sửa" : "Xác nhận từ chối"}
              </button>
            </div>
          </div>
        </div>
      )}
    </FeaturePage>
  );
};
