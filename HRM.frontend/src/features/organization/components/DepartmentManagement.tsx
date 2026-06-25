import { useMemo, useState, type FormEvent } from "react";
import {
  Building2,
  CheckCircle2,
  GitBranch,
  Pencil,
  Plus,
  Power,
  RefreshCcw,
  Save,
  Trash2,
  UsersRound,
  X,
} from "lucide-react";
import { PageHeader } from "../../../components/layout";
import {
  Badge,
  Button,
  Card,
  ConfirmDialog,
  EmptyState,
  LoadingState,
  Tabs,
} from "../../../components/ui";
import { useNotification } from "../../../core/context/NotificationContext";
import { useDepartments } from "../hooks/useDepartments";
import type { DepartmentTree, UpdateDepartmentPayload } from "../types/department";

type DepartmentOption = {
  id: number;
  name: string;
};

type DepartmentForm = {
  deptCode: string;
  deptName: string;
  parentDeptId: string;
};

type DepartmentEditForm = {
  deptName: string;
  parentDeptId: string;
};

const emptyForm: DepartmentForm = {
  deptCode: "",
  deptName: "",
  parentDeptId: "",
};

const toEditForm = (node: DepartmentTree): DepartmentEditForm => ({
  deptName: node.deptName,
  parentDeptId: node.parentDeptId ? String(node.parentDeptId) : "",
});

const flattenDepartments = (
  nodes: DepartmentTree[],
  level = 0,
): Array<DepartmentTree & { level: number }> =>
  nodes.flatMap((node) => [
    { ...node, level },
    ...flattenDepartments(node.children || [], level + 1),
  ]);

const filterDepartmentTree = (
  nodes: DepartmentTree[],
  predicate: (node: DepartmentTree) => boolean,
): DepartmentTree[] =>
  nodes
    .filter(predicate)
    .map((node) => ({
      ...node,
      children: filterDepartmentTree(node.children || [], predicate),
    }));

const normalizeStatus = (status?: string | null) => status?.toLowerCase() ?? "";

const isActiveDepartment = (status?: string | null) => {
  const value = normalizeStatus(status);
  return !value || value === "active" || value === "đang hoạt động";
};

const getStatusLabel = (status?: string | null) =>
  isActiveDepartment(status) ? "Đang hoạt động" : "Tạm ngừng";

const toParentOptions = (departments: Array<DepartmentTree & { level: number }>): DepartmentOption[] =>
  departments.map((department) => ({
    id: department.id,
    name: `${"— ".repeat(department.level)}${department.deptName}`,
  }));

