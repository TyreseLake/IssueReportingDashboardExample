import { HttpClient } from '@angular/common/http';
import { Component, OnInit, ViewChild } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { GoogleMap, MapMarker } from '@angular/google-maps';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService, ModalOptions } from 'ngx-bootstrap/modal';
import { ToastrService } from 'ngx-toastr';
import { catchError, map, Observable, of, take } from 'rxjs';
import { ConfirmationModalComponent } from 'src/app/modals/confirmation-modal/confirmation-modal.component';
import { SearchIssuesModalComponent } from 'src/app/modals/search-issues-modal/search-issues-modal.component';
import { fadeInOutGrow } from 'src/app/_animations/animations';
import { AccountService } from 'src/app/_services/account.service';
import { DataService } from 'src/app/_services/data.service';
import { IssueReportingService } from 'src/app/_services/issue-reporting.service';

@Component({
  selector: 'app-report-issue',
  templateUrl: './report-issue.component.html',
  styleUrls: ['./report-issue.component.css'],
  animations: [
    fadeInOutGrow
  ]
})
export class ReportIssueComponent implements OnInit {
  reportIssueForm: FormGroup;
  issueTypes: string[] = [];
  districts: string[] = [];
  platforms: string[] = [];
  validationErrors = [];

  myMap: GoogleMap;

  apiLoaded: Observable<boolean>;
  mapHeight = "270px";
  mapWidth = "270px";
  options: google.maps.MapOptions = {
    zoom: 8,
    disableDefaultUI: true,
    fullscreenControl: true,
    zoomControl: true,
    controlSize: 30
  };

  locationLat: number;
  locationLng: number;

  useCoordinates: boolean = false;

  useImages: boolean = false;

  addOtherInfo: boolean = false;

  bsModalRefSearch: BsModalRef;

  bsModalRefConfirm: BsModalRef;

  selectedImages: any[];

  userHasAdmin = false;

  constructor(private fb: FormBuilder,
              private dataService: DataService,
              private issueReportingService: IssueReportingService,
              private toastr: ToastrService,
              private httpClient: HttpClient,
              private router: Router,
              private modalService: BsModalService,
              public accountService: AccountService) {
    this.apiLoaded = httpClient.jsonp('https://maps.googleapis.com/maps/api/js?key=AIzaSyB4qqFW9_puuzb-MqhMT-Qv7BYJiWjU8x4', 'callback')
      .pipe(
        map(() => true),
        catchError((e) => {
          console.log(e);
          return of(false);
        }),
      );
  }

  ngOnInit(): void {
    this.accountService.currentUser$.pipe(take(1)).subscribe({
      next: (user) => {
        this.userHasAdmin = user.adminPrivileges;
      }
    }).unsubscribe();

    this.dataService.getDistricts(true).subscribe((districts) => {
      this.districts = districts;
    });

    this.dataService.getIssueTypes(true).subscribe((issueTypes) => {
      this.issueTypes = issueTypes;
    });

    this.dataService.getPlatforms().subscribe((platforms) => {
      this.platforms = platforms;
    });

    this.initializeForm();
  }

  initializeForm() {
    this.reportIssueForm = this.fb.group({
      subject: [''],
      issueType: ['', Validators.required],
      description: [''],
      locationDescription: [''],
      district: ['', [this.notNull()]],
      platform: ['', Validators.required],
      phoneNumber: [''],
      email: ['', Validators.email],
      address: ['']
    });
  }

  notNull(): ValidatorFn {
    return (control: AbstractControl) => {
      return (this.userHasAdmin || control?.value != "") ? null : { notNull: true }
    }
  }

  reportNewIssue() {
    if(this.reportIssueForm.invalid){
      this.toastr.error("This form contains errors")
      this.reportIssueForm.markAllAsTouched();
      return;
    }
    var message = "Submit report as new issue?";
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
        this.submitIssueReport();
      }
    })
  }

  submitIssueReport(statusId:number = null){
    var report = {...this.reportIssueForm.value}
    // if(this.useCoordinates && this.locationLat && this.locationLng){
    //   report['locationLongitude'] = this.locationLng;
    //   report['locationLatitude'] = this.locationLat;
    // }
    // else
    // {
    //   report['locationLongitude'] = "";
    //   report['locationLatitude'] = "";
    // }
    // if(statusId){
    //   report['statusId'] = statusId
    // }
    // else{
    //   report['statusId'] = ""
    // }

    if(this.useCoordinates && this.locationLat && this.locationLng){
      report['locationLongitude'] = this.locationLng;
      report['locationLatitude'] = this.locationLat;
    }

    if(statusId){
      report['statusId'] = statusId
    }

    this.issueReportingService.addNewIssueReport(report, this.selectedImages).subscribe({
      next: (result) => {
        this.toastr.success("Issue Report Submitted");
        this.resetForm();
        this.router.navigate(['reporting/issue-view/' + result['statusId']]);
      }
    });
  }

  resetForm() {
    this.reportIssueForm.reset();
    this.reportIssueForm.controls["subject"].setValue("");
    this.reportIssueForm.controls["issueType"].setValue("");
    this.reportIssueForm.controls["description"].setValue("");
    this.reportIssueForm.controls["locationDescription"].setValue("");
    this.reportIssueForm.controls["district"].setValue("");
    this.reportIssueForm.controls["platform"].setValue("");
    this.useCoordinates = false;
    this.useImages = false;
    this.addOtherInfo = false;
    this.locationLat = null;
    this.locationLng = null;
    this.selectedImages = null;
    this.clearReporterInfo();
  }

  setLocation(mapMarker: any){
    this.locationLat = mapMarker.marker.position.lat()
    this.locationLng = mapMarker.marker.position.lng()
  }

  display(i: any){
    console.log(i);
  }

  clearReporterInfo(){
    this.reportIssueForm.controls["phoneNumber"].setValue("");
    this.reportIssueForm.controls["address"].setValue("");
    this.reportIssueForm.controls["email"].setValue("");
  }

  openSearchModal(){
    if(this.reportIssueForm.invalid){
      this.toastr.error("This form contains errors")
      this.reportIssueForm.markAllAsTouched();
      return;
    }

    var searchInfo = {
      issueType: this.reportIssueForm.controls['issueType'].value,
      district: this.reportIssueForm.controls['district'].value,
      description: this.reportIssueForm.controls['description'].value,
      locationDescription: this.reportIssueForm.controls['locationDescription'].value,
      subject: this.reportIssueForm.controls['subject'].value,
    }

    if(this.useCoordinates){
      searchInfo['locationLat'] = this.locationLat;
      searchInfo['locationLng'] = this.locationLng;
    }

    const config:ModalOptions<SearchIssuesModalComponent> = {
      class: 'modal-dialog-centered modal-lg',
      keyboard: false,
      initialState: {
        searchInfo
      },
      id: 9990
    }
    this.bsModalRefSearch = this.modalService.show(SearchIssuesModalComponent, config)
      this.bsModalRefSearch.content.selection.subscribe(result => {
        console.log(result)
        if(result['selected'] > 0){
          this.submitIssueReport(result['selected']);
        }
        else
        {
          this.submitIssueReport();
        }
      })
  }

  setImages(images: any[]){
    this.selectedImages = images;
  }
}
