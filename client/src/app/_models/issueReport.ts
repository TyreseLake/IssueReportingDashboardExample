import { Image } from 'src/app/_models/image'

export interface IssueReport {
  id: number;
  userName: string;
  subject: string;
  issueType: string;
  description: string;
  locationDescription: string;
  locationLongitude: number | null;
  locationLatitude: number | null;
  district: string;
  platform: string;
  dateReported: Date | null;
  phoneNumber: string;
  email: string;
  address: string;
  moved: boolean;
  dateMoved: Date | null;
  orignalIssueSourceId: number | null;
  images: Image[];
}
