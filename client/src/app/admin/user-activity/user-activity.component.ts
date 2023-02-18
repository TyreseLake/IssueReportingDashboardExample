import { Component, OnInit } from '@angular/core';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ActivityInfoModalComponent } from 'src/app/modals/activity-info-modal/activity-info-modal.component';
import { fadeInStagger } from 'src/app/_animations/animations';
import { Pagination } from 'src/app/_models/pagination';
import { AdminService } from 'src/app/_services/admin.service';

@Component({
  selector: 'app-user-activity',
  templateUrl: './user-activity.component.html',
  styleUrls: ['./user-activity.component.css'],
  animations: [
    fadeInStagger
  ]
})
export class UserActivityComponent implements OnInit {
  activity = [];
  loading = false;
  pageNumber = 1;
  pageSize = 8;
  pagination: Pagination;

  bsModalRef: BsModalRef;

  constructor(private adminService: AdminService,
              private modalService: BsModalService) { }

  ngOnInit(): void {
    this.loadActivity()
  }

  loadActivity() {
    this.loading = true;
    this.adminService.getUserActivity(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        this.activity = response.result;
        console.log(this.activity);
        this.pagination = response.pagination;
        this.loading = false;
      }
    })
  }

  pageChanged(event: any){
    if (this.pageNumber !== event.page){
      this.pageNumber = event.page;
      this.loadActivity();
    }
  }

  displayActivity(id: number){
    var info = this.activity[id];
    console.log(info);
    const config:ModalOptions<ActivityInfoModalComponent> = {
      class: 'modal-dialog-centered',
      id: 9997,
      initialState: {
        id: info["id"],
        accountType: info["accountType"],
        action: info["action"],
        actionGroup: info["actionGroup"],
        activityDate: info["activityDate"],
        completionDate: info["completionDate"],
        data: info["data"],
        email: info["email"],
        ipAddress: info["ipAddress"],
        response: info["response"],
        status: info["status"],
        userName: info["userName"]
      }
    }
    this.bsModalRef = this.modalService.show(ActivityInfoModalComponent, config)
  }

}
