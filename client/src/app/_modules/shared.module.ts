import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BsDropdownModule } from 'ngx-bootstrap/dropdown';
import { PaginationModule } from 'ngx-bootstrap/pagination';
import { ToastrModule } from 'ngx-toastr';
import { FileUploadModule } from 'ng2-file-upload';
import { TimeagoModule } from 'ngx-timeago';
import { ModalModule } from 'ngx-bootstrap/modal';
import { TabsModule } from 'ngx-bootstrap/tabs'
import { PopoverModule } from 'ngx-bootstrap/popover'
import { GoogleMapsModule } from '@angular/google-maps';
import { HttpClientJsonpModule } from '@angular/common/http';
import { BsDatepickerModule } from 'ngx-bootstrap/datepicker';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { JwtHelperService } from '@auth0/angular-jwt';
import { NgxGalleryModule } from '@kolkov/ngx-gallery';
import { GoogleChartsModule } from 'angular-google-charts';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    BrowserAnimationsModule,
    BsDropdownModule.forRoot(),
    ToastrModule.forRoot({
      positionClass: 'toast-bottom-right'
    }),
    FileUploadModule,
    PaginationModule,
    TimeagoModule.forRoot(),
    ModalModule.forRoot(),
    TabsModule.forRoot(),
    PopoverModule,
    GoogleMapsModule,
    HttpClientJsonpModule,
    BsDatepickerModule.forRoot(),
    NgxGalleryModule,
    GoogleChartsModule
  ],
  exports: [
    BrowserAnimationsModule,
    BsDropdownModule,
    ToastrModule,
    FileUploadModule,
    PaginationModule,
    TimeagoModule,
    ModalModule,
    TabsModule,
    PopoverModule,
    GoogleMapsModule,
    HttpClientJsonpModule,
    BsDatepickerModule,
    NgxGalleryModule,
    GoogleChartsModule
  ]
})
export class SharedModule { }
