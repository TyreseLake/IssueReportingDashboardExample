import { Component, OnInit } from '@angular/core';
import { fadeInStagger } from 'src/app/_animations/animations';
import { Pagination } from 'src/app/_models/pagination';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-manage-user',
  templateUrl: './manage-user.component.html',
  styleUrls: ['./manage-user.component.css'],
  animations: [
    fadeInStagger
  ]
})
export class ManageUserComponent implements OnInit {
  users = [];
  loading = false;
  pageNumber = 1;
  pageSize = 20;
  pagination: Pagination;

  constructor(private adminService: AdminService) { }

  ngOnInit(): void {
    this.loadUsers()
  }

  loadUsers() {
    this.loading = true;
    this.adminService.getUsers(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        this.users = response.result;
        this.pagination = response.pagination;
        this.loading = false;
      }
    })
  }

  pageChanged(event: any){
    if (this.pageNumber !== event.page){
      this.pageNumber = event.page;
      this.loadUsers();
    }
  }

}
