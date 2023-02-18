import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Observable, of } from 'rxjs';
import { fadeInOut } from 'src/app/_animations/animations';
import { AdminService } from 'src/app/_services/admin.service';
import { DataService } from 'src/app/_services/data.service';

@Component({
  selector: 'app-create-user',
  templateUrl: './create-user.component.html',
  styleUrls: ['./create-user.component.css'],
  animations: [
    fadeInOut,
    trigger('fadeAnim', [
      transition('void => *', [
        style({ opacity: 0, transform: 'translateY(-5%)' }),
        animate('100ms ease-in', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
      transition('* => void', [
        animate('100ms ease-out', style({ opacity: 0, transform: 'translateY(-5%)' }))
      ])
    ])
  ]
})
export class CreateUserComponent implements OnInit {
  userCreateForm: FormGroup;
  issueTypes: string[] = [];
  districts: string[] = [];
  accountTypes: string[] = [];
  showExtra: boolean;
  validationErrors = [];

  issueTypeSearchKey: string;
  districtSearchKey: string;

  showIssueTypes: boolean;
  showDistricts: boolean;

  rolesWithRestrict: string[] = []

  constructor(private fb: FormBuilder,
              private dataService: DataService,
              private adminService: AdminService,
              private router: Router,
              private toastr: ToastrService) { }

  ngOnInit(): void {
    this.dataService.getDistricts(true).subscribe((districts) => {
      this.districts = districts;
    });

    this.dataService.getIssueTypes(true).subscribe((issueTypes) => {
      this.issueTypes = issueTypes;
    });

    this.dataService.getRoles().subscribe({
      next: (roles) => {
        this.accountTypes = roles;
      }
    })

    this.dataService.getRoles("restrict").subscribe({
      next: (roles) => {
        this.rolesWithRestrict = roles;
      },
      complete: () => {
        this.checkForRestrict()
      }
    })

    this.initializeForm();
  }

  initializeForm() {
    this.userCreateForm = this.fb.group({
      userName: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(255)]],
      firstName: ['', Validators.required],
      lastName: [''],
      email: ['', [Validators.email]],
      accountType: ['', Validators.required],
      issueReportAccessPermissions: this.fb.array([]),
      districtAccessPermissions: this.fb.array([])
    });

    this.userCreateForm.controls['accountType'].valueChanges.subscribe({
      next: async () => {
        this.checkForRestrict();
        if(!this.showExtra){
          this.resetFormArray(this.userCreateForm, 'issueReportAccessPermissions');
          this.resetFormArray(this.userCreateForm, 'districtAccessPermissions');
        }
      }
    });
  }

  onCheckboxChange(itemValue: string, isChecked: boolean, formArrayName: string) {
    const formArray: FormArray = this.userCreateForm.controls[formArrayName] as FormArray;
    if(!formArray.touched)
      formArray.markAsTouched();
    if (isChecked) {
      formArray.push(new FormControl(itemValue));
    } else {
      const index = formArray.controls.findIndex(x => x.value === itemValue);
      formArray.removeAt(index);
    }
  }

  onAccountTypeSelectChange(e) {
    this.showExtra = this.rolesWithRestrict.includes(e.target.value);
  }

  checkForRestrict() {
    this.showExtra = this.rolesWithRestrict.includes(this.userCreateForm.controls["accountType"].value)
  }

  submitForm() {
    // if(this.userCreateForm.controls["accountType"].value == 'Default' || this.userCreateForm.controls['accountType'].value == 'Issue Manager'){
    //   if(this.userCreateForm.controls["issueReportAccessPermissions"].value.length == 0 || this.userCreateForm.controls["districtAccessPermissions"].value.length == 0 ){
    //     this.userCreateForm.markAllAsTouched();
    //     this.toastr.error("This form contains errors")
    //     return;
    //   }
    // }
    // if(this.userCreateForm.invalid){
    //   this.userCreateForm.markAllAsTouched();
    //   this.toastr.error("This form contains errors")
    //   return;
    // }
    console.log(this.userCreateForm.value);
    if(this.userCreateForm.valid){
      // console.log(this.userCreateForm.value);
      this.adminService.createUser(this.userCreateForm.value).subscribe({
        next: (response) => {
          this.userCreateForm.reset();
          this.userCreateForm.controls['accountType'].setValue('Default');
          this.toastr.success("User successfully created");
          this.resetForm();
          console.log(response);
          this.router.navigate(['admin/user-manage/' + response['id']]);
        },
        error: (error) => {
          this.validationErrors = error;
          this.toastr.error("This form contains errors")
          this.userCreateForm.markAllAsTouched();
        }
      })
    }
    else{
      this.toastr.error("This form contains errors")
      this.userCreateForm.markAllAsTouched();
    }
  }

  cancel() {
    this.resetForm();
  }

  resetForm() {
    this.userCreateForm.controls['userName'].setValue('');
    this.userCreateForm.controls['password'].setValue('');
    this.userCreateForm.controls['firstName'].setValue('');
    this.userCreateForm.controls['lastName'].setValue('');
    this.userCreateForm.controls['email'].setValue('');
    this.userCreateForm.controls['accountType'].setValue('Default');

    this.validationErrors = [];

    // this.userCreateForm.controls['userName'].setValue('jim');
    // this.userCreateForm.controls['password'].setValue('Jim123!');
    // this.userCreateForm.controls['firstName'].setValue('Jim');
    // this.userCreateForm.controls['lastName'].setValue('Johnson');
    // this.userCreateForm.controls['email'].setValue('jim@mail.com');
    // this.userCreateForm.controls['accountType'].setValue('Default');

    var checkBoxes = document.getElementsByClassName("form-check-input");
    for (let index = 0; index < checkBoxes.length; index++) {
      checkBoxes.item(index)["checked"] = false;
    }

    this.resetFormArray(this.userCreateForm, 'issueReportAccessPermissions');
    this.resetFormArray(this.userCreateForm, 'districtAccessPermissions');
  }

  resetFormArray(formGroup: FormGroup, formArrayName: string){
    const formArray: FormArray = formGroup.controls[formArrayName] as FormArray;
    while(formArray.value.length > 0){
      formArray.removeAt(0);
    }
    formArray.markAsUntouched();
  }
}
