import { Component, EventEmitter, Input, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { Pagination } from 'src/app/_models/pagination';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-description-modal',
  templateUrl: './description-modal.component.html',
  styleUrls: ['./description-modal.component.css']
})
export class DescriptionModalComponent implements OnInit {
  @Input() updateDescription = new EventEmitter();

  id: number;
  description: string;
  descriptionList: string[] = [];

  bsModalRefConfirm: BsModalRef;

  descriptionForm: FormGroup;

  loading = false;
  pageNumber = 1;
  pageSize = 6;
  pagination: Pagination;

  dropOpen = false;

  constructor(public bsModalRef: BsModalRef,
              private modalService: BsModalService,
              private fb: FormBuilder,
              private issueReportingService: IssueReportingService) { }

  ngOnInit(): void {
    this.initForm();
    this.loadDescriptions();
  }

  loadDescriptions() {
    this.loading = true;
    this.issueReportingService.getIssueDescriptions(this.id, this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        this.descriptionList = response.result;
        this.pagination = response.pagination;
        this.loading = false;
      }
    })
  }

  pageChanged(event: any){
    if (this.pageNumber !== event.page){
      this.pageNumber = event.page;
      this.loadDescriptions();
    }
  }

  initForm() {
    this.descriptionForm = this.fb.group({
      description: [this.description, Validators.required]
    })
  }

  updateIssueDescription() {
    var message = "Save changes?";
    const config:ModalOptions<ConfirmationModalComponent> = {
      class: 'modal-dialog-centered modal-sm',
      initialState: {
        message,
      },
      keyboard: false,
      id: 9996
    }
    this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
    this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
      if(result == true){
        this.updateDescription.emit({description:this.descriptionForm.controls['description'].value});
        this.bsModalRef.hide();
      }
    })
  }

  cancel() {
    if(this.descriptionForm.touched){
      const config:ModalOptions<ConfirmationModalComponent> = {
        class: 'modal-dialog-centered modal-sm',
        keyboard: false,
        id: 9994
      }
      this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
      this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
        if(result == true){
          this.bsModalRef.hide();
        }
      })
    }else{
      this.bsModalRef.hide();
    }
  }
}
