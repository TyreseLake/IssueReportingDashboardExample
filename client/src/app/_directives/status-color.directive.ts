import { AfterViewInit, Directive, ElementRef, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';

@Directive({
  selector: '[statusColor]'
})
export class StatusColorDirective implements OnChanges {
  @Input() statusColor: string;

  constructor(private elRef: ElementRef) {
  }
  ngOnChanges(changes: SimpleChanges): void {
    var color;
    // switch (this.statusColor) {
    // case "Pending":
    //   color = "#00897b"
    //   break;
    // case "Under Review":
    //   color = "#00796b"
    //   break;
    // case "Under Investigation":
    //   color = "#00695c"
    //   break;
    // case "Pending Approvals":
    //   color = "#43a047"
    //   break;
    // case "Work in Progress":
    //   color = "#388e3c"
    //   break;
    // case "Completed":
    //   color = "#2e7d32"
    //   break;
    // case "Canceled":
    //   color = "#616161"
    //   break;
    // case "Success":
    //   color = "#4cdc6e"
    //   break;
    // case "Error":
    //   color = "#ffdd00"
    //   break;
    // case "Failure":
    //   color = "#eb6060"
    //   break;
    // case "Processing":
    //   color = "#a8a8a8"
    //   break;
    // default:
    //   color = "#424242"
    //   break;
    // }
    switch (this.statusColor) {
      case "New/Pending Review":
        color = "#6991A7"
        break;
      case "Under Review":
        color = "#619FFC"
        break;
      case "Under Investigation":
        color = "#A777C9"
        break;
      case "Pending Approval":
        color = "#389688"
        break;
      case "In Progress":
        color = "#80d12e"
        break;
      case "Paused/On Hold":
        color = "#f7b708"
        break;
      case "Cancelled":
        color = "#F2451C"
        break;
      case "Not Within Ministry Remit":
        color = "#F49025"
        break;
      case "Completed":
        color = "#43BF57"
        break;
      case "Success":
        color = "#43BF57"
        break;
      case "Error":
        color = "#f7b708"
        break;
      case "Failure":
        color = "#F2451C"
        break;
      case "Processing":
        color = "#a8a8a8"
        break;
      default:
        color = "#424242"
        break;
      }
    // console.log(color);
    this.elRef?.nativeElement?.setAttribute('style', 'background-color: ' + color)
  }

}
