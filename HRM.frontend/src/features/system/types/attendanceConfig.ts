export interface AttendanceOfficeLocation {
  name: string;
  latitude: number;
  longitude: number;
  radiusInMeters: number;
  allowedIpRanges: string[];
  isActive: boolean;
}

export interface AttendanceConfig {
  latitude: number;
  longitude: number;
  radiusInMeters: number;
  allowedIpRanges: string[];
  officeLocations: AttendanceOfficeLocation[];
}
