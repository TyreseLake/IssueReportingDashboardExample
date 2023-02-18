import { HttpClient } from '@angular/common/http';
import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { TabDirective } from 'ngx-bootstrap/tabs';
import { ToastrService } from 'ngx-toastr';
import { catchError, map, Observable, of, take } from 'rxjs';
import { DescriptionModalComponent } from 'src/app/modals/description-modal/description-modal.component';
import { LocationModalComponent } from 'src/app/modals/location-modal/location-modal.component';
import { RemarkModalComponent } from 'src/app/modals/remark-modal/remark-modal.component';
import { SelectIssueStatusModalComponent } from 'src/app/modals/select-issue-status-modal/select-issue-status-modal.component';
import { StatusModalComponent } from 'src/app/modals/status-modal/status-modal.component';
import { fadeInOutGrow, slideInOut } from 'src/app/_animations/animations';
import { IssueReport } from 'src/app/_models/issueReport';
import { IssueStatusDetails } from 'src/app/_models/issueStatusDetails';
import { Pagination } from 'src/app/_models/pagination';
import { AccountService } from 'src/app/_services/account.service';
import { DataService } from 'src/app/_services/data.service';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';

@Component({
  selector: 'app-view-report-details',
  templateUrl: './view-report-details.component.html',
  styleUrls: ['./view-report-details.component.scss'],
  encapsulation: ViewEncapsulation.None,
  animations: [
    fadeInOutGrow,
    slideInOut
  ]
})
export class ViewReportDetailsComponent implements OnInit {
  issueReportDetails: IssueStatusDetails;

  issueReports: IssueReport[];
  pagination: Pagination;
  pageNumber = 1;
  pageSize = 20;
  statuses: any[];
  statusPagination: Pagination;
  statusPageNumber = 1;
  statusPageSize = 20;
  loading = false;
  issueStatusId: number;
  bsModalRef: BsModalRef;

  activeTab: TabDirective;

  apiLoaded: Observable<boolean>;

  mapHeight = "270px";
  mapWidth = "270px";

  options: google.maps.MapOptions = {
    zoom: 12,
    disableDefaultUI: true,
    fullscreenControl: true,
    zoomControl: true,
    controlSize: 30
  };

  showMap = false;
  showReportMap = [];
  showReportImages = [];
  selectedReports = [];

  editing: number;
  editForm: FormGroup;

  platformList: string[] = [];
  issueTypeList: string[] = [];
  districtList: string[] = [];

  userName: string;
  userCanEditAll: boolean = false;

  constructor(private route: ActivatedRoute,
    private issueReportingService: IssueReportingService,
    private modalService: BsModalService,
    private toastrService: ToastrService,
    private httpClient: HttpClient,
    private router: Router,
    private dataService: DataService,
    private fb: FormBuilder,
    private accountService: AccountService) {
      this.accountService.currentUser$.pipe(take(1)).subscribe({
        next: (user) => {
          this.userCanEditAll = user.issueManagementPrivileges;
          this.userName = user.userName;
          // this.dataService.getRoles("issue").subscribe({
          //   next: (result) => {
          //     var userRoles = user.roles;
          //     console.log(userRoles);
          //     userRoles.forEach(userRole => {
          //       console.log(userRole);
          //       if(result.includes(userRole)){

          //       }
          //     });
          //   }
          // })
        }
      }).unsubscribe();

      this.apiLoaded = httpClient.jsonp('https://maps.googleapis.com/maps/api/js?key=AIzaSyB4qqFW9_puuzb-MqhMT-Qv7BYJiWjU8x4', 'callback')
        .pipe(
          map(() => true),
          catchError((e) => {
            console.log(e);
            return of(false);
          }),
        );

      this.initializeEditForm();
    }

  ngOnInit(): void {
    this.route.data.subscribe(data => {
      this.issueReportDetails = data["issueReportDetails"];
      this.issueStatusId = this.issueReportDetails.id;
    });
    // this.loadReports();
    this.loadStatusUpdates();
    console.log(this.issueReportDetails);
  }

  initializeEditForm() {
    this.editForm = this.fb.group({
      newSubject: [""],
      newDescription: [""],
      newLocationDescription: [""],
      newEmail: [''],
      newPhoneNumber: [''],
      newAddress: [''],
      newDistrict: [''],
      newPlatform: [''],
      newIssueType: ['']
    })
  }

