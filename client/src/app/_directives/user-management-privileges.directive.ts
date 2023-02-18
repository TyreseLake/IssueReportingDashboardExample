import { Directive, TemplateRef, ViewContainerRef } from '@angular/core';
import { take } from 'rxjs';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';
import { DataService } from '../_services/data.service';

@Directive({
  selector: '[appUserManagementPrivileges]'
})
export class UserManagementPrivilegesDirective {
  user: User;
  userManagementRoles = []

  constructor(private viewContainerRef: ViewContainerRef,
    private templateRef: TemplateRef<any>,
    private accountService: AccountService) {
    //Get the current user
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.user = user;
    });
    // //Get UserManagementRoles
    // this.dataService.getRoles("user").subscribe({
    //   next: (result) => {
    //     this.userManagementRoles = result;
    //   },
    //   complete: () => {
    //     if(this.user?.roles.some(r => this.userManagementRoles.includes(r))){
    //       this.viewContainerRef.createEmbeddedView(this.templateRef);
    //     } else {
    //       this.viewContainerRef.clear();
    //     }
    //   }
    // })
  }

  ngOnInit(): void {
    //Clear view if no files
    if (!this.user?.roles || this.user == null){
      this.viewContainerRef.clear();
      return;
    }

    if(this.user?.userManagementPrivileges){
      this.viewContainerRef.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainerRef.clear();
    }
  }
}
