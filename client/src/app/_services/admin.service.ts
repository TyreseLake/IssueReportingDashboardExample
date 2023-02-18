import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { getPaginatedResult, getPaginationHeaders } from './paginationHelper';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  createUser(userInfo: any) {
    return this.http.post(this.baseUrl + 'admin/create-user', userInfo);
  }

  getUsers(pageNumber, pageSize) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    return getPaginatedResult<any[]>(this.baseUrl + 'admin/users', params, this.http);
  }

  getUserDetails(id) {
    return this.http.get<any>(this.baseUrl + 'admin/user-info/' + id);
  }

  updateUserDetails(userDetails: any){
    return this.http.post<any>(this.baseUrl + 'admin/update-user-info', userDetails);
  }

  updateUserPassword(newPassword){
    console.log(newPassword);
    return this.http.post<string>(this.baseUrl + 'admin/update-user-password', newPassword);
  }

  getUserActivity(pageNumber, pageSize) {
    let params = getPaginationHeaders(pageNumber, pageSize);
    return getPaginatedResult<any[]>(this.baseUrl + 'admin/user-activity', params, this.http);
  }
}
