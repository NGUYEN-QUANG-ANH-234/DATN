import axiosClient from "../../../core/api/axiosClient";
import type {
  CompanyCalendar,
  CompanyCalendarResponse,
  SaveCompanyCalendarPayload,
} from "../types/companyCalendar";

const ENDPOINT = "/system/company-calendar";

export const companyCalendarApi = {
  getByYear: async (year: number): Promise<CompanyCalendarResponse<CompanyCalendar[]>> => {
    return await axiosClient.get(`${ENDPOINT}/${year}`);
  },

  save: async (
    year: number,
    payload: SaveCompanyCalendarPayload,
  ): Promise<CompanyCalendarResponse<CompanyCalendar>> => {
    return await axiosClient.put(`${ENDPOINT}/${year}`, payload);
  },
};
