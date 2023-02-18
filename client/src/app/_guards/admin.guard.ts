import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, RouterStateSnapshot, UrlTree } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { ToastrService } from 'ngx-toastr';
import { map, Observable } from 'rxjs';
import { AccountService } from '../_services/account.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {

  constructor(private accountService: AccountService,
              private toastr: ToastrService,
              private jwtHelper: JwtHelperService){}

  canActivate(): Observable<boolean> {
    return this.accountService.currentUser$.pipe(
      map(user => {
        if (user) {
          // if(this.jwtHelper.isTokenExpired(user.token)){
          //   // this.toastr.warning('Sorry, your session has expired. Please login.');
          //   // this.accountService.logout();
          //   // this.router.navigate(['login']);
          //   return false;
          // }
        };
        if (user.roles.includes("Admin")) return true;
        this.toastr.error('You do not have access to this page!');
        return false;
      })
    )
  }

}
