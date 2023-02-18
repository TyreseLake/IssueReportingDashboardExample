import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { ToastrService } from 'ngx-toastr';
import { map, Observable } from 'rxjs';
import { AccountService } from '../_services/account.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private accountService: AccountService,
              private toastr: ToastrService,
              private router: Router,
              private jwtHelper: JwtHelperService){}

  canActivate(): Observable<boolean>{
    return this.accountService.currentUser$.pipe(
      map(user => {
        if (user) {
          if(this.jwtHelper.isTokenExpired(user.token)){
            this.logout();
          }else{
            return true;
          }
        };
        this.toastr.error('You must be logged in to access to this page!');
        this.router.navigate(['login']);
        return false;
      })
    );
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }
}
