import { AfterViewInit, ChangeDetectorRef, Component, EventEmitter, Input, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, NgForm, ValidatorFn, Validators } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { fadeInOutGrow, fadeInStagger } from 'src/app/_animations/animations';
import { DataService } from 'src/app/_services/data.service';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-status-modal',
  templateUrl: './status-modal.component.html',
  styleUrls: ['./status-modal.component.scss'],
  animations: [
    fadeInOutGrow
  ]
})
export class StatusModalComponent implements AfterViewInit {
  @Input() updateStatus = new EventEmitter();
  // @ViewChild('statusUpdateForm') statusUpdateForm: NgForm;
  status: string;
  reviewType = null;
  bsModalRefConfirm: BsModalRef;
  preventStatusChange = false;

  statusForm: FormGroup;

  // currentStatus: string = "Pending Review";
  // statusList: string[] = [];

  currentStatus: string = "New/Pending Review";
  currentStatusInfo: any;
  titleText = "Update Issue Status"
  allowImages = true;

  statusList = ['Under Review',
                'Under Investigation',
                'Pending Approval',
                'In Progress',
                'Paused/On Hold',
                'Cancelled',
                'Not Within Ministry Remit',
                'Completed']

  statusUpdateForm: FormGroup;
  dateUpperLimmit: Date;
  formErrors: any;

  useImages: boolean = false;

  selectedImages: any[];

  constructor(public bsModalRef: BsModalRef,
              private modalService: BsModalService,
              private fb: FormBuilder,
              private dataService: DataService,
              private cdr: ChangeDetectorRef,
              private toastr: ToastrService) {}

  ngAfterViewInit(): void {
    //Get Todays Date
    this.dateUpperLimmit = new Date();

    this.initForm();

    this.cdr.detectChanges();

    // if(this.currentStatusInfo?.['date']){
    //   console.log(this.currentStatusInfo);
    //   console.log(new Date(this.currentStatusInfo["date"]));
    // }

  }

  initForm(){
    if(!this.currentStatusInfo){
      this.statusUpdateForm = this.fb.group({
        newStatus: ["", Validators.required],
        responsibleUnit: ["", Validators.required],
        newUnit: ["", Validators.required],
        date: [this.dateUpperLimmit, Validators.required],
        approvalItems: this.fb.array([]),
        reasonDetails: ["", Validators.required],
        statusUpdateDetails: [""],
        workType: [null, Validators.required]
      })
    }else{
      this.statusUpdateForm = this.fb.group({
        newStatus: [this.currentStatusInfo["status"] ? this.currentStatusInfo["status"] : "", Validators.required],
        responsibleUnit: [this.currentStatusInfo["responsibleUnit"] ? this.currentStatusInfo["responsibleUnit"] : "", Validators.required],
        newUnit: [this.currentStatusInfo["newUnit"] ? this.currentStatusInfo["newUnit"] : "", Validators.required],
        date: [this.dateUpperLimmit ? new Date(this.currentStatusInfo["date"]) : this.dateUpperLimmit, Validators.required],
        approvalItems: this.fb.array(this.currentStatusInfo["approvalItems"] ? this.currentStatusInfo["approvalItems"] : []),
        reasonDetails: [this.currentStatusInfo["reasonDetails"] ? this.currentStatusInfo["reasonDetails"] : "", Validators.required],
        statusUpdateDetails: [this.currentStatusInfo["statusUpdateDetails"] ? this.currentStatusInfo["statusUpdateDetails"] : ""],
        workType: [this.currentStatusInfo["workType"] ? this.currentStatusInfo["workType"] : null, Validators.required]
      })
    }


    this.statusUpdateForm.controls?.['newStatus'].valueChanges.subscribe({
      next: () => {
        this.resetFromStatusChange();
      }
    })
  }

