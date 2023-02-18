import { Image } from 'src/app/_models/image'

export interface IssueStatusDetails {
  id: number;
  issueType: string;
  district: string;
  description: string;
  issueLocation: string;
  locationLongitude: number;
  locationLatitude: number;
  issueStatus: string;
  reviewType: string;
  issueRemarks: RemarkDto[];
  dateReported: Date;
  images: Image[];
  reportCount: number;
  platformCounts: ItemCount[];
  currentStatus: string;
}

export interface RemarkDto {
  userName: string;
  remarkType: string;
  remark: string;
  dateRemarked: Date;
}

export interface ItemCount {
  name: string;
  count: number;
}
