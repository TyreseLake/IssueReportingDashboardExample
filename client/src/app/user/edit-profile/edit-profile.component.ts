import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { ConfirmationModalComponent } from 'src/app/modals/confirmation-modal/confirmation-modal.component';
import { UserDetails } from 'src/app/_models/userDetails';
import { AccountService } from 'src/app/_services/account.service';

@Component({
  selector: 'app-edit-profile',
  templateUrl: './edit-profile.component.html',
  styleUrls: ['./edit-profile.component.css']
})
export class EditProfileComponent implements OnInit {
  userDetails : UserDetails;
  userDetailsForm: FormGroup;
  initialForm:any;
  resetPasswordForm: FormGroup;
  formChanged = false;
  updatingProfile = false;
  bsModalRefConfirm: BsModalRef;

  @ViewChild('passwordField', { static: true }) passwordField: ElementRef;

  constructor(private route: ActivatedRoute,
              private fb: FormBuilder,
              private modalService: BsModalService,
              private accountService: AccountService,
              private toastrService: ToastrService,
              private activateRoute: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.data.subscribe(data => {
      // console.log(data["userDetails"]);
      this.userDetails = data["userDetails"];
    });

    this.initializeForm();
    this.initializePasswordForm();

    this.route.queryParams.subscribe(params => {
      if(params['loc'] == 'password'){
        this.passwordField.nativeElement.focus();
      }
    })
  }

  initializeForm() {
    this.userDetailsForm = this.fb.group({
      firstName: [this.userDetails['firstName'], Validators.required],
      lastName: [this.userDetails['lastName']],
      email: [this.userDetails['email'], [Validators.email]],
      accountType: [this.userDetails['accountType'], Validators.required],
    });

    // this.userDetailsForm = this.fb.group({
    //   userName: [this.userDetails['userName'], Validators.required],
    //   firstName: [this.userDetails['firstName'], Validators.required],
    //   lastName: [this.userDetails['lastName']],
    //   email: [this.userDetails['email'], [Validators.email]],
    //   accountType: [this.userDetails['accountType'], Validators.required],
    // });

    this.initialForm = this.userDetailsForm.value;

    this.userDetailsForm.valueChanges.subscribe({
      next: () => {
        this.checkFormChanged();
      }
    })
  }

  initializePasswordForm() {
    this.resetPasswordForm = this.fb.group({
      currentPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(255)]],
      newPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(255)]],
      confirmPassword: ['', [Validators.required, this.matchValues('newPassword')]]
    })

    this.resetPasswordForm.controls["newPassword"].valueChanges.subscribe(() => {
      this.resetPasswordForm.controls["confirmPassword"].updateValueAndValidity();
    });
  }

  matchValues(matchTo: string): ValidatorFn {
    return (control: AbstractControl) => {
      return control?.value === control?.parent?.controls[matchTo].value ? null : { notMatching: true }
    }
  }

  checkFormChanged() {
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
          var userDetails = this.userDetailsForm.value
          this.accountService.updateProfile(userDetails).subscribe({
            next: (result) => {
              this.updatingProfile = true;
              this.initialForm = userDetails;
              this.checkFormChanged();
              this.toastrService.success("Your profile updated successfully");
              this.accountService.currentUser$.pipe(take(1)).subscribe({
                next: (user) => {
                  user.firstName = result['firstName']
                  user.lastName = result['lastName']
                  this.accountService.setCurrentUser(user);
                  this.updatingProfile = false;
                },
                error: () => {
                  this.updatingProfile = false;
                }
              }).unsubscribe();
            }
          })
        }
      },
      error: () => {
        this.updatingProfile = false;
      }
    })
  }

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
        var passwordDetails = {"currentPassword":this.resetPasswordForm.controls["currentPassword"].value, "newPassword":this.resetPasswordForm.controls["newPassword"].value}
        this.accountService.changePassword(passwordDetails).subscribe({
          next: (result) => {
            this.toastrService.success(result['result']);
            this.resetPasswordForm.reset();
            this.accountService.currentUser$.pipe(take(1)).subscribe({
              next: (user) => {
                user.requirePasswordReset = false;
                this.accountService.setCurrentUser(user);
                this.updatingProfile = false;
              },
              error: () => {
                this.updatingProfile = false;
              }
            }).unsubscribe();
          },
          error: () => {
            this.updatingProfile = false;
          }
        })
      }
    })
  }
}
