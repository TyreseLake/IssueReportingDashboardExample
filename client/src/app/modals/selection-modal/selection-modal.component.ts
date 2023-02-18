import { Component, EventEmitter, Input, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-selection-modal',
  templateUrl: './selection-modal.component.html',
  styleUrls: ['./selection-modal.component.css']
})
export class SelectionModalComponent implements OnInit {
  @Input() selection = new EventEmitter();
  @ViewChild('selectionForm') selectionForm: NgForm;

  bsModalRefConfirm: BsModalRef;

  title="Make Selection"
  elements:string[];
  default:string[];
  selected = {};

  errorMessage:string;


  constructor(public bsModalRef: BsModalRef,
              private modalService: BsModalService) { }

  ngOnInit(): void {
    this.elements.forEach(ele => {
      this.selected[ele] = false;
    });
    this.default.forEach(ele => {
      this.selected[ele] = true;
    })
  }

  confirm() {

    const config:ModalOptions<ConfirmationModalComponent> = {
      class: 'modal-dialog-centered modal-sm',
      keyboard: false,
      id: 9995
    }

    var selectedOptions = []

    for(var i in this.selected){
      if(this.selected[i])
      selectedOptions.push(i);
    }

    var defaultOptions = this.default.sort();

    var unchanged = (selectedOptions.length == this.default.length && selectedOptions.sort().every((val, index) => val === defaultOptions[index]));

    if(!unchanged){
      if(selectedOptions.length > 0){
        this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
        this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
          if(result == true){
            this.selection.emit(selectedOptions);
            this.bsModalRef.hide();
          }
        })
      }
      else{
        this.errorMessage ="* Must select at least (1)";
      }
    }
    else{
      this.bsModalRef.hide();
    }
  }

  cancel() {
    if(this.selectionForm.touched){
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
