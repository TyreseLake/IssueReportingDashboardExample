import { Component, OnInit } from '@angular/core';
import { take } from 'rxjs';
import { fadeInOut } from 'src/app/_animations/animations';
import { AccountService } from 'src/app/_services/account.service';

@Component({
  selector: 'app-base-layout',
  templateUrl: './base-layout.component.html',
  styleUrls: ['./base-layout.component.scss'],
  animations: [
    fadeInOut
  ]
})
export class BaseLayoutComponent implements OnInit {

  showRequireReset = false;

  constructor(private accountService: AccountService) { }

  ngOnInit(): void {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: (user) => {
        this.showRequireReset = user.requirePasswordReset;
      }
    }).unsubscribe();
  }

  hideReset() {
    this.showRequireReset = false;
  }

}
