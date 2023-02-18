import { Component, EventEmitter, Input, OnInit } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-confirmation-modal',
  templateUrl: './confirmation-modal.component.html',
  styleUrls: ['./confirmation-modal.component.css']
})
export class ConfirmationModalComponent implements OnInit {
  @Input() confirmationResult = new EventEmitter();
  message = "Are you sure?";

  constructor(public bsModalRef: BsModalRef) {}

  ngOnInit(): void {
  }

  confirm(result:boolean) {
    this.confirmationResult.emit(result);
    this.bsModalRef.hide();
  }
}
