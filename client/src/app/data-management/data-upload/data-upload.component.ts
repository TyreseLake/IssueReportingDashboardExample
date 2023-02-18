import { animate, state, style, transition, trigger } from '@angular/animations';
import { Component, OnInit } from '@angular/core';
import { FileUploader } from 'ng2-file-upload';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-data-upload',
  templateUrl: './data-upload.component.html',
  styleUrls: ['./data-upload.component.css']
})
export class DataUploadComponent implements OnInit {
  // uploader: FileUploader;
  // hasBaseDropzoneOver = false;
  // baseUrl = environment.apiUrl;
  // user: User;
  url = environment.apiUrl + 'issuereporting/import-issue-reports';

  // constructor(private accountService: AccountService, private toastr: ToastrService) {
  //   this.accountService.currentUser$.pipe(take(1)).subscribe(user => this.user = user);
  // }

  constructor() {

  }

  ngOnInit(): void {
    // this.initializeUploader();
  }

  // fileOverBase(e: any) {
  //   this.hasBaseDropzoneOver = e;
  // }

  // initializeUploader() {
  //   this.uploader = new FileUploader({
  //     url: this.baseUrl + 'issuereporting/import-issue-reports',
  //     authToken: 'Bearer ' + this.user.token,
  //     isHTML5: true,
  //     allowedFileType: ['xls'], //try 'xls/csv'
  //     removeAfterUpload: true,
  //     maxFileSize: 10 * 1024 * 1024, //10MB
  //     queueLimit: 1
  //   });

  //   this.uploader.onAfterAddingFile = (file) => {
  //     file.withCredentials = false;
  //   }

  //   this.uploader.onSuccessItem = (item, response, status, headers) => {
  //     if (response) {
  //       if(response == "No new Issue Reports were added")
  //         this.toastr.info("No new Issue Reports were added");
  //       else
  //         this.toastr.success("Successfully imported issue reports");
  //     }
  //   }

  //   this.uploader.onErrorItem = (item, response, status, headers) => {
  //     if(response) {
  //       var parsedResponse = JSON.parse(response);
  //       if(parsedResponse["error"])
  //         this.toastr.error(parsedResponse["error"]);
  //       else
  //         this.toastr.error(response);
  //     }
  //   }
  // }
}
