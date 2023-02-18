import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DataService {
  baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  getDistricts(filterByRole = false) {
    return this.http.get<string[]>(this.baseUrl + "data/districts" + "?filterByRole=" + filterByRole);
  }

  getIssueTypes(filterByRole = false) {
    return this.http.get<string[]>(this.baseUrl + "data/issue-types"+ "?filterByRole=" + filterByRole);
  }

  getPlatforms() {
    return this.http.get<string[]>(this.baseUrl + "data/platforms");
  }

  getStatuses() {
    return this.http.get<string[]>(this.baseUrl + "data/statuses");
  }

  getRoles(type = "userRole") {
    return this.http.get<string[]>(this.baseUrl + "data/roles" + "?type=" + type)
  }


}
