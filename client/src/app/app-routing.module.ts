import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CreateUserComponent } from './admin/create-user/create-user.component';
import { ManageUserInfoComponent } from './admin/manage-user-info/manage-user-info.component';
import { ManageUserComponent } from './admin/manage-user/manage-user.component';
import { UserActivityComponent } from './admin/user-activity/user-activity.component';
import { DataExportComponent } from './data-management/data-export/data-export.component';
import { DataUploadComponent } from './data-management/data-upload/data-upload.component';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrrorComponent } from './errors/server-errror/server-errror.component';
import { ReportIssueComponent } from './issue-reporting/report-issue/report-issue.component';
import { ViewReportDetailsComponent } from './issue-reporting/view-report-details/view-report-details.component';
import { ViewReportsComponent } from './issue-reporting/view-reports/view-reports.component';
import { BaseLayoutComponent } from './layouts/base-layout/base-layout.component';
import { LoginComponent } from './login/login.component';
import { MainComponent } from './main/main.component';
import { EditProfileComponent } from './user/edit-profile/edit-profile.component';
import { AccessGuard } from './_guards/access.guard';
import { AdminGuard } from './_guards/admin.guard';
import { AuthGuard } from './_guards/auth.guard';
import { IssueReportDetailsResolver } from './_resolvers/issue-report-details.resolver';
import { ManageUserDetailsResolver } from './_resolvers/manage-user-details.resolver';
import { ProfileDetailsResolver } from './_resolvers/profile-details.resolver';

const routes: Routes = [
  {
    path: '',
    canActivate: [AccessGuard],
    children: [{ path: '', component: LoginComponent }]
  },
  {
    path: '',
    component: BaseLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'home', component: MainComponent },
      { path: 'profile', component: EditProfileComponent, resolve: {userDetails: ProfileDetailsResolver} },
      {
        path: 'admin',
        children: [
          {path: "user-create", component: CreateUserComponent},
          {path: "user-manage", component: ManageUserComponent},
          {path: "user-activity", component: UserActivityComponent},
          {path: "user-manage/:id", component: ManageUserInfoComponent, resolve: {userDetails: ManageUserDetailsResolver}}
        ]
      },
      {
        path: 'reporting',
        children: [
          {path: "issue-report", component: ReportIssueComponent},
          {path: "issue-view", component: ViewReportsComponent},
          {path: "issue-view/:id", component: ViewReportDetailsComponent, resolve: {issueReportDetails: IssueReportDetailsResolver}}
        ]
      },
      {
        path: 'data',
        children: [
          {path: 'upload', component: DataUploadComponent},
          {path: 'export', component: DataExportComponent}
        ]
      }
    ]
  },
  { path: 'not-found', component: NotFoundComponent },
  { path: 'server-error', component: ServerErrrorComponent},
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [AccessGuard],
    data: { currentLoginCheck: true}
  },
  { path: '**', redirectTo: '/login', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
