import { Injectable } from '@angular/core';
import {
  Router, Resolve,
  RouterStateSnapshot,
  ActivatedRouteSnapshot
} from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, map, Observable, of } from 'rxjs';
import { IssueStatusDetails } from '../_models/issueStatusDetails';
import { IssueReportingService } from '../_services/issue-reporting.service';

@Injectable({
  providedIn: 'root'
})
export class IssueReportDetailsResolver implements Resolve<IssueStatusDetails> {
  constructor(private issueReportingService : IssueReportingService, private router: Router, private toastr : ToastrService) {

  }

  resolve(route: ActivatedRouteSnapshot): Observable<IssueStatusDetails> {
    return this.issueReportingService.getIssueDetails(route.paramMap.get('id')).pipe(
      map((details) => {
        return details;
      }),
      catchError((error) => {
        this.router.navigate(['/reporting/issue-view']);
        console.error(error)
        this.toastr.error("Bad Request", error);
        return of(null)
      })
    );
  }
}
