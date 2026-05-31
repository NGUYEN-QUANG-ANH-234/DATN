export type AccountStatus = "Active" | "Inactive" | "Locked" | "Suspended";

export interface Account {
  id: number;
  email: string;
  fullName: string;
  roleId: number;
  roleName?: string;
  status: AccountStatus;
  isMfaEnabled: boolean;
  createdAt?: string;
  avatarUrl?: string | null;
}

export interface CreateAccountDto {
  email: string;
  fullName: string;
  roleId: number;
  password?: string;
}

export interface CreateAccountResultDto {
  accountId: number;
  temporaryPassword?: string | null;
  isGeneratedPassword: boolean;
}

export interface ResetPasswordResultDto {
  temporaryPassword: string;
}