  editReport(issueReport) {
    if(!(this.platformList?.length > 0 && this.issueTypeList?.length > 0 && this.districtList?.length > 0)){
      this.dataService.getDistricts(true).subscribe((districts) => {
        this.districtList = districts;
      });

      this.dataService.getIssueTypes(true).subscribe((issueTypes) => {
        this.issueTypeList = issueTypes;
      });

      this.dataService.getPlatforms().subscribe((platforms) => {
        this.platformList = platforms;
      });
    }
    this.editForm.controls['newSubject'].setValue(issueReport['subject']);
    this.editForm.controls['newDescription'].setValue(issueReport['description']);
    this.editForm.controls['newLocationDescription'].setValue(issueReport['locationDescription']);
    this.editForm.controls['newEmail'].setValue(issueReport['email']);
    this.editForm.controls['newPhoneNumber'].setValue(issueReport['phoneNumber']);
    this.editForm.controls['newAddress'].setValue(issueReport['address']);
    this.editForm.controls['newDistrict'].setValue(issueReport['district']);
    this.editForm.controls['newPlatform'].setValue(issueReport['platform']);
    this.editForm.controls['newIssueType'].setValue(issueReport['issueType']);
    this.editing = issueReport['id'];
    this.editForm.markAsUntouched();
  }

  submitReportEdit() {
    var editData = { ...this.editForm.value };
    editData['issueReportId'] = this.editing;
    this.loading = true;
    // console.log(editData);
    this.issueReportingService.updateIssueReport(editData).subscribe({
      next: (response) => {
        // console.log(response);
        if(response['result']){
          this.toastrService.info(response['result']);
        } else {
          console.log(response);
          this.toastrService.success("Issue Report Updated Successfully");
          var index = this.issueReports.findIndex((e) => { return e.id === response['id'] });
          this.issueReports[index]['subject'] = response['subject'];
          this.issueReports[index]['description'] = response['description'];
          this.issueReports[index]['locationDescription'] = response['locationDescription'];
          this.issueReports[index]['phoneNumber'] = response['reporterPhoneNumber'];
          this.issueReports[index]['email'] = response['reporterEmail'];
          this.issueReports[index]['address'] = response['reporterAddress'];
          this.issueReports[index]['district'] = response['district'];
          this.issueReports[index]['platform'] = response['platform'];
          this.issueReports[index]['issueType'] = response['issueType'];
        }
        this.editing = null;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    })
  }

  loadReports() {
    this.loading = true;
    this.issueReportingService.getIssueReports(this.issueStatusId, this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        console.log(response);
        this.issueReports = response.result;
        this.pagination = response.pagination;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    })
  }

  pageChanged(event: any){
    if (this.pageNumber !== event.page){
      this.pageNumber = event.page;
      this.loadReports();
    }
  }

