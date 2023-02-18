import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { fadeInStagger } from 'src/app/_animations/animations';
import { IssueStatus } from 'src/app/_models/issueStatus';

@Component({
  selector: 'app-issues-table',
  templateUrl: './issues-table.component.html',
  styleUrls: ['./issues-table.component.css'],
  animations: [
    fadeInStagger
  ]
})
export class IssuesTableComponent implements OnChanges {
  @Input() issueStatuses : IssueStatus[];

  @Output() selectEvent = new EventEmitter<number>();

  @Input() selected: number;
  @Input() issueStatusId: number;

  constructor(private router: Router) { }

  ngOnChanges(): void {
  }

  openLink(id:number){
    const url = this.router.serializeUrl(
      this.router.createUrlTree([`/reporting/issue-view/${id}`])
    );
    window.open(url, '_blank');
  }

  select(id:number){
    this.selectEvent.emit(id);
  }

}
