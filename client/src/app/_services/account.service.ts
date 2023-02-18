import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { pipe, ReplaySubject } from 'rxjs';
import { first, map, take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { User } from '../_models/user';
import { UserDetails } from '../_models/userDetails';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  baseUrl = environment.apiUrl;
  private currentUserSource = new ReplaySubject<User>(1);
  currentUser$ = this.currentUserSource.asObservable();

  constructor(private http: HttpClient) { }

  login(model: any) {
    return this.http.post<User>(this.baseUrl + 'account/login', model).pipe(
      map((response) => {
        const user = response;
        if (user) {
          this.setCurrentUser(user);
        }
        return user;
      })
    )
  }

  getCurrentUser(){
    var subscription$ = this.currentUser$.pipe(first()).subscribe({
      next: (user) => {
        subscription$.unsubscribe();
        return user;
      }
    });
  }

  setCurrentUser(user: User){
    if(user){
      user.roles = [];
      const roles = this.getDecodedToken(user.token).role;
      Array.isArray(roles) ? user.roles = roles : user.roles.push(roles);
    }
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSource.next(user);
  }

  logout() {
    localStorage.removeItem('user');
    this.currentUserSource.next(undefined);
  }

  getDecodedToken(token: string) {
    return JSON.parse(atob(token.split('.')[1]));
  }

  getProfile() {
    return this.http.get<UserDetails>(this.baseUrl + 'account');
  }

  updateProfile(userDetails: any){
    return this.http.post<any>(this.baseUrl + 'account/update-profile', userDetails);
  }

  changePassword(passwordDetails: any){
    return this.http.post<any>(this.baseUrl + 'account/update-password', passwordDetails);
  }
}
