import { formatDate } from '@angular/common';
import { Component, OnInit, TemplateRef, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { ConfirmationModalComponent } from 'src/app/modals/confirmation-modal/confirmation-modal.component';
import { SelectIssueStatusModalComponent } from 'src/app/modals/select-issue-status-modal/select-issue-status-modal.component';
import { DataService } from 'src/app/_services/data.service';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';

@Component({
  selector: 'app-data-export',
  templateUrl: './data-export.component.html',
  styleUrls: ['./data-export.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class DataExportComponent implements OnInit {
  fileName = "Issue_Export"
  result:string;

  exportForm: FormGroup;

  bsModalRef: BsModalRef;
  bsModalRefConfirm: BsModalRef;
  bsModalRefStatus: BsModalRef;

  dateUpperLimmit = new Date();

  searchIssueTypes: string;
  searchDistricts: string;
  searchStatus: string;

  selectedIssueTypesSet: Set<string> = new Set<string>();
  selectedDistrictsSet: Set<string> = new Set<string>();
  selectedStatusesSet: Set<string> = new Set<string>();

  issueTypes: string[];
  districts: string[];
  statusList: string[];
  dataLoaded = false;

  bsConfig = {
    containerClass: 'theme-green',
    dateInputFormat: 'DD MMMM YYYY'
  }

  selected: number;
  selectedIssue: any;

  constructor(private modalService: BsModalService,
      private issueReportingService: IssueReportingService,
      private fb: FormBuilder,
      private toastrService: ToastrService,
      private dataService: DataService) { }

  ngOnInit(): void {
    this.initExportForm();
  }

  openModal(template: TemplateRef<any>) {
    const config: ModalOptions<any> = {
      class: 'modal-dialog-centered',
      id: 9999,
      keyboard: false,
      backdrop: 'static'
    }
    this.bsModalRef = this.modalService.show(template, config);
 }

 openStatusModal() {
  const config:ModalOptions<SelectIssueStatusModalComponent> = {
    class: 'modal-dialog-centered modal-lg',
    keyboard: false,
    initialState: {
      allowNew: false,
    },
    id: 9994
  }
  this.bsModalRefStatus = this.modalService.show(SelectIssueStatusModalComponent, config)
  this.bsModalRefStatus.content.selection.subscribe(result => {
    console.log(result)
    this.selected = result['selected'];
    this.selectedIssue = result['data'];
  })
 }

  exportIssueReports() {
    this.issueReportingService.getExportedIssueReports().subscribe({
      next: (result) => {
        console.log(JSON.stringify(result))
        var blob = new Blob([JSON.stringify(result)], {type: "octet/stream"});
        this.result = JSON.stringify(result);
        const a = document.createElement('a');
        a.setAttribute('type', 'hidden');
        a.href = URL.createObjectURL(blob);
        a.download = this.fileName + " " + formatDate(new Date(), 'yyyy-MM-dd h-mm-ssa', 'en_US') + '.json';
        a.click();
        a.remove();

        this.toastrService.success("Successfully exported")
      }
    })
  }

  initExportForm(){
    this.exportForm = this.fb.group({
      exportType: ["issueReport"],
      dateUpper: [""],
      dateLower: [""],
    })
  }

  cancelExport(){
    if(this.exportForm?.touched){
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

  loadData() {
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

    this.dataLoaded = true;
  }

  exportSheet(){
    var content = {...this.exportForm.value};
    content['issueTypes'] = [...this.selectedIssueTypesSet];
    content['districts'] = [...this.selectedDistrictsSet];
    content['statuses'] = [...this.selectedStatusesSet];
    if(this.selected)
      content['statusId'] = this.selected;

    console.log(content)

    this.issueReportingService.getExportedSheet(content).subscribe({
      next: (result : any) => {
        console.log(result);
        var file = result.file;
        if(file == null) {
          this.toastrService.error(result.error)
        }
        if(file != null) {
          const a = document.createElement('a');
          a.setAttribute('type', 'hidden');
          a.href = URL.createObjectURL(file);
          let fileName
          switch(content['exportType']) {
            case "issueStatus":
              fileName = "Issues"
              break;
            case "issueReport":
              fileName = "Issue Reports"
              break;
            case "statusUpdate":
              fileName = "Issue Statuses"
              break;
            default:
              fileName = "Exported Sheet"
              break;
          }
          a.download = fileName + " " + formatDate(new Date(), 'yyyy-MM-dd h-mm-ssa', 'en_US') + '.xlsx';
          a.click();
          a.remove();

          this.toastrService.success("Successfully exported")
          this.bsModalRef.hide();
        }
      },
      error: (e) => {
        this.bsModalRef.hide();
      }
    })
  }

  updateIssueTypeChecklist(event: any){
    var targetValue = event.target?.value;
    var targetChecked = event.target?.checked;
    var setHasValue = this.selectedIssueTypesSet.has(targetValue);
    if(!targetChecked && setHasValue){
      this.selectedIssueTypesSet.delete(targetValue);
    }
    if(targetChecked && !setHasValue){
      this.selectedIssueTypesSet.add(targetValue);
    }
    // this.issueReportsParams.issueTypeAccess = [...this.selectedIssueTypesSet];
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
    // this.issueReportsParams.districtAccess = [...this.selectedDistrictsSet];
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
    // this.issueReportsParams.status = [...this.selectedStatusesSet];
  }

  clearIssueTypeChecklist(){
    this.selectedIssueTypesSet.clear();
    // this.issueReportsParams.issueTypeAccess = [];
  }

  clearDistrictChecklist(){
    this.selectedDistrictsSet.clear();
    // this.issueReportsParams.districtAccess = [];
  }

  clearStatusChecklist(){
    this.selectedStatusesSet.clear();
    // this.issueReportsParams.status = [];
  }

  clearSelected(){
    this.selected = null;
    this.selectedIssue = null;
  }
}
