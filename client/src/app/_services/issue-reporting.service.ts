import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { environment } from 'src/environments/environment';
import { IssueReport } from '../_models/issueReport';
import { IssueReportsParams } from '../_models/issueReportsParams';
import { IssueStatus } from '../_models/issueStatus';
import { IssueStatusDetails } from '../_models/issueStatusDetails';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';

@Injectable({
  providedIn: 'root'
})
export class IssueReportingService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // addNewIssueReport(report) {
  //   return this.http.post<any>(this.baseUrl + 'issuereporting/add-issue-report', report);
  // }

  // addNewIssueReport(data, images = null) {
  //   return this.http.post<any>(this.baseUrl + 'issuereporting/test', data);
  // }


  addNewIssueReport(report, images) {
    // console.log(report)
    // console.log(images)

    var formData:FormData = new FormData();

    // console.log(report);
    for (let key in report) {
      if(report[key]){
        formData.append(key, report[key]);
      }
    }

    // console.log(report.value)
    // console.log(formData);

    if(images?.length > 0){
      // formData.append('files', images);
      // report['images'] = images
      images.forEach((image, index) => {
        formData.append('image_'+index, image);
      });
    }

    // if(images?.length > 0){
    //   formData.append('image_1', images[0]);
    //   formData.append('image_2', images[1]);
    //   formData.append('image_3', images[2]);
    //   formData.append('image_4', images[3]);
    //   formData.append('image_5', images[4]);
    // }

    // formData.append('platform', report['platform']);
    // formData.append('statusId', report['statusId']);
    // formData.append('issueType', report['issueType']);
    // formData.append('subject', report['subject']);
    // formData.append('description', report['description']);
    // formData.append('locationDescription', report['locationDescription']);
    // formData.append('district', report['district']);
    // formData.append('phoneNumber', report['phoneNumber']);
    // formData.append('address', report['address']);
    // formData.append('email', report['email']);
    // formData.append('locationLatitude', report['locationLatitude']);
    // formData.append('locationLongitude', report['locationLongitude']);
    // formData.append('images', images);

    return this.http.post<any>(this.baseUrl + 'issuereporting/add-issue-report', formData);
  }


  getIssueStatuses(issueReportsParams:IssueReportsParams) {
    let params = getPaginationHeaders(issueReportsParams.pageNumber, issueReportsParams.pageSize);

    if(issueReportsParams.key)
      params = params.append('key', issueReportsParams.key);

    if(issueReportsParams.dateUpper)
      params = params.append('dateUpper', issueReportsParams.dateUpper.toDateString());

    if(issueReportsParams.dateLower)
      params = params.append('dateLower', issueReportsParams.dateLower.toDateString());

    if(issueReportsParams.issueTypeAccess?.length > 0)
      params = params.append('issueTypes', issueReportsParams.issueTypeAccess.toString());

    if(issueReportsParams.districtAccess?.length > 0)
      params = params.append('districts', issueReportsParams.districtAccess.toString());

    if(issueReportsParams.maxReportCount>0)
      params = params.append('maxReportCount', issueReportsParams.maxReportCount.toString());

    if(issueReportsParams.minReportCount>0)
      params = params.append('minReportCount', issueReportsParams.minReportCount.toString());

    if(issueReportsParams.sortBy && issueReportsParams.sortBy != "")
      params = params.append('sortBy', issueReportsParams.sortBy.toString());

    if(issueReportsParams.order && issueReportsParams.order != "")
      params = params.append('order', issueReportsParams.order.toString());

    if(issueReportsParams.status?.length > 0)
      params = params.append('status', issueReportsParams.status.toString());

    if(issueReportsParams.pinnedOnTop != null)
      params = params.append('pinnedOnTop', issueReportsParams.pinnedOnTop);

    if(issueReportsParams.showHidden != null)
      params = params.append('showHidden', issueReportsParams.showHidden);

    if(issueReportsParams.showClosed != null)
      params = params.append('showClosed', issueReportsParams.showClosed);

    return getPaginatedResult<IssueStatus[]>(this.baseUrl + 'issuereporting/issue-report-statuses', params, this.http);
  }

  getIssueDetails(id){
    return this.http.get<any>(this.baseUrl + "issuereporting/issue-report-details/" + id);
  }

  getIssueReports(id, pageNumber, pageSize) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    return getPaginatedResult<IssueReport[]>(this.baseUrl + 'issuereporting/issue-reports/' + id, params, this.http);
  }

  getIssueDescriptions(id, pageNumber, pageSize) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    return getPaginatedResult<string[]>(this.baseUrl + 'issuereporting/issue-report-descriptions/' + id, params, this.http);
  }

  getIssueLocations(id, pageNumber, pageSize) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    return getPaginatedResult<string[]>(this.baseUrl + 'issuereporting/issue-report-locations/' + id, params, this.http);
  }

  addIssueStatusUpdate(issueStatusUpdate, images = null) {
    console.log("Status Update Values")
    console.log(issueStatusUpdate)

    var formData:FormData = new FormData();

    for (let key in issueStatusUpdate) {
      if(issueStatusUpdate[key]){
        if(key == 'date'){
          formData.append(key, issueStatusUpdate[key].toJSON());
        }else if(key == 'approvalItems'){
          formData.append(key, JSON.stringify(issueStatusUpdate[key]));
        }
        else{
          formData.append(key, issueStatusUpdate[key]);
        }
      }
    }

    if(images?.length > 0){
      // formData.append('files', images);
      // report['images'] = images
      images.forEach((image, index) => {
        formData.append('image_'+index, image);
      });
    }

    return this.http.post<any>(this.baseUrl + 'issuereporting/issue-status-update', formData);
    // return this.http.post<any>(this.baseUrl + 'issuereporting/update-issue-status', issueStatusUpdate);
  }

  editStatusUpdate(statusUpdateData){
    console.log("Edit Status Update Values")
    console.log(statusUpdateData)

    var formData:FormData = new FormData();

    for (let key in statusUpdateData) {
      if(statusUpdateData[key]){
        if(key == 'date'){
          formData.append(key, statusUpdateData[key].toJSON());
        }else if(key == 'approvalItems'){
          formData.append(key, JSON.stringify(statusUpdateData[key]));
        }
        else{
          formData.append(key, statusUpdateData[key]);
        }
      }
    }

    return this.http.post<any>(this.baseUrl + 'issuereporting/edit-issue-status-update', formData);
  }

  getIssueStatusUpdates(id, pageNumber, pageSize) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    return getPaginatedResult<string[]>(this.baseUrl + 'issuereporting/issue-status-updates/' + id, params, this.http);
  }

  getExportedIssueReports(){
    return this.http.get<any>(this.baseUrl + 'issuereporting/export-issue-reports');
  }

  getExportedSheet(exportInfo){
    let params = new HttpParams();


    params = params.append('exportType', exportInfo['exportType']);
    if(exportInfo['districts'] != null && exportInfo['districts'].length > 0)
      params = params.append('districts', exportInfo['districts'].toString())
    if(exportInfo['issueTypes'] != null && exportInfo['issueTypes'].length > 0)
      params = params.append('issueTypes', exportInfo['issueTypes'].toString())
    if(exportInfo['statuses'] != null && exportInfo['statuses'].length > 0)
      params = params.append('statuses', exportInfo['statuses'].toString())
    if(exportInfo['dateLower'] != null && exportInfo['dateLower'] != "")
      params = params.append('dateLower', exportInfo['dateLower'].toDateString())
    if(exportInfo['dateUpper'] != null && exportInfo['dateUpper'] != "")
      params = params.append('dateUpper', exportInfo['dateUpper'].toDateString())
    if(exportInfo['statusId'] != null && exportInfo['statusId'] != "")
      params = params.append('statusId', exportInfo['statusId'])


    // return this.http.get<any>(this.baseUrl + 'issuereporting/export-sheet', {params: exportInfo});
    return this.http.get(
      this.baseUrl + 'issuereporting/export-sheet',
      {
        observe: 'response',
        params: params,
        responseType: 'blob'
      }).pipe(
        map((response)=>{
          let data = {
            file: new Blob([response.body], {type: response.headers.get('Content-Type')}),
          }
          return(data);
        }),
        catchError(async (error) => {
          var value = await error.error.text();
          let data = {
            error: value,
            file: null
          }
          return data
        })
      );
  }

  // addRemark(remarkDetails){
  //   return this.http.post<any>(this.baseUrl + 'issuereporting/add-remark', remarkDetails);
  // }

  updateIssueStatusLocation(issueStatusUpdate) {
    return this.http.post<any>(this.baseUrl + 'issuereporting/update-issue-status-location', issueStatusUpdate);
  }

  updateIssueStatusDescription(issueStatusUpdate) {
    return this.http.post<any>(this.baseUrl + 'issuereporting/update-issue-status-description', issueStatusUpdate);
  }

  moveIssueReport(moveData) {
    return this.http.post<any>(this.baseUrl + 'issuereporting/move-issue-reports', moveData);
  }

  searchIssues(searchData) {
    return this.http.get<any>(this.baseUrl + 'issuereporting/search-issues', {params: searchData});
  }

  searchIssueCoordinates(coordinatesData) {
    return this.http.get<any>(this.baseUrl + 'issuereporting/search-issue-coordinates', {params: coordinatesData});
  }

  updateIssueReport(editData) {
    return this.http.post<any>(this.baseUrl + 'issuereporting/update-issue-report', editData);
  }

  pinIssue(issueList) {
    return this.http.post<any>(this.baseUrl + 'issuereporting/issue-status-pin', issueList);
  }

  hideIssue(issueList) {
    return this.http.post<any>(this.baseUrl + 'issuereporting/issue-status-hide', issueList);
  }

  getIssuesSummary() {
    return this.http.get<any>(this.baseUrl + 'issuereporting/summary');
  }
}
