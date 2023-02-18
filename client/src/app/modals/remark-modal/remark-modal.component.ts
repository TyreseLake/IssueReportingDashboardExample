import { Component, EventEmitter, Input, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, NgForm, Validators } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-remark-modal',
  templateUrl: './remark-modal.component.html',
  styleUrls: ['./remark-modal.component.css']
})
export class RemarkModalComponent implements OnInit {
  @Input() addRemark = new EventEmitter();
  // @ViewChild('addRemarkForm') remarkForm: NgForm;
  type = "Private"
  data = ""
  bsModalRefConfirm: BsModalRef;

  userRemarkForm: FormGroup;

  constructor(public bsModalRef: BsModalRef,
              private modalService: BsModalService,
              private fb: FormBuilder) { }

  ngOnInit(): void {
    this.initForm();
  }

  initForm(){
    this.userRemarkForm = this.fb.group({
      type: [this.type, Validators.required],
      data: [this.data, Validators.required]
    });
  }

  async addNewRemark() {
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
      //console.log(result)
      if(result == true){
        this.addRemark.emit({remarkType:this.userRemarkForm.controls["type"].value, remarkData:this.userRemarkForm.controls["data"].value});
        // this.addRemark.emit({remarkType:this.type, remarkData:this.data});
        this.bsModalRef.hide();
      }
    })
  }

  cancel() {
    if(this.userRemarkForm.touched){
      const config:ModalOptions<ConfirmationModalComponent> = {
        class: 'modal-dialog-centered modal-sm',
        keyboard: false,
        id: 9994
      }
      this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
      this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
        //console.log(result)
        if(result == true){
          this.bsModalRef.hide();
        }
      })
    }else{
      this.bsModalRef.hide();
    }
  }
}
