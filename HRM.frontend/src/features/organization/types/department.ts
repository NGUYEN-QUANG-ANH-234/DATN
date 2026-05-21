export interface DepartmentTree {
  id: number;
  deptCode: string;
  deptName: string;
  parentDeptId: number | null;
  managerId: number | null;
  status: string;
  children: DepartmentTree[];
}
