export interface AttendanceConfig {
  latitude: number;
  longitude: number;
  radiusInMeters: number;
  allowedIpRanges: string[]; // Mảng các dải IP
}
