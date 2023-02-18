import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor
} from '@angular/common/http';
import { catchError, Observable, throwError } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { NavigationExtras, Router } from '@angular/router';
import { AccountService } from '../_services/account.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private router: Router, private toastr: ToastrService, private accountService: AccountService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError(error => {
        if(error){
          switch (error.status) {
            case 400:
              console.error(error);
              if(error?.error?.errors){
                const modalStateErrors = [];
                for (const key in error?.error?.errors){
                  if(error?.error?.errors[key]){
                    modalStateErrors.push(error?.error?.errors[key])
                  }
                }
                this.toastr.error(modalStateErrors.flat().join("\n"));
                throw modalStateErrors.flat();
              } else if (typeof(error?.error) === 'object') {
                // var hasDescription = false;
                if(error?.error?.forEach)
                  error?.error?.forEach(e => {
                    if(e.description){
                      this.toastr.error(e.description);
                    }
                  });
                // if(!hasDescription)
                //   this.toastr.error("Bad Request", error.status);
              } else {
                this.toastr.error(error.error, error.status);
              }
              break;

            case 401:
              this.accountService.logout();
              console.log(error);
              this.router.navigateByUrl('/login');
              this.toastr.error(error.error, "Unauthorized");
              break;

            case 404:
              this.router.navigateByUrl('/not-found');
              break;

            case 500:
              const navigationExtras: NavigationExtras = {state: {error: error.error}}
              this.router.navigateByUrl('/server-error', navigationExtras);
              break;

            default:
              this.toastr.error('Somthing unexpected went wrong');
              console.log(error);
              break;
          }
        }
        return throwError(() => error);
      })
    )
  }
}
