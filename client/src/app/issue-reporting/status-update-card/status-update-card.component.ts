import { Component, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { StatusModalComponent } from 'src/app/modals/status-modal/status-modal.component';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';

@Component({
  selector: 'app-status-update-card',
  templateUrl: './status-update-card.component.html',
  styleUrls: ['./status-update-card.component.scss']
})
export class StatusUpdateCardComponent implements OnInit {
  @Input() statusUpdate: any;
  @Input() index: number;
  @Input() allowEditing = false;
  @Input() allowEditingStatus = false;

  statusUpdateInfo: any;

  editing = false;

  showImages = [];
  bsModalRef: BsModalRef;

  constructor(private fb: FormBuilder,
    private modalService: BsModalService,
    private issueReportingService: IssueReportingService,
    private toastr: ToastrService) { }

  ngOnInit(): void {
    this.statusUpdateInfo = {...this.statusUpdate}
  }

  toggleEditing(){
    if(this.editing){
      this.editing = false;
      this.statusUpdateInfo = {...this.statusUpdate}
    }
    else{
      this.editing = true;
    }
  }

  openStatusUpdateModel(){
    var currentStatus = this.statusUpdate['status'];
    var currentStatusInfo = this.statusUpdate;
    var preventStatusChange = !this.allowEditingStatus;
    var titleText = "Edit Status Update"
    var allowImages = false;
    const config:ModalOptions<StatusModalComponent> = {
      class: 'modal-dialog-centered modal-lg',
      id: 9997,
      initialState: {
        currentStatus,
        currentStatusInfo,
        preventStatusChange,
        titleText,
        allowImages
      },
      keyboard: false,
      backdrop: 'static'
    }
    this.bsModalRef = this.modalService.show(StatusModalComponent, config);

    this.bsModalRef.content.updateStatus.subscribe({
      next: (issueStatusUpdate) => {
        if(issueStatusUpdate){
          var formData = issueStatusUpdate["formData"]
          formData["statusUpdateId"]=this.statusUpdate['id'];

          this.statusUpdateInfo = {...this.statusUpdate, status: formData["newStatus"],  ...formData};

          console.log("Sending: ", formData)
          this.issueReportingService.editStatusUpdate(formData).subscribe({
            next: (result) => {
              if(result){
                console.log("Current: ", {...this.statusUpdate})
                console.log("Result: ", result)
                console.log("Date Conversion", new Date(result['date']))
                this.statusUpdate = result;
                this.toastr.success("Edit successful")
                this.toggleEditing();
              }
            }
          })
        }
      }
    })
  }

}