  loadStatusUpdates() {
    this.loading = true;
    this.issueReportingService.getIssueStatusUpdates(this.issueStatusId, this.statusPageNumber, this.statusPageSize).subscribe({
      next: (response) => {
        this.statuses = response.result;
        console.log(this.statuses);
        this.statusPagination = response.pagination;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    })
  }

  statusPageChanged(event: any){
    if (this.statusPageNumber !== event.page){
      this.statusPageNumber = event.page;
      this.loadStatusUpdates();
    }
  }

  openStatusModal(currentStatus:string) {
    const config:ModalOptions<StatusModalComponent> = {
      class: 'modal-dialog-centered modal-lg',
      id: 9997,
      initialState: {
        currentStatus
      },
      keyboard: false,
      backdrop: 'static'
    }
    this.bsModalRef = this.modalService.show(StatusModalComponent, config)
    this.bsModalRef.content.updateStatus.subscribe(issueStatusUpdate => {
      var formData = issueStatusUpdate["formData"]
      var images = issueStatusUpdate['images']
      formData["issueStatusId"]=this.issueReportDetails.id;
      console.log("Sending: ", formData)
      // console.log(issueStatusUpdate);
      this.issueReportingService.addIssueStatusUpdate(formData, images).subscribe({
        next: (response) => {
          this.toastrService.success("Update Successful");
          this.issueReportDetails.currentStatus = response["newStatus"];
          this.loadStatusUpdates();
          // this.issueReportDetails.reviewType = response["reviewType"];
          //Review Date
        }
      })
    })
  }

  // openRemarkModal() {
  //   // const config:ModalOptions<RemarkModalComponent> = {
  //   //   class: 'modal-dialog-centered',
  //   //   id: 9997,
  //   //   backdrop: 'static',
  //   //   keyboard: false
  //   // }
  //   // this.bsModalRef = this.modalService.show(RemarkModalComponent, config)
  //   // this.bsModalRef.content.addRemark.subscribe(remarkDetails => {
  //   //   remarkDetails["statusId"] = this.issueReportDetails.id;
  //   //   this.issueReportingService.addRemark(remarkDetails).subscribe({
  //   //     next: (response) => {
  //   //       console.log(response);
  //   //       this.toastrService.success("Remark Added Successfully");
  //   //       // var remark = {
  //   //       //   userName: response["userName"],
  //   //       //   remarkType: response["remarkType"],
  //   //       //   remark: response["remarkData"],
  //   //       //   dateRemarked: response["dateRemarked"]
  //   //       // }
  //   //       this.issueReportDetails.issueRemarks.push(response);
  //   //     }
  //   //   })
  //   //   console.log(remarkDetails);
  //   // })
  // }

  openUpdateLocationModal(district: string, locationDescription: string) {
    var id = this.issueReportDetails.id;
    const config:ModalOptions<LocationModalComponent> = {
      class: 'modal-dialog-centered modal-lg',
      id: 9997,
      keyboard: false,
      initialState: {
        district,
        locationDescription,
        id
      },
      backdrop: 'static'
    }
    this.bsModalRef = this.modalService.show(LocationModalComponent, config)
    this.bsModalRef.content.updateStatus.subscribe(updateDetails => {
      updateDetails["statusId"] = this.issueReportDetails.id;
      this.issueReportingService.updateIssueStatusLocation(updateDetails).subscribe({
        next: (result) => {
          // console.log(result);
          if(result["message"]){
            this.toastrService.info(result["message"]);
          }else{
            this.issueReportDetails["district"] = result["districtName"];
            this.issueReportDetails["issueLocation"] = result["issueLocation"];
            this.toastrService.success("Issue Updated Successfully");
          }
        }
      })
      // console.log(updateDetails);
    })
  }

  openDescriptionModal() {
    var description = this.issueReportDetails.description;
    var id = this.issueReportDetails.id;
    const config:ModalOptions<DescriptionModalComponent> = {
      class: 'modal-dialog-centered modal-lg',
      id: 9997,
      keyboard: false,
      initialState: {
        description,
        id
      },
      backdrop: 'static'
    }
    this.bsModalRef = this.modalService.show(DescriptionModalComponent, config)
    this.bsModalRef.content.updateDescription.subscribe(details => {
      details["statusId"] = this.issueReportDetails.id;
      // console.log(details);
      this.issueReportingService.updateIssueStatusDescription(details).subscribe({
        next: (result) => {
          // console.log(result);
          if(result["message"]){
            this.toastrService.info(result["message"]);
          }else{
            this.issueReportDetails['description'] = result["description"];
            this.toastrService.success("Issue Updated Successfully")
          }
        }
      })
    })
  }

  openSelectIssueStatusModal() {
    const config:ModalOptions<SelectIssueStatusModalComponent> = {
      class: 'modal-dialog-centered modal-lg',
      id: 9997,
      keyboard: false,
      initialState: {
        issueStatusId: this.issueStatusId
      },
      backdrop: 'static'
    }
    this.bsModalRef = this.modalService.show(SelectIssueStatusModalComponent, config)
    this.bsModalRef.content.selection.subscribe(result => {
      if(result['selected']){
        var moveData = {
          sourceId: this.issueReportDetails.id,
          destinationId:result['selected'],
          issueReportIds:this.selectedReports
        };
        this.moveReport(moveData);
      }
    });
  }

  moveReport(moveData:any) {
    this.loading = true;
    this.router.navigateByUrl('/reporting/issue-view');
    this.issueReportingService.moveIssueReport(moveData).subscribe({
      next: (result) => {
        // console.log(result);
        this.router.navigateByUrl('/reporting/issue-view/' + result['issueDestination']['id']);
        this.toastrService.success("Move successful")
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
      }
    })
  }

  onTabActivated(data: TabDirective) {
    this.activeTab = data;
    // console.log("HEYYYYY -> " + data)
    // if(this.activeTab.heading === 'Status Updates' && this.statuses?.length === 0) {
    //   this.loadStatusUpdates();
    // }
    if(this.activeTab.heading === 'Reports') {
      if(!(this.issueReports?.length > 0)){
        this.loadReports();
      }
    }
  }

  toggleMap() {
    this.showMap = this.showMap ? false : true
  }

  toggleReportMap(id) {
    this.showReportMap[id] = this.showReportMap[id] ? false : true
  }

  selectIssue(id, checked:boolean) {
    if(checked && !this.selectedReports.includes(id)){
      this.selectedReports.push(id);
    }
    if(!checked && this.selectedReports.includes(id)){
      this.selectedReports.splice(this.selectedReports.indexOf(id), 1);
    }

    // console.log(this.selectedReports);
  }

  openLink(id:number){
    const url = this.router.serializeUrl(
      this.router.createUrlTree([`/reporting/issue-view/${id}`])
    );
    window.open(url, '_blank');
  }
}
