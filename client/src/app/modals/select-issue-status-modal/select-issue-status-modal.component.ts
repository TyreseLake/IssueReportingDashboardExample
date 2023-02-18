import { Component, EventEmitter, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { IssueReportsParams } from 'src/app/_models/issueReportsParams';
import { IssueStatus } from 'src/app/_models/issueStatus';
import { Pagination } from 'src/app/_models/pagination';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-select-issue-status-modal',
  templateUrl: './select-issue-status-modal.component.html',
  styleUrls: ['./select-issue-status-modal.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class SelectIssueStatusModalComponent implements OnInit {
  @Input() selection = new EventEmitter();
  issueStatusId: number;
  issueStatuses: IssueStatus[];
  pagination: Pagination;
  issueReportsParams: IssueReportsParams;
  loading : boolean;
  pageNumber = 1;
  selected:number = null;
  bsModalRefConfirm: BsModalRef;
  allowNew = true;
  selectedData: any;

  constructor(public bsModalRef: BsModalRef,
              private modalService: BsModalService,
              public issueReportingService: IssueReportingService,
              public router: Router) { }

  ngOnInit(): void {
    this.loading = true;
    this.issueReportsParams = new IssueReportsParams();
    this.issueReportsParams.pageSize = 8;
    this.issueReportingService.getIssueStatuses(this.issueReportsParams).subscribe({
      next: (response) => {
        this.issueStatuses = response.result,
        this.pagination = response.pagination
        this.loading = false;
      }
    })
  }

  openLink(id:number){
    const url = this.router.serializeUrl(
      this.router.createUrlTree([`/reporting/issue-view/${id}`])
    );
    window.open(url, '_blank');
  }

  selectIssue(){
    var message = "Add issue report(s) to this issue?";
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
      //console.log(result)
      if(result == true){
        this.selection.emit({selected:this.selected, data: this.selectedData});
        this.bsModalRef.hide();
      }
    })

  }

  cancel(){
    this.bsModalRef.hide();
  }

  pageChanged(event){
    if (this.issueReportsParams.pageNumber !== event.page){
      this.issueReportsParams.pageNumber = event.page;
      this.loadReports();
    }
  }

  loadReports() {
    this.loading = true;

    this.issueReportingService.getIssueStatuses(this.issueReportsParams).subscribe({
      next: (response) => {
        this.issueStatuses = response.result;
        this.pagination = response.pagination;
        this.loading = false;
        console.log(this.issueStatuses);
      }
    });
  }

  selectNew() {
    var message = "Move issue report(s) to a new issue?";
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
      //console.log(result)
      if(result == true){
        this.selection.emit({selected:-1});
        this.bsModalRef.hide();
      }
    })
  }

  setSelected(e:number){
    this.selected = e;
    this.selectedData = this.issueStatuses.find( x => x.id == e)
  }
}
