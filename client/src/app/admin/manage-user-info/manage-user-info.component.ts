import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { ConfirmationModalComponent } from 'src/app/modals/confirmation-modal/confirmation-modal.component';
import { SelectionModalComponent } from 'src/app/modals/selection-modal/selection-modal.component';
import { fadeInStagger } from 'src/app/_animations/animations';
import { UserDetails } from 'src/app/_models/userDetails';
import { AccountService } from 'src/app/_services/account.service';
import { AdminService } from 'src/app/_services/admin.service';
import { DataService } from 'src/app/_services/data.service';

@Component({
  selector: 'app-manage-user-info',
  templateUrl: './manage-user-info.component.html',
  styleUrls: ['./manage-user-info.component.css'],
  animations: [
    fadeInStagger
  ]
})
export class ManageUserInfoComponent implements OnInit {
  userDetails : UserDetails;
  userDetailsForm: FormGroup;
  initialForm:any;
  accountTypes: string[] = [];

  bsModalRef: BsModalRef;

  issueTypes: string[];
  districts: string[];

  resetPasswordForm: FormGroup;

  formChanged = false;
  updatingProfile = false;

  issueTypesValid = true;
  districtsValid = true;

  bsModalRefConfirm: BsModalRef;

  allIssueTypes:boolean;
  allDistricts:boolean;


  constructor(private route: ActivatedRoute,
    private fb: FormBuilder,
    public accountService: AccountService,
    private modalService: BsModalService,
    private dataService: DataService,
    private adminService: AdminService,
    private toastrService: ToastrService) { }

  ngOnInit(): void {
    this.route.data.subscribe(data => {
      this.userDetails = data["userDetails"];
      this.initializeForm();
      this.initializePasswordForm();
      this.allIssueTypes = this.userDetails['issueTypesAccess'].length == 0;
      this.allDistricts = this.userDetails['districtsAccess'].length == 0;
    })

    this.dataService.getIssueTypes(true).subscribe({
      next: (result) => {
        this.issueTypes = result;
      }
    })

    this.dataService.getDistricts(true).subscribe({
      next: (result) => {
        this.districts = result;
      }
    })

    this.dataService.getRoles().subscribe({
      next: (result) => {
        this.accountTypes = result;
      }
    })
  }

  initializeForm() {
    this.userDetailsForm = this.fb.group({
      userName: [this.userDetails['userName'], Validators.required],
      firstName: [this.userDetails['firstName'], Validators.required],
      lastName: [this.userDetails['lastName']],
      email: [this.userDetails['email'], [Validators.email]],
      accountType: [this.userDetails['accountType'], Validators.required],
      issueTypesAccess: [this.userDetails['issueTypesAccess'].sort()],
      districtsAccess: [this.userDetails['districtsAccess'].sort()]
    });

    this.initialForm = this.userDetailsForm.value;

    this.userDetailsForm.valueChanges.subscribe({
      next: () => {
        this.checkFormChanged();
      }
    })
  }

