import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { take } from 'rxjs';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';

@Directive({
  selector: '[appExcludeRole]'
})
export class ExcludeRoleDirective {
  @Input() appExcludeRole: string[];
  user: User;

  constructor(private viewContainerRef: ViewContainerRef,
    private templateRef: TemplateRef<any>,
    private accountService: AccountService) {
      //Get the current user
      this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
        this.user = user;
      })
    }

    ngOnInit(): void {
      //Clear view if no files
      if (!this.user?.roles || this.user == null){
        this.viewContainerRef.clear();
        return;
      }

      if(this.user?.roles.every(r => !this.appExcludeRole.includes(r))){
        this.viewContainerRef.createEmbeddedView(this.templateRef);
      } else {
        this.viewContainerRef.clear();
      }
    }
}
