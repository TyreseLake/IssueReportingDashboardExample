import { Injectable } from '@angular/core';
import { NgxSpinnerService } from 'ngx-spinner';

@Injectable({
  providedIn: 'root'
})
export class BusyService {
  busyRequestCount = 0;

  constructor(private spinnerService: NgxSpinnerService) { }

  busy () {
    this.busyRequestCount++;
    this.spinnerService.show(undefined, {
      type: 'line-scale-pulse-out',
      bdColor: 'rgba(0,0,0,0.0)',
      size: 'medium',
      color: 'rgba(200, 200, 200, 0.6)'
    })
  }

  idle () {
    this.busyRequestCount--;
    if (this.busyRequestCount <= 0){
      this.busyRequestCount = 0;
      this.spinnerService.hide();
    }
  }
}
