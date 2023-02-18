import { Directive, TemplateRef, ViewContainerRef } from '@angular/core';
import { take } from 'rxjs';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';
import { DataService } from '../_services/data.service';

@Directive({
  selector: '[appStatusUpdatePrivileges]'
})
export class StatusUpdatePrivilegesDirective {
  user: User;
  statusUpdaterRoles = []

  constructor(private viewContainerRef: ViewContainerRef,
    private templateRef: TemplateRef<any>,
    private accountService: AccountService) {
    //Get the current user
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      this.user = user;
    });
    // //Get StatusUpdaterRoles
    // this.dataService.getRoles("status").subscribe({
    //   next: (result) => {
    //     this.statusUpdaterRoles = result;
    //   },
    //   complete: () => {
    //     if(this.user?.roles.some(r => this.statusUpdaterRoles.includes(r))){
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

    if(this.user?.issueStatusUpdaterPrivileges){
      this.viewContainerRef.createEmbeddedView(this.templateRef);
    } else {
      this.viewContainerRef.clear();
    }
  }
}
