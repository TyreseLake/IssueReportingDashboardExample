import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-server-errror',
  templateUrl: './server-errror.component.html',
  styleUrls: ['./server-errror.component.css']
})
export class ServerErrrorComponent implements OnInit {

  error: any;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    console.log(navigation);
    this.error = navigation?.extras?.state?.['error'];
    console.log(this.error);
  }

  ngOnInit(): void {
  }

}