const DepartmentTreeRow = ({
  node,
  flatList,
  onUpdateDepartment,
  onDeactivate,
}: {
  node: DepartmentTree;
  flatList: DepartmentOption[];
  onUpdateDepartment: (id: number, data: UpdateDepartmentPayload) => Promise<boolean>;
  onDeactivate: (id: number, name: string) => void;
}) => {
  const active = isActiveDepartment(node.status);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState<DepartmentEditForm>(() => toEditForm(node));
  const [isSavingEdit, setIsSavingEdit] = useState(false);

  const startEditing = () => {
    setEditForm(toEditForm(node));
    setIsEditing(true);
  };

  const cancelEditing = () => {
    setEditForm(toEditForm(node));
    setIsEditing(false);
  };

  const handleParentChange = (value: string) => {
    setEditForm((current) => ({ ...current, parentDeptId: value }));
  };

  const onSubmitEdit = async (event: FormEvent) => {
    event.preventDefault();
    if (!editForm.deptName.trim()) return;

    setIsSavingEdit(true);
    const success = await onUpdateDepartment(node.id, {
      deptName: editForm.deptName.trim(),
      parentDeptId: editForm.parentDeptId ? Number(editForm.parentDeptId) : null,
    });
    setIsSavingEdit(false);

    if (success) {
      setIsEditing(false);
    }
  };

  return (
    <div className="relative pl-4 sm:pl-6">
      <div className="absolute bottom-0 left-1 top-0 w-px bg-[var(--hicas-border-soft)]" />
      <div className="absolute left-1 top-7 h-px w-4 bg-[var(--hicas-border-soft)] sm:w-5" />

      <div className="rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4 shadow-sm transition hover:border-[var(--hicas-orange)] hover:shadow-[var(--shadow-card)]">
        <form onSubmit={onSubmitEdit} className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex min-w-0 items-start gap-3">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
              <Building2 size={20} />
            </div>
            <div className="min-w-0">
              {isEditing ? (
                <label className="block min-w-[240px] text-sm font-medium text-[var(--hicas-text-main)]">
                  Tên phòng ban
                  <input
                    required
                    value={editForm.deptName}
                    onChange={(event) =>
                      setEditForm((current) => ({ ...current, deptName: event.target.value }))
                    }
                    className="hicas-input mt-1 h-10 text-sm"
                    placeholder="Nhập tên phòng ban"
                    disabled={isSavingEdit}
                  />
                </label>
              ) : (
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="truncate text-base font-semibold text-[var(--hicas-text-main)]">
                    {node.deptName}
                  </h3>
                  <Badge variant={active ? "success" : "neutral"}>{getStatusLabel(node.status)}</Badge>
                </div>
              )}
              <div className="mt-2 flex flex-wrap items-center gap-2 text-xs text-[var(--hicas-text-secondary)]">
                <span className="rounded-full bg-[var(--hicas-bg-soft)] px-2 py-1 font-medium">
                  {node.deptCode}
                </span>
                <span>{node.children?.length || 0} phòng ban trực thuộc</span>
                {node.managerId ? <span>Quản lý #{node.managerId}</span> : <span>Chưa gán quản lý</span>}
              </div>
            </div>
          </div>

          <div className="grid gap-3 sm:grid-cols-[minmax(220px,1fr)_auto] lg:min-w-[420px]">
            <label className="block">
              <span className="mb-1 block text-xs font-semibold text-[var(--hicas-text-secondary)]">
                Trực thuộc
              </span>
              <select
                className="hicas-select h-10 text-sm"
                value={isEditing ? editForm.parentDeptId : node.parentDeptId || ""}
                onChange={(event) => handleParentChange(event.target.value)}
                disabled={!isEditing || isSavingEdit}
              >
                <option value="">Cấp cao nhất</option>
                {flatList.map((department) => (
                  <option
                    key={department.id}
                    value={department.id}
                    disabled={department.id === node.id}
                  >
                    {department.name}
                  </option>
                ))}
              </select>
            </label>

            <div className="flex flex-wrap items-end gap-2">
              {isEditing ? (
                <>
                  <Button
                    type="submit"
                    size="sm"
                    iconLeft={<Save size={15} />}
                    isLoading={isSavingEdit}
                    disabled={!active || !editForm.deptName.trim()}
                  >
                    Lưu
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="secondary"
                    iconLeft={<X size={15} />}
                    onClick={cancelEditing}
                    disabled={isSavingEdit}
                  >
                    Hủy
                  </Button>
                </>
              ) : (
                <>
                  <Button
                    type="button"
                    size="sm"
                    variant="secondary"
                    iconLeft={<Pencil size={15} />}
                    onClick={startEditing}
                    disabled={!active}
                  >
                    Sửa
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="danger"
                    iconLeft={<Power size={15} />}
                    onClick={() => onDeactivate(node.id, node.deptName)}
                    disabled={!active}
                  >
                    Tạm ngừng
                  </Button>
                </>
              )}
            </div>
          </div>
        </form>
      </div>

      {node.children?.length ? (
        <div className="mt-3 space-y-3">
          {node.children.map((child) => (
            <DepartmentTreeRow
              key={child.id}
              node={child}
              flatList={flatList}
              onUpdateDepartment={onUpdateDepartment}
              onDeactivate={onDeactivate}
            />
          ))}
        </div>
      ) : null}
    </div>
  );
};

export const DepartmentManagement = () => {
  const {
    treeData,
    loading,
    handleUpdateDepartment,
    handleDeactivate,
    handleActivate,
    handleDelete,
    handleCreate,
  } = useDepartments();
  const { triggerAlert } = useNotification();

  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState<DepartmentForm>(emptyForm);
  const [pendingDeactivate, setPendingDeactivate] = useState<{
    id: number;
    name: string;
  } | null>(null);
  const [pendingDelete, setPendingDelete] = useState<{
    id: number;
    name: string;
  } | null>(null);
  const [departmentTab, setDepartmentTab] = useState<"active" | "inactive">("active");
  const [saving, setSaving] = useState(false);
  const [deactivating, setDeactivating] = useState(false);
  const [activatingId, setActivatingId] = useState<number | null>(null);
  const [deleting, setDeleting] = useState(false);

  const flatDepartments = useMemo(() => flattenDepartments(treeData), [treeData]);
  const parentOptions = useMemo(() => toParentOptions(flatDepartments), [flatDepartments]);
  const activeTreeData = useMemo(
    () => filterDepartmentTree(treeData, (department) => isActiveDepartment(department.status)),
    [treeData],
  );
  const inactiveDepartments = useMemo(
    () => flatDepartments.filter((department) => !isActiveDepartment(department.status)),
    [flatDepartments],
  );

  const activeCount = flatDepartments.filter((department) =>
    isActiveDepartment(department.status),
  ).length;
  const rootCount = flatDepartments.filter((department) => !department.parentDeptId).length;

  const onSubmitCreate = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);

    const payload = {
      deptCode: formData.deptCode.trim().toUpperCase(),
      deptName: formData.deptName.trim(),
      parentDeptId: formData.parentDeptId ? Number(formData.parentDeptId) : null,
    };

    const success = await handleCreate(payload);
    setSaving(false);

    if (success) {
      setShowForm(false);
      setFormData(emptyForm);
      triggerAlert("success", "Đã tạo phòng ban", `${payload.deptName} đã sẵn sàng sử dụng.`);
      return;
    }

    triggerAlert(
      "error",
      "Không thể tạo phòng ban",
      "Vui lòng kiểm tra mã phòng ban hoặc dữ liệu vừa nhập.",
    );
  };

  const onUpdateDepartment = async (
    id: number,
    data: UpdateDepartmentPayload,
  ): Promise<boolean> => {
    const success = await handleUpdateDepartment(id, data);

    if (success) {
      triggerAlert("success", "Đã lưu thay đổi", `${data.deptName} đã được cập nhật.`);
      return true;
    }

    triggerAlert(
      "error",
      "Không thể lưu phòng ban",
      "Vui lòng kiểm tra tên phòng ban hoặc quan hệ trực thuộc vừa chọn.",
    );
    return false;
  };

  const onConfirmDeactivate = async () => {
    if (!pendingDeactivate) return;

    setDeactivating(true);
    const success = await handleDeactivate(pendingDeactivate.id);
    setDeactivating(false);

    if (success) {
      triggerAlert("success", "Đã tạm ngừng", `${pendingDeactivate.name} đã được cập nhật trạng thái.`);
      setPendingDeactivate(null);
      return;
    }

    triggerAlert(
      "warning",
      "Chưa thể tạm ngừng",
      "Hãy chuyển nhân sự ra khỏi phòng ban này trước khi tiếp tục.",
    );
  };

  const onConfirmDelete = async () => {
    if (!pendingDelete) return;

    setDeleting(true);
    const success = await handleDelete(pendingDelete.id);
    setDeleting(false);

    if (success) {
      triggerAlert("success", "Đã xóa phòng ban", `${pendingDelete.name} đã được xóa khỏi danh sách.`);
      setPendingDelete(null);
      return;
    }

    triggerAlert(
      "warning",
      "Chưa thể xóa phòng ban",
      "Phòng ban còn dữ liệu liên quan, hãy giữ ở trạng thái tạm ngừng.",
    );
  };

  const onActivateDepartment = async (id: number, name: string) => {
    setActivatingId(id);
    const success = await handleActivate(id);
    setActivatingId(null);

    if (success) {
      triggerAlert("success", "Đã bật lại", `${name} đã sẵn sàng sử dụng.`);
      return;
    }

    triggerAlert(
      "warning",
      "Chưa thể bật lại",
      "Hãy kiểm tra phòng ban cha trước khi bật lại phòng ban này.",
    );
  };

  if (loading) {
    return (
      <div className="space-y-5">
        <PageHeader
          title="Phòng ban"
          description="Quản lý sơ đồ phòng ban và quan hệ trực thuộc trong công ty."
          breadcrumb={[{ label: "Cấu hình hệ thống" }, { label: "Phòng ban" }]}
        />
        <LoadingState description="Đang tải sơ đồ phòng ban..." />
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title="Phòng ban"
        description="Quản lý sơ đồ phòng ban và quan hệ trực thuộc trong công ty."
        breadcrumb={[{ label: "Cấu hình hệ thống" }, { label: "Phòng ban" }]}
        actions={
          <Button
            iconLeft={showForm ? <RefreshCcw size={16} /> : <Plus size={16} />}
            variant={showForm ? "secondary" : "primary"}
            onClick={() => setShowForm((value) => !value)}
          >
            {showForm ? "Ẩn biểu mẫu" : "Thêm phòng ban"}
          </Button>
        }
      />

      <div className="grid gap-4 md:grid-cols-3">
        <Card className="bg-white" padded>
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-orange-soft)] text-[var(--hicas-orange)]">
              <Building2 size={20} />
            </div>
            <div>
              <p className="text-sm text-[var(--hicas-text-secondary)]">Tổng phòng ban</p>
              <p className="text-2xl font-bold text-[var(--hicas-text-main)]">{flatDepartments.length}</p>
            </div>
          </div>
        </Card>
        <Card className="bg-white" padded>
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-success-soft)] text-[var(--hicas-success)]">
              <CheckCircle2 size={20} />
            </div>
            <div>
              <p className="text-sm text-[var(--hicas-text-secondary)]">Đang hoạt động</p>
              <p className="text-2xl font-bold text-[var(--hicas-text-main)]">{activeCount}</p>
            </div>
          </div>
        </Card>
        <Card className="bg-white" padded>
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-[var(--radius-md)] bg-[var(--hicas-info-soft)] text-[var(--hicas-info)]">
              <GitBranch size={20} />
            </div>
            <div>
              <p className="text-sm text-[var(--hicas-text-secondary)]">Nhánh cấp cao nhất</p>
              <p className="text-2xl font-bold text-[var(--hicas-text-main)]">{rootCount}</p>
            </div>
          </div>
        </Card>
      </div>

      {showForm ? (
        <Card
          title="Thêm phòng ban"
          description="Nhập mã, tên và phòng ban trực thuộc nếu có."
        >
          <form onSubmit={onSubmitCreate} className="grid gap-4 lg:grid-cols-[180px_1fr_1fr_auto]">
            <label className="block text-sm font-medium text-[var(--hicas-text-main)]">
              Mã phòng ban
              <input
                required
                value={formData.deptCode}
                onChange={(event) =>
                  setFormData((current) => ({ ...current, deptCode: event.target.value }))
                }
                className="hicas-input mt-1 uppercase"
                placeholder="VD: HCNS"
              />
            </label>

            <label className="block text-sm font-medium text-[var(--hicas-text-main)]">
              Tên phòng ban
              <input
                required
                value={formData.deptName}
                onChange={(event) =>
                  setFormData((current) => ({ ...current, deptName: event.target.value }))
                }
                className="hicas-input mt-1"
                placeholder="VD: Phòng Hành chính Nhân sự"
              />
            </label>

            <label className="block text-sm font-medium text-[var(--hicas-text-main)]">
              Trực thuộc
              <select
                value={formData.parentDeptId}
                onChange={(event) =>
                  setFormData((current) => ({ ...current, parentDeptId: event.target.value }))
                }
                className="hicas-select mt-1"
              >
                <option value="">Cấp cao nhất</option>
                {parentOptions.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
            </label>

            <div className="flex items-end">
              <Button type="submit" fullWidth iconLeft={<Save size={16} />} isLoading={saving}>
                Lưu
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <Card
        title="Sơ đồ phòng ban"
        description="Điều chỉnh phòng ban trực thuộc hoặc tạm ngừng phòng ban không còn sử dụng."
      >
        <div className="mb-4">
          <Tabs
            value={departmentTab}
            onChange={(value) => setDepartmentTab(value as "active" | "inactive")}
            items={[
              {
                value: "active",
                label: "Đang hoạt động",
                badge: <Badge variant="success">{activeCount}</Badge>,
              },
              {
                value: "inactive",
                label: "Tạm ngừng",
                badge: <Badge variant="neutral">{inactiveDepartments.length}</Badge>,
              },
            ]}
          />
        </div>

        {departmentTab === "inactive" && (
          <div className="max-h-[640px] space-y-3 overflow-y-auto pr-1">
            {inactiveDepartments.map((department) => (
              <div
                key={department.id}
                className="flex flex-col gap-3 rounded-[var(--radius-lg)] border border-[var(--hicas-border)] bg-white p-4 sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-semibold text-[var(--hicas-text-main)]">{department.deptName}</h3>
                    <Badge variant="neutral">{getStatusLabel(department.status)}</Badge>
                  </div>
                  <p className="mt-1 text-sm text-[var(--hicas-text-secondary)]">
                    {department.deptCode} · {department.children?.length || 0} phòng ban trực thuộc
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Button
                    size="sm"
                    variant="secondary"
                    iconLeft={<RefreshCcw size={15} />}
                    isLoading={activatingId === department.id}
                    onClick={() => onActivateDepartment(department.id, department.deptName)}
                  >
                    Bật lại
                  </Button>
                  <Button
                    size="sm"
                    variant="danger"
                    iconLeft={<Trash2 size={15} />}
                    onClick={() => setPendingDelete({ id: department.id, name: department.deptName })}
                  >
                    Xóa hẳn
                  </Button>
                </div>
              </div>
            ))}
            {!inactiveDepartments.length && (
              <EmptyState
                icon={<UsersRound size={22} />}
                title="Chưa có phòng ban tạm ngừng"
                description="Các phòng ban đã tạm ngừng sẽ được hiển thị tại đây."
              />
            )}
          </div>
        )}

        {departmentTab === "active" && activeTreeData.length ? (
          <div className="max-h-[680px] space-y-3 overflow-y-auto pr-1">
            {activeTreeData.map((node) => (
              <DepartmentTreeRow
                key={node.id}
                node={node}
                flatList={parentOptions}
                onUpdateDepartment={onUpdateDepartment}
                onDeactivate={(id, name) => setPendingDeactivate({ id, name })}
              />
            ))}
          </div>
        ) : departmentTab === "active" ? (
          <EmptyState
            icon={<UsersRound size={22} />}
            title="Chưa có phòng ban"
            description="Tạo phòng ban đầu tiên để bắt đầu xây dựng sơ đồ tổ chức."
            action={
              <Button iconLeft={<Plus size={16} />} onClick={() => setShowForm(true)}>
                Thêm phòng ban
              </Button>
            }
          />
        ) : null}
      </Card>

      <ConfirmDialog
        open={Boolean(pendingDeactivate)}
        title="Tạm ngừng phòng ban?"
        description={
          pendingDeactivate
            ? `Phòng ban ${pendingDeactivate.name} sẽ không còn được sử dụng cho hồ sơ mới.`
            : undefined
        }
        confirmLabel="Tạm ngừng"
        cancelLabel="Hủy"
        tone="danger"
        isLoading={deactivating}
        onConfirm={onConfirmDeactivate}
        onClose={() => setPendingDeactivate(null)}
      />

      <ConfirmDialog
        open={Boolean(pendingDelete)}
        title="Xóa hẳn phòng ban?"
        description={
          pendingDelete
            ? `Phòng ban ${pendingDelete.name} sẽ bị xóa khỏi danh sách nếu không còn dữ liệu liên quan.`
            : undefined
        }
        confirmLabel="Xóa hẳn"
        cancelLabel="Hủy"
        tone="danger"
        isLoading={deleting}
        onConfirm={onConfirmDelete}
        onClose={() => setPendingDelete(null)}
      />
    </div>
  );
};
