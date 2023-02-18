import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { ToastrService } from 'ngx-toastr';
import { map, Observable } from 'rxjs';
import { AccountService } from '../_services/account.service';

@Injectable({
  providedIn: 'root'
})
export class AccessGuard implements CanActivate {
  constructor(private accountService: AccountService,
              private toastr: ToastrService,
              private router: Router,
              private jwtHelper: JwtHelperService){}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean>{
    const currentLoginCheck = route.data['currentLoginCheck'] || false;
    return this.accountService.currentUser$.pipe(
      map(user => {
        if (user) {
          // if(this.jwtHelper.isTokenExpired(user.token)){
          //   // this.toastr.warning('Sorry, your session has expired. Please login.');
          //   // this.accountService.logout();
          //   // this.router.navigate(['login']);
          //   return false;
          // }
          this.router.navigate(['home']);
          if(currentLoginCheck)
            this.toastr.warning('You are already logged in.');
          return false;
        }
        return true;
      })
    );
  }
}
