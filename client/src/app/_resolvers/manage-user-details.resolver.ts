import { Injectable } from '@angular/core';
import {
  Router, Resolve,
  RouterStateSnapshot,
  ActivatedRouteSnapshot
} from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, map, Observable, of } from 'rxjs';
import { UserDetails } from '../_models/userDetails';
import { AdminService } from '../_services/admin.service';

@Injectable({
  providedIn: 'root'
})
export class ManageUserDetailsResolver implements Resolve<UserDetails> {
  constructor(private adminService : AdminService, private router: Router, private toastr : ToastrService) {

  }

  resolve(route: ActivatedRouteSnapshot): Observable<UserDetails> {
    return this.adminService.getUserDetails(route.paramMap.get('id')).pipe(
      map((details) => {return details}),
      catchError((error) => {
        this.router.navigate(['/admin/user-manage']);
        console.error(error);
        this.toastr.error("Bad Request", error);
        return of(null)
      })
    );
  }
}
