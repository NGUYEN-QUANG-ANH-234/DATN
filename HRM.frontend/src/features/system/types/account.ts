export type AccountStatus = "Active" | "Inactive" | "Locked" | "Suspended";

export interface Account {
  id: number;
  email: string;
  fullName: string;
  roleId: number;
  status: AccountStatus;
  isMfaEnabled: boolean;
}

export interface CreateAccountDto {
  email: string;
  fullName: string;
  roleId: number;
}