  initializePasswordForm() {
    this.resetPasswordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(255)]],
      confirmPassword: ['', [Validators.required, this.matchValues('password')]]
    })
    this.resetPasswordForm.controls["password"].valueChanges.subscribe(() => {
      // console.log(this.resetPasswordForm.controls["password"].value);
      // console.log(this.resetPasswordForm.controls['confirmPassword'].value);
      this.resetPasswordForm.controls["confirmPassword"].updateValueAndValidity();
    });
  }

  matchValues(matchTo: string): ValidatorFn {
    return (control: AbstractControl) => {
      return control?.value === control?.parent?.controls[matchTo].value ? null : { notMatching: true }
    }
  }

  checkFormChanged() {
    // console.log(JSON.stringify(this.initialForm));
    // console.log(JSON.stringify(this.userDetailsForm.value));
    if(JSON.stringify(this.initialForm) == JSON.stringify(this.userDetailsForm.value)){
      this.formChanged = false;
      return;
    }
    this.formChanged = true;
  }

  saveUserDetails() {
    const config:ModalOptions<ConfirmationModalComponent> = {
      class: 'modal-dialog-centered modal-sm',
      keyboard: false,
      id: 9995
    }

    this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
    this.bsModalRefConfirm.content.confirmationResult.subscribe({
      next: (result) => {
        if(result == true){
          this.updatingProfile = true;
          // var userDetails = this.userDetailsForm.value;
          var userDetails = {...this.userDetailsForm.value};
          // console.log(this.userDetailsForm.value);
          userDetails.id = this.userDetails.id;
          this.adminService.updateUserDetails(userDetails).subscribe({
            next: (result) => {
              this.updatingProfile = true;
              // console.log(result);
              // console.log(userDetails);
              // this.initialForm = userDetails;
              this.initialForm = this.userDetailsForm.value;
              // console.log(this.userDetailsForm.value);
              // this.checkFormChanged();
              this.formChanged = false;
              this.toastrService.success("User profile updated successfully");
              this.allIssueTypes = this.userDetailsForm?.controls['issueTypesAccess'].value.length == 0;
              this.allDistricts = this.userDetailsForm?.controls['districtsAccess'].value.length == 0;
              this.updatingProfile = false;
            },
            error: (error) => {
              this.updatingProfile = false;
            }
          })
        }
      },
      error: () => {
        this.updatingProfile = false;
      }
    })
  }

  openIssueTypeModal() {
    const config:ModalOptions<SelectionModalComponent> = {
      class: 'modal-dialog-centered',
      id: 9997,
      initialState: {
        elements: this.issueTypes,
        default: this.userDetailsForm.controls['issueTypesAccess'].value
      }
    }

    this.bsModalRef = this.modalService.show(SelectionModalComponent, config);

    this.bsModalRef.content.selection.subscribe((result:string[]) => {
      // if(result.length > 0)
      // {
      //   this.userDetailsForm.controls['issueTypesAccess'].setValue(result);
      //   this.userDetailsForm.controls['issueTypesAccess'].markAsDirty();
      //   this.checkIssueTypesValid();
      // }
      this.userDetailsForm.controls['issueTypesAccess'].setValue(result);
      this.userDetailsForm.controls['issueTypesAccess'].markAsDirty();
      // this.checkIssueTypesValid();
    })
  }

  openDistrictModal() {
    const config:ModalOptions<SelectionModalComponent> = {
      class: 'modal-dialog-centered',
      id: 9997,
      initialState: {
        elements: this.districts,
        default: this.userDetailsForm.controls['districtsAccess'].value
      }
    }

    this.bsModalRef = this.modalService.show(SelectionModalComponent, config);

    this.bsModalRef.content.selection.subscribe((result:string[]) => {
      // if(result.length > 0)
      // {
      //   this.userDetailsForm.controls['districtsAccess'].setValue(result);
      //   this.userDetailsForm.controls['districtsAccess'].markAsDirty();
      //   this.checkDistrictsValid();
      // }
        this.userDetailsForm.controls['districtsAccess'].setValue(result);
        this.userDetailsForm.controls['districtsAccess'].markAsDirty();
        // this.checkDistrictsValid();
      })
  }

  // checkAccountType(){
  //   var empty: string[] = []
  //   if (['Default', 'Issue Manager'].includes(this.userDetailsForm.controls['accountType'].value)){
  //     if(this.userDetails['issueTypesAccess'].length > 0){
  //       this.userDetailsForm.controls['issueTypesAccess'].setValue(this.userDetails['issueTypesAccess']);
  //       this.issueTypesValid = true;
  //     }
  //     else{
  //       this.userDetailsForm.controls['issueTypesAccess'].setValue(empty);
  //       this.issueTypesValid = false;
  //     }

  //     if(this.userDetails['districtsAccess'].length > 0){
  //       this.userDetailsForm.controls['districtsAccess'].setValue(this.userDetails['districtsAccess']);
  //       this.districtsValid = true;
  //     }
  //     else{
  //       this.userDetailsForm.controls['districtsAccess'].setValue(empty);
  //       this.districtsValid = false;
  //     }
  //   }
  //   else
  //   {
  //     this.userDetailsForm.controls['issueTypesAccess'].setValue(empty);
  //     this.userDetailsForm.controls['districtsAccess'].setValue(empty);

  //     this.issueTypesValid = true;
  //     this.districtsValid = true;
  //   }
  // }

  resetIssueTypes(){
    var empty: string[] = [];
    if(this.userDetails['issueTypesAccess'].length > 0){
      this.userDetailsForm.controls['issueTypesAccess'].setValue(this.userDetails['issueTypesAccess']);
    }
    else{
      this.userDetailsForm.controls['issueTypesAccess'].setValue(empty);
    }
    // console.log("resetting -");
    // console.log("updating " + this.updatingProfile);
    // console.log("changed " + this.formChanged)
    // console.log("invalid " + this.userDetailsForm.invalid);
  }

  clearIssueTypes(){
    var empty: string[] = [];
    this.userDetailsForm.controls['issueTypesAccess'].setValue(empty);
    // console.log("clearing -");
    // console.log("updating " + this.updatingProfile);
    // console.log("changed " + this.formChanged)
    // console.log("invalid " + this.userDetailsForm.invalid);
  }

  resetDistricts(){
    var empty: string[] = [];
    if(this.userDetails['districtsAccess'].length > 0){
      this.userDetailsForm.controls['districtsAccess'].setValue(this.userDetails['districtsAccess']);
    }
    else{
      this.userDetailsForm.controls['districtsAccess'].setValue(empty);
    }
  }

  clearDistricts(){
    var empty: string[] = [];
    this.userDetailsForm.controls['districtsAccess'].setValue(empty);
  }

  // checkIssueTypesValid(){
  //   if(['Default', 'Issue Manager'].includes(this.userDetailsForm.controls['accountType'].value)){
  //     this.issueTypesValid = this.userDetailsForm.controls['issueTypesAccess'].value != [];
  //     return;
  //   }
  //   this.issueTypesValid = true;
  // }

  // checkDistrictsValid(){
  //   if(['Default', 'Issue Manager'].includes(this.userDetailsForm.controls['accountType'].value)){
  //     this.districtsValid = this.userDetailsForm.controls['districtsAccess'].value != [];
  //     return;
  //   }
  //   this.districtsValid = true;
  // }

  resetPassword(){
    const config:ModalOptions<ConfirmationModalComponent> = {
      class: 'modal-dialog-centered modal-sm',
      keyboard: false,
      id: 9995
    }

    this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config);
    this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
      if(result == true){
        this.updatingProfile = true;
        var body = {};
        body['newPassword'] = this.resetPasswordForm.controls["password"].value;
        body['id'] = this.userDetails.id;
        this.adminService.updateUserPassword(body).subscribe({
          next: (result) => {
            this.toastrService.success(result['result']);
            this.resetPasswordForm.reset();
            this.updatingProfile = false;
          },
          error: () => {
            this.updatingProfile = false;
          }
        })
      }
    })
  }
}
