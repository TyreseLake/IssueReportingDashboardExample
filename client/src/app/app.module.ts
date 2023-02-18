import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NavComponent } from './navigation/nav/nav.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { LoginComponent } from './login/login.component';
import { MainComponent } from './main/main.component';
import { SharedModule } from './_modules/shared.module';
import { BaseLayoutComponent } from './layouts/base-layout/base-layout.component';
import { SideNavComponent } from './navigation/side-nav/side-nav.component';
import { HasRoleDirective } from './_directives/has-role.directive';
import { DateInputComponent } from './_forms/date-input/date-input.component';
import { TextInputComponent } from './_forms/text-input/text-input.component';
import { CreateUserComponent } from './admin/create-user/create-user.component';
import { ManageUserComponent } from './admin/manage-user/manage-user.component';
import { JwtInterceptor } from './_interceptors/jwt.interceptor';
import { NotFoundComponent } from './errors/not-found/not-found.component';
import { ServerErrrorComponent } from './errors/server-errror/server-errror.component';
import { ReportIssueComponent } from './issue-reporting/report-issue/report-issue.component';
import { ViewReportsComponent } from './issue-reporting/view-reports/view-reports.component';
import { DataUploadComponent } from './data-management/data-upload/data-upload.component';
import { DataExportComponent } from './data-management/data-export/data-export.component';
import { ViewReportDetailsComponent } from './issue-reporting/view-report-details/view-report-details.component';
import { ErrorInterceptor } from './_interceptors/error.interceptor';
import { StatusModalComponent } from './modals/status-modal/status-modal.component';
import { ConfirmationModalComponent } from './modals/confirmation-modal/confirmation-modal.component';
import { ExcludeRoleDirective } from './_directives/exclude-role.directive';
import { RemarkModalComponent } from './modals/remark-modal/remark-modal.component';
import { LocationModalComponent } from './modals/location-modal/location-modal.component';
import { UpdateUserModalComponent } from './modals/update-user-modal/update-user-modal.component';
import { ManageUserInfoComponent } from './admin/manage-user-info/manage-user-info.component';
import { SelectInputComponent } from './_forms/select-input/select-input.component';
import { TextAreaInputComponent } from './_forms/text-area-input/text-area-input.component';
import { NumbersOnlyDirective } from './_directives/numbers-only.directive';
import { SelectionModalComponent } from './modals/selection-modal/selection-modal.component';
import { EditProfileComponent } from './user/edit-profile/edit-profile.component';
import { SelectIssueStatusModalComponent } from './modals/select-issue-status-modal/select-issue-status-modal.component';
import { JwtHelperService, JWT_OPTIONS } from '@auth0/angular-jwt';
import { UserActivityComponent } from './admin/user-activity/user-activity.component';
import { DescriptionModalComponent } from './modals/description-modal/description-modal.component';
import { TruncatePipe } from './_pipes/truncate.pipe';
import { StatusColorDirective } from './_directives/status-color.directive';
import { LoadingInterceptor } from './_interceptors/loading.interceptor';
import { NgxSpinnerModule } from 'ngx-spinner';
import { ActivityInfoModalComponent } from './modals/activity-info-modal/activity-info-modal.component';
import { SearchIssuesModalComponent } from './modals/search-issues-modal/search-issues-modal.component';
import { FileUploaderComponent } from './_components/file-uploader/file-uploader.component';
import { ImageViewerComponent } from './_components/image-viewer/image-viewer.component';
import { StatusUpdateCardComponent } from './issue-reporting/status-update-card/status-update-card.component';
import { IssuesTableComponent } from './_components/issues-table/issues-table.component';
import { IssueManagementPrivilegesDirective } from './_directives/issue-management-privileges.directive';
import { NoIssueManagementPrivilegesDirective } from './_directives/no-issue-management-privileges.directive';
import { AdminPrivilegesDirective } from './_directives/admin-privileges.directive';
import { DataManagementPrivilegesDirective } from './_directives/data-management-privileges.directive';
import { UserManagementPrivilegesDirective } from './_directives/user-management-privileges.directive';
import { StatusUpdatePrivilegesDirective } from './_directives/status-update-privileges.directive';


@NgModule({
  declarations: [
    AppComponent,
    NavComponent,
    LoginComponent,
    MainComponent,
    BaseLayoutComponent,
    DateInputComponent,
    TextInputComponent,
    SideNavComponent,
    HasRoleDirective,
    CreateUserComponent,
    ManageUserComponent,
    NotFoundComponent,
    ServerErrrorComponent,
    ReportIssueComponent,
    ViewReportsComponent,
    DataUploadComponent,
    DataExportComponent,
    ViewReportDetailsComponent,
    StatusModalComponent,
    ConfirmationModalComponent,
    ExcludeRoleDirective,
    RemarkModalComponent,
    LocationModalComponent,
    UpdateUserModalComponent,
    ManageUserInfoComponent,
    SelectInputComponent,
    TextAreaInputComponent,
    NumbersOnlyDirective,
    SelectionModalComponent,
    EditProfileComponent,
    SelectIssueStatusModalComponent,
    UserActivityComponent,
    DescriptionModalComponent,
    TruncatePipe,
    StatusColorDirective,
    ActivityInfoModalComponent,
    SearchIssuesModalComponent,
    FileUploaderComponent,
    ImageViewerComponent,
    StatusUpdateCardComponent,
    IssuesTableComponent,
    IssueManagementPrivilegesDirective,
    NoIssueManagementPrivilegesDirective,
    AdminPrivilegesDirective,
    DataManagementPrivilegesDirective,
    UserManagementPrivilegesDirective,
    StatusUpdatePrivilegesDirective
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule,
    NgxSpinnerModule
  ],
  providers: [
    {provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true},
    {provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true},
    {provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true},
    {provide: JWT_OPTIONS, useValue: JWT_OPTIONS },
    JwtHelperService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
