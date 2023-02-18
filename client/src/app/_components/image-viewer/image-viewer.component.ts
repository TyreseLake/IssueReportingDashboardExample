import { Component, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { NgxGalleryAnimation, NgxGalleryImage, NgxGalleryOptions } from '@kolkov/ngx-gallery';
import { Image } from 'src/app/_models/image'
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-image-viewer',
  templateUrl: './image-viewer.component.html',
  encapsulation: ViewEncapsulation.None,
  styleUrls: ['./image-viewer.component.css']
})
export class ImageViewerComponent implements OnInit {
  @Input() imageUrls: Image[];

  documentUrl: string = environment.documentUrl;

  galleryOptions: NgxGalleryOptions[] = [];
  galleryOptionsSmall: NgxGalleryOptions[] = [];
  galleryImages: NgxGalleryImage[] = [];

  constructor() { }

  ngOnInit(): void {
    this.galleryOptions = [
      {
        width: '400px',
        height: '400px',
        imagePercent: 100,
        thumbnailsColumns: 4,
        imageAnimation: NgxGalleryAnimation.Slide,
        preview: true,
        imageArrowsAutoHide: true,
        imageSwipe: true,
        previewFullscreen: true,
        previewCloseOnClick: true,
        previewCloseOnEsc: true,
        previewKeyboardNavigation: true,
        previewInfinityMove: true,
        previewZoom: true,
      }
    ]

    this.galleryOptionsSmall = [
      {
        width: '280px',
        height: '300px',
        imagePercent: 100,
        thumbnailsColumns: 4,
        imageAnimation: NgxGalleryAnimation.Slide,
        preview: true,
        imageArrowsAutoHide: true,
        imageSwipe: true,
        previewFullscreen: true,
        previewCloseOnClick: true,
        previewCloseOnEsc: true,
        previewKeyboardNavigation: true,
        previewInfinityMove: true,
        previewZoom: true,
      }
    ]

    this.galleryImages = this.getImages();
  }

  getImages(): NgxGalleryImage[] {
    const imageUrls = [];
    for (const photo of this.imageUrls) {
      imageUrls.push({
        small: this.documentUrl + photo["path"],
        medium: this.documentUrl + photo["path"],
        big: this.documentUrl + photo["path"]
      })
    }
    return imageUrls;
  }
}
