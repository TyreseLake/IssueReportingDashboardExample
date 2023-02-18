import { Injectable } from '@angular/core';
import {
  Router, Resolve,
  RouterStateSnapshot,
  ActivatedRouteSnapshot
} from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { catchError, map, Observable, of } from 'rxjs';
import { UserDetails } from '../_models/userDetails';
import { AccountService } from '../_services/account.service';

@Injectable({
  providedIn: 'root'
})
export class ProfileDetailsResolver implements Resolve<UserDetails> {
  constructor(private accountService : AccountService,
              private router: Router,
              private toastr : ToastrService) { }

  resolve(route: ActivatedRouteSnapshot): Observable<UserDetails> {
    return this.accountService.getProfile().pipe(
      map((details) => {return details}),
      catchError((error) => {
        this.router.navigate(['']);
        console.error(error);
        this.toastr.error("Bad Request", error);
        return of(null)
      })
    );
  }
}
