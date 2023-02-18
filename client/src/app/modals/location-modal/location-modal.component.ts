import { AfterViewInit, ChangeDetectorRef, Component, EventEmitter, Input, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, NgForm, Validators } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { Pagination } from 'src/app/_models/pagination';
import { DataService } from 'src/app/_services/data.service';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-location-modal',
  templateUrl: './location-modal.component.html',
  styleUrls: ['./location-modal.component.css']
})
export class LocationModalComponent implements AfterViewInit  {
  @Input() updateStatus = new EventEmitter();
  // @ViewChild('updateStatusForm') updateStatusForm: NgForm;

  district: string;
  locationDescription: string;

  id: number;
  locationList: string[] = [];
  loading = false;
  pageNumber = 1;
  pageSize = 6;
  pagination: Pagination;
  dropOpen = false;

  updatedDistrict: string = null;
  updatedLocation: string = null;

  districts: string[];

  bsModalRefConfirm: BsModalRef;

  statusForm: FormGroup;

  constructor(public bsModalRef: BsModalRef,
              private modalService: BsModalService,
              private dataService:DataService,
              private fb: FormBuilder,
              private cdr: ChangeDetectorRef,
              private issueReportingService: IssueReportingService) { }

  ngAfterViewInit(): void {
    this.dataService.getDistricts(true).subscribe({
      next: (result) => {
        this.districts = result;
        this.initForm();
        this.cdr.detectChanges();
        this.loadLocations();
      }
    });
  }

  pageChanged(event: any){
    if (this.pageNumber !== event.page){
      this.pageNumber = event.page;
      this.loadLocations();
    }
  }

  loadLocations() {
    this.loading = true;
    this.issueReportingService.getIssueLocations(this.id, this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        this.locationList = response.result;
        this.pagination = response.pagination;
        this.loading = false;
      }
    })
  }

  initForm() {
    this.statusForm = this.fb.group({
      district: [this.district],
      locationDescription: [this.locationDescription, Validators.required]
    })
  }

  updateIssueStatus() {
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
        this.updateStatus.emit({district:this.statusForm.controls['district'].value, locationDescription:this.statusForm.controls['locationDescription'].value});
        this.bsModalRef.hide();
      }
    })
  }

  cancel() {
    if(this.statusForm.touched){
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
