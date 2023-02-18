export interface IssueStatus {
  id: number;
  status: string;
  reviewType?: string;
  dateReported: Date;
  dateUpdated: Date;
  issueType: string;
  district?: string;
  issueReportCount: number;
  locationDescription?: string;
  description?: string;
}