  submitStatusUpdate(){
    var newStatus = this.statusUpdateForm.controls?.['newStatus'].value;
    if(newStatus == ""){
      this.toastr.error("Select a status");
      return;
    }

    if(newStatus == "Under Review" &&
      ( this.statusUpdateForm.controls['responsibleUnit'].value == undefined ||
        this.statusUpdateForm.controls['responsibleUnit'].value?.trim() == "" ||
        this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid)){
      this.statusUpdateForm.markAllAsTouched();
      this.toastr.error("Form contains errors");
      return;
    }
    if(newStatus == "Under Investigation" &&
      ( this.statusUpdateForm.controls['responsibleUnit'].value?.trim() == "" ||
        this.statusUpdateForm.controls['responsibleUnit'].value == undefined ||
        this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid)){
      this.toastr.error("Form contains errors");
      return;
    }
    if(newStatus == "Pending Approval"){
      var approvalItems = this.statusUpdateForm.controls['approvalItems'] as FormArray;
      var emptyApprovalItems = approvalItems.value.filter((f)=>(f== null || f.trim()==''));
      if( this.statusUpdateForm.controls['date'].value == null ||
          this.statusUpdateForm.controls['date'].invalid ||
          approvalItems.controls?.length == 0 ||
          emptyApprovalItems.length > 0){
        approvalItems.markAllAsTouched()
        this.statusUpdateForm.markAllAsTouched();
        this.toastr.error("Form contains errors");
        return;
      }
    }
    if(newStatus == "In Progress" &&
      ( this.statusUpdateForm.controls['responsibleUnit'].value?.trim() == "" ||
        this.statusUpdateForm.controls['responsibleUnit'].value == undefined ||
        this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid ||
        this.statusUpdateForm.controls['workType'].invalid)){
      this.statusUpdateForm.markAllAsTouched();
      this.toastr.error("Form contains errors");
      return;
    }
    if(newStatus == "Paused/On Hold" &&
      ( this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid ||
        this.statusUpdateForm.controls['reasonDetails'].value?.trim() == "" ||
        this.statusUpdateForm.controls['reasonDetails'].value == undefined)){
      this.statusUpdateForm.markAllAsTouched();
      this.toastr.error("Form contains errors");
      return;
    }
    if(newStatus == "Cancelled" &&
      ( this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid ||
        this.statusUpdateForm.controls['reasonDetails'].value?.trim() == "" ||
        this.statusUpdateForm.controls['reasonDetails'].value == undefined)){
      this.statusUpdateForm.markAllAsTouched();
      this.toastr.error("Form contains errors");
      return;
    }
    if(newStatus == "Not Within Ministry Remit" &&
      ( this.statusUpdateForm.controls['newUnit'].value?.trim() == "" ||
        this.statusUpdateForm.controls['newUnit'].value == undefined ||
        this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid ||
        this.statusUpdateForm.controls['reasonDetails'].value?.trim() == "" ||
        this.statusUpdateForm.controls['reasonDetails'].value == undefined)){
      this.statusUpdateForm.markAllAsTouched();
      this.toastr.error("Form contains errors");
      return;
    }
    if(newStatus == "Completed" &&
      ( this.statusUpdateForm.controls['date'].value == null ||
        this.statusUpdateForm.controls['date'].invalid)){
      this.statusUpdateForm.markAllAsTouched();
      this.toastr.error("Form contains errors");
      return;
    }
    this.statusUpdate();
  }

  resetFromStatusChange(){
    if(!this.currentStatusInfo){
      this.statusUpdateForm.controls['responsibleUnit']?.setValue(null);
      this.statusUpdateForm.controls['newUnit']?.setValue(null);
      this.statusUpdateForm.controls['date']?.setValue(this.dateUpperLimmit);
      var formArray = this.statusUpdateForm.controls['approvalItems'] as FormArray;
      formArray.clear();
      this.statusUpdateForm.controls['reasonDetails']?.setValue(null);
      this.statusUpdateForm.controls['statusUpdateDetails']?.setValue(null);
      this.statusUpdateForm.controls['workType']?.setValue(null);
      this.statusUpdateForm.markAsPristine();
      this.statusUpdateForm.markAsUntouched();
      this.selectedImages = null;
      this.useImages = false;
    }else{
      this.statusUpdateForm.controls['responsibleUnit']?.setValue(this.currentStatusInfo["responsibleUnit"]);
      this.statusUpdateForm.controls['newUnit']?.setValue(this.currentStatusInfo["newUnit"]);
      this.statusUpdateForm.controls['date']?.setValue(this.currentStatusInfo["date"] ? new Date(this.currentStatusInfo["date"]) : this.dateUpperLimmit);
      var formArray = this.statusUpdateForm.controls['approvalItems'] as FormArray;
      formArray.clear();
      this.statusUpdateForm.controls['approvalItems'] = this.fb.array(this.currentStatusInfo["approvalItems"] ? this.currentStatusInfo["approvalItems"] : []);
      this.statusUpdateForm.controls['reasonDetails']?.setValue(this.currentStatusInfo["reasonDetails"]);
      this.statusUpdateForm.controls['statusUpdateDetails']?.setValue(this.currentStatusInfo["statusUpdateDetails"]);
      this.statusUpdateForm.controls['workType']?.setValue(this.currentStatusInfo["workType"]);
      this.statusUpdateForm.markAsPristine();
      this.statusUpdateForm.markAsUntouched();
      this.selectedImages = null;
      this.useImages = false;
    }
  }

  statusUpdate() {
    // console.log(this.statusUpdateForm.controls['date']?.value);
    var message = "Save changes?";
    const config:ModalOptions<ConfirmationModalComponent> = {
      class: 'modal-dialog-centered modal-sm',
      keyboard: false,
      initialState: {
        message,
      },
      id: 9996
    }
    this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
    this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
      if(result == true){
        this.updateStatus.emit({formData: this.statusUpdateForm.value, images: this.selectedImages})
        this.bsModalRef.hide();
      }
    });
  }

  cancelUpdate() {
    if(this.statusForm?.touched){
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

  addItem() {
    const formArray: FormArray = this.statusUpdateForm.controls['approvalItems'] as FormArray;
    formArray.push(new FormControl());
  }

  closeItem(position) {
    const formArray: FormArray = this.statusUpdateForm.controls['approvalItems'] as FormArray;
    formArray.controls.splice(position, 1);
  }

  setImages(images: any[]){
    this.selectedImages = images;
  }
}
