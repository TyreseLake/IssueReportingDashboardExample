import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FileUploader } from 'ng2-file-upload';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { fadeInStagger } from 'src/app/_animations/animations';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';

@Component({
  selector: 'app-file-uploader',
  templateUrl: './file-uploader.component.html',
  styleUrls: ['./file-uploader.component.css'],
  animations: [
    fadeInStagger
  ]
})

export class FileUploaderComponent implements OnInit {
  user: User;
  uploader: FileUploader;
  hasBaseDropzoneOver = false;

  @Input() url: string;
  @Input() allowedFileType: string;
  @Input() queueLimit?: number = 1;
  @Input() selectMessage = "Select a file"
  @Input() successMessage = "Success";
  @Input() unchangedMessage = "No changes";
  @Input() fileSelect = false;

  @Output() filesEvent = new EventEmitter<any[]>();

  constructor(private accountService: AccountService, private toastr: ToastrService) {
    this.accountService.currentUser$.pipe(take(1)).subscribe(user => this.user = user);
  }

  ngOnInit(): void {
    this.initializeUploader();
  }

  fileOverBase(e: any) {
    this.hasBaseDropzoneOver = e;
  }

  initializeUploader() {
    this.uploader = new FileUploader({
      url: this.url,
      authToken: 'Bearer ' + this.user.token,
      isHTML5: true,
      allowedFileType: [this.allowedFileType], //try 'xls/csv'
      removeAfterUpload: true,
      maxFileSize: 10 * 1024 * 1024, //10MB
      queueLimit: this.queueLimit
    });

    this.uploader.onAfterAddingFile = (file) => {
      file.withCredentials = false;
      // console.log(this.uploader.queue.lastIndexOf(file));
      // console.log(this.uploader.queue);
      console.log(file);
      if(this.allowedFileType == 'image'){
        file['url'] = URL.createObjectURL(file._file);
        console.log(file['url']);
      }
      this.filesEvent.emit(this.uploader.queue.map(item=>item['file']['rawFile']));
    }

    this.uploader.onSuccessItem = (item, response, status, headers) => {
      if (response) {
        var parsedResponse = JSON.parse(response);
        if(parsedResponse["noChanges"])
          this.toastr.info(this.unchangedMessage);
        else
          this.toastr.success(this.successMessage);
      }
    }

    this.uploader.onErrorItem = (item, response, status, headers) => {
      if(response) {
        var parsedResponse = JSON.parse(response);
        if(parsedResponse["error"])
          this.toastr.error(parsedResponse["error"]);
        else
          this.toastr.error(response);
      }
    }
  }

  removeFromQueue(item){
    this.uploader.removeFromQueue(item);
  }

  // getImage(imageFile){
  //   var imgPath = URL.createObjectURL(imageFile['file']['rawFile']);
  //   return "url('" + imgPath + "')"
  // }
}
