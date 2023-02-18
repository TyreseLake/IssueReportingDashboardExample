export class User {
  userName: string;
  token: string;
  firstName: string;
  lastName: string;
  requirePasswordReset: boolean;
  roles: string[];
  params: Params;
  adminPrivileges: boolean;
  issueManagementPrivileges: boolean;
  userManagementPrivileges: boolean;
  dataManagementPrivileges: boolean;
  issueStatusUpdaterPrivileges: boolean;
}

export class Params {
  districtAccess: string[];
  issueTypeAccess:string[];
  order: string;
  pinnedOnTop: boolean;
  showClosed: boolean;
  showHidden: boolean;
  sortBy: string;
  statusList: string[];
}


