import axiosClient from "../../../core/api/axiosClient";
import type {
  ApiResponse,
  DashboardDrilldown,
  DashboardResponse,
} from "../types/dashboard";

export const dashboardApi = {
  getDashboard: (month: number, year: number) =>
    axiosClient.get<ApiResponse<DashboardResponse>, ApiResponse<DashboardResponse>>(
      "/dashboard",
      { params: { month, year } },
    ),

  getDrilldown: (
    type: string,
    params: { month: number; year: number; scope?: string },
  ) =>
    axiosClient.get<ApiResponse<DashboardDrilldown>, ApiResponse<DashboardDrilldown>>(
      `/dashboard/drilldowns/${encodeURIComponent(type)}`,
      { params },
    ),
};
