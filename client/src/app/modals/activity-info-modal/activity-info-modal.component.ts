import { Time } from '@angular/common';
import { Component, EventEmitter, Input, OnInit } from '@angular/core';
import { BsModalRef } from 'ngx-bootstrap/modal';

@Component({
  selector: 'app-activity-info-modal',
  templateUrl: './activity-info-modal.component.html',
  styleUrls: ['./activity-info-modal.component.css']
})
export class ActivityInfoModalComponent implements OnInit {
  @Input() activity = new EventEmitter();

  id: number;
  accountType: string;
  action: string;
  actionGroup: string;
  activityDate: Date;
  completionDate: Date;
  data: string;
  dataObject: any;
  email: string;
  ipAddress: string;
  response: string;
  responseObject: any;
  duration: {
    hours: number,
    minutes: number,
    seconds: number
  };
  status: string;
  userName: string;

  constructor(public bsModalRef: BsModalRef) {
  }

  ngOnInit(): void {
    if(this.data){
      try {
        this.dataObject = JSON.parse(this.data)
      } catch (error) {
        this.dataObject = "none";
      }
    }

    if(this.response){
      try {
        this.responseObject = JSON.parse(this.response)
      } catch (error) {
        this.responseObject = "none";
      }
    }

    if(this.activityDate && this.completionDate){
      var start = new Date(this.activityDate);
      var end = new Date(this.completionDate);
      var sec = end.getUTCSeconds() - start.getUTCSeconds();
      // var sec = (this.activityDate?.getSeconds() - this.completionDate?.getSeconds());
      var hours = Math.floor(sec / 3600);
      var minutes = Math.floor((sec - (hours * 3600)) / 60);
      var seconds = sec - (hours * 3600) - (minutes * 60);
      this.duration = {
        hours,
        minutes,
        seconds
      }
    }
  }

  cancel() {
    this.bsModalRef.hide();
  }
}
