import axiosClient from "../../../core/api/axiosClient";
import type {
  CompanyCalendar,
  CompanyCalendarResponse,
  SaveCompanyCalendarPayload,
} from "../types/companyCalendar";

const ENDPOINT = "/system/company-calendar";

export const companyCalendarApi = {
  getActiveByYear: async (year: number): Promise<CompanyCalendarResponse<CompanyCalendar | null>> => {
    return await axiosClient.get(`${ENDPOINT}/active/${year}`);
  },

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
