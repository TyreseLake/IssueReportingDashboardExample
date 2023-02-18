import { animate, style, transition, trigger } from '@angular/animations';
import { Component, OnInit, ViewChild, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, NgForm, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { BsDatepickerConfig } from 'ngx-bootstrap/datepicker';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { fadeInStagger, fadeInOut } from 'src/app/_animations/animations';
import { IssueReportsParams } from 'src/app/_models/issueReportsParams';
import { Params } from 'src/app/_models/user';
import { IssueStatus } from 'src/app/_models/issueStatus';
import { Pagination } from 'src/app/_models/pagination';
import { AccountService } from 'src/app/_services/account.service';
import { DataService } from 'src/app/_services/data.service';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';

@Component({
  selector: 'app-view-reports',
  templateUrl: './view-reports.component.html',
  styleUrls: ['./view-reports.component.scss'],
  encapsulation: ViewEncapsulation.None,
  animations: [
    fadeInOut,
    fadeInStagger
  ]
})
export class ViewReportsComponent implements OnInit {
  issueStatuses: IssueStatus[];
  issueReportsParams: IssueReportsParams;
  pagination: Pagination;

  loading = false;
  bsConfig!: Partial<BsDatepickerConfig>;
  maxDate: Date;
  issueTypes: string[];
  districts: string[];
  statusList: string[];

  selected: Set<number> = new Set<number>();

  selectedFilters = {};

  selectedIssueTypesSet: Set<string> = new Set<string>();
  selectedDistrictsSet: Set<string> = new Set<string>();
  selectedStatusesSet: Set<string> = new Set<string>();
  @ViewChild('filtersForm') filtersForm!: NgForm;

  limit: number;

  searchIssueTypes: string;
  searchDistricts: string;
  searchStatus: string;

  unpin: Set<number> = new Set<number>();
  hidden: Set<number> = new Set<number>();

  canReset: boolean;

  constructor(private issueReportingService : IssueReportingService,
      private dataService: DataService,
      private router: Router,
      private toastr: ToastrService,
      public accountService: AccountService) { }

  ngOnInit(): void {
    this.issueReportsParams = new IssueReportsParams();

    var userParams;
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: (user) => {
        userParams = user?.params;
        // console.log(user);
        if(userParams != null)
        {
          this.issueReportsParams.districtAccess = userParams?.districtAccess;
          this.issueReportsParams.issueTypeAccess = userParams?.issueTypeAccess;
          this.issueReportsParams.order = userParams?.order;
          this.issueReportsParams.pinnedOnTop = userParams?.pinnedOnTop;
          this.issueReportsParams.showClosed = userParams?.showClosed;
          this.issueReportsParams.showHidden = userParams?.showHidden;
          this.issueReportsParams.sortBy = userParams?.sortBy;
          this.issueReportsParams.status = userParams?.statusList;
        }
        this.checkReset();
        // console.log(this.issueReportsParams);
        this.loadReports();
      }
    }).unsubscribe();

    this.bsConfig = {
      isAnimated: true,
      containerClass: 'theme-green',
      dateInputFormat: 'DD MMMM YYYY'
    }

    this.maxDate = new Date();
    this.maxDate.getDate();

    this.limit = Math.floor(window.innerWidth/30);

    this.dataService.getIssueTypes(true).subscribe({
      next: (result) => {
        this.issueTypes = result;
      }
    });

    this.dataService.getDistricts(true).subscribe({
      next: (result) => {
        this.districts = result;
      }
    })

    this.dataService.getStatuses().subscribe({
      next: (result) => {
        this.statusList = result;
      }
    })
  }

  //Check if the filter/search parameters can be resetted
  checkReset() {
    var emptyParams = new IssueReportsParams();
    this.canReset =  JSON.stringify(this.issueReportsParams) != JSON.stringify(emptyParams);
  }

  loadReports() {
    this.loading = true;

    this.issueReportingService.getIssueStatuses(this.issueReportsParams).subscribe({
      next: (response) => {
        this.issueStatuses = response.result;
        this.pagination = response.pagination;
        this.loading = false;

        this.accountService.currentUser$.pipe(take(1)).subscribe({
          next: (user) => {
            var userParams = user.params;
            if(!userParams){
              userParams = new Params();
            }
            userParams.districtAccess = this.issueReportsParams?.districtAccess;
            userParams.issueTypeAccess = this.issueReportsParams?.issueTypeAccess;
            userParams.order = this.issueReportsParams?.order;
            userParams.pinnedOnTop = this.issueReportsParams?.pinnedOnTop;
            userParams.showClosed = this.issueReportsParams?.showClosed;
            userParams.showHidden = this.issueReportsParams?.showHidden;
            userParams.sortBy = this.issueReportsParams?.sortBy;
            userParams.statusList = this.issueReportsParams?.status;
            this.accountService.setCurrentUser(user);
          }
        }).unsubscribe();

        if(this.issueReportsParams?.issueTypeAccess != null && this.issueReportsParams?.issueTypeAccess?.length != 0){
          console.log(this.issueReportsParams.issueTypeAccess)
          this.selectedFilters['issueType'] = true;
          if(this.selectedIssueTypesSet.size == 0)
            this.selectedIssueTypesSet = new Set<string>(this.issueReportsParams?.issueTypeAccess)
        }

        if(this.issueReportsParams?.districtAccess != null && this.issueReportsParams?.districtAccess?.length != 0){
          this.selectedFilters['district'] = true;
          if(this.selectedDistrictsSet.size == 0)
            this.selectedDistrictsSet = new Set<string>(this.issueReportsParams?.districtAccess)
        }

        if(this.issueReportsParams?.status != null && this.issueReportsParams?.status?.length != 0){
          this.selectedFilters['status'] = true;
          if(this.selectedStatusesSet.size == 0)
            this.selectedStatusesSet = new Set<string>(this.issueReportsParams?.status)
        }

        this.checkReset();

        console.log(this.issueStatuses);
      }
    });
  }

  reloadReports(){
    this.filtersForm.control.markAsPristine();
    console.log(this.issueReportsParams)
    this.loadReports();
  }

  resetParams() {
    this.issueReportsParams = new IssueReportsParams();
    this.selectedFilters = {}
    this.clearIssueTypeChecklist();
    this.clearDistrictChecklist();
    this.clearStatusChecklist();
    this.canReset = false;
  }

  toggleDateRange(){
    this.selectedFilters['dateRange']?this.selectedFilters['dateRange']=false:this.selectedFilters['dateRange']=true;
    this.issueReportsParams.dateLower = null;
    this.issueReportsParams.dateUpper = null;
  }

  toggleReportCount(){
    this.selectedFilters['reportCount']?this.selectedFilters['reportCount']=false:this.selectedFilters['reportCount']=true;
    this.issueReportsParams.maxReportCount = null;
    this.issueReportsParams.minReportCount = null;
  }

  toggleIssueType(){
    this.selectedFilters['issueType']?this.selectedFilters['issueType']=false:this.selectedFilters['issueType']=true;
    this.issueReportsParams.issueTypeAccess = null;
  }

  toggleDistrict(){
    this.selectedFilters['district']?this.selectedFilters['district']=false:this.selectedFilters['district']=true;
    this.issueReportsParams.districtAccess = null;
  }


  toggleStatus(){
    this.selectedFilters['status']?this.selectedFilters['status']=false:this.selectedFilters['status']=true;
    this.issueReportsParams.status = null;
  }

  pageChanged(event: any){
    if (this.issueReportsParams.pageNumber !== event.page){
      this.issueReportsParams.pageNumber = event.page;
      this.loadReports();
    }
  }


  updateIssueTypeChecklist(event: any){var targetValue = event.target?.value;
    var targetChecked = event.target?.checked;
    var setHasValue = this.selectedIssueTypesSet.has(targetValue);
    if(!targetChecked && setHasValue){
      this.selectedIssueTypesSet.delete(targetValue);
    }
    if(targetChecked && !setHasValue){
      this.selectedIssueTypesSet.add(targetValue);
    }
    this.issueReportsParams.issueTypeAccess = [...this.selectedIssueTypesSet];
  }

  updateDistrictChecklist(event: any){var targetValue = event.target?.value;
    var targetChecked = event.target?.checked;
    var setHasValue = this.selectedDistrictsSet.has(targetValue);
    if(!targetChecked && setHasValue){
      this.selectedDistrictsSet.delete(targetValue);
    }
    if(targetChecked && !setHasValue){
      this.selectedDistrictsSet.add(targetValue);
    }
    this.issueReportsParams.districtAccess = [...this.selectedDistrictsSet];
  }

  updateStatusChecklist(event: any){
    var targetValue = event.target?.value;
    var targetChecked = event.target?.checked;
    var setHasValue = this.selectedStatusesSet.has(targetValue);
    if(!targetChecked && setHasValue){
      this.selectedStatusesSet.delete(targetValue);
    }
    if(targetChecked && !setHasValue){
      this.selectedStatusesSet.add(targetValue);
    }
    this.issueReportsParams.status = [...this.selectedStatusesSet];
  }

  clearIssueTypeChecklist(){
    this.selectedIssueTypesSet.clear();
    this.issueReportsParams.issueTypeAccess = [];
  }

  clearDistrictChecklist(){
    this.selectedDistrictsSet.clear();
    this.issueReportsParams.districtAccess = [];
  }

  clearStatusChecklist(){
    this.selectedStatusesSet.clear();
    this.issueReportsParams.status = [];
  }

  openLink(id:number){
    const url = this.router.serializeUrl(
      this.router.createUrlTree([`/reporting/issue-view/${id}`])
    );
    window.open(url, '_blank');
  }

  updateSelected(e, issueStatusItem){
    var id = issueStatusItem['id']
    var checked = e['target']?.['checked']
    if(checked){
      if (!this.selected.has(id))
        this.selected.add(id);
      if(!this.unpin.has(id) && issueStatusItem['pinned']){
        this.unpin.add(id);
      }
      if(!this.hidden.has(id) && issueStatusItem['hidden']){
        this.hidden.add(id);
      }
    }
    else{
      if (this.selected.has(id))
        this.selected.delete(id);
      if(this.unpin.has(id)){
        this.unpin.delete(id);
      }
      if(this.hidden.has(id)){
        this.hidden.delete(id);
      }
    }
  }

  pinIssues() {
    this.issueReportingService.pinIssue([...this.selected]).subscribe({
      next: (result) => {
        console.log(result);
        if(result['result'])
          this.toastr.success(result['result']);
        if(result['unpin']){
          if(this.unpin.size > 0)
            this.unpin.clear();
        }else{
          if(result['values'])
            this.unpin = new Set<number>(result['values'])
        }
        this.issueReportsParams.pageNumber = 1;
        this.reloadReports();
      }
    });
  }

  hideIssues() {
    this.issueReportingService.hideIssue([...this.selected]).subscribe({
      next: (result) => {
        console.log(result);
        if(result['result'])
          this.toastr.success(result['result']);
        if(result['unhide']){
          if(this.hidden.size > 0)
            this.hidden.clear();
        }else{
          if(this.issueReportsParams.showHidden){
            if(result['values'])
              this.hidden = new Set<number>(result['values'])
          }else{
            this.hidden.clear();
            this.selected.clear();
          }
        }
        this.issueReportsParams.pageNumber = 1;
        this.reloadReports();
      }
    });
  }

  selectAll(e) {
    if(e['target']?.['checked']){
      if(this.selected.size == 0){
        var values = this.issueStatuses.map(i => <number>i['id']);
        this.selected = new Set<number>(values);
        console.log(this.issueStatuses);
      }
    }else{
      if(this.selected.size > 0){
        this.selected.clear();
      }
    }
  }
}
