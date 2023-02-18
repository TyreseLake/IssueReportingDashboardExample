import { Component, EventEmitter, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { fadeInStagger } from 'src/app/_animations/animations';
import { IssueReportsParams } from 'src/app/_models/issueReportsParams';
import { IssueStatus } from 'src/app/_models/issueStatus';
import { Pagination } from 'src/app/_models/pagination';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';

@Component({
  selector: 'app-search-issues-modal',
  templateUrl: './search-issues-modal.component.html',
  styleUrls: ['./search-issues-modal.component.css'],
  animations: [
    fadeInStagger
  ]
})
export class SearchIssuesModalComponent implements OnInit {
  @Input() selection = new EventEmitter();
  issueStatuses: IssueStatus[];
  nearByIssueStatuses: IssueStatus[];
  allIssueStatuses: IssueStatus[];
  pagination: Pagination;
  issueReportsParams: IssueReportsParams;
  loading : boolean;
  selected:number = null;
  bsModalRefConfirm: BsModalRef;

  searchInfo: any = null;
  showCriteria = false;

  constructor(public bsModalRef: BsModalRef,
    private modalService: BsModalService,
    public issueReportingService: IssueReportingService,
    private router: Router) { }

    ngOnInit(): void {
      this.loading = true;
      this.issueReportsParams = new IssueReportsParams();
      this.loadReports();
      if(this.searchInfo['locationLat'] && this.searchInfo['locationLng'])
        this.loadReportsWithCoords();
      this.loadAllIssueReports();
    }

    selectIssue(){
      var message = "Add issue to this exisiting report?";
      const config:ModalOptions<ConfirmationModalComponent> = {
        class: 'modal-dialog-centered modal-sm',
        keyboard: false,
        initialState: {
          message,
        },
        id: 9996
      };
      this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
      this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
        //console.log(result)
        if(result == true){
          this.selection.emit({selected:this.selected});
          this.bsModalRef.hide();
        }
      });
    }

    selectNew(){
      var message = "Submit report as new issue?";
      const config:ModalOptions<ConfirmationModalComponent> = {
        class: 'modal-dialog-centered modal-sm',
        keyboard: false,
        initialState: {
          message,
        },
        id: 9996
      };
      this.bsModalRefConfirm = this.modalService.show(ConfirmationModalComponent, config)
      this.bsModalRefConfirm.content.confirmationResult.subscribe(result => {
        //console.log(result)
        if(result == true){
          this.selection.emit({selected:-1});
          this.bsModalRef.hide();
        }
      });
    }

    cancel(){
      this.bsModalRef.hide();
    }

    setSelected(e:number){
      this.selected = e;
    }

    loadReports() {
      this.loading = true;
      this.issueReportingService.searchIssues(this.searchInfo).subscribe({
        next: (result) => {
          this.issueStatuses = result;
          this.loading = false;
          console.log(this.issueStatuses);
        },
        error: () => {
          this.loading = false;
        }
      });
    }

    loadReportsWithCoords() {
      this.loading = true;
      var locationCoords = {
        'IssueType': this.searchInfo['issueType'],
        'LocationLatitude':this.searchInfo['locationLat'],
        'LocationLongitude':this.searchInfo['locationLng']
      };
      this.issueReportingService.searchIssueCoordinates(locationCoords).subscribe({
        next: (result) => {
          this.nearByIssueStatuses = result;
          this.loading = false;
          console.log(this.nearByIssueStatuses);
        },
        error: () => {
          this.loading = false;
        }
      });
    }

    openLink(id:number){
      const url = this.router.serializeUrl(
        this.router.createUrlTree([`/reporting/issue-view/${id}`])
      );
      window.open(url, '_blank');
    }

    pageChanged(event){
      if (this.issueReportsParams.pageNumber!== event.page){
        this.issueReportsParams.pageNumber = event.page;
        this.loadAllIssueReports();
      }
    }

    loadAllIssueReports(){
      this.loading = true;

      console.log(this.issueReportsParams);

      this.issueReportingService.getIssueStatuses(this.issueReportsParams).subscribe({
        next: (response) => {
          this.allIssueStatuses = response.result;
          this.pagination = response.pagination;
          this.loading = false;
        }
      });
    }
}
