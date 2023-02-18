import { Directive, TemplateRef, ViewContainerRef } from '@angular/core';
import { take } from 'rxjs';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';
import { DataService } from '../_services/data.service';

@Directive({
  selector: '[appDataManagementPrivileges]'
})
export class DataManagementPrivilegesDirective {
  user: User;
  dataManagementRoles = []

  constructor(private viewContainerRef: ViewContainerRef,
    private templateRef: TemplateRef<any>,
    private accountService: AccountService) {
    //Get the current user
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.user = user;
    });
    // //Get Data Roles
    // this.dataService.getRoles("data").subscribe({
    //   next: (result) => {
    //     this.dataManagementRoles = result;
    //   },
    //   complete: () => {
    //     if(this.user?.roles.some(r => this.dataManagementRoles.includes(r))){
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

    if(this.user?.dataManagementPrivileges){
      this.viewContainerRef.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainerRef.clear();
    }
  }

}
