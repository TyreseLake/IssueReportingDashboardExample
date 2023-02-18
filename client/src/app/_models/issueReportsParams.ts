export class IssueReportsParams {
  pageNumber = 1;
  pageSize = 50;
  key: string;
  dateUpper: Date;
  dateLower: Date;
  issueTypeAccess: string[] = [];
  districtAccess: string[] = [];
  maxReportCount: number = null;
  minReportCount: number = null;
  status: string[] = [];
  sortBy: string = "DateReported";
  order: string = "Descending";
  showClosed: boolean = true;
  pinnedOnTop: boolean = true;
  showHidden: boolean = false;
}
